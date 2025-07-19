using Microsoft.Extensions.Logging;
using MCP.Server.Services;
using MCP.Server.Models;
using System.Text.Json;
using Microsoft.Build.Locator;

namespace MCP.IntegrationTests;

/// <summary>
/// REAL integration tests that exercise the actual production system
/// These tests will show up in code coverage reports and validate the real system works
/// They may be slower but provide actual validation of production functionality
/// </summary>
public class RealSystemIntegrationTests : IDisposable
{
    private readonly ILogger<RoslynAnalysisService> _logger;
    private static bool _msbuildInitialized = false;
    private static readonly object _lock = new();

    public RealSystemIntegrationTests()
    {
        // Initialize MSBuild once for all tests
        lock (_lock)
        {
            if (!_msbuildInitialized)
            {
                try
                {
                    if (!MSBuildLocator.IsRegistered)
                    {
                        MSBuildLocator.RegisterDefaults();
                    }
                    _msbuildInitialized = true;
                }
                catch (Exception ex)
                {
                    // MSBuild initialization may fail in some environments, that's OK
                    Console.WriteLine($"MSBuild initialization failed: {ex.Message}");
                }
            }
        }

        var loggerFactory = LoggerFactory.Create(builder => 
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });
        _logger = loggerFactory.CreateLogger<RoslynAnalysisService>();
    }

    [Fact]
    public void RoslynAnalysisService_CanBeInstantiated()
    {
        // Test that we can create the real production service
        using var service = new RoslynAnalysisService(_logger);
        
        Assert.NotNull(service);
    }

    [Fact]
    public async Task RoslynAnalysisService_GetCompilationErrorsAsync_WithoutSolution_ReturnsEmptyList()
    {
        // Test the REAL production method
        using var service = new RoslynAnalysisService(_logger);

        var errors = await service.GetCompilationErrorsAsync();

        Assert.NotNull(errors);
        Assert.Empty(errors);
    }

    [Fact]
    public async Task RoslynAnalysisService_GetSolutionInfoAsync_WithoutSolution_ReturnsNull()
    {
        // Test the REAL production method
        using var service = new RoslynAnalysisService(_logger);

        var solutionInfo = await service.GetSolutionInfoAsync();

        Assert.Null(solutionInfo);
    }

    [Fact]
    public async Task RoslynAnalysisService_AnalyzeFileAsync_WithoutSolution_ReturnsEmptyList()
    {
        // Test the REAL production method
        using var service = new RoslynAnalysisService(_logger);

        var errors = await service.AnalyzeFileAsync("test.cs");

        Assert.NotNull(errors);
        Assert.Empty(errors);
    }

    [Fact]
    public async Task RoslynAnalysisService_LoadSolutionAsync_WithInvalidPath_ReturnsFalse()
    {
        // Test the REAL production method with error handling
        using var service = new RoslynAnalysisService(_logger);

        var result = await service.LoadSolutionAsync("nonexistent.sln");

        Assert.False(result);
    }

    [Fact]
    public async Task RoslynAnalysisService_GetCodeSuggestionsAsync_WithoutSolution_ReturnsEmptyList()
    {
        // Test the REAL code suggestions functionality
        using var service = new RoslynAnalysisService(_logger);

        var suggestions = await service.GetCodeSuggestionsAsync();

        Assert.NotNull(suggestions);
        Assert.Empty(suggestions);
    }

    [Fact]
    public async Task RoslynAnalysisService_GetFileSuggestionsAsync_WithoutSolution_ReturnsEmptyList()
    {
        // Test the REAL file-specific code suggestions functionality
        using var service = new RoslynAnalysisService(_logger);

        var suggestions = await service.GetFileSuggestionsAsync("test.cs");

        Assert.NotNull(suggestions);
        Assert.Empty(suggestions);
    }

    [Fact]
    public async Task RoslynAnalysisService_GetCodeSuggestionsAsync_WithOptions_ReturnsEmptyList()
    {
        // Test the REAL code suggestions functionality with options
        using var service = new RoslynAnalysisService(_logger);
        var options = new SuggestionAnalysisOptions
        {
            MaxSuggestions = 50,
            MinimumPriority = SuggestionPriority.High
        };

        var suggestions = await service.GetCodeSuggestionsAsync(options);

        Assert.NotNull(suggestions);
        Assert.Empty(suggestions);
    }

    [Fact]
    public async Task RoslynAnalysisService_GetFileSuggestionsAsync_WithOptions_ReturnsEmptyList()
    {
        // Test the REAL file-specific code suggestions functionality with options
        using var service = new RoslynAnalysisService(_logger);
        var options = new SuggestionAnalysisOptions
        {
            MaxSuggestions = 25,
            MinimumPriority = SuggestionPriority.Medium
        };

        var suggestions = await service.GetFileSuggestionsAsync("test.cs", options);

        Assert.NotNull(suggestions);
        Assert.Empty(suggestions);
    }

    [Fact]
    public async Task McpServerService_GetCompilationErrors_WithRealService_ReturnsValidJson()
    {
        // Test the REAL MCP server service
        using var analysisService = new RoslynAnalysisService(_logger);

        var result = await DotNetAnalysisTools.GetCompilationErrors(analysisService);

        Assert.NotNull(result);
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(response.GetProperty("success").GetBoolean());
        Assert.Equal(0, response.GetProperty("error_count").GetInt32());
        Assert.Equal(0, response.GetProperty("warning_count").GetInt32());
        Assert.True(response.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task McpServerService_GetSolutionInfo_WithRealService_ReturnsValidJson()
    {
        // Test the REAL MCP server service
        using var analysisService = new RoslynAnalysisService(_logger);

        var result = await DotNetAnalysisTools.GetSolutionInfo(analysisService);

        Assert.NotNull(result);
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(response.GetProperty("success").GetBoolean());
        Assert.True(response.TryGetProperty("solution_info", out _));
    }

    [Fact]
    public async Task McpServerService_AnalyzeFile_WithRealService_ReturnsValidJson()
    {
        // Test the REAL MCP server service
        using var analysisService = new RoslynAnalysisService(_logger);

        var result = await DotNetAnalysisTools.AnalyzeFile(analysisService, "test.cs");

        Assert.NotNull(result);
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(response.GetProperty("success").GetBoolean());
        Assert.Equal("test.cs", response.GetProperty("file_path").GetString());
        Assert.Equal(0, response.GetProperty("error_count").GetInt32());
        Assert.True(response.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task McpServerService_GetCodeSuggestions_WithRealService_ReturnsValidJson()
    {
        // Test the REAL code suggestions MCP tool
        using var analysisService = new RoslynAnalysisService(_logger);

        var result = await DotNetAnalysisTools.GetCodeSuggestions(analysisService);

        Assert.NotNull(result);
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(response.GetProperty("success").GetBoolean());
        Assert.Equal(0, response.GetProperty("suggestion_count").GetInt32());
        Assert.True(response.TryGetProperty("categories_analyzed", out _));
        Assert.True(response.TryGetProperty("suggestions", out _));
    }

    [Fact]
    public async Task McpServerService_GetFileSuggestions_WithRealService_ReturnsValidJson()
    {
        // Test the REAL file-specific code suggestions MCP tool
        using var analysisService = new RoslynAnalysisService(_logger);

        var result = await DotNetAnalysisTools.GetFileSuggestions(analysisService, "test.cs");

        Assert.NotNull(result);
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(response.GetProperty("success").GetBoolean());
        Assert.Equal("test.cs", response.GetProperty("file_path").GetString());
        Assert.Equal(0, response.GetProperty("suggestion_count").GetInt32());
        Assert.True(response.TryGetProperty("suggestions", out _));
    }

    [Fact]
    public async Task McpServerService_GetSuggestionCategories_ReturnsValidJson()
    {
        // Test the REAL suggestion categories MCP tool
        
        var result = await DotNetAnalysisTools.GetSuggestionCategories();

        Assert.NotNull(result);
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(response.GetProperty("success").GetBoolean());
        Assert.True(response.TryGetProperty("categories", out _));
        Assert.True(response.TryGetProperty("priorities", out _));
        Assert.True(response.TryGetProperty("impacts", out _));
        
        // Verify categories structure
        var categories = response.GetProperty("categories").EnumerateArray().ToList();
        Assert.True(categories.Count > 0);
        
        var firstCategory = categories[0];
        Assert.True(firstCategory.TryGetProperty("name", out _));
        Assert.True(firstCategory.TryGetProperty("description", out _));
    }

    [Fact]
    public async Task McpServerService_GetCodeSuggestions_WithParameters_ReturnsValidJson()
    {
        // Test the REAL code suggestions MCP tool with parameters
        using var analysisService = new RoslynAnalysisService(_logger);

        var result = await DotNetAnalysisTools.GetCodeSuggestions(
            analysisService, 
            categories: "Performance,Security", 
            minimumPriority: "Medium", 
            maxSuggestions: 50);

        Assert.NotNull(result);
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(response.GetProperty("success").GetBoolean());
        Assert.Equal(0, response.GetProperty("suggestion_count").GetInt32());
        Assert.Equal("Medium", response.GetProperty("minimum_priority").GetString());
        
        var categoriesAnalyzed = response.GetProperty("categories_analyzed").EnumerateArray()
            .Select(c => c.GetString()).ToList();
        Assert.Contains("Performance", categoriesAnalyzed);
        Assert.Contains("Security", categoriesAnalyzed);
    }

    [Fact]
    public async Task McpServerService_GetFileSuggestions_WithParameters_ReturnsValidJson()
    {
        // Test the REAL file suggestions MCP tool with parameters
        using var analysisService = new RoslynAnalysisService(_logger);

        var result = await DotNetAnalysisTools.GetFileSuggestions(
            analysisService, 
            "Program.cs",
            categories: "Style,Modernization", 
            minimumPriority: "High", 
            maxSuggestions: 25);

        Assert.NotNull(result);
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(response.GetProperty("success").GetBoolean());
        Assert.Equal("Program.cs", response.GetProperty("file_path").GetString());
        Assert.Equal(0, response.GetProperty("suggestion_count").GetInt32());
        Assert.Equal("High", response.GetProperty("minimum_priority").GetString());
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
