using Microsoft.Extensions.Logging;
using MCP.Server.Services;
using MCP.Server.Models;
using System.Diagnostics;

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

    [Test]
    public async Task LoadSolution_Performance_CompletesWithinReasonableTime()
    {
        // Arrange
        using var service = new RoslynAnalysisService(_logger);
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await service.LoadSolutionAsync(_testSolutionPath);
        stopwatch.Stop();

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(stopwatch.ElapsedMilliseconds).IsLessThan(10000); // Should complete within 10 seconds

        Console.WriteLine($"Solution loading took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Test]
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
        await Assert.That(errors).IsNotNull();
        await Assert.That(stopwatch.ElapsedMilliseconds).IsLessThan(5000); // Should complete within 5 seconds

        Console.WriteLine($"Error analysis took {stopwatch.ElapsedMilliseconds}ms for {errors.Count} errors");
    }

    [Test]
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
        await Assert.That(errors1.Count).IsEqualTo(errors2.Count);
        await Assert.That(stopwatch2.ElapsedMilliseconds).IsLessThanOrEqualTo(stopwatch1.ElapsedMilliseconds);

        Console.WriteLine($"First call: {stopwatch1.ElapsedMilliseconds}ms, Second call: {stopwatch2.ElapsedMilliseconds}ms");
    }

    [Test]
    public async Task ErrorDetails_Validation_ContainsExpectedInformation()
    {
        // Arrange
        using var service = new RoslynAnalysisService(_logger);
        await service.LoadSolutionAsync(_testSolutionPath);

        // Act
        var errors = await service.GetCompilationErrorsAsync();

        // Assert
        await Assert.That(errors).IsNotNull();
        await Assert.That(errors.Count).IsGreaterThan(0);

        // Validate error structure
        foreach (var error in errors)
        {
            await Assert.That(error.Id).IsNotNull();
            await Assert.That(error.Message).IsNotNull();
            await Assert.That(error.FilePath).IsNotNull();
            await Assert.That(error.StartLine).IsGreaterThan(0);
            await Assert.That(error.StartColumn).IsGreaterThan(0);
            await Assert.That(error.ProjectName).IsNotNull();

            // Validate severity is a known value
            var validSeverities = new[] { "Error", "Warning", "Info", "Hidden" };
            await Assert.That(validSeverities).Contains(error.Severity.ToString());
        }
    }

    [Test]
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
        await Assert.That(errorsByType.ContainsKey("CS0103")).IsTrue();
        var cs0103Errors = errorsByType["CS0103"];
        await Assert.That(cs0103Errors.Any(e => e.Message.Contains("undeclaredVariable"))).IsTrue();

        // CS0246: The type or namespace name could not be found
        await Assert.That(errorsByType.ContainsKey("CS0246")).IsTrue();
        var cs0246Errors = errorsByType["CS0246"];
        await Assert.That(cs0246Errors.Any(e => e.Message.Contains("UnknownType"))).IsTrue();

        // CS0161: Not all code paths return a value
        await Assert.That(errorsByType.ContainsKey("CS0161")).IsTrue();

        // CS1002: Syntax error
        await Assert.That(errorsByType.ContainsKey("CS1002")).IsTrue();
    }

    [Test]
    public async Task ProjectStructure_Validation_CorrectlyIdentifiesProjects()
    {
        // Arrange
        using var service = new RoslynAnalysisService(_logger);
        await service.LoadSolutionAsync(_testSolutionPath);

        // Act
        var solutionInfo = await service.GetSolutionInfoAsync();

        // Assert
        await Assert.That(solutionInfo).IsNotNull();
        await Assert.That(solutionInfo!.Projects.Count).IsEqualTo(2);

        var testProject = solutionInfo.Projects.FirstOrDefault(p => p.Name == "TestProject");
        var testLibrary = solutionInfo.Projects.FirstOrDefault(p => p.Name == "TestLibrary");

        await Assert.That(testProject).IsNotNull();
        await Assert.That(testLibrary).IsNotNull();

        // Validate TestProject
        await Assert.That(testProject!.OutputType).IsEqualTo("Exe");
        await Assert.That(testProject.HasCompilationErrors).IsTrue();
        await Assert.That(testProject.SourceFiles.Any(f => f.EndsWith("Program.cs"))).IsTrue();

        // Validate TestLibrary
        await Assert.That(testLibrary!.OutputType).IsEqualTo("Library");
        await Assert.That(testLibrary.HasCompilationErrors).IsTrue();
        await Assert.That(testLibrary.SourceFiles.Any(f => f.EndsWith("Calculator.cs"))).IsTrue();
        await Assert.That(testLibrary.SourceFiles.Any(f => f.EndsWith("ValidClass.cs"))).IsTrue();
    }

    [Test]
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
        await Assert.That(programErrors.Count).IsGreaterThan(0);
        await Assert.That(programErrors.All(e => e.FilePath.EndsWith("Program.cs"))).IsTrue();

        // Calculator.cs should have at least one error (syntax error)
        await Assert.That(calculatorErrors.Count).IsGreaterThan(0);
        await Assert.That(calculatorErrors.All(e => e.FilePath.EndsWith("Calculator.cs"))).IsTrue();
        await Assert.That(calculatorErrors.Any(e => e.Id == "CS1002")).IsTrue(); // Missing semicolon

        // ValidClass.cs should have no errors
        await Assert.That(validClassErrors.Count).IsEqualTo(0);
    }

    [Test]
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
        await Assert.That(memoryIncrease).IsLessThan(100_000_000); // Less than 100MB increase

        Console.WriteLine($"Memory increase: {memoryIncrease / 1024 / 1024}MB");
    }
}