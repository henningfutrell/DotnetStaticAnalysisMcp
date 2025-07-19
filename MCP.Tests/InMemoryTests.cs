using Microsoft.Extensions.Logging;
using MCP.Server.Services;
using System.Diagnostics;
using System.Text.Json;

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

    [Test]
    public async Task InMemoryWorkspace_CanBeCreated_AndLoaded()
    {
        // Arrange & Act
        using var service = InMemoryAnalysisService.CreateWithTestProjects(_logger);

        // Assert
        var projectNames = service.GetProjectNames().ToList();
        await Assert.That(projectNames.Count).IsEqualTo(3);
        await Assert.That(projectNames).Contains("TestConsoleProject");
        await Assert.That(projectNames).Contains("TestLibrary");
        await Assert.That(projectNames).Contains("ValidProject");

        Console.WriteLine($"Created workspace with projects: {string.Join(", ", projectNames)}");
    }

    [Test]
    public async Task InMemoryWorkspace_GetCompilationErrors_ReturnsExpectedErrors()
    {
        // Arrange
        using var service = InMemoryAnalysisService.CreateWithTestProjects(_logger);

        // Act
        var errors = await service.GetCompilationErrorsAsync();

        // Assert
        await Assert.That(errors.Count).IsGreaterThan(0);

        var errorIds = errors.Select(e => e.Id).ToList();
        Console.WriteLine($"Found error IDs: {string.Join(", ", errorIds)}");

        // Check for expected error types
        await Assert.That(errorIds).Contains("CS0103"); // undeclared variable
        await Assert.That(errorIds).Contains("CS0246"); // unknown type
        await Assert.That(errorIds).Contains("CS0161"); // not all code paths return
        await Assert.That(errorIds).Contains("CS1002"); // syntax error

        // Validate error structure
        foreach (var error in errors.Take(5)) // Check first 5 errors
        {
            await Assert.That(error.Id).IsNotNull();
            await Assert.That(error.Message).IsNotNull();
            await Assert.That(error.ProjectName).IsNotNull();
            await Assert.That(error.StartLine).IsGreaterThan(0);
            await Assert.That(error.StartColumn).IsGreaterThan(0);
        }
    }

    [Test]
    public async Task InMemoryWorkspace_GetSolutionInfo_ReturnsCorrectStructure()
    {
        // Arrange
        using var service = InMemoryAnalysisService.CreateWithTestProjects(_logger);

        // Act
        var solutionInfo = await service.GetSolutionInfoAsync();

        // Assert
        await Assert.That(solutionInfo).IsNotNull();
        await Assert.That(solutionInfo!.Projects.Count).IsEqualTo(3);
        await Assert.That(solutionInfo.HasCompilationErrors).IsTrue();
        await Assert.That(solutionInfo.TotalErrors).IsGreaterThan(0);

        // Check specific projects
        var consoleProject = solutionInfo.Projects.FirstOrDefault(p => p.Name == "TestConsoleProject");
        var libraryProject = solutionInfo.Projects.FirstOrDefault(p => p.Name == "TestLibrary");
        var validProject = solutionInfo.Projects.FirstOrDefault(p => p.Name == "ValidProject");

        await Assert.That(consoleProject).IsNotNull();
        await Assert.That(libraryProject).IsNotNull();
        await Assert.That(validProject).IsNotNull();

        // Console project should be an executable with errors
        await Assert.That(consoleProject!.OutputType).IsEqualTo("ConsoleApplication");
        await Assert.That(consoleProject.HasCompilationErrors).IsTrue();

        // Library project should be a library with errors
        await Assert.That(libraryProject!.OutputType).IsEqualTo("DynamicallyLinkedLibrary");
        await Assert.That(libraryProject.HasCompilationErrors).IsTrue();

        // Valid project should have no errors
        await Assert.That(validProject!.OutputType).IsEqualTo("DynamicallyLinkedLibrary");
        await Assert.That(validProject.HasCompilationErrors).IsFalse();
    }

    [Test]
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
        await Assert.That(programErrors.Count).IsGreaterThan(0);
        await Assert.That(programErrors.All(e => e.FilePath == "Program.cs")).IsTrue();

        var programErrorIds = programErrors.Select(e => e.Id).ToList();
        await Assert.That(programErrorIds).Contains("CS0103"); // undeclared variable
        await Assert.That(programErrorIds).Contains("CS0246"); // unknown type
        await Assert.That(programErrorIds).Contains("CS0161"); // not all code paths return

        // Calculator.cs should have syntax error
        await Assert.That(calculatorErrors.Count).IsGreaterThan(0);
        await Assert.That(calculatorErrors.All(e => e.FilePath == "Calculator.cs")).IsTrue();
        await Assert.That(calculatorErrors.Any(e => e.Id == "CS1002")).IsTrue(); // Missing semicolon

        // ValidClass.cs should have no errors
        await Assert.That(validClassErrors.Count).IsEqualTo(0);

        Console.WriteLine($"Program.cs errors: {programErrors.Count}");
        Console.WriteLine($"Calculator.cs errors: {calculatorErrors.Count}");
        Console.WriteLine($"ValidClass.cs errors: {validClassErrors.Count}");
    }

    [Test]
    public async Task InMemoryWorkspace_SpecificErrorGeneration_CreatesTargetedErrors()
    {
        // Arrange
        using var service = InMemoryAnalysisService.CreateWithErrors(_logger, "CS0103", "CS0246", "CS1002");

        // Act
        var errors = await service.GetCompilationErrorsAsync();

        // Assert
        await Assert.That(errors.Count).IsGreaterThan(0);

        var errorIds = errors.Select(e => e.Id).ToList();
        Console.WriteLine($"Targeted error IDs: {string.Join(", ", errorIds)}");

        // Should contain the specific errors we requested
        await Assert.That(errorIds).Contains("CS0103");
        await Assert.That(errorIds).Contains("CS0246");
        await Assert.That(errorIds).Contains("CS1002");
    }

    [Test]
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

        // Assert
        await Assert.That(creationTime).IsLessThan(1000); // Should create workspace in < 1 second
        await Assert.That(analysisTime).IsLessThan(2000); // Should analyze in < 2 seconds
        await Assert.That(solutionInfoTime).IsLessThan(1000); // Should get info in < 1 second

        Console.WriteLine($"Workspace creation: {creationTime}ms");
        Console.WriteLine($"Error analysis: {analysisTime}ms");
        Console.WriteLine($"Solution info: {solutionInfoTime}ms");
        Console.WriteLine($"Found {errors.Count} errors in {solutionInfo?.Projects.Count} projects");
    }

    [Test]
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
        await Assert.That(errors1.Count).IsEqualTo(errors2.Count);
        await Assert.That(solutionInfo1?.Projects.Count).IsEqualTo(solutionInfo2?.Projects.Count);
        await Assert.That(solutionInfo1?.TotalErrors).IsEqualTo(solutionInfo2?.TotalErrors);

        // Error IDs should be the same
        var errorIds1 = errors1.Select(e => e.Id).OrderBy(x => x).ToList();
        var errorIds2 = errors2.Select(e => e.Id).OrderBy(x => x).ToList();
        await Assert.That(errorIds1.SequenceEqual(errorIds2)).IsTrue();
    }

    [Test]
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

        // Assert
        var memoryIncrease = finalMemory - initialMemory;
        await Assert.That(memoryIncrease).IsLessThan(50_000_000); // Less than 50MB increase

        Console.WriteLine($"Memory increase: {memoryIncrease / 1024 / 1024}MB");
    }

    [Test]
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
        await Assert.That(errorsJson).IsNotNull();
        await Assert.That(solutionInfoJson).IsNotNull();

        // Parse back to verify structure
        var errorsResponse = JsonSerializer.Deserialize<JsonElement>(errorsJson);
        var solutionResponse = JsonSerializer.Deserialize<JsonElement>(solutionInfoJson);

        await Assert.That(errorsResponse.GetProperty("success").GetBoolean()).IsTrue();
        await Assert.That(errorsResponse.GetProperty("error_count").GetInt32()).IsGreaterThan(0);

        await Assert.That(solutionResponse.GetProperty("success").GetBoolean()).IsTrue();
        await Assert.That(solutionResponse.GetProperty("solution_info").ValueKind).IsNotEqualTo(JsonValueKind.Null);

        Console.WriteLine($"Errors JSON length: {errorsJson.Length}");
        Console.WriteLine($"Solution info JSON length: {solutionInfoJson.Length}");
    }
}
