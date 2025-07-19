using Microsoft.Extensions.Logging;
using MCP.Server.Services;
using System.Text.Json;
using Xunit;

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

    [Fact]
    public async Task LoadSolution_ValidPath_ReturnsSuccessJson()
    {
        // Arrange
        using var analysisService = new RoslynAnalysisService(_logger);

        // Act
        var result = await DotNetAnalysisTools.LoadSolution(analysisService, _testSolutionPath);

        // Assert
        Assert.NotNull(result);

        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(response.GetProperty("success").GetBoolean());
        Assert.Equal("Solution loaded successfully", response.GetProperty("message").GetString());
    }

    [Fact]
    public async Task LoadSolution_InvalidPath_ReturnsErrorJson()
    {
        // Arrange
        using var analysisService = new RoslynAnalysisService(_logger);
        var invalidPath = "/invalid/path/solution.sln";

        // Act
        var result = await DotNetAnalysisTools.LoadSolution(analysisService, invalidPath);

        // Assert
        Assert.NotNull(result);

        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.False(response.GetProperty("success").GetBoolean());
        Assert.Equal("Failed to load solution", response.GetProperty("message").GetString());
    }

    [Fact]
    public async Task GetCompilationErrors_WithLoadedSolution_ReturnsErrorsJson()
    {
        // Arrange
        using var analysisService = new RoslynAnalysisService(_logger);
        await analysisService.LoadSolutionAsync(_testSolutionPath);

        // Act
        var result = await DotNetAnalysisTools.GetCompilationErrors(analysisService);

        // Assert
        Assert.NotNull(result);

        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(response.GetProperty("success").GetBoolean());
        Assert.True(response.GetProperty("error_count").GetInt32() > 0);
        Assert.True(response.GetProperty("warning_count").GetInt32() >= 0);

        var errors = response.GetProperty("errors").EnumerateArray().ToList();
        Assert.True(errors.Count > 0);

        // Check first error structure
        var firstError = errors[0];
        Assert.NotNull(firstError.GetProperty("Id").GetString());
        Assert.NotNull(firstError.GetProperty("Message").GetString());
        // Severity is serialized as a number (enum value), so use GetInt32()
        Assert.True(firstError.GetProperty("Severity").GetInt32() >= 0);
        Assert.NotNull(firstError.GetProperty("FilePath").GetString());
        Assert.True(firstError.GetProperty("StartLine").GetInt32() > 0);
        Assert.NotNull(firstError.GetProperty("ProjectName").GetString());
    }

    [Fact]
    public async Task GetCompilationErrors_WithoutLoadedSolution_ReturnsEmptyErrorsJson()
    {
        // Arrange
        using var analysisService = new RoslynAnalysisService(_logger);

        // Act
        var result = await DotNetAnalysisTools.GetCompilationErrors(analysisService);

        // Assert
        Assert.NotNull(result);

        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(response.GetProperty("success").GetBoolean());
        Assert.Equal(0, response.GetProperty("error_count").GetInt32());
        Assert.Equal(0, response.GetProperty("warning_count").GetInt32());

        var errors = response.GetProperty("errors").EnumerateArray().ToList();
        Assert.Empty(errors);
    }

    [Fact]
    public async Task GetSolutionInfo_WithLoadedSolution_ReturnsSolutionInfoJson()
    {
        // Arrange
        using var analysisService = new RoslynAnalysisService(_logger);
        await analysisService.LoadSolutionAsync(_testSolutionPath);

        // Act
        var result = await DotNetAnalysisTools.GetSolutionInfo(analysisService);

        // Assert
        Assert.NotNull(result);

        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(response.GetProperty("success").GetBoolean());

        var solutionInfo = response.GetProperty("solution_info");
        Assert.Equal("TestSolution", solutionInfo.GetProperty("Name").GetString());
        Assert.Contains("TestSolution.sln", solutionInfo.GetProperty("FilePath").GetString());
        Assert.True(solutionInfo.GetProperty("HasCompilationErrors").GetBoolean());
        Assert.True(solutionInfo.GetProperty("TotalErrors").GetInt32() > 0);

        var projects = solutionInfo.GetProperty("Projects").EnumerateArray().ToList();
        Assert.Equal(2, projects.Count);

        var projectNames = projects.Select(p => p.GetProperty("Name").GetString() ?? "").Where(name => !string.IsNullOrEmpty(name)).ToList();
        Assert.Contains("TestProject", projectNames);
        Assert.Contains("TestLibrary", projectNames);
    }

    [Fact]
    public async Task GetSolutionInfo_WithoutLoadedSolution_ReturnsNullSolutionInfoJson()
    {
        // Arrange
        using var analysisService = new RoslynAnalysisService(_logger);

        // Act
        var result = await DotNetAnalysisTools.GetSolutionInfo(analysisService);

        // Assert
        Assert.NotNull(result);

        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(response.GetProperty("success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, response.GetProperty("solution_info").ValueKind);
    }

    [Fact]
    public async Task AnalyzeFile_ValidFile_ReturnsFileAnalysisJson()
    {
        // Arrange
        using var analysisService = new RoslynAnalysisService(_logger);
        await analysisService.LoadSolutionAsync(_testSolutionPath);

        // Act
        var result = await DotNetAnalysisTools.AnalyzeFile(analysisService, _testFilePath);

        // Assert
        Assert.NotNull(result);

        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(response.GetProperty("success").GetBoolean());
        Assert.Equal(response.GetProperty("file_path").GetString(), _testFilePath);
        Assert.True(response.GetProperty("error_count").GetInt32() > 0);

        var errors = response.GetProperty("errors").EnumerateArray().ToList();
        Assert.True(errors.Count > 0);

        // All errors should be from the specified file
        foreach (var error in errors)
        {
            Assert.Contains("Program.cs", error.GetProperty("FilePath").GetString());
        }
    }

    [Fact]
    public async Task AnalyzeFile_FileNotInSolution_ReturnsEmptyAnalysisJson()
    {
        // Arrange
        using var analysisService = new RoslynAnalysisService(_logger);
        await analysisService.LoadSolutionAsync(_testSolutionPath);
        var nonExistentFile = "/path/to/nonexistent/file.cs";

        // Act
        var result = await DotNetAnalysisTools.AnalyzeFile(analysisService, nonExistentFile);

        // Assert
        Assert.NotNull(result);

        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(response.GetProperty("success").GetBoolean());
        Assert.Equal(response.GetProperty("file_path").GetString(), nonExistentFile);
        Assert.Equal(0, response.GetProperty("error_count").GetInt32());
        Assert.Equal(0, response.GetProperty("warning_count").GetInt32());

        var errors = response.GetProperty("errors").EnumerateArray().ToList();
        Assert.Empty(errors);
    }
}