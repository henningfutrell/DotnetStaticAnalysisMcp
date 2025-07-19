using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using MCP.Server.Models;
using MCP.Server.Services;
using System.Text.Json;

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

    [Test]
    public async Task RoslynAnalysisService_Constructor_CreatesInstance()
    {
        // This tests the REAL production constructor
        using var service = new RoslynAnalysisService(_logger);
        
        // Assert the service was created
        await Assert.That(service).IsNotNull();
    }

    [Test]
    public async Task RoslynAnalysisService_GetCompilationErrorsAsync_WithoutSolution_ReturnsEmptyList()
    {
        // This tests the REAL production method
        using var service = new RoslynAnalysisService(_logger);

        // Act - Call the REAL production method
        var errors = await service.GetCompilationErrorsAsync();

        // Assert
        await Assert.That(errors).IsNotNull();
        await Assert.That(errors.Count).IsEqualTo(0);
    }

    [Test]
    public async Task RoslynAnalysisService_GetSolutionInfoAsync_WithoutSolution_ReturnsNull()
    {
        // This tests the REAL production method
        using var service = new RoslynAnalysisService(_logger);

        // Act - Call the REAL production method
        var solutionInfo = await service.GetSolutionInfoAsync();

        // Assert
        await Assert.That(solutionInfo).IsNull();
    }

    [Test]
    public async Task RoslynAnalysisService_AnalyzeFileAsync_WithoutSolution_ReturnsEmptyList()
    {
        // This tests the REAL production method
        using var service = new RoslynAnalysisService(_logger);

        // Act - Call the REAL production method
        var errors = await service.AnalyzeFileAsync("test.cs");

        // Assert
        await Assert.That(errors).IsNotNull();
        await Assert.That(errors.Count).IsEqualTo(0);
    }

    [Test]
    public async Task RoslynAnalysisService_LoadSolutionAsync_WithInvalidPath_ReturnsFalse()
    {
        // This tests the REAL production method
        using var service = new RoslynAnalysisService(_logger);

        // Act - Call the REAL production method
        var result = await service.LoadSolutionAsync("nonexistent.sln");

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task RoslynAnalysisService_GetCodeSuggestionsAsync_WithoutSolution_ReturnsEmptyList()
    {
        // This tests the REAL production code suggestions method
        using var service = new RoslynAnalysisService(_logger);

        // Act - Call the REAL production method
        var suggestions = await service.GetCodeSuggestionsAsync();

        // Assert
        await Assert.That(suggestions).IsNotNull();
        await Assert.That(suggestions.Count).IsEqualTo(0);
    }

    [Test]
    public async Task RoslynAnalysisService_GetFileSuggestionsAsync_WithoutSolution_ReturnsEmptyList()
    {
        // This tests the REAL production code suggestions method
        using var service = new RoslynAnalysisService(_logger);

        // Act - Call the REAL production method
        var suggestions = await service.GetFileSuggestionsAsync("test.cs");

        // Assert
        await Assert.That(suggestions).IsNotNull();
        await Assert.That(suggestions.Count).IsEqualTo(0);
    }

    [Test]
    public async Task CompilationError_Constructor_CreatesInstance()
    {
        // This tests the REAL production model
        var error = new CompilationError();

        // Assert
        await Assert.That(error).IsNotNull();
        await Assert.That(error.Id).IsEqualTo(string.Empty);
        await Assert.That(error.Message).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task CompilationError_Properties_CanBeSetAndRetrieved()
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
        await Assert.That(error.Id).IsEqualTo("CS0103");
        await Assert.That(error.Message).IsEqualTo("Test error");
        await Assert.That(error.Severity).IsEqualTo(DiagnosticSeverity.Error);
        await Assert.That(error.FilePath).IsEqualTo("test.cs");
        await Assert.That(error.StartLine).IsEqualTo(10);
        await Assert.That(error.EndLine).IsEqualTo(10);
        await Assert.That(error.ProjectName).IsEqualTo("TestProject");
    }

    [Test]
    public async Task SolutionInfo_Constructor_CreatesInstance()
    {
        // This tests the REAL production model
        var solutionInfo = new MCP.Server.Models.SolutionInfo();

        // Assert
        await Assert.That(solutionInfo).IsNotNull();
        await Assert.That(solutionInfo.Name).IsEqualTo(string.Empty);
        await Assert.That(solutionInfo.Projects).IsNotNull();
        await Assert.That(solutionInfo.Projects.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ProjectInfo_Constructor_CreatesInstance()
    {
        // This tests the REAL production model
        var projectInfo = new MCP.Server.Models.ProjectInfo();

        // Assert
        await Assert.That(projectInfo).IsNotNull();
        await Assert.That(projectInfo.Name).IsEqualTo(string.Empty);
        await Assert.That(projectInfo.OutputType).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task CodeSuggestion_Constructor_CreatesInstance()
    {
        // This tests the REAL production model
        var suggestion = new CodeSuggestion();

        // Assert
        await Assert.That(suggestion).IsNotNull();
        await Assert.That(suggestion.Id).IsEqualTo(string.Empty);
        await Assert.That(suggestion.Title).IsEqualTo(string.Empty);
        await Assert.That(suggestion.Tags).IsNotNull();
        await Assert.That(suggestion.Tags.Count).IsEqualTo(0);
    }

    [Test]
    public async Task SuggestionAnalysisOptions_Constructor_CreatesInstance()
    {
        // This tests the REAL production model
        var options = new SuggestionAnalysisOptions();

        // Assert
        await Assert.That(options).IsNotNull();
        await Assert.That(options.IncludedCategories).IsNotNull();
        await Assert.That(options.IncludedCategories.Count).IsGreaterThan(0);
        await Assert.That(options.MaxSuggestions).IsEqualTo(100);
    }

    [Test]
    public async Task McpServerService_GetCompilationErrors_WithRealService_ReturnsValidJson()
    {
        // This tests the REAL production MCP service
        using var analysisService = new RoslynAnalysisService(_logger);

        // Act - Call the REAL production MCP tool
        var result = await DotNetAnalysisTools.GetCompilationErrors(analysisService);

        // Assert
        await Assert.That(result).IsNotNull();
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(response.GetProperty("success").GetBoolean()).IsTrue();
        await Assert.That(response.GetProperty("error_count").GetInt32()).IsEqualTo(0);
    }

    [Test]
    public async Task McpServerService_GetSolutionInfo_WithRealService_ReturnsValidJson()
    {
        // This tests the REAL production MCP service
        using var analysisService = new RoslynAnalysisService(_logger);

        // Act - Call the REAL production MCP tool
        var result = await DotNetAnalysisTools.GetSolutionInfo(analysisService);

        // Assert
        await Assert.That(result).IsNotNull();
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(response.GetProperty("success").GetBoolean()).IsTrue();
    }

    [Test]
    public async Task McpServerService_AnalyzeFile_WithRealService_ReturnsValidJson()
    {
        // This tests the REAL production MCP service
        using var analysisService = new RoslynAnalysisService(_logger);

        // Act - Call the REAL production MCP tool
        var result = await DotNetAnalysisTools.AnalyzeFile(analysisService, "test.cs");

        // Assert
        await Assert.That(result).IsNotNull();
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(response.GetProperty("success").GetBoolean()).IsTrue();
        await Assert.That(response.GetProperty("file_path").GetString()).IsEqualTo("test.cs");
    }

    [Test]
    public async Task McpServerService_GetCodeSuggestions_WithRealService_ReturnsValidJson()
    {
        // This tests the REAL production MCP service
        using var analysisService = new RoslynAnalysisService(_logger);

        // Act - Call the REAL production MCP tool
        var result = await DotNetAnalysisTools.GetCodeSuggestions(analysisService);

        // Assert
        await Assert.That(result).IsNotNull();
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(response.GetProperty("success").GetBoolean()).IsTrue();
        await Assert.That(response.GetProperty("suggestion_count").GetInt32()).IsEqualTo(0);
    }

    [Test]
    public async Task McpServerService_GetFileSuggestions_WithRealService_ReturnsValidJson()
    {
        // This tests the REAL production MCP service
        using var analysisService = new RoslynAnalysisService(_logger);

        // Act - Call the REAL production MCP tool
        var result = await DotNetAnalysisTools.GetFileSuggestions(analysisService, "test.cs");

        // Assert
        await Assert.That(result).IsNotNull();
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(response.GetProperty("success").GetBoolean()).IsTrue();
        await Assert.That(response.GetProperty("file_path").GetString()).IsEqualTo("test.cs");
    }

    [Test]
    public async Task McpServerService_GetSuggestionCategories_ReturnsValidJson()
    {
        // This tests the REAL production MCP service
        
        // Act - Call the REAL production MCP tool
        var result = await DotNetAnalysisTools.GetSuggestionCategories();

        // Assert
        await Assert.That(result).IsNotNull();
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(response.GetProperty("success").GetBoolean()).IsTrue();
        await Assert.That(response.TryGetProperty("categories", out _)).IsTrue();
    }
}
