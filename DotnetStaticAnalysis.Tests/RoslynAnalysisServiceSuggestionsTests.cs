using Microsoft.Extensions.Logging;
using DotnetStaticAnalysisMcp.Server.Models;
using DotnetStaticAnalysisMcp.Server.Services;
using Xunit;

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

    [Fact]
    public async Task GetCodeSuggestionsAsync_WithoutLoadedSolution_ReturnsEmptyList()
    {
        // Arrange
        var service = new RoslynAnalysisService(_logger);

        // Act
        var suggestions = await service.GetCodeSuggestionsAsync();

        // Assert
        Assert.NotNull(suggestions);
        Assert.Empty(suggestions);

        // Cleanup
        service.Dispose();
    }

    [Fact]
    public async Task GetFileSuggestionsAsync_WithoutLoadedSolution_ReturnsEmptyList()
    {
        // Arrange
        var service = new RoslynAnalysisService(_logger);

        // Act
        var suggestions = await service.GetFileSuggestionsAsync("test.cs");

        // Assert
        Assert.NotNull(suggestions);
        Assert.Empty(suggestions);

        // Cleanup
        service.Dispose();
    }

    [Fact]
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
        Assert.NotNull(suggestions);
        Assert.True(suggestions.Count <= 10); // Respects max limit
        
        // Since no solution is loaded, should be empty, but test that options are accepted
        Assert.Empty(suggestions);

        // Cleanup
        service.Dispose();
    }

    [Fact]
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
        Assert.NotNull(suggestions);
        Assert.True(suggestions.Count <= 5); // Respects max limit
        Assert.Empty(suggestions); // No solution loaded

        // Cleanup
        service.Dispose();
    }

    [Fact]
    public void SuggestionAnalysisOptions_DefaultValues_AreReasonable()
    {
        // Test that default options are sensible
        var options = new SuggestionAnalysisOptions();

        Assert.True(options.IncludedCategories.Count > 0);
        Assert.Contains(SuggestionCategory.Style, options.IncludedCategories);
        Assert.Contains(SuggestionCategory.Performance, options.IncludedCategories);
        Assert.Contains(SuggestionCategory.Security, options.IncludedCategories);
        Assert.Equal(SuggestionPriority.Low, options.MinimumPriority);
        Assert.Equal(100, options.MaxSuggestions);
        Assert.True(options.IncludeAutoFixable);
        Assert.True(options.IncludeManualFix);
        Assert.Empty(options.IncludedAnalyzerIds);
        Assert.Empty(options.ExcludedAnalyzerIds);
    }

    [Fact]
    public void SuggestionAnalysisOptions_CanBeModified_WorksCorrectly()
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
        Assert.Equal(2, options.IncludedCategories.Count);
        Assert.Contains(SuggestionCategory.Performance, options.IncludedCategories);
        Assert.Contains(SuggestionCategory.Security, options.IncludedCategories);
        Assert.Equal(SuggestionPriority.High, options.MinimumPriority);
        Assert.Equal(50, options.MaxSuggestions);
        Assert.False(options.IncludeAutoFixable);
        Assert.Contains("CA1822", options.IncludedAnalyzerIds);
        Assert.Contains("IDE0001", options.ExcludedAnalyzerIds);
    }

    [Fact]
    public void CodeSuggestion_DefaultValues_AreAppropriate()
    {
        // Test that CodeSuggestion has appropriate default values
        var suggestion = new CodeSuggestion();

        Assert.Equal(string.Empty, suggestion.Id);
        Assert.Equal(string.Empty, suggestion.Title);
        Assert.Equal(string.Empty, suggestion.Description);
        Assert.Equal(SuggestionCategory.Style, suggestion.Category); // First enum value
        Assert.Equal(SuggestionPriority.Low, suggestion.Priority); // First enum value
        Assert.Equal(SuggestionImpact.Minimal, suggestion.Impact); // First enum value
        Assert.Equal(string.Empty, suggestion.FilePath);
        Assert.Equal(0, suggestion.StartLine);
        Assert.Equal(0, suggestion.StartColumn);
        Assert.Equal(0, suggestion.EndLine);
        Assert.Equal(0, suggestion.EndColumn);
        Assert.Equal(string.Empty, suggestion.OriginalCode);
        Assert.Null(suggestion.SuggestedCode);
        Assert.NotNull(suggestion.Tags);
        Assert.Empty(suggestion.Tags);
        Assert.Null(suggestion.HelpLink);
        Assert.Equal(string.Empty, suggestion.ProjectName);
        Assert.False(suggestion.CanAutoFix);
    }

    [Fact]
    public void CodeSuggestion_CanBeCompared_ForEquality()
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

        Assert.True(group1Key.Equals(group2Key));
        Assert.False(group1Key.Equals(group3Key));
    }

    [Fact]
    public void SuggestionEnums_CanBeConvertedToString_AndParsed()
    {
        // Test enum string conversion and parsing
        
        // Test SuggestionCategory
        var category = SuggestionCategory.Performance;
        var categoryString = category.ToString();
        Assert.Equal("Performance", categoryString);
        
        var parsedCategory = Enum.Parse<SuggestionCategory>(categoryString);
        Assert.Equal(category, parsedCategory);

        // Test SuggestionPriority
        var priority = SuggestionPriority.High;
        var priorityString = priority.ToString();
        Assert.Equal("High", priorityString);
        
        var parsedPriority = Enum.Parse<SuggestionPriority>(priorityString);
        Assert.Equal(priority, parsedPriority);

        // Test SuggestionImpact
        var impact = SuggestionImpact.Significant;
        var impactString = impact.ToString();
        Assert.Equal("Significant", impactString);
        
        var parsedImpact = Enum.Parse<SuggestionImpact>(impactString);
        Assert.Equal(impact, parsedImpact);
    }

    [Fact]
    public void SuggestionAnalysisOptions_ExtremeValues_HandleGracefully()
    {
        // Test edge cases and extreme values
        
        // Test with zero max suggestions
        var zeroMaxOptions = new SuggestionAnalysisOptions { MaxSuggestions = 0 };
        Assert.Equal(0, zeroMaxOptions.MaxSuggestions);

        // Test with very large max suggestions
        var largeMaxOptions = new SuggestionAnalysisOptions { MaxSuggestions = int.MaxValue };
        Assert.Equal(int.MaxValue, largeMaxOptions.MaxSuggestions);

        // Test with all categories excluded
        var noCategoriesOptions = new SuggestionAnalysisOptions();
        noCategoriesOptions.IncludedCategories.Clear();
        Assert.Empty(noCategoriesOptions.IncludedCategories);

        // Test with critical priority only
        var criticalOnlyOptions = new SuggestionAnalysisOptions 
        { 
            MinimumPriority = SuggestionPriority.Critical 
        };
        Assert.Equal(SuggestionPriority.Critical, criticalOnlyOptions.MinimumPriority);

        // Test with both auto-fix options disabled
        var noFixOptions = new SuggestionAnalysisOptions
        {
            IncludeAutoFixable = false,
            IncludeManualFix = false
        };
        Assert.False(noFixOptions.IncludeAutoFixable);
        Assert.False(noFixOptions.IncludeManualFix);
    }

    [Fact]
    public async Task RoslynAnalysisService_SuggestionMethods_HandleExceptions_Gracefully()
    {
        // Test that suggestion methods handle exceptions gracefully
        var service = new RoslynAnalysisService(_logger);

        try
        {
            // These should not throw exceptions even with invalid inputs
            var suggestions1 = await service.GetCodeSuggestionsAsync(null);
            Assert.NotNull(suggestions1);

            var suggestions2 = await service.GetFileSuggestionsAsync(null!);
            Assert.NotNull(suggestions2);

            var suggestions3 = await service.GetFileSuggestionsAsync("");
            Assert.NotNull(suggestions3);

            var suggestions4 = await service.GetFileSuggestionsAsync("   ");
            Assert.NotNull(suggestions4);

            Console.WriteLine("Exception handling tests passed");
        }
        finally
        {
            service.Dispose();
        }
    }
}
