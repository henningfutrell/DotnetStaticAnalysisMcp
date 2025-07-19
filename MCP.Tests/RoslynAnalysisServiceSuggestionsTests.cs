using Microsoft.Extensions.Logging;
using MCP.Server.Models;
using MCP.Server.Services;

namespace MCP.Tests;

/// <summary>
/// Tests specifically for the code suggestions methods in RoslynAnalysisService
/// These test the core analysis service methods directly
/// </summary>
public class RoslynAnalysisServiceSuggestionsTests
{
    private readonly ILogger<RoslynAnalysisService> _logger;

    public RoslynAnalysisServiceSuggestionsTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => 
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });
        _logger = loggerFactory.CreateLogger<RoslynAnalysisService>();
    }

    [Test]
    public async Task GetCodeSuggestionsAsync_WithoutLoadedSolution_ReturnsEmptyList()
    {
        // Arrange
        var service = new RoslynAnalysisService(_logger);

        // Act
        var suggestions = await service.GetCodeSuggestionsAsync();

        // Assert
        await Assert.That(suggestions).IsNotNull();
        await Assert.That(suggestions.Count).IsEqualTo(0);

        // Cleanup
        service.Dispose();
    }

    [Test]
    public async Task GetFileSuggestionsAsync_WithoutLoadedSolution_ReturnsEmptyList()
    {
        // Arrange
        var service = new RoslynAnalysisService(_logger);

        // Act
        var suggestions = await service.GetFileSuggestionsAsync("test.cs");

        // Assert
        await Assert.That(suggestions).IsNotNull();
        await Assert.That(suggestions.Count).IsEqualTo(0);

        // Cleanup
        service.Dispose();
    }

    [Test]
    public async Task GetCodeSuggestionsAsync_WithCustomOptions_RespectsConfiguration()
    {
        // Arrange
        var service = new RoslynAnalysisService(_logger);
        var options = new SuggestionAnalysisOptions
        {
            IncludedCategories = new HashSet<SuggestionCategory> { SuggestionCategory.Performance },
            MinimumPriority = SuggestionPriority.High,
            MaxSuggestions = 10,
            IncludeAutoFixable = false,
            IncludeManualFix = true
        };

        // Act
        var suggestions = await service.GetCodeSuggestionsAsync(options);

        // Assert
        await Assert.That(suggestions).IsNotNull();
        await Assert.That(suggestions.Count).IsLessThanOrEqualTo(10); // Respects max limit
        
        // Since no solution is loaded, should be empty, but test that options are accepted
        await Assert.That(suggestions.Count).IsEqualTo(0);

        // Cleanup
        service.Dispose();
    }

    [Test]
    public async Task GetFileSuggestionsAsync_WithCustomOptions_RespectsConfiguration()
    {
        // Arrange
        var service = new RoslynAnalysisService(_logger);
        var options = new SuggestionAnalysisOptions
        {
            IncludedCategories = new HashSet<SuggestionCategory> { SuggestionCategory.Style },
            MinimumPriority = SuggestionPriority.Medium,
            MaxSuggestions = 5
        };

        // Act
        var suggestions = await service.GetFileSuggestionsAsync("nonexistent.cs", options);

        // Assert
        await Assert.That(suggestions).IsNotNull();
        await Assert.That(suggestions.Count).IsLessThanOrEqualTo(5); // Respects max limit
        await Assert.That(suggestions.Count).IsEqualTo(0); // No solution loaded

        // Cleanup
        service.Dispose();
    }

    [Test]
    public async Task SuggestionAnalysisOptions_DefaultValues_AreReasonable()
    {
        // Test that default options are sensible
        var options = new SuggestionAnalysisOptions();

        await Assert.That(options.IncludedCategories.Count).IsGreaterThan(0);
        await Assert.That(options.IncludedCategories).Contains(SuggestionCategory.Style);
        await Assert.That(options.IncludedCategories).Contains(SuggestionCategory.Performance);
        await Assert.That(options.IncludedCategories).Contains(SuggestionCategory.Security);
        await Assert.That(options.MinimumPriority).IsEqualTo(SuggestionPriority.Low);
        await Assert.That(options.MaxSuggestions).IsEqualTo(100);
        await Assert.That(options.IncludeAutoFixable).IsTrue();
        await Assert.That(options.IncludeManualFix).IsTrue();
        await Assert.That(options.IncludedAnalyzerIds.Count).IsEqualTo(0);
        await Assert.That(options.ExcludedAnalyzerIds.Count).IsEqualTo(0);
    }

    [Test]
    public async Task SuggestionAnalysisOptions_CanBeModified_WorksCorrectly()
    {
        // Test that options can be modified after creation
        var options = new SuggestionAnalysisOptions();

        // Modify categories
        options.IncludedCategories.Clear();
        options.IncludedCategories.Add(SuggestionCategory.Performance);
        options.IncludedCategories.Add(SuggestionCategory.Security);

        // Modify other settings
        options.MinimumPriority = SuggestionPriority.High;
        options.MaxSuggestions = 50;
        options.IncludeAutoFixable = false;

        // Add analyzer filters
        options.IncludedAnalyzerIds.Add("CA1822");
        options.ExcludedAnalyzerIds.Add("IDE0001");

        // Assert changes
        await Assert.That(options.IncludedCategories.Count).IsEqualTo(2);
        await Assert.That(options.IncludedCategories).Contains(SuggestionCategory.Performance);
        await Assert.That(options.IncludedCategories).Contains(SuggestionCategory.Security);
        await Assert.That(options.MinimumPriority).IsEqualTo(SuggestionPriority.High);
        await Assert.That(options.MaxSuggestions).IsEqualTo(50);
        await Assert.That(options.IncludeAutoFixable).IsFalse();
        await Assert.That(options.IncludedAnalyzerIds).Contains("CA1822");
        await Assert.That(options.ExcludedAnalyzerIds).Contains("IDE0001");
    }

    [Test]
    public async Task CodeSuggestion_DefaultValues_AreAppropriate()
    {
        // Test that CodeSuggestion has appropriate default values
        var suggestion = new CodeSuggestion();

        await Assert.That(suggestion.Id).IsEqualTo(string.Empty);
        await Assert.That(suggestion.Title).IsEqualTo(string.Empty);
        await Assert.That(suggestion.Description).IsEqualTo(string.Empty);
        await Assert.That(suggestion.Category).IsEqualTo(SuggestionCategory.Style); // First enum value
        await Assert.That(suggestion.Priority).IsEqualTo(SuggestionPriority.Low); // First enum value
        await Assert.That(suggestion.Impact).IsEqualTo(SuggestionImpact.Minimal); // First enum value
        await Assert.That(suggestion.FilePath).IsEqualTo(string.Empty);
        await Assert.That(suggestion.StartLine).IsEqualTo(0);
        await Assert.That(suggestion.StartColumn).IsEqualTo(0);
        await Assert.That(suggestion.EndLine).IsEqualTo(0);
        await Assert.That(suggestion.EndColumn).IsEqualTo(0);
        await Assert.That(suggestion.OriginalCode).IsEqualTo(string.Empty);
        await Assert.That(suggestion.SuggestedCode).IsNull();
        await Assert.That(suggestion.Tags).IsNotNull();
        await Assert.That(suggestion.Tags.Count).IsEqualTo(0);
        await Assert.That(suggestion.HelpLink).IsNull();
        await Assert.That(suggestion.ProjectName).IsEqualTo(string.Empty);
        await Assert.That(suggestion.CanAutoFix).IsFalse();
    }

    [Test]
    public async Task CodeSuggestion_CanBeCompared_ForEquality()
    {
        // Test that CodeSuggestion objects can be compared
        var suggestion1 = new CodeSuggestion
        {
            Id = "TEST001",
            Title = "Test",
            FilePath = "test.cs",
            StartLine = 10,
            StartColumn = 5
        };

        var suggestion2 = new CodeSuggestion
        {
            Id = "TEST001",
            Title = "Test",
            FilePath = "test.cs",
            StartLine = 10,
            StartColumn = 5
        };

        var suggestion3 = new CodeSuggestion
        {
            Id = "TEST002", // Different ID
            Title = "Test",
            FilePath = "test.cs",
            StartLine = 10,
            StartColumn = 5
        };

        // Test that objects with same values are considered equal for grouping purposes
        var group1Key = new { suggestion1.Id, suggestion1.FilePath, suggestion1.StartLine, suggestion1.StartColumn };
        var group2Key = new { suggestion2.Id, suggestion2.FilePath, suggestion2.StartLine, suggestion2.StartColumn };
        var group3Key = new { suggestion3.Id, suggestion3.FilePath, suggestion3.StartLine, suggestion3.StartColumn };

        await Assert.That(group1Key.Equals(group2Key)).IsTrue();
        await Assert.That(group1Key.Equals(group3Key)).IsFalse();
    }

    [Test]
    public async Task SuggestionEnums_CanBeConvertedToString_AndParsed()
    {
        // Test enum string conversion and parsing
        
        // Test SuggestionCategory
        var category = SuggestionCategory.Performance;
        var categoryString = category.ToString();
        await Assert.That(categoryString).IsEqualTo("Performance");
        
        var parsedCategory = Enum.Parse<SuggestionCategory>(categoryString);
        await Assert.That(parsedCategory).IsEqualTo(category);

        // Test SuggestionPriority
        var priority = SuggestionPriority.High;
        var priorityString = priority.ToString();
        await Assert.That(priorityString).IsEqualTo("High");
        
        var parsedPriority = Enum.Parse<SuggestionPriority>(priorityString);
        await Assert.That(parsedPriority).IsEqualTo(priority);

        // Test SuggestionImpact
        var impact = SuggestionImpact.Significant;
        var impactString = impact.ToString();
        await Assert.That(impactString).IsEqualTo("Significant");
        
        var parsedImpact = Enum.Parse<SuggestionImpact>(impactString);
        await Assert.That(parsedImpact).IsEqualTo(impact);
    }

    [Test]
    public async Task SuggestionAnalysisOptions_ExtremeValues_HandleGracefully()
    {
        // Test edge cases and extreme values
        
        // Test with zero max suggestions
        var zeroMaxOptions = new SuggestionAnalysisOptions { MaxSuggestions = 0 };
        await Assert.That(zeroMaxOptions.MaxSuggestions).IsEqualTo(0);

        // Test with very large max suggestions
        var largeMaxOptions = new SuggestionAnalysisOptions { MaxSuggestions = int.MaxValue };
        await Assert.That(largeMaxOptions.MaxSuggestions).IsEqualTo(int.MaxValue);

        // Test with all categories excluded
        var noCategoriesOptions = new SuggestionAnalysisOptions();
        noCategoriesOptions.IncludedCategories.Clear();
        await Assert.That(noCategoriesOptions.IncludedCategories.Count).IsEqualTo(0);

        // Test with critical priority only
        var criticalOnlyOptions = new SuggestionAnalysisOptions 
        { 
            MinimumPriority = SuggestionPriority.Critical 
        };
        await Assert.That(criticalOnlyOptions.MinimumPriority).IsEqualTo(SuggestionPriority.Critical);

        // Test with both auto-fix options disabled
        var noFixOptions = new SuggestionAnalysisOptions
        {
            IncludeAutoFixable = false,
            IncludeManualFix = false
        };
        await Assert.That(noFixOptions.IncludeAutoFixable).IsFalse();
        await Assert.That(noFixOptions.IncludeManualFix).IsFalse();
    }

    [Test]
    public async Task RoslynAnalysisService_SuggestionMethods_HandleExceptions_Gracefully()
    {
        // Test that suggestion methods handle exceptions gracefully
        var service = new RoslynAnalysisService(_logger);

        try
        {
            // These should not throw exceptions even with invalid inputs
            var suggestions1 = await service.GetCodeSuggestionsAsync(null);
            await Assert.That(suggestions1).IsNotNull();

            var suggestions2 = await service.GetFileSuggestionsAsync(null!);
            await Assert.That(suggestions2).IsNotNull();

            var suggestions3 = await service.GetFileSuggestionsAsync("");
            await Assert.That(suggestions3).IsNotNull();

            var suggestions4 = await service.GetFileSuggestionsAsync("   ");
            await Assert.That(suggestions4).IsNotNull();

            Console.WriteLine("Exception handling tests passed");
        }
        finally
        {
            service.Dispose();
        }
    }
}
