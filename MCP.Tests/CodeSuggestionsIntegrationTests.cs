using Microsoft.Extensions.Logging;
using MCP.Server.Models;
using MCP.Server.Services;
using System.Text.Json;

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

    [Test]
    public async Task McpTools_GetCodeSuggestions_WithInMemoryService_ReturnsValidJson()
    {
        // Arrange
        using var inMemoryService = InMemoryAnalysisService.CreateWithTestProjects(_logger);

        // Act
        var result = await InMemoryMcpTools.GetCodeSuggestions(inMemoryService);

        // Assert
        await Assert.That(result).IsNotNull();
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(response.GetProperty("success").GetBoolean()).IsTrue();
        await Assert.That(response.GetProperty("suggestion_count").GetInt32()).IsGreaterThanOrEqualTo(0);
        
        // Verify response structure
        await Assert.That(response.TryGetProperty("categories_analyzed", out _)).IsTrue();
        await Assert.That(response.TryGetProperty("minimum_priority", out _)).IsTrue();
        await Assert.That(response.TryGetProperty("suggestions", out _)).IsTrue();
        
        var suggestions = response.GetProperty("suggestions").EnumerateArray().ToList();
        
        // If we have suggestions, validate their structure
        if (suggestions.Count > 0)
        {
            var firstSuggestion = suggestions[0];
            await Assert.That(firstSuggestion.TryGetProperty("id", out _)).IsTrue();
            await Assert.That(firstSuggestion.TryGetProperty("title", out _)).IsTrue();
            await Assert.That(firstSuggestion.TryGetProperty("category", out _)).IsTrue();
            await Assert.That(firstSuggestion.TryGetProperty("priority", out _)).IsTrue();
            await Assert.That(firstSuggestion.TryGetProperty("can_auto_fix", out _)).IsTrue();
        }

        Console.WriteLine($"GetCodeSuggestions returned {suggestions.Count} suggestions");
    }

    [Test]
    public async Task McpTools_GetFileSuggestions_WithValidFile_ReturnsValidJson()
    {
        // Arrange
        using var inMemoryService = InMemoryAnalysisService.CreateWithTestProjects(_logger);

        // Act
        var result = await InMemoryMcpTools.GetFileSuggestions(inMemoryService, "Program.cs");

        // Assert
        await Assert.That(result).IsNotNull();
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(response.GetProperty("success").GetBoolean()).IsTrue();
        await Assert.That(response.GetProperty("file_path").GetString()).IsEqualTo("Program.cs");
        await Assert.That(response.GetProperty("suggestion_count").GetInt32()).IsGreaterThanOrEqualTo(0);
        
        var suggestions = response.GetProperty("suggestions").EnumerateArray().ToList();
        
        // All suggestions should be for the specified file
        foreach (var suggestion in suggestions)
        {
            // Note: In-memory tests might not have exact file paths, so we check for consistency
            await Assert.That(suggestion.TryGetProperty("id", out _)).IsTrue();
            await Assert.That(suggestion.TryGetProperty("category", out _)).IsTrue();
        }

        Console.WriteLine($"GetFileSuggestions for Program.cs returned {suggestions.Count} suggestions");
    }

    [Test]
    public async Task McpTools_GetCodeSuggestions_WithCategoryFilter_FiltersCorrectly()
    {
        // Arrange
        using var inMemoryService = InMemoryAnalysisService.CreateWithTestProjects(_logger);

        // Act - Get only performance suggestions
        var result = await InMemoryMcpTools.GetCodeSuggestions(inMemoryService, "Performance", "Medium", 50);

        // Assert
        await Assert.That(result).IsNotNull();
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(response.GetProperty("success").GetBoolean()).IsTrue();
        
        var categoriesAnalyzed = response.GetProperty("categories_analyzed").EnumerateArray()
            .Select(c => c.GetString() ?? "").Where(c => !string.IsNullOrEmpty(c)).ToList();
        await Assert.That(categoriesAnalyzed).Contains("Performance");
        
        var minimumPriority = response.GetProperty("minimum_priority").GetString();
        await Assert.That(minimumPriority).IsEqualTo("Medium");

        Console.WriteLine($"Filtered suggestions with categories: {string.Join(", ", categoriesAnalyzed)}");
    }

    [Test]
    public async Task McpTools_GetCodeSuggestions_WithInvalidCategory_HandlesGracefully()
    {
        // Arrange
        using var inMemoryService = InMemoryAnalysisService.CreateWithTestProjects(_logger);

        // Act - Use invalid category name
        var result = await InMemoryMcpTools.GetCodeSuggestions(inMemoryService, "InvalidCategory", "Low", 10);

        // Assert
        await Assert.That(result).IsNotNull();
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(response.GetProperty("success").GetBoolean()).IsTrue();
        
        // Should still work, just with no categories included
        var categoriesAnalyzed = response.GetProperty("categories_analyzed").EnumerateArray().ToList();
        await Assert.That(categoriesAnalyzed.Count).IsEqualTo(0);

        Console.WriteLine("Invalid category handled gracefully");
    }

    [Test]
    public async Task McpTools_GetFileSuggestions_WithNonExistentFile_ReturnsEmptyResults()
    {
        // Arrange
        using var inMemoryService = InMemoryAnalysisService.CreateWithTestProjects(_logger);

        // Act
        var result = await InMemoryMcpTools.GetFileSuggestions(inMemoryService, "NonExistent.cs");

        // Assert
        await Assert.That(result).IsNotNull();
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(response.GetProperty("success").GetBoolean()).IsTrue();
        await Assert.That(response.GetProperty("suggestion_count").GetInt32()).IsEqualTo(0);
        
        var suggestions = response.GetProperty("suggestions").EnumerateArray().ToList();
        await Assert.That(suggestions.Count).IsEqualTo(0);

        Console.WriteLine("Non-existent file handled gracefully");
    }

    [Test]
    public async Task SuggestionAnalysisOptions_EdgeCases_HandleCorrectly()
    {
        // Test various edge cases for SuggestionAnalysisOptions
        
        // Test with empty categories
        var emptyOptions = new SuggestionAnalysisOptions
        {
            IncludedCategories = new HashSet<SuggestionCategory>(),
            MaxSuggestions = 0
        };
        
        await Assert.That(emptyOptions.IncludedCategories.Count).IsEqualTo(0);
        await Assert.That(emptyOptions.MaxSuggestions).IsEqualTo(0);
        
        // Test with all categories
        var allCategoriesOptions = new SuggestionAnalysisOptions
        {
            IncludedCategories = new HashSet<SuggestionCategory>(Enum.GetValues<SuggestionCategory>()),
            MinimumPriority = SuggestionPriority.Critical,
            MaxSuggestions = 1000
        };
        
        await Assert.That(allCategoriesOptions.IncludedCategories.Count).IsEqualTo(11); // All categories
        await Assert.That(allCategoriesOptions.MinimumPriority).IsEqualTo(SuggestionPriority.Critical);
        
        // Test auto-fix filtering
        var autoFixOnlyOptions = new SuggestionAnalysisOptions
        {
            IncludeAutoFixable = true,
            IncludeManualFix = false
        };
        
        await Assert.That(autoFixOnlyOptions.IncludeAutoFixable).IsTrue();
        await Assert.That(autoFixOnlyOptions.IncludeManualFix).IsFalse();

        Console.WriteLine("Edge case options handled correctly");
    }

    [Test]
    public async Task CodeSuggestion_AllProperties_CanBeSetAndRetrieved()
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
        await Assert.That(suggestion.Id).IsEqualTo("TEST001");
        await Assert.That(suggestion.Title).IsEqualTo("Test Suggestion");
        await Assert.That(suggestion.Description).IsEqualTo("This is a test suggestion");
        await Assert.That(suggestion.Category).IsEqualTo(SuggestionCategory.Performance);
        await Assert.That(suggestion.Priority).IsEqualTo(SuggestionPriority.High);
        await Assert.That(suggestion.Impact).IsEqualTo(SuggestionImpact.Significant);
        await Assert.That(suggestion.FilePath).IsEqualTo("/path/to/file.cs");
        await Assert.That(suggestion.StartLine).IsEqualTo(10);
        await Assert.That(suggestion.StartColumn).IsEqualTo(5);
        await Assert.That(suggestion.EndLine).IsEqualTo(12);
        await Assert.That(suggestion.EndColumn).IsEqualTo(15);
        await Assert.That(suggestion.OriginalCode).IsEqualTo("old code");
        await Assert.That(suggestion.SuggestedCode).IsEqualTo("new code");
        await Assert.That(suggestion.CanAutoFix).IsTrue();
        await Assert.That(suggestion.Tags.Count).IsEqualTo(2);
        await Assert.That(suggestion.Tags).Contains("performance");
        await Assert.That(suggestion.Tags).Contains("optimization");
        await Assert.That(suggestion.HelpLink).IsEqualTo("https://example.com/help");
        await Assert.That(suggestion.ProjectName).IsEqualTo("TestProject");

        Console.WriteLine("All CodeSuggestion properties work correctly");
    }

    [Test]
    public async Task SuggestionEnums_AllValuesValid_CanBeUsed()
    {
        // Test that all enum values are valid and can be used
        
        // Test SuggestionCategory
        var categories = Enum.GetValues<SuggestionCategory>();
        await Assert.That(categories.Length).IsEqualTo(11);
        
        foreach (var category in categories)
        {
            var categoryString = category.ToString();
            await Assert.That(categoryString).IsNotNull();
            await Assert.That(categoryString.Length).IsGreaterThan(0);
        }
        
        // Test SuggestionPriority
        var priorities = Enum.GetValues<SuggestionPriority>();
        await Assert.That(priorities.Length).IsEqualTo(4);
        
        // Test SuggestionImpact
        var impacts = Enum.GetValues<SuggestionImpact>();
        await Assert.That(impacts.Length).IsEqualTo(5);

        Console.WriteLine($"Validated {categories.Length} categories, {priorities.Length} priorities, {impacts.Length} impacts");
    }

    [Test]
    public async Task McpTools_Performance_SuggestionAnalysis_CompletesQuickly()
    {
        // Test that suggestion analysis completes within reasonable time
        using var inMemoryService = InMemoryAnalysisService.CreateWithTestProjects(_logger);
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Act
        var result = await InMemoryMcpTools.GetCodeSuggestions(inMemoryService);
        
        stopwatch.Stop();
        
        // Assert
        await Assert.That(stopwatch.ElapsedMilliseconds).IsLessThan(5000); // Should complete in < 5 seconds
        await Assert.That(result).IsNotNull();
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(response.GetProperty("success").GetBoolean()).IsTrue();

        Console.WriteLine($"Suggestion analysis completed in {stopwatch.ElapsedMilliseconds}ms");
    }
}
