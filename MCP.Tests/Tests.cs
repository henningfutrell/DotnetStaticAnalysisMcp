using Microsoft.Extensions.Logging;
using MCP.Server.Services;
using MCP.Server.Models;
using System.Text.Json;

namespace MCP.Tests;

/// <summary>
/// Unit tests for RoslynAnalysisService
/// </summary>
public class RoslynAnalysisServiceTests
{
    private readonly ILogger<RoslynAnalysisService> _logger;
    private readonly string _testSolutionPath;

    public RoslynAnalysisServiceTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<RoslynAnalysisService>();

        // Get the path to our test solution
        var testDataPath = Path.Combine(AppContext.BaseDirectory, "TestData", "TestSolution");
        _testSolutionPath = Path.Combine(testDataPath, "TestSolution.sln");
    }

    [Test]
    public async Task LoadSolutionAsync_ValidSolution_ReturnsTrue()
    {
        // Arrange
        var service = new RoslynAnalysisService(_logger);

        // Act
        var result = await service.LoadSolutionAsync(_testSolutionPath);

        // Assert
        await Assert.That(result).IsTrue();

        // Cleanup
        service.Dispose();
    }

    [Test]
    public async Task LoadSolutionAsync_InvalidPath_ReturnsFalse()
    {
        // Arrange
        var service = new RoslynAnalysisService(_logger);
        var invalidPath = "/invalid/path/solution.sln";

        // Act
        var result = await service.LoadSolutionAsync(invalidPath);

        // Assert
        await Assert.That(result).IsFalse();

        // Cleanup
        service.Dispose();
    }

    [Test]
    public async Task GetCompilationErrorsAsync_WithoutLoadingSolution_ReturnsEmptyList()
    {
        // Arrange
        var service = new RoslynAnalysisService(_logger);

        // Act
        var errors = await service.GetCompilationErrorsAsync();

        // Assert
        await Assert.That(errors).IsNotNull();
        await Assert.That(errors.Count).IsEqualTo(0);

        // Cleanup
        service.Dispose();
    }

    [Test]
    public async Task GetCompilationErrorsAsync_WithLoadedSolution_ReturnsErrors()
    {
        // Arrange
        var service = new RoslynAnalysisService(_logger);
        await service.LoadSolutionAsync(_testSolutionPath);

        // Act
        var errors = await service.GetCompilationErrorsAsync();

        // Assert
        await Assert.That(errors).IsNotNull();
        await Assert.That(errors.Count).IsGreaterThan(0);

        // Check that we have the expected error types
        var errorIds = errors.Select(e => e.Id).ToList();
        await Assert.That(errorIds).Contains("CS0103"); // undeclared variable
        await Assert.That(errorIds).Contains("CS0246"); // unknown type
        await Assert.That(errorIds).Contains("CS0161"); // not all code paths return
        await Assert.That(errorIds).Contains("CS1002"); // syntax error

        // Cleanup
        service.Dispose();
    }

    [Test]
    public async Task GetSolutionInfoAsync_WithoutLoadingSolution_ReturnsNull()
    {
        // Arrange
        using var service = new RoslynAnalysisService(_logger);

        // Act
        var solutionInfo = await service.GetSolutionInfoAsync();

        // Assert
        await Assert.That(solutionInfo).IsNull();
    }

    [Test]
    public async Task GetSolutionInfoAsync_WithLoadedSolution_ReturnsValidInfo()
    {
        // Arrange
        using var service = new RoslynAnalysisService(_logger);
        await service.LoadSolutionAsync(_testSolutionPath);

        // Act
        var solutionInfo = await service.GetSolutionInfoAsync();

        // Assert
        await Assert.That(solutionInfo).IsNotNull();
        await Assert.That(solutionInfo!.Name).IsEqualTo("TestSolution");
        await Assert.That(solutionInfo.Projects.Count).IsEqualTo(2);
        await Assert.That(solutionInfo.HasCompilationErrors).IsTrue();
        await Assert.That(solutionInfo.TotalErrors).IsGreaterThan(0);

        // Check project names
        var projectNames = solutionInfo.Projects.Select(p => p.Name).ToList();
        await Assert.That(projectNames).Contains("TestProject");
        await Assert.That(projectNames).Contains("TestLibrary");
    }

    [Test]
    public async Task AnalyzeFileAsync_ValidFile_ReturnsFileSpecificErrors()
    {
        // Arrange
        using var service = new RoslynAnalysisService(_logger);
        await service.LoadSolutionAsync(_testSolutionPath);

        var programFilePath = Path.Combine(
            Path.GetDirectoryName(_testSolutionPath)!,
            "TestProject",
            "Program.cs");

        // Act
        var errors = await service.AnalyzeFileAsync(programFilePath);

        // Assert
        await Assert.That(errors).IsNotNull();
        await Assert.That(errors.Count).IsGreaterThan(0);

        // All errors should be from the Program.cs file
        await Assert.That(errors.All(e => e.FilePath.EndsWith("Program.cs"))).IsTrue();

        // Should contain specific errors from Program.cs
        var errorIds = errors.Select(e => e.Id).ToList();
        await Assert.That(errorIds).Contains("CS0103"); // undeclared variable
        await Assert.That(errorIds).Contains("CS0246"); // unknown type
        await Assert.That(errorIds).Contains("CS0161"); // not all code paths return
    }

    [Test]
    public async Task AnalyzeFileAsync_FileNotInSolution_ReturnsEmptyList()
    {
        // Arrange
        using var service = new RoslynAnalysisService(_logger);
        await service.LoadSolutionAsync(_testSolutionPath);
        var nonExistentFile = "/path/to/nonexistent/file.cs";

        // Act
        var errors = await service.AnalyzeFileAsync(nonExistentFile);

        // Assert
        await Assert.That(errors).IsNotNull();
        await Assert.That(errors.Count).IsEqualTo(0);
    }
}