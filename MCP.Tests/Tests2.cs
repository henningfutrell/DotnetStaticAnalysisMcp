using Microsoft.Extensions.Logging;
using MCP.Server.Services;
using System.Text.Json;

namespace MCP.Tests;

/// <summary>
/// Integration tests for MCP Tools (DotNetAnalysisTools)
/// </summary>
public class McpToolsTests
{
    private readonly ILogger<RoslynAnalysisService> _logger;
    private readonly string _testSolutionPath;
    private readonly string _testFilePath;

    public McpToolsTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<RoslynAnalysisService>();

        // Get the path to our test solution
        var testDataPath = Path.Combine(AppContext.BaseDirectory, "TestData", "TestSolution");
        _testSolutionPath = Path.Combine(testDataPath, "TestSolution.sln");
        _testFilePath = Path.Combine(testDataPath, "TestProject", "Program.cs");
    }

    [Test]
    public async Task LoadSolution_ValidPath_ReturnsSuccessJson()
    {
        // Arrange
        using var analysisService = new RoslynAnalysisService(_logger);

        // Act
        var result = await DotNetAnalysisTools.LoadSolution(analysisService, _testSolutionPath);

        // Assert
        await Assert.That(result).IsNotNull();

        var response = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(response.GetProperty("success").GetBoolean()).IsTrue();
        await Assert.That(response.GetProperty("message").GetString()).IsEqualTo("Solution loaded successfully");
    }

    [Test]
    public async Task LoadSolution_InvalidPath_ReturnsErrorJson()
    {
        // Arrange
        using var analysisService = new RoslynAnalysisService(_logger);
        var invalidPath = "/invalid/path/solution.sln";

        // Act
        var result = await DotNetAnalysisTools.LoadSolution(analysisService, invalidPath);

        // Assert
        await Assert.That(result).IsNotNull();

        var response = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(response.GetProperty("success").GetBoolean()).IsFalse();
        await Assert.That(response.GetProperty("message").GetString()).IsEqualTo("Failed to load solution");
    }

    [Test]
    public async Task GetCompilationErrors_WithLoadedSolution_ReturnsErrorsJson()
    {
        // Arrange
        using var analysisService = new RoslynAnalysisService(_logger);
        await analysisService.LoadSolutionAsync(_testSolutionPath);

        // Act
        var result = await DotNetAnalysisTools.GetCompilationErrors(analysisService);

        // Assert
        await Assert.That(result).IsNotNull();

        var response = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(response.GetProperty("success").GetBoolean()).IsTrue();
        await Assert.That(response.GetProperty("error_count").GetInt32()).IsGreaterThan(0);
        await Assert.That(response.GetProperty("warning_count").GetInt32()).IsGreaterThanOrEqualTo(0);

        var errors = response.GetProperty("errors").EnumerateArray().ToList();
        await Assert.That(errors.Count).IsGreaterThan(0);

        // Check first error structure
        var firstError = errors[0];
        await Assert.That(firstError.GetProperty("Id").GetString()).IsNotNull();
        await Assert.That(firstError.GetProperty("Message").GetString()).IsNotNull();
        // Severity is serialized as a number (enum value), so use GetInt32()
        await Assert.That(firstError.GetProperty("Severity").GetInt32()).IsGreaterThanOrEqualTo(0);
        await Assert.That(firstError.GetProperty("FilePath").GetString()).IsNotNull();
        await Assert.That(firstError.GetProperty("StartLine").GetInt32()).IsGreaterThan(0);
        await Assert.That(firstError.GetProperty("ProjectName").GetString()).IsNotNull();
    }

    [Test]
    public async Task GetCompilationErrors_WithoutLoadedSolution_ReturnsEmptyErrorsJson()
    {
        // Arrange
        using var analysisService = new RoslynAnalysisService(_logger);

        // Act
        var result = await DotNetAnalysisTools.GetCompilationErrors(analysisService);

        // Assert
        await Assert.That(result).IsNotNull();

        var response = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(response.GetProperty("success").GetBoolean()).IsTrue();
        await Assert.That(response.GetProperty("error_count").GetInt32()).IsEqualTo(0);
        await Assert.That(response.GetProperty("warning_count").GetInt32()).IsEqualTo(0);

        var errors = response.GetProperty("errors").EnumerateArray().ToList();
        await Assert.That(errors.Count).IsEqualTo(0);
    }

    [Test]
    public async Task GetSolutionInfo_WithLoadedSolution_ReturnsSolutionInfoJson()
    {
        // Arrange
        using var analysisService = new RoslynAnalysisService(_logger);
        await analysisService.LoadSolutionAsync(_testSolutionPath);

        // Act
        var result = await DotNetAnalysisTools.GetSolutionInfo(analysisService);

        // Assert
        await Assert.That(result).IsNotNull();

        var response = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(response.GetProperty("success").GetBoolean()).IsTrue();

        var solutionInfo = response.GetProperty("solution_info");
        await Assert.That(solutionInfo.GetProperty("Name").GetString()).IsEqualTo("TestSolution");
        await Assert.That(solutionInfo.GetProperty("FilePath").GetString()).Contains("TestSolution.sln");
        await Assert.That(solutionInfo.GetProperty("HasCompilationErrors").GetBoolean()).IsTrue();
        await Assert.That(solutionInfo.GetProperty("TotalErrors").GetInt32()).IsGreaterThan(0);

        var projects = solutionInfo.GetProperty("Projects").EnumerateArray().ToList();
        await Assert.That(projects.Count).IsEqualTo(2);

        var projectNames = projects.Select(p => p.GetProperty("Name").GetString() ?? "").Where(name => !string.IsNullOrEmpty(name)).ToList();
        await Assert.That(projectNames).Contains("TestProject");
        await Assert.That(projectNames).Contains("TestLibrary");
    }

    [Test]
    public async Task GetSolutionInfo_WithoutLoadedSolution_ReturnsNullSolutionInfoJson()
    {
        // Arrange
        using var analysisService = new RoslynAnalysisService(_logger);

        // Act
        var result = await DotNetAnalysisTools.GetSolutionInfo(analysisService);

        // Assert
        await Assert.That(result).IsNotNull();

        var response = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(response.GetProperty("success").GetBoolean()).IsTrue();
        await Assert.That(response.GetProperty("solution_info").ValueKind).IsEqualTo(JsonValueKind.Null);
    }

    [Test]
    public async Task AnalyzeFile_ValidFile_ReturnsFileAnalysisJson()
    {
        // Arrange
        using var analysisService = new RoslynAnalysisService(_logger);
        await analysisService.LoadSolutionAsync(_testSolutionPath);

        // Act
        var result = await DotNetAnalysisTools.AnalyzeFile(analysisService, _testFilePath);

        // Assert
        await Assert.That(result).IsNotNull();

        var response = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(response.GetProperty("success").GetBoolean()).IsTrue();
        await Assert.That(response.GetProperty("file_path").GetString()).IsEqualTo(_testFilePath);
        await Assert.That(response.GetProperty("error_count").GetInt32()).IsGreaterThan(0);

        var errors = response.GetProperty("errors").EnumerateArray().ToList();
        await Assert.That(errors.Count).IsGreaterThan(0);

        // All errors should be from the specified file
        foreach (var error in errors)
        {
            await Assert.That(error.GetProperty("FilePath").GetString()).Contains("Program.cs");
        }
    }

    [Test]
    public async Task AnalyzeFile_FileNotInSolution_ReturnsEmptyAnalysisJson()
    {
        // Arrange
        using var analysisService = new RoslynAnalysisService(_logger);
        await analysisService.LoadSolutionAsync(_testSolutionPath);
        var nonExistentFile = "/path/to/nonexistent/file.cs";

        // Act
        var result = await DotNetAnalysisTools.AnalyzeFile(analysisService, nonExistentFile);

        // Assert
        await Assert.That(result).IsNotNull();

        var response = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(response.GetProperty("success").GetBoolean()).IsTrue();
        await Assert.That(response.GetProperty("file_path").GetString()).IsEqualTo(nonExistentFile);
        await Assert.That(response.GetProperty("error_count").GetInt32()).IsEqualTo(0);
        await Assert.That(response.GetProperty("warning_count").GetInt32()).IsEqualTo(0);

        var errors = response.GetProperty("errors").EnumerateArray().ToList();
        await Assert.That(errors.Count).IsEqualTo(0);
    }
}