using Microsoft.Extensions.Logging;
using DotnetStaticAnalysisMcp.Server.Services;
using DotnetStaticAnalysisMcp.Server.Models;
using System.Diagnostics;
using Xunit;

namespace MCP.Tests;

/// <summary>
/// Performance and integration tests for the MCP server
/// </summary>
public class PerformanceAndIntegrationTests
{
    private readonly ILogger<RoslynAnalysisService> _logger;
    private readonly string _testSolutionPath;

    public PerformanceAndIntegrationTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<RoslynAnalysisService>();

        var testDataPath = Path.Combine(AppContext.BaseDirectory, "TestData", "TestSolution");
        _testSolutionPath = Path.Combine(testDataPath, "TestSolution.sln");
    }

    [Fact]
    public async Task LoadSolution_Performance_CompletesWithinReasonableTime()
    {
        // Arrange
        using var service = new RoslynAnalysisService(_logger);
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await service.LoadSolutionAsync(_testSolutionPath);
        stopwatch.Stop();

        // Assert
        Assert.True(result);
        Assert.True(stopwatch.ElapsedMilliseconds < 10000); // Should complete within 10 seconds

        Console.WriteLine($"Solution loading took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task GetCompilationErrors_Performance_CompletesWithinReasonableTime()
    {
        // Arrange
        using var service = new RoslynAnalysisService(_logger);
        await service.LoadSolutionAsync(_testSolutionPath);

        var stopwatch = Stopwatch.StartNew();

        // Act
        var errors = await service.GetCompilationErrorsAsync();
        stopwatch.Stop();

        // Assert
        Assert.NotNull(errors);
        Assert.True(stopwatch.ElapsedMilliseconds < 5000); // Should complete within 5 seconds

        Console.WriteLine($"Error analysis took {stopwatch.ElapsedMilliseconds}ms for {errors.Count} errors");
    }

    [Fact]
    public async Task MultipleAnalysisCalls_Performance_SubsequentCallsAreFaster()
    {
        // Arrange
        using var service = new RoslynAnalysisService(_logger);
        await service.LoadSolutionAsync(_testSolutionPath);

        // First call (may be slower due to initialization)
        var stopwatch1 = Stopwatch.StartNew();
        var errors1 = await service.GetCompilationErrorsAsync();
        stopwatch1.Stop();

        // Second call (should be faster due to caching)
        var stopwatch2 = Stopwatch.StartNew();
        var errors2 = await service.GetCompilationErrorsAsync();
        stopwatch2.Stop();

        // Assert
        Assert.Equal(errors2.Count, errors1.Count);
        Assert.True(stopwatch2.ElapsedMilliseconds <= stopwatch1.ElapsedMilliseconds);

        Console.WriteLine($"First call: {stopwatch1.ElapsedMilliseconds}ms, Second call: {stopwatch2.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task ErrorDetails_Validation_ContainsExpectedInformation()
    {
        // Arrange
        using var service = new RoslynAnalysisService(_logger);
        await service.LoadSolutionAsync(_testSolutionPath);

        // Act
        var errors = await service.GetCompilationErrorsAsync();

        // Assert
        Assert.NotNull(errors);
        Assert.True(errors.Count > 0);

        // Validate error structure
        foreach (var error in errors)
        {
            Assert.NotNull(error.Id);
            Assert.NotNull(error.Message);
            Assert.NotNull(error.FilePath);
            Assert.True(error.StartLine > 0);
            Assert.True(error.StartColumn > 0);
            Assert.NotNull(error.ProjectName);

            // Validate severity is a known value
            var validSeverities = new[] { "Error", "Warning", "Info", "Hidden" };
            Assert.Contains(error.Severity.ToString(), validSeverities);
        }
    }

    [Fact]
    public async Task SpecificErrorTypes_Validation_ContainsExpectedErrors()
    {
        // Arrange
        using var service = new RoslynAnalysisService(_logger);
        await service.LoadSolutionAsync(_testSolutionPath);

        // Act
        var errors = await service.GetCompilationErrorsAsync();

        // Assert - Check for specific errors we expect from our test files
        var errorsByType = errors.GroupBy(e => e.Id).ToDictionary(g => g.Key, g => g.ToList());

        // CS0103: The name 'undeclaredVariable' does not exist in the current context
        Assert.Contains("CS0103", errorsByType.Keys);
        var cs0103Errors = errorsByType["CS0103"];
        Assert.Contains(cs0103Errors, e => e.Message.Contains("undeclaredVariable"));

        // CS0246: The type or namespace name could not be found
        Assert.Contains("CS0246", errorsByType.Keys);
        var cs0246Errors = errorsByType["CS0246"];
        Assert.Contains(cs0246Errors, e => e.Message.Contains("UnknownType"));

        // CS0161: Not all code paths return a value
        Assert.Contains("CS0161", errorsByType.Keys);

        // CS1002: Syntax error
        Assert.Contains("CS1002", errorsByType.Keys);
    }

    [Fact]
    public async Task ProjectStructure_Validation_CorrectlyIdentifiesProjects()
    {
        // Arrange
        using var service = new RoslynAnalysisService(_logger);
        await service.LoadSolutionAsync(_testSolutionPath);

        // Act
        var solutionInfo = await service.GetSolutionInfoAsync();

        // Assert
        Assert.NotNull(solutionInfo);
        Assert.Equal(2, solutionInfo!.Projects.Count);

        var testProject = solutionInfo.Projects.FirstOrDefault(p => p.Name == "TestProject");
        var testLibrary = solutionInfo.Projects.FirstOrDefault(p => p.Name == "TestLibrary");

        Assert.NotNull(testProject);
        Assert.NotNull(testLibrary);

        // Validate TestProject (OutputKind.ConsoleApplication becomes "ConsoleApplication")
        Assert.Equal("ConsoleApplication", testProject!.OutputType);
        Assert.True(testProject.HasCompilationErrors);
        Assert.Contains(testProject.SourceFiles, f => f.EndsWith("Program.cs"));

        // Validate TestLibrary (OutputKind.DynamicallyLinkedLibrary becomes "DynamicallyLinkedLibrary")
        Assert.Equal("DynamicallyLinkedLibrary", testLibrary!.OutputType);
        Assert.True(testLibrary.HasCompilationErrors);
        Assert.Contains(testLibrary.SourceFiles, f => f.EndsWith("Calculator.cs"));
        Assert.Contains(testLibrary.SourceFiles, f => f.EndsWith("ValidClass.cs"));
    }

    [Fact]
    public async Task FileAnalysis_Validation_IsolatesErrorsCorrectly()
    {
        // Arrange
        using var service = new RoslynAnalysisService(_logger);
        await service.LoadSolutionAsync(_testSolutionPath);

        var programFilePath = Path.Combine(Path.GetDirectoryName(_testSolutionPath)!, "TestProject", "Program.cs");
        var calculatorFilePath = Path.Combine(Path.GetDirectoryName(_testSolutionPath)!, "TestLibrary", "Calculator.cs");
        var validClassFilePath = Path.Combine(Path.GetDirectoryName(_testSolutionPath)!, "TestLibrary", "ValidClass.cs");

        // Act
        var programErrors = await service.AnalyzeFileAsync(programFilePath);
        var calculatorErrors = await service.AnalyzeFileAsync(calculatorFilePath);
        var validClassErrors = await service.AnalyzeFileAsync(validClassFilePath);

        // Assert
        // Program.cs should have multiple errors
        Assert.True(programErrors.Count > 0);
        Assert.True(programErrors.All(e => e.FilePath.EndsWith("Program.cs")));

        // Calculator.cs should have at least one error (syntax error)
        Assert.True(calculatorErrors.Count > 0);
        Assert.True(calculatorErrors.All(e => e.FilePath.EndsWith("Calculator.cs")));
        Assert.Contains(calculatorErrors, e => e.Id == "CS1002"); // Missing semicolon

        // ValidClass.cs should have no errors
        Assert.Empty(validClassErrors);
    }

    [Fact]
    public async Task MemoryUsage_Validation_DisposalCleansUpResources()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);

        // Act
        using (var service = new RoslynAnalysisService(_logger))
        {
            await service.LoadSolutionAsync(_testSolutionPath);
            await service.GetCompilationErrorsAsync();
            await service.GetSolutionInfoAsync();
        }

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(true);

        // Assert
        // Memory usage should not have increased dramatically
        var memoryIncrease = finalMemory - initialMemory;
        Assert.True(memoryIncrease < 100_000_000); // Less than 100MB increase

        Console.WriteLine($"Memory increase: {memoryIncrease / 1024 / 1024}MB");
    }
}