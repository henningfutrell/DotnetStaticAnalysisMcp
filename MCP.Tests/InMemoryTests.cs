using Microsoft.Extensions.Logging;
using MCP.Server.Services;
using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace MCP.Tests;

/// <summary>
/// Tests using in-memory projects to avoid MSBuild dependencies
/// </summary>
public class InMemoryTests
{
    private readonly ILogger<InMemoryAnalysisService> _logger;

    public InMemoryTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => 
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning); // Reduce noise
        });
        _logger = loggerFactory.CreateLogger<InMemoryAnalysisService>();
    }

    [Fact]
    public void InMemoryWorkspace_CanBeCreated_AndLoaded()
    {
        // Arrange & Act
        using var service = InMemoryAnalysisService.CreateWithTestProjects(_logger);

        // Assert
        var projectNames = service.GetProjectNames().ToList();
        Assert.Equal(3, projectNames.Count);
        Assert.Contains("TestConsoleProject", projectNames);
        Assert.Contains("TestLibrary", projectNames);
        Assert.Contains("ValidProject", projectNames);

        Console.WriteLine($"Created workspace with projects: {string.Join(", ", projectNames)}");
    }

    [Fact]
    public async Task InMemoryWorkspace_GetCompilationErrors_ReturnsExpectedErrors()
    {
        // Arrange
        using var service = InMemoryAnalysisService.CreateWithTestProjects(_logger);

        // Act
        var errors = await service.GetCompilationErrorsAsync();

        // Assert
        Assert.True(errors.Count > 0);

        var errorIds = errors.Select(e => e.Id).ToList();
        Console.WriteLine($"Found error IDs: {string.Join(", ", errorIds)}");

        // Check for expected error types
        Assert.Contains("CS0103", errorIds); // undeclared variable
        Assert.Contains("CS0246", errorIds); // unknown type
        Assert.Contains("CS0161", errorIds); // not all code paths return
        Assert.Contains("CS1002", errorIds); // syntax error

        // Validate error structure
        foreach (var error in errors.Take(5)) // Check first 5 errors
        {
            Assert.NotNull(error.Id);
            Assert.NotNull(error.Message);
            Assert.NotNull(error.ProjectName);
            Assert.True(error.StartLine > 0);
            Assert.True(error.StartColumn > 0);
        }
    }

    [Fact]
    public async Task InMemoryWorkspace_GetSolutionInfo_ReturnsCorrectStructure()
    {
        // Arrange
        using var service = InMemoryAnalysisService.CreateWithTestProjects(_logger);

        // Act
        var solutionInfo = await service.GetSolutionInfoAsync();

        // Assert
        Assert.NotNull(solutionInfo);
        Assert.Equal(3, solutionInfo!.Projects.Count);
        Assert.True(solutionInfo.HasCompilationErrors);
        Assert.True(solutionInfo.TotalErrors > 0);

        // Check specific projects
        var consoleProject = solutionInfo.Projects.FirstOrDefault(p => p.Name == "TestConsoleProject");
        var libraryProject = solutionInfo.Projects.FirstOrDefault(p => p.Name == "TestLibrary");
        var validProject = solutionInfo.Projects.FirstOrDefault(p => p.Name == "ValidProject");

        Assert.NotNull(consoleProject);
        Assert.NotNull(libraryProject);
        Assert.NotNull(validProject);

        // Console project should be an executable with errors
        Assert.Equal("ConsoleApplication", consoleProject!.OutputType);
        Assert.True(consoleProject.HasCompilationErrors);

        // Library project should be a library with errors
        Assert.Equal("DynamicallyLinkedLibrary", libraryProject!.OutputType);
        Assert.True(libraryProject.HasCompilationErrors);

        // Valid project should have no errors
        Assert.Equal("DynamicallyLinkedLibrary", validProject!.OutputType);
        Assert.False(validProject.HasCompilationErrors);
    }

    [Fact]
    public async Task InMemoryWorkspace_AnalyzeSpecificDocument_IsolatesErrors()
    {
        // Arrange
        using var service = InMemoryAnalysisService.CreateWithTestProjects(_logger);

        // Act
        var programErrors = await service.AnalyzeDocumentAsync("Program.cs");
        var calculatorErrors = await service.AnalyzeDocumentAsync("Calculator.cs");
        var validClassErrors = await service.AnalyzeDocumentAsync("ValidClass.cs");

        // Assert
        // Program.cs should have multiple errors
        Assert.True(programErrors.Count > 0);
        Assert.True(programErrors.All(e => e.FilePath == "Program.cs"));

        var programErrorIds = programErrors.Select(e => e.Id).ToList();
        Assert.Contains("CS0103", programErrorIds); // undeclared variable
        Assert.Contains("CS0246", programErrorIds); // unknown type
        Assert.Contains("CS0161", programErrorIds); // not all code paths return

        // Calculator.cs should have syntax error
        Assert.True(calculatorErrors.Count > 0);
        Assert.True(calculatorErrors.All(e => e.FilePath == "Calculator.cs"));
        Assert.Contains(calculatorErrors, e => e.Id == "CS1002"); // Missing semicolon

        // ValidClass.cs should have no compilation errors (warnings are OK)
        var validClassCompilationErrors = validClassErrors.Where(e => e.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).ToList();
        Assert.Empty(validClassCompilationErrors);

        Console.WriteLine($"Program.cs errors: {programErrors.Count}");
        Console.WriteLine($"Calculator.cs errors: {calculatorErrors.Count}");
        Console.WriteLine($"ValidClass.cs errors: {validClassErrors.Count}");
    }

    [Fact]
    public async Task InMemoryWorkspace_SpecificErrorGeneration_CreatesTargetedErrors()
    {
        // Arrange
        using var service = InMemoryAnalysisService.CreateWithErrors(_logger, "CS0103", "CS0246", "CS1002");

        // Act
        var errors = await service.GetCompilationErrorsAsync();

        // Assert
        Assert.True(errors.Count > 0);

        var errorIds = errors.Select(e => e.Id).ToList();
        Console.WriteLine($"Targeted error IDs: {string.Join(", ", errorIds)}");

        // Should contain the specific errors we requested
        Assert.Contains("CS0103", errorIds);
        Assert.Contains("CS0246", errorIds);
        Assert.Contains("CS1002", errorIds);
    }

    [Fact]
    public async Task InMemoryWorkspace_Performance_CompletesQuickly()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act
        using var service = InMemoryAnalysisService.CreateWithTestProjects(_logger);
        var creationTime = stopwatch.ElapsedMilliseconds;

        stopwatch.Restart();
        var errors = await service.GetCompilationErrorsAsync();
        var analysisTime = stopwatch.ElapsedMilliseconds;

        stopwatch.Restart();
        var solutionInfo = await service.GetSolutionInfoAsync();
        var solutionInfoTime = stopwatch.ElapsedMilliseconds;

        // Assert - More realistic thresholds for CI environments
        Assert.True(creationTime < 5000); // Should create workspace in < 5 seconds
        Assert.True(analysisTime < 10000); // Should analyze in < 10 seconds
        Assert.True(solutionInfoTime < 5000); // Should get info in < 5 seconds

        Console.WriteLine($"Workspace creation: {creationTime}ms");
        Console.WriteLine($"Error analysis: {analysisTime}ms");
        Console.WriteLine($"Solution info: {solutionInfoTime}ms");
        Console.WriteLine($"Found {errors.Count} errors in {solutionInfo?.Projects.Count} projects");
    }

    [Fact]
    public async Task InMemoryWorkspace_MultipleAnalysisCalls_AreConsistent()
    {
        // Arrange
        using var service = InMemoryAnalysisService.CreateWithTestProjects(_logger);

        // Act
        var errors1 = await service.GetCompilationErrorsAsync();
        var errors2 = await service.GetCompilationErrorsAsync();
        var solutionInfo1 = await service.GetSolutionInfoAsync();
        var solutionInfo2 = await service.GetSolutionInfoAsync();

        // Assert
        Assert.Equal(errors2.Count, errors1.Count);
        Assert.Equal(solutionInfo2?.Projects.Count, solutionInfo1?.Projects.Count);
        Assert.Equal(solutionInfo2?.TotalErrors, solutionInfo1?.TotalErrors);

        // Error IDs should be the same
        var errorIds1 = errors1.Select(e => e.Id).OrderBy(x => x).ToList();
        var errorIds2 = errors2.Select(e => e.Id).OrderBy(x => x).ToList();
        Assert.True(errorIds1.SequenceEqual(errorIds2));
    }

    [Fact]
    public async Task InMemoryWorkspace_MemoryUsage_IsReasonable()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);

        // Act
        using (var service = InMemoryAnalysisService.CreateWithTestProjects(_logger))
        {
            await service.GetCompilationErrorsAsync();
            await service.GetSolutionInfoAsync();
            
            // Analyze all documents
            foreach (var docName in service.GetDocumentNames())
            {
                await service.AnalyzeDocumentAsync(docName);
            }
        }

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(true);

        // Assert - More realistic memory threshold for CI environments
        var memoryIncrease = finalMemory - initialMemory;
        Assert.True(memoryIncrease < 200_000_000); // Less than 200MB increase

        Console.WriteLine($"Memory increase: {memoryIncrease / 1024 / 1024}MB");
    }

    [Fact]
    public async Task InMemoryWorkspace_WithMcpTools_ReturnsValidJson()
    {
        // This test verifies that our in-memory approach works with the actual MCP tools
        
        // Arrange
        using var service = InMemoryAnalysisService.CreateWithTestProjects(_logger);
        var errors = await service.GetCompilationErrorsAsync();
        var solutionInfo = await service.GetSolutionInfoAsync();

        // Act - Simulate MCP tool responses
        var errorsJson = JsonSerializer.Serialize(new
        {
            success = true,
            error_count = errors.Count(e => e.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error),
            warning_count = errors.Count(e => e.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Warning),
            errors = errors.Take(10) // Limit for testing
        });

        var solutionInfoJson = JsonSerializer.Serialize(new
        {
            success = true,
            solution_info = solutionInfo
        });

        // Assert
        Assert.NotNull(errorsJson);
        Assert.NotNull(solutionInfoJson);

        // Parse back to verify structure
        var errorsResponse = JsonSerializer.Deserialize<JsonElement>(errorsJson);
        var solutionResponse = JsonSerializer.Deserialize<JsonElement>(solutionInfoJson);

        Assert.True(errorsResponse.GetProperty("success").GetBoolean());
        Assert.True(errorsResponse.GetProperty("error_count").GetInt32() > 0);

        Assert.True(solutionResponse.GetProperty("success").GetBoolean());
        Assert.NotEqual(JsonValueKind.Null, solutionResponse.GetProperty("solution_info").ValueKind);

        Console.WriteLine($"Errors JSON length: {errorsJson.Length}");
        Console.WriteLine($"Solution info JSON length: {solutionInfoJson.Length}");
    }
}
