using Microsoft.CodeAnalysis;

namespace MCP.Server.Models;

/// <summary>
/// Represents a code improvement suggestion from Roslyn analyzers
/// </summary>
public class CodeSuggestion
{
    /// <summary>
    /// Unique identifier for the suggestion (e.g., "IDE0001", "CA1822")
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Short title describing the suggestion
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the suggestion
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Category of the suggestion
    /// </summary>
    public SuggestionCategory Category { get; set; }

    /// <summary>
    /// Priority/severity of the suggestion
    /// </summary>
    public SuggestionPriority Priority { get; set; }

    /// <summary>
    /// File path where the suggestion applies
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Starting line number (1-based)
    /// </summary>
    public int StartLine { get; set; }

    /// <summary>
    /// Starting column number (1-based)
    /// </summary>
    public int StartColumn { get; set; }

    /// <summary>
    /// Ending line number (1-based)
    /// </summary>
    public int EndLine { get; set; }

    /// <summary>
    /// Ending column number (1-based)
    /// </summary>
    public int EndColumn { get; set; }

    /// <summary>
    /// The original code that could be improved
    /// </summary>
    public string OriginalCode { get; set; } = string.Empty;

    /// <summary>
    /// Suggested replacement code (if available)
    /// </summary>
    public string? SuggestedCode { get; set; }

    /// <summary>
    /// Tags associated with this suggestion
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// URL to documentation about this suggestion
    /// </summary>
    public string? HelpLink { get; set; }

    /// <summary>
    /// Name of the project containing this suggestion
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// Whether this suggestion can be automatically fixed
    /// </summary>
    public bool CanAutoFix { get; set; }

    /// <summary>
    /// Estimated impact of applying this suggestion
    /// </summary>
    public SuggestionImpact Impact { get; set; }
}

/// <summary>
/// Categories of code suggestions
/// </summary>
public enum SuggestionCategory
{
    /// <summary>
    /// General code style improvements
    /// </summary>
    Style,

    /// <summary>
    /// Performance optimizations
    /// </summary>
    Performance,

    /// <summary>
    /// Code modernization (newer language features)
    /// </summary>
    Modernization,

    /// <summary>
    /// Best practices and maintainability
    /// </summary>
    BestPractices,

    /// <summary>
    /// Security improvements
    /// </summary>
    Security,

    /// <summary>
    /// Reliability and correctness
    /// </summary>
    Reliability,

    /// <summary>
    /// Accessibility improvements
    /// </summary>
    Accessibility,

    /// <summary>
    /// Design and architecture
    /// </summary>
    Design,

    /// <summary>
    /// Naming conventions
    /// </summary>
    Naming,

    /// <summary>
    /// Documentation improvements
    /// </summary>
    Documentation,

    /// <summary>
    /// Unused code removal
    /// </summary>
    Cleanup
}

/// <summary>
/// Priority levels for suggestions
/// </summary>
public enum SuggestionPriority
{
    /// <summary>
    /// Low priority suggestion
    /// </summary>
    Low,

    /// <summary>
    /// Medium priority suggestion
    /// </summary>
    Medium,

    /// <summary>
    /// High priority suggestion
    /// </summary>
    High,

    /// <summary>
    /// Critical suggestion that should be addressed
    /// </summary>
    Critical
}

/// <summary>
/// Expected impact of applying a suggestion
/// </summary>
public enum SuggestionImpact
{
    /// <summary>
    /// Minimal impact, mostly cosmetic
    /// </summary>
    Minimal,

    /// <summary>
    /// Small improvement in readability or maintainability
    /// </summary>
    Small,

    /// <summary>
    /// Moderate improvement in code quality
    /// </summary>
    Moderate,

    /// <summary>
    /// Significant improvement in performance or correctness
    /// </summary>
    Significant,

    /// <summary>
    /// Major improvement that affects application behavior
    /// </summary>
    Major
}

/// <summary>
/// Configuration for code suggestion analysis
/// </summary>
public class SuggestionAnalysisOptions
{
    /// <summary>
    /// Categories of suggestions to include
    /// </summary>
    public HashSet<SuggestionCategory> IncludedCategories { get; set; } = new()
    {
        SuggestionCategory.Style,
        SuggestionCategory.Performance,
        SuggestionCategory.Modernization,
        SuggestionCategory.BestPractices,
        SuggestionCategory.Security,
        SuggestionCategory.Reliability
    };

    /// <summary>
    /// Minimum priority level to include
    /// </summary>
    public SuggestionPriority MinimumPriority { get; set; } = SuggestionPriority.Low;

    /// <summary>
    /// Maximum number of suggestions to return
    /// </summary>
    public int MaxSuggestions { get; set; } = 100;

    /// <summary>
    /// Whether to include suggestions that can be auto-fixed
    /// </summary>
    public bool IncludeAutoFixable { get; set; } = true;

    /// <summary>
    /// Whether to include suggestions that require manual intervention
    /// </summary>
    public bool IncludeManualFix { get; set; } = true;

    /// <summary>
    /// Specific analyzer IDs to include (empty means all)
    /// </summary>
    public HashSet<string> IncludedAnalyzerIds { get; set; } = new();

    /// <summary>
    /// Specific analyzer IDs to exclude
    /// </summary>
    public HashSet<string> ExcludedAnalyzerIds { get; set; } = new();
}
