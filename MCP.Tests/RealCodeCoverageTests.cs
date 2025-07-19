using Microsoft.Extensions.Logging;
using MCP.Server.Models;
using MCP.Server.Services;
using System.Text.Json;

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

    [Test]
    public async Task RoslynAnalysisService_WithoutSolution_GetCompilationErrors_ReturnsEmpty()
    {
        // This test exercises the REAL RoslynAnalysisService code
        using var service = new RoslynAnalysisService(_logger);

        // Act - This calls the real production method
        var errors = await service.GetCompilationErrorsAsync();

        // Assert
        await Assert.That(errors).IsNotNull();
        await Assert.That(errors.Count).IsEqualTo(0);
    }

    [Test]
    public async Task RoslynAnalysisService_WithoutSolution_GetSolutionInfo_ReturnsNull()
    {
        // This test exercises the REAL RoslynAnalysisService code
        using var service = new RoslynAnalysisService(_logger);

        // Act - This calls the real production method
        var solutionInfo = await service.GetSolutionInfoAsync();

        // Assert
        await Assert.That(solutionInfo).IsNull();
    }

    [Test]
    public async Task RoslynAnalysisService_WithoutSolution_AnalyzeFile_ReturnsEmpty()
    {
        // This test exercises the REAL RoslynAnalysisService code
        using var service = new RoslynAnalysisService(_logger);

        // Act - This calls the real production method
        var errors = await service.AnalyzeFileAsync("nonexistent.cs");

        // Assert
        await Assert.That(errors).IsNotNull();
        await Assert.That(errors.Count).IsEqualTo(0);
    }

    [Test]
    public async Task RoslynAnalysisService_GetCodeSuggestions_WithoutSolution_ReturnsEmpty()
    {
        // This test exercises the REAL code suggestions functionality
        using var service = new RoslynAnalysisService(_logger);

        // Act - This calls the real production method
        var suggestions = await service.GetCodeSuggestionsAsync();

        // Assert
        await Assert.That(suggestions).IsNotNull();
        await Assert.That(suggestions.Count).IsEqualTo(0);
    }

    [Test]
    public async Task RoslynAnalysisService_GetFileSuggestions_WithoutSolution_ReturnsEmpty()
    {
        // This test exercises the REAL code suggestions functionality
        using var service = new RoslynAnalysisService(_logger);

        // Act - This calls the real production method
        var suggestions = await service.GetFileSuggestionsAsync("test.cs");

        // Assert
        await Assert.That(suggestions).IsNotNull();
        await Assert.That(suggestions.Count).IsEqualTo(0);
    }

    [Test]
    public async Task RoslynAnalysisService_LoadInvalidSolution_HandlesGracefully()
    {
        // This test exercises the REAL error handling code
        using var service = new RoslynAnalysisService(_logger);

        // Act - This calls the real production method with invalid input
        var result = await service.LoadSolutionAsync("nonexistent.sln");

        // Assert - Should handle gracefully without throwing
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task McpServerService_GetCompilationErrors_WithRealService_ReturnsValidJson()
    {
        // This test exercises the REAL MCP server service code
        using var analysisService = new RoslynAnalysisService(_logger);

        // Act - This calls the real production MCP tool method
        var result = await DotNetAnalysisTools.GetCompilationErrors(analysisService);

        // Assert
        await Assert.That(result).IsNotNull();
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(response.GetProperty("success").GetBoolean()).IsTrue();
        await Assert.That(response.GetProperty("error_count").GetInt32()).IsEqualTo(0);
        await Assert.That(response.GetProperty("warning_count").GetInt32()).IsEqualTo(0);
    }

    [Test]
    public async Task McpServerService_GetSolutionInfo_WithRealService_ReturnsValidJson()
    {
        // This test exercises the REAL MCP server service code
        using var analysisService = new RoslynAnalysisService(_logger);

        // Act - This calls the real production MCP tool method
        var result = await DotNetAnalysisTools.GetSolutionInfo(analysisService);

        // Assert
        await Assert.That(result).IsNotNull();
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(response.GetProperty("success").GetBoolean()).IsTrue();
        await Assert.That(response.TryGetProperty("solution_info", out _)).IsTrue();
    }

    [Test]
    public async Task McpServerService_AnalyzeFile_WithRealService_ReturnsValidJson()
    {
        // This test exercises the REAL MCP server service code
        using var analysisService = new RoslynAnalysisService(_logger);

        // Act - This calls the real production MCP tool method
        var result = await DotNetAnalysisTools.AnalyzeFile(analysisService, "test.cs");

        // Assert
        await Assert.That(result).IsNotNull();
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(response.GetProperty("success").GetBoolean()).IsTrue();
        await Assert.That(response.GetProperty("file_path").GetString()).IsEqualTo("test.cs");
        await Assert.That(response.GetProperty("error_count").GetInt32()).IsEqualTo(0);
    }

    [Test]
    public async Task McpServerService_GetCodeSuggestions_WithRealService_ReturnsValidJson()
    {
        // This test exercises the REAL code suggestions MCP tool
        using var analysisService = new RoslynAnalysisService(_logger);

        // Act - This calls the real production MCP tool method
        var result = await DotNetAnalysisTools.GetCodeSuggestions(analysisService);

        // Assert
        await Assert.That(result).IsNotNull();
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(response.GetProperty("success").GetBoolean()).IsTrue();
        await Assert.That(response.GetProperty("suggestion_count").GetInt32()).IsEqualTo(0);
        await Assert.That(response.TryGetProperty("categories_analyzed", out _)).IsTrue();
    }

    [Test]
    public async Task McpServerService_GetFileSuggestions_WithRealService_ReturnsValidJson()
    {
        // This test exercises the REAL code suggestions MCP tool
        using var analysisService = new RoslynAnalysisService(_logger);

        // Act - This calls the real production MCP tool method
        var result = await DotNetAnalysisTools.GetFileSuggestions(analysisService, "test.cs");

        // Assert
        await Assert.That(result).IsNotNull();
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(response.GetProperty("success").GetBoolean()).IsTrue();
        await Assert.That(response.GetProperty("file_path").GetString()).IsEqualTo("test.cs");
        await Assert.That(response.GetProperty("suggestion_count").GetInt32()).IsEqualTo(0);
    }

    [Test]
    public async Task McpServerService_GetSuggestionCategories_ReturnsValidJson()
    {
        // This test exercises the REAL code suggestions MCP tool
        
        // Act - This calls the real production MCP tool method
        var result = await DotNetAnalysisTools.GetSuggestionCategories();

        // Assert
        await Assert.That(result).IsNotNull();
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(response.GetProperty("success").GetBoolean()).IsTrue();
        await Assert.That(response.TryGetProperty("categories", out _)).IsTrue();
        await Assert.That(response.TryGetProperty("priorities", out _)).IsTrue();
        await Assert.That(response.TryGetProperty("impacts", out _)).IsTrue();
    }

    [Test]
    public async Task SuggestionAnalysisOptions_RealInstantiation_WorksCorrectly()
    {
        // This test exercises the REAL model classes
        var options = new SuggestionAnalysisOptions();

        // Act - Exercise the real properties
        options.IncludedCategories.Add(SuggestionCategory.Performance);
        options.MinimumPriority = SuggestionPriority.High;
        options.MaxSuggestions = 50;

        // Assert
        await Assert.That(options.IncludedCategories).Contains(SuggestionCategory.Performance);
        await Assert.That(options.MinimumPriority).IsEqualTo(SuggestionPriority.High);
        await Assert.That(options.MaxSuggestions).IsEqualTo(50);
    }

    [Test]
    public async Task CodeSuggestion_RealInstantiation_WorksCorrectly()
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
        await Assert.That(suggestion.Id).IsEqualTo("TEST001");
        await Assert.That(suggestion.Category).IsEqualTo(SuggestionCategory.Performance);
        await Assert.That(suggestion.Tags).Contains("performance");
        await Assert.That(suggestion.CanAutoFix).IsTrue();
    }

    [Test]
    public async Task CompilationError_RealInstantiation_WorksCorrectly()
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
        await Assert.That(error.Id).IsEqualTo("CS0103");
        await Assert.That(error.Severity).IsEqualTo(Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
        await Assert.That(error.FilePath).IsEqualTo("test.cs");
        await Assert.That(error.StartLine).IsEqualTo(10);
    }

    [Test]
    public async Task SolutionInfo_RealInstantiation_WorksCorrectly()
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
        await Assert.That(solutionInfo.Name).IsEqualTo("TestSolution");
        await Assert.That(solutionInfo.HasCompilationErrors).IsTrue();
        await Assert.That(solutionInfo.TotalErrors).IsEqualTo(5);
        await Assert.That(solutionInfo.Projects).IsNotNull();
    }

    [Test]
    public async Task ProjectInfo_RealInstantiation_WorksCorrectly()
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
        await Assert.That(projectInfo.Name).IsEqualTo("TestProject");
        await Assert.That(projectInfo.OutputType).IsEqualTo("ConsoleApplication");
        await Assert.That(projectInfo.HasCompilationErrors).IsFalse();
        await Assert.That(projectInfo.ErrorCount).IsEqualTo(0);
    }
}
