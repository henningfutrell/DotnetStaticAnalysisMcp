using Microsoft.Extensions.Logging;
using MCP.Server.Models;
using MCP.Server.Services;
using System.Text.Json;
using Xunit;

namespace MCP.Tests;

/// <summary>
/// Tests that exercise the REAL production code to ensure proper code coverage
/// These tests use the actual RoslynAnalysisService and McpServerService classes
/// </summary>
public class RealCodeCoverageTests
{
    private readonly ILogger<RoslynAnalysisService> _logger;

    public RealCodeCoverageTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => 
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });
        _logger = loggerFactory.CreateLogger<RoslynAnalysisService>();
    }

    [Fact]
    public async Task RoslynAnalysisService_WithoutSolution_GetCompilationErrors_ReturnsEmpty()
    {
        // This test exercises the REAL RoslynAnalysisService code
        using var service = new RoslynAnalysisService(_logger);

        // Act - This calls the real production method
        var errors = await service.GetCompilationErrorsAsync();

        // Assert
        Assert.NotNull(errors);
        Assert.Empty(errors);
    }

    [Fact]
    public async Task RoslynAnalysisService_WithoutSolution_GetSolutionInfo_ReturnsNull()
    {
        // This test exercises the REAL RoslynAnalysisService code
        using var service = new RoslynAnalysisService(_logger);

        // Act - This calls the real production method
        var solutionInfo = await service.GetSolutionInfoAsync();

        // Assert
        Assert.Null(solutionInfo);
    }

    [Fact]
    public async Task RoslynAnalysisService_WithoutSolution_AnalyzeFile_ReturnsEmpty()
    {
        // This test exercises the REAL RoslynAnalysisService code
        using var service = new RoslynAnalysisService(_logger);

        // Act - This calls the real production method
        var errors = await service.AnalyzeFileAsync("nonexistent.cs");

        // Assert
        Assert.NotNull(errors);
        Assert.Empty(errors);
    }

    [Fact]
    public async Task RoslynAnalysisService_GetCodeSuggestions_WithoutSolution_ReturnsEmpty()
    {
        // This test exercises the REAL code suggestions functionality
        using var service = new RoslynAnalysisService(_logger);

        // Act - This calls the real production method
        var suggestions = await service.GetCodeSuggestionsAsync();

        // Assert
        Assert.NotNull(suggestions);
        Assert.Empty(suggestions);
    }

    [Fact]
    public async Task RoslynAnalysisService_GetFileSuggestions_WithoutSolution_ReturnsEmpty()
    {
        // This test exercises the REAL code suggestions functionality
        using var service = new RoslynAnalysisService(_logger);

        // Act - This calls the real production method
        var suggestions = await service.GetFileSuggestionsAsync("test.cs");

        // Assert
        Assert.NotNull(suggestions);
        Assert.Empty(suggestions);
    }

    [Fact]
    public async Task RoslynAnalysisService_LoadInvalidSolution_HandlesGracefully()
    {
        // This test exercises the REAL error handling code
        using var service = new RoslynAnalysisService(_logger);

        // Act - This calls the real production method with invalid input
        var result = await service.LoadSolutionAsync("nonexistent.sln");

        // Assert - Should handle gracefully without throwing
        Assert.False(result);
    }

    [Fact]
    public async Task McpServerService_GetCompilationErrors_WithRealService_ReturnsValidJson()
    {
        // This test exercises the REAL MCP server service code
        using var analysisService = new RoslynAnalysisService(_logger);

        // Act - This calls the real production MCP tool method
        var result = await DotNetAnalysisTools.GetCompilationErrors(analysisService);

        // Assert
        Assert.NotNull(result);
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(response.GetProperty("success").GetBoolean());
        Assert.Equal(0, response.GetProperty("error_count").GetInt32());
        Assert.Equal(0, response.GetProperty("warning_count").GetInt32());
    }

    [Fact]
    public async Task McpServerService_GetSolutionInfo_WithRealService_ReturnsValidJson()
    {
        // This test exercises the REAL MCP server service code
        using var analysisService = new RoslynAnalysisService(_logger);

        // Act - This calls the real production MCP tool method
        var result = await DotNetAnalysisTools.GetSolutionInfo(analysisService);

        // Assert
        Assert.NotNull(result);
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(response.GetProperty("success").GetBoolean());
        Assert.True(response.TryGetProperty("solution_info", out _));
    }

    [Fact]
    public async Task McpServerService_AnalyzeFile_WithRealService_ReturnsValidJson()
    {
        // This test exercises the REAL MCP server service code
        using var analysisService = new RoslynAnalysisService(_logger);

        // Act - This calls the real production MCP tool method
        var result = await DotNetAnalysisTools.AnalyzeFile(analysisService, "test.cs");

        // Assert
        Assert.NotNull(result);
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(response.GetProperty("success").GetBoolean());
        Assert.Equal("test.cs", response.GetProperty("file_path").GetString());
        Assert.Equal(0, response.GetProperty("error_count").GetInt32());
    }

    [Fact]
    public async Task McpServerService_GetCodeSuggestions_WithRealService_ReturnsValidJson()
    {
        // This test exercises the REAL code suggestions MCP tool
        using var analysisService = new RoslynAnalysisService(_logger);

        // Act - This calls the real production MCP tool method
        var result = await DotNetAnalysisTools.GetCodeSuggestions(analysisService);

        // Assert
        Assert.NotNull(result);
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(response.GetProperty("success").GetBoolean());
        Assert.Equal(0, response.GetProperty("suggestion_count").GetInt32());
        Assert.True(response.TryGetProperty("categories_analyzed", out _));
    }

    [Fact]
    public async Task McpServerService_GetFileSuggestions_WithRealService_ReturnsValidJson()
    {
        // This test exercises the REAL code suggestions MCP tool
        using var analysisService = new RoslynAnalysisService(_logger);

        // Act - This calls the real production MCP tool method
        var result = await DotNetAnalysisTools.GetFileSuggestions(analysisService, "test.cs");

        // Assert
        Assert.NotNull(result);
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(response.GetProperty("success").GetBoolean());
        Assert.Equal("test.cs", response.GetProperty("file_path").GetString());
        Assert.Equal(0, response.GetProperty("suggestion_count").GetInt32());
    }

    [Fact]
    public async Task McpServerService_GetSuggestionCategories_ReturnsValidJson()
    {
        // This test exercises the REAL code suggestions MCP tool
        
        // Act - This calls the real production MCP tool method
        var result = await DotNetAnalysisTools.GetSuggestionCategories();

        // Assert
        Assert.NotNull(result);
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(response.GetProperty("success").GetBoolean());
        Assert.True(response.TryGetProperty("categories", out _));
        Assert.True(response.TryGetProperty("priorities", out _));
        Assert.True(response.TryGetProperty("impacts", out _));
    }

    [Fact]
    public void SuggestionAnalysisOptions_RealInstantiation_WorksCorrectly()
    {
        // This test exercises the REAL model classes
        var options = new SuggestionAnalysisOptions();

        // Act - Exercise the real properties
        options.IncludedCategories.Add(SuggestionCategory.Performance);
        options.MinimumPriority = SuggestionPriority.High;
        options.MaxSuggestions = 50;

        // Assert
        Assert.Contains(SuggestionCategory.Performance, options.IncludedCategories);
        Assert.Equal(SuggestionPriority.High, options.MinimumPriority);
        Assert.Equal(50, options.MaxSuggestions);
    }

    [Fact]
    public void CodeSuggestion_RealInstantiation_WorksCorrectly()
    {
        // This test exercises the REAL model classes
        var suggestion = new CodeSuggestion
        {
            Id = "TEST001",
            Title = "Test Suggestion",
            Category = SuggestionCategory.Performance,
            Priority = SuggestionPriority.High,
            Impact = SuggestionImpact.Significant
        };

        // Act - Exercise the real properties
        suggestion.Tags.Add("performance");
        suggestion.CanAutoFix = true;

        // Assert
        Assert.Equal("TEST001", suggestion.Id);
        Assert.Equal(SuggestionCategory.Performance, suggestion.Category);
        Assert.Contains("performance", suggestion.Tags);
        Assert.True(suggestion.CanAutoFix);
    }

    [Fact]
    public void CompilationError_RealInstantiation_WorksCorrectly()
    {
        // This test exercises the REAL model classes
        var error = new CompilationError
        {
            Id = "CS0103",
            Message = "The name 'test' does not exist in the current context",
            Severity = Microsoft.CodeAnalysis.DiagnosticSeverity.Error,
            FilePath = "test.cs",
            StartLine = 10,
            ProjectName = "TestProject"
        };

        // Act - Exercise the real properties
        error.EndLine = 10;
        error.Category = "Compiler";

        // Assert
        Assert.Equal("CS0103", error.Id);
        Assert.Equal(Microsoft.CodeAnalysis.DiagnosticSeverity.Error, error.Severity);
        Assert.Equal("test.cs", error.FilePath);
        Assert.Equal(10, error.StartLine);
    }

    [Fact]
    public void SolutionInfo_RealInstantiation_WorksCorrectly()
    {
        // This test exercises the REAL model classes
        var solutionInfo = new SolutionInfo
        {
            Name = "TestSolution",
            FilePath = "test.sln",
            Projects = new List<ProjectInfo>()
        };

        // Act - Exercise the real properties
        solutionInfo.HasCompilationErrors = true;
        solutionInfo.TotalErrors = 5;

        // Assert
        Assert.Equal("TestSolution", solutionInfo.Name);
        Assert.True(solutionInfo.HasCompilationErrors);
        Assert.Equal(5, solutionInfo.TotalErrors);
        Assert.NotNull(solutionInfo.Projects);
    }

    [Fact]
    public void ProjectInfo_RealInstantiation_WorksCorrectly()
    {
        // This test exercises the REAL model classes
        var projectInfo = new ProjectInfo
        {
            Name = "TestProject",
            FilePath = "test.csproj",
            OutputType = "ConsoleApplication"
        };

        // Act - Exercise the real properties
        projectInfo.HasCompilationErrors = false;
        projectInfo.ErrorCount = 0;

        // Assert
        Assert.Equal("TestProject", projectInfo.Name);
        Assert.Equal("ConsoleApplication", projectInfo.OutputType);
        Assert.False(projectInfo.HasCompilationErrors);
        Assert.Equal(0, projectInfo.ErrorCount);
    }
}
