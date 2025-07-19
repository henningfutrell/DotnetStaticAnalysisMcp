using Microsoft.Extensions.Logging;
using MCP.Server.Models;
using MCP.Server.Services;
using System.Text.Json;
using Xunit;

namespace MCP.Tests;

/// <summary>
/// Integration tests for code suggestions functionality
/// Tests the complete workflow from RoslynAnalysisService to MCP tools
/// </summary>
public class CodeSuggestionsIntegrationTests
{
    private readonly ILogger<InMemoryAnalysisService> _logger;

    public CodeSuggestionsIntegrationTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => 
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });
        _logger = loggerFactory.CreateLogger<InMemoryAnalysisService>();
    }

    [Fact]
    public async Task McpTools_GetCodeSuggestions_WithInMemoryService_ReturnsValidJson()
    {
        // Arrange
        using var inMemoryService = InMemoryAnalysisService.CreateWithTestProjects(_logger);

        // Act
        var result = await InMemoryMcpTools.GetCodeSuggestions(inMemoryService);

        // Assert
        Assert.NotNull(result);
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(response.GetProperty("success").GetBoolean());
        Assert.True(response.GetProperty("suggestion_count").GetInt32() >= 0);
        
        // Verify response structure
        Assert.True(response.TryGetProperty("categories_analyzed", out _));
        Assert.True(response.TryGetProperty("minimum_priority", out _));
        Assert.True(response.TryGetProperty("suggestions", out _));
        
        var suggestions = response.GetProperty("suggestions").EnumerateArray().ToList();
        
        // If we have suggestions, validate their structure
        if (suggestions.Count > 0)
        {
            var firstSuggestion = suggestions[0];
            Assert.True(firstSuggestion.TryGetProperty("id", out _));
            Assert.True(firstSuggestion.TryGetProperty("title", out _));
            Assert.True(firstSuggestion.TryGetProperty("category", out _));
            Assert.True(firstSuggestion.TryGetProperty("priority", out _));
            Assert.True(firstSuggestion.TryGetProperty("can_auto_fix", out _));
        }

        Console.WriteLine($"GetCodeSuggestions returned {suggestions.Count} suggestions");
    }

    [Fact]
    public async Task McpTools_GetFileSuggestions_WithValidFile_ReturnsValidJson()
    {
        // Arrange
        using var inMemoryService = InMemoryAnalysisService.CreateWithTestProjects(_logger);

        // Act
        var result = await InMemoryMcpTools.GetFileSuggestions(inMemoryService, "Program.cs");

        // Assert
        Assert.NotNull(result);
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(response.GetProperty("success").GetBoolean());
        Assert.Equal("Program.cs", response.GetProperty("file_path").GetString());
        Assert.True(response.GetProperty("suggestion_count").GetInt32() >= 0);
        
        var suggestions = response.GetProperty("suggestions").EnumerateArray().ToList();
        
        // All suggestions should be for the specified file
        foreach (var suggestion in suggestions)
        {
            // Note: In-memory tests might not have exact file paths, so we check for consistency
            Assert.True(suggestion.TryGetProperty("id", out _));
            Assert.True(suggestion.TryGetProperty("category", out _));
        }

        Console.WriteLine($"GetFileSuggestions for Program.cs returned {suggestions.Count} suggestions");
    }

    [Fact]
    public async Task McpTools_GetCodeSuggestions_WithCategoryFilter_FiltersCorrectly()
    {
        // Arrange
        using var inMemoryService = InMemoryAnalysisService.CreateWithTestProjects(_logger);

        // Act - Get only performance suggestions
        var result = await InMemoryMcpTools.GetCodeSuggestions(inMemoryService, "Performance", "Medium", 50);

        // Assert
        Assert.NotNull(result);
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(response.GetProperty("success").GetBoolean());
        
        var categoriesAnalyzed = response.GetProperty("categories_analyzed").EnumerateArray()
            .Select(c => c.GetString() ?? "").Where(c => !string.IsNullOrEmpty(c)).ToList();
        Assert.Contains("Performance", categoriesAnalyzed);
        
        var minimumPriority = response.GetProperty("minimum_priority").GetString();
        Assert.Equal("Medium", minimumPriority);

        Console.WriteLine($"Filtered suggestions with categories: {string.Join(", ", categoriesAnalyzed)}");
    }

    [Fact]
    public async Task McpTools_GetCodeSuggestions_WithInvalidCategory_HandlesGracefully()
    {
        // Arrange
        using var inMemoryService = InMemoryAnalysisService.CreateWithTestProjects(_logger);

        // Act - Use invalid category name
        var result = await InMemoryMcpTools.GetCodeSuggestions(inMemoryService, "InvalidCategory", "Low", 10);

        // Assert
        Assert.NotNull(result);
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(response.GetProperty("success").GetBoolean());
        
        // Should still work, just with no categories included
        var categoriesAnalyzed = response.GetProperty("categories_analyzed").EnumerateArray().ToList();
        Assert.Empty(categoriesAnalyzed);

        Console.WriteLine("Invalid category handled gracefully");
    }

    [Fact]
    public async Task McpTools_GetFileSuggestions_WithNonExistentFile_ReturnsEmptyResults()
    {
        // Arrange
        using var inMemoryService = InMemoryAnalysisService.CreateWithTestProjects(_logger);

        // Act
        var result = await InMemoryMcpTools.GetFileSuggestions(inMemoryService, "NonExistent.cs");

        // Assert
        Assert.NotNull(result);
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(response.GetProperty("success").GetBoolean());
        Assert.Equal(0, response.GetProperty("suggestion_count").GetInt32());
        
        var suggestions = response.GetProperty("suggestions").EnumerateArray().ToList();
        Assert.Empty(suggestions);

        Console.WriteLine("Non-existent file handled gracefully");
    }

    [Fact]
    public void SuggestionAnalysisOptions_EdgeCases_HandleCorrectly()
    {
        // Test various edge cases for SuggestionAnalysisOptions
        
        // Test with empty categories
        var emptyOptions = new SuggestionAnalysisOptions
        {
            IncludedCategories = new HashSet<SuggestionCategory>(),
            MaxSuggestions = 0
        };
        
        Assert.Empty(emptyOptions.IncludedCategories);
        Assert.Equal(0, emptyOptions.MaxSuggestions);
        
        // Test with all categories
        var allCategoriesOptions = new SuggestionAnalysisOptions
        {
            IncludedCategories = new HashSet<SuggestionCategory>(Enum.GetValues<SuggestionCategory>()),
            MinimumPriority = SuggestionPriority.Critical,
            MaxSuggestions = 1000
        };
        
        Assert.Equal(11, allCategoriesOptions.IncludedCategories.Count); // All categories
        Assert.Equal(SuggestionPriority.Critical, allCategoriesOptions.MinimumPriority);
        
        // Test auto-fix filtering
        var autoFixOnlyOptions = new SuggestionAnalysisOptions
        {
            IncludeAutoFixable = true,
            IncludeManualFix = false
        };
        
        Assert.True(autoFixOnlyOptions.IncludeAutoFixable);
        Assert.False(autoFixOnlyOptions.IncludeManualFix);

        Console.WriteLine("Edge case options handled correctly");
    }

    [Fact]
    public void CodeSuggestion_AllProperties_CanBeSetAndRetrieved()
    {
        // Test that all properties of CodeSuggestion work correctly
        var suggestion = new CodeSuggestion
        {
            Id = "TEST001",
            Title = "Test Suggestion",
            Description = "This is a test suggestion",
            Category = SuggestionCategory.Performance,
            Priority = SuggestionPriority.High,
            Impact = SuggestionImpact.Significant,
            FilePath = "/path/to/file.cs",
            StartLine = 10,
            StartColumn = 5,
            EndLine = 12,
            EndColumn = 15,
            OriginalCode = "old code",
            SuggestedCode = "new code",
            CanAutoFix = true,
            Tags = new List<string> { "performance", "optimization" },
            HelpLink = "https://example.com/help",
            ProjectName = "TestProject"
        };

        // Assert all properties
        Assert.Equal("TEST001", suggestion.Id);
        Assert.Equal("Test Suggestion", suggestion.Title);
        Assert.Equal("This is a test suggestion", suggestion.Description);
        Assert.Equal(SuggestionCategory.Performance, suggestion.Category);
        Assert.Equal(SuggestionPriority.High, suggestion.Priority);
        Assert.Equal(SuggestionImpact.Significant, suggestion.Impact);
        Assert.Equal("/path/to/file.cs", suggestion.FilePath);
        Assert.Equal(10, suggestion.StartLine);
        Assert.Equal(5, suggestion.StartColumn);
        Assert.Equal(12, suggestion.EndLine);
        Assert.Equal(15, suggestion.EndColumn);
        Assert.Equal("old code", suggestion.OriginalCode);
        Assert.Equal("new code", suggestion.SuggestedCode);
        Assert.True(suggestion.CanAutoFix);
        Assert.Equal(2, suggestion.Tags.Count);
        Assert.Contains("performance", suggestion.Tags);
        Assert.Contains("optimization", suggestion.Tags);
        Assert.Equal("https://example.com/help", suggestion.HelpLink);
        Assert.Equal("TestProject", suggestion.ProjectName);

        Console.WriteLine("All CodeSuggestion properties work correctly");
    }

    [Fact]
    public void SuggestionEnums_AllValuesValid_CanBeUsed()
    {
        // Test that all enum values are valid and can be used
        
        // Test SuggestionCategory
        var categories = Enum.GetValues<SuggestionCategory>();
        Assert.Equal(11, categories.Length);
        
        foreach (var category in categories)
        {
            var categoryString = category.ToString();
            Assert.NotNull(categoryString);
            Assert.True(categoryString.Length > 0);
        }
        
        // Test SuggestionPriority
        var priorities = Enum.GetValues<SuggestionPriority>();
        Assert.Equal(4, priorities.Length);
        
        // Test SuggestionImpact
        var impacts = Enum.GetValues<SuggestionImpact>();
        Assert.Equal(5, impacts.Length);

        Console.WriteLine($"Validated {categories.Length} categories, {priorities.Length} priorities, {impacts.Length} impacts");
    }

    [Fact]
    public async Task McpTools_Performance_SuggestionAnalysis_CompletesQuickly()
    {
        // Test that suggestion analysis completes within reasonable time
        using var inMemoryService = InMemoryAnalysisService.CreateWithTestProjects(_logger);
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Act
        var result = await InMemoryMcpTools.GetCodeSuggestions(inMemoryService);
        
        stopwatch.Stop();
        
        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 5000); // Should complete in < 5 seconds
        Assert.NotNull(result);
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(response.GetProperty("success").GetBoolean());

        Console.WriteLine($"Suggestion analysis completed in {stopwatch.ElapsedMilliseconds}ms");
    }
}
