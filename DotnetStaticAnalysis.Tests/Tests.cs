using Microsoft.Extensions.Logging;
using DotnetStaticAnalysisMcp.Server.Services;
using DotnetStaticAnalysisMcp.Server.Models;
using System.Text.Json;
using Xunit;

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

    [Fact]
    public async Task LoadSolutionAsync_ValidSolution_ReturnsTrue()
    {
        // Arrange
        var service = new RoslynAnalysisService(_logger);

        // Act
        var result = await service.LoadSolutionAsync(_testSolutionPath);

        // Assert
        Assert.True(result);

        // Cleanup
        service.Dispose();
    }

    [Fact]
    public async Task LoadSolutionAsync_InvalidPath_ReturnsFalse()
    {
        // Arrange
        var service = new RoslynAnalysisService(_logger);
        var invalidPath = "/invalid/path/solution.sln";

        // Act
        var result = await service.LoadSolutionAsync(invalidPath);

        // Assert
        Assert.False(result);

        // Cleanup
        service.Dispose();
    }

    [Fact]
    public async Task GetCompilationErrorsAsync_WithoutLoadingSolution_ReturnsEmptyList()
    {
        // Arrange
        var service = new RoslynAnalysisService(_logger);

        // Act
        var errors = await service.GetCompilationErrorsAsync();

        // Assert
        Assert.NotNull(errors);
        Assert.Empty(errors);

        // Cleanup
        service.Dispose();
    }

    [Fact]
    public async Task GetCompilationErrorsAsync_WithLoadedSolution_ReturnsErrors()
    {
        // Arrange
        var service = new RoslynAnalysisService(_logger);
        await service.LoadSolutionAsync(_testSolutionPath);

        // Act
        var errors = await service.GetCompilationErrorsAsync();

        // Assert
        Assert.NotNull(errors);
        Assert.True(errors.Count > 0);

        // Check that we have the expected error types
        var errorIds = errors.Select(e => e.Id).ToList();
        Assert.Contains("CS0103", errorIds); // undeclared variable
        Assert.Contains("CS0246", errorIds); // unknown type
        Assert.Contains("CS0161", errorIds); // not all code paths return
        Assert.Contains("CS1002", errorIds); // syntax error

        // Cleanup
        service.Dispose();
    }

    [Fact]
    public async Task GetSolutionInfoAsync_WithoutLoadingSolution_ReturnsNull()
    {
        // Arrange
        using var service = new RoslynAnalysisService(_logger);

        // Act
        var solutionInfo = await service.GetSolutionInfoAsync();

        // Assert
        Assert.Null(solutionInfo);
    }

    [Fact]
    public async Task GetSolutionInfoAsync_WithLoadedSolution_ReturnsValidInfo()
    {
        // Arrange
        using var service = new RoslynAnalysisService(_logger);
        await service.LoadSolutionAsync(_testSolutionPath);

        // Act
        var solutionInfo = await service.GetSolutionInfoAsync();

        // Assert
        Assert.NotNull(solutionInfo);
        Assert.Equal("TestSolution", solutionInfo!.Name);
        Assert.Equal(2, solutionInfo.Projects.Count);
        Assert.True(solutionInfo.HasCompilationErrors);
        Assert.True(solutionInfo.TotalErrors > 0);

        // Check project names
        var projectNames = solutionInfo.Projects.Select(p => p.Name).ToList();
        Assert.Contains("TestProject", projectNames);
        Assert.Contains("TestLibrary", projectNames);
    }

    [Fact]
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
        Assert.NotNull(errors);
        Assert.True(errors.Count > 0);

        // All errors should be from the Program.cs file
        Assert.True(errors.All(e => e.FilePath.EndsWith("Program.cs")));

        // Should contain specific errors from Program.cs
        var errorIds = errors.Select(e => e.Id).ToList();
        Assert.Contains("CS0103", errorIds); // undeclared variable
        Assert.Contains("CS0246", errorIds); // unknown type
        Assert.Contains("CS0161", errorIds); // not all code paths return
    }

    [Fact]
    public async Task AnalyzeFileAsync_FileNotInSolution_ReturnsEmptyList()
    {
        // Arrange
        using var service = new RoslynAnalysisService(_logger);
        await service.LoadSolutionAsync(_testSolutionPath);
        var nonExistentFile = "/path/to/nonexistent/file.cs";

        // Act
        var errors = await service.AnalyzeFileAsync(nonExistentFile);

        // Assert
        Assert.NotNull(errors);
        Assert.Empty(errors);
    }
}