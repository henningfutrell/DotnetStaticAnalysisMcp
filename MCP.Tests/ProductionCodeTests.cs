using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using MCP.Server.Models;
using MCP.Server.Services;
using System.Text.Json;
using Xunit;

namespace MCP.Tests;

/// <summary>
/// Tests that exercise the ACTUAL production code in MCP.Server
/// These tests will show up in code coverage reports
/// </summary>
public class ProductionCodeTests
{
    private readonly ILogger<RoslynAnalysisService> _logger;

    public ProductionCodeTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => 
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });
        _logger = loggerFactory.CreateLogger<RoslynAnalysisService>();
    }

    [Fact]
    public void RoslynAnalysisService_Constructor_CreatesInstance()
    {
        // This tests the REAL production constructor
        using var service = new RoslynAnalysisService(_logger);
        
        // Assert the service was created
        Assert.NotNull(service);
    }

    [Fact]
    public async Task RoslynAnalysisService_GetCompilationErrorsAsync_WithoutSolution_ReturnsEmptyList()
    {
        // This tests the REAL production method
        using var service = new RoslynAnalysisService(_logger);

        // Act - Call the REAL production method
        var errors = await service.GetCompilationErrorsAsync();

        // Assert
        Assert.NotNull(errors);
        Assert.Empty(errors);
    }

    [Fact]
    public async Task RoslynAnalysisService_GetSolutionInfoAsync_WithoutSolution_ReturnsNull()
    {
        // This tests the REAL production method
        using var service = new RoslynAnalysisService(_logger);

        // Act - Call the REAL production method
        var solutionInfo = await service.GetSolutionInfoAsync();

        // Assert
        Assert.Null(solutionInfo);
    }

    [Fact]
    public async Task RoslynAnalysisService_AnalyzeFileAsync_WithoutSolution_ReturnsEmptyList()
    {
        // This tests the REAL production method
        using var service = new RoslynAnalysisService(_logger);

        // Act - Call the REAL production method
        var errors = await service.AnalyzeFileAsync("test.cs");

        // Assert
        Assert.NotNull(errors);
        Assert.Empty(errors);
    }

    [Fact]
    public async Task RoslynAnalysisService_LoadSolutionAsync_WithInvalidPath_ReturnsFalse()
    {
        // This tests the REAL production method
        using var service = new RoslynAnalysisService(_logger);

        // Act - Call the REAL production method
        var result = await service.LoadSolutionAsync("nonexistent.sln");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RoslynAnalysisService_GetCodeSuggestionsAsync_WithoutSolution_ReturnsEmptyList()
    {
        // This tests the REAL production code suggestions method
        using var service = new RoslynAnalysisService(_logger);

        // Act - Call the REAL production method
        var suggestions = await service.GetCodeSuggestionsAsync();

        // Assert
        Assert.NotNull(suggestions);
        Assert.Empty(suggestions);
    }

    [Fact]
    public async Task RoslynAnalysisService_GetFileSuggestionsAsync_WithoutSolution_ReturnsEmptyList()
    {
        // This tests the REAL production code suggestions method
        using var service = new RoslynAnalysisService(_logger);

        // Act - Call the REAL production method
        var suggestions = await service.GetFileSuggestionsAsync("test.cs");

        // Assert
        Assert.NotNull(suggestions);
        Assert.Empty(suggestions);
    }

    [Fact]
    public void CompilationError_Constructor_CreatesInstance()
    {
        // This tests the REAL production model
        var error = new CompilationError();

        // Assert
        Assert.NotNull(error);
        Assert.Equal(string.Empty, error.Id);
        Assert.Equal(string.Empty, error.Message);
    }

    [Fact]
    public void CompilationError_Properties_CanBeSetAndRetrieved()
    {
        // This tests the REAL production model
        var error = new CompilationError
        {
            Id = "CS0103",
            Message = "Test error",
            Severity = DiagnosticSeverity.Error,
            FilePath = "test.cs",
            StartLine = 10,
            EndLine = 10,
            ProjectName = "TestProject"
        };

        // Assert
        Assert.Equal("CS0103", error.Id);
        Assert.Equal("Test error", error.Message);
        Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        Assert.Equal("test.cs", error.FilePath);
        Assert.Equal(10, error.StartLine);
        Assert.Equal(10, error.EndLine);
        Assert.Equal("TestProject", error.ProjectName);
    }

    [Fact]
    public void SolutionInfo_Constructor_CreatesInstance()
    {
        // This tests the REAL production model
        var solutionInfo = new MCP.Server.Models.SolutionInfo();

        // Assert
        Assert.NotNull(solutionInfo);
        Assert.Equal(string.Empty, solutionInfo.Name);
        Assert.NotNull(solutionInfo.Projects);
        Assert.Empty(solutionInfo.Projects);
    }

    [Fact]
    public void ProjectInfo_Constructor_CreatesInstance()
    {
        // This tests the REAL production model
        var projectInfo = new MCP.Server.Models.ProjectInfo();

        // Assert
        Assert.NotNull(projectInfo);
        Assert.Equal(string.Empty, projectInfo.Name);
        Assert.Equal(string.Empty, projectInfo.OutputType);
    }

    [Fact]
    public void CodeSuggestion_Constructor_CreatesInstance()
    {
        // This tests the REAL production model
        var suggestion = new CodeSuggestion();

        // Assert
        Assert.NotNull(suggestion);
        Assert.Equal(string.Empty, suggestion.Id);
        Assert.Equal(string.Empty, suggestion.Title);
        Assert.NotNull(suggestion.Tags);
        Assert.Empty(suggestion.Tags);
    }

    [Fact]
    public void SuggestionAnalysisOptions_Constructor_CreatesInstance()
    {
        // This tests the REAL production model
        var options = new SuggestionAnalysisOptions();

        // Assert
        Assert.NotNull(options);
        Assert.NotNull(options.IncludedCategories);
        Assert.True(options.IncludedCategories.Count > 0);
        Assert.Equal(100, options.MaxSuggestions);
    }

    [Fact]
    public async Task McpServerService_GetCompilationErrors_WithRealService_ReturnsValidJson()
    {
        // This tests the REAL production MCP service
        using var analysisService = new RoslynAnalysisService(_logger);

        // Act - Call the REAL production MCP tool
        var result = await DotNetAnalysisTools.GetCompilationErrors(analysisService);

        // Assert
        Assert.NotNull(result);
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(response.GetProperty("success").GetBoolean());
        Assert.Equal(0, response.GetProperty("error_count").GetInt32());
    }

    [Fact]
    public async Task McpServerService_GetSolutionInfo_WithRealService_ReturnsValidJson()
    {
        // This tests the REAL production MCP service
        using var analysisService = new RoslynAnalysisService(_logger);

        // Act - Call the REAL production MCP tool
        var result = await DotNetAnalysisTools.GetSolutionInfo(analysisService);

        // Assert
        Assert.NotNull(result);
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(response.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task McpServerService_AnalyzeFile_WithRealService_ReturnsValidJson()
    {
        // This tests the REAL production MCP service
        using var analysisService = new RoslynAnalysisService(_logger);

        // Act - Call the REAL production MCP tool
        var result = await DotNetAnalysisTools.AnalyzeFile(analysisService, "test.cs");

        // Assert
        Assert.NotNull(result);
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(response.GetProperty("success").GetBoolean());
        Assert.Equal("test.cs", response.GetProperty("file_path").GetString());
    }

    [Fact]
    public async Task McpServerService_GetCodeSuggestions_WithRealService_ReturnsValidJson()
    {
        // This tests the REAL production MCP service
        using var analysisService = new RoslynAnalysisService(_logger);

        // Act - Call the REAL production MCP tool
        var result = await DotNetAnalysisTools.GetCodeSuggestions(analysisService);

        // Assert
        Assert.NotNull(result);
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(response.GetProperty("success").GetBoolean());
        Assert.Equal(0, response.GetProperty("suggestion_count").GetInt32());
    }

    [Fact]
    public async Task McpServerService_GetFileSuggestions_WithRealService_ReturnsValidJson()
    {
        // This tests the REAL production MCP service
        using var analysisService = new RoslynAnalysisService(_logger);

        // Act - Call the REAL production MCP tool
        var result = await DotNetAnalysisTools.GetFileSuggestions(analysisService, "test.cs");

        // Assert
        Assert.NotNull(result);
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(response.GetProperty("success").GetBoolean());
        Assert.Equal("test.cs", response.GetProperty("file_path").GetString());
    }

    [Fact]
    public async Task McpServerService_GetSuggestionCategories_ReturnsValidJson()
    {
        // This tests the REAL production MCP service
        
        // Act - Call the REAL production MCP tool
        var result = await DotNetAnalysisTools.GetSuggestionCategories();

        // Assert
        Assert.NotNull(result);
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(response.GetProperty("success").GetBoolean());
        Assert.True(response.TryGetProperty("categories", out _));
    }
}
