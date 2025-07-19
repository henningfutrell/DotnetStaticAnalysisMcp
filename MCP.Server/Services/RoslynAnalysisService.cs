using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;
using MCP.Server.Models;
using System.Collections.Immutable;

namespace MCP.Server.Services;

/// <summary>
/// Service for performing Roslyn-based static analysis on .NET solutions and projects
/// </summary>
public class RoslynAnalysisService : IDisposable
{
    private readonly ILogger<RoslynAnalysisService> _logger;
    private MSBuildWorkspace? _workspace;
    private Solution? _currentSolution;

    public RoslynAnalysisService(ILogger<RoslynAnalysisService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Loads a solution file for analysis
    /// </summary>
    public async Task<bool> LoadSolutionAsync(string solutionPath)
    {
        try
        {
            _logger.LogInformation("Loading solution: {SolutionPath}", solutionPath);
            
            // Dispose existing workspace if any
            _workspace?.Dispose();
            
            // Create new workspace
            _workspace = MSBuildWorkspace.Create();
            
            // Load the solution
            _currentSolution = await _workspace.OpenSolutionAsync(solutionPath);
            
            _logger.LogInformation("Successfully loaded solution with {ProjectCount} projects", 
                _currentSolution.Projects.Count());
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load solution: {SolutionPath}", solutionPath);
            return false;
        }
    }

    /// <summary>
    /// Gets all compilation errors and warnings from the loaded solution
    /// </summary>
    public async Task<List<CompilationError>> GetCompilationErrorsAsync()
    {
        if (_currentSolution == null)
        {
            _logger.LogWarning("No solution loaded");
            return new List<CompilationError>();
        }

        var errors = new List<CompilationError>();

        foreach (var project in _currentSolution.Projects)
        {
            try
            {
                _logger.LogDebug("Analyzing project: {ProjectName}", project.Name);
                
                var compilation = await project.GetCompilationAsync();
                if (compilation == null) continue;

                var diagnostics = compilation.GetDiagnostics();
                
                foreach (var diagnostic in diagnostics)
                {
                    // Skip hidden diagnostics unless they're errors
                    if (diagnostic.Severity == DiagnosticSeverity.Hidden && 
                        diagnostic.Severity != DiagnosticSeverity.Error)
                        continue;

                    var location = diagnostic.Location;
                    var lineSpan = location.GetLineSpan();

                    errors.Add(new CompilationError
                    {
                        Id = diagnostic.Id,
                        Title = diagnostic.Descriptor.Title.ToString(),
                        Message = diagnostic.GetMessage(),
                        Severity = diagnostic.Severity,
                        FilePath = location.SourceTree?.FilePath ?? string.Empty,
                        StartLine = lineSpan.StartLinePosition.Line + 1,
                        StartColumn = lineSpan.StartLinePosition.Character + 1,
                        EndLine = lineSpan.EndLinePosition.Line + 1,
                        EndColumn = lineSpan.EndLinePosition.Character + 1,
                        Category = diagnostic.Descriptor.Category,
                        HelpLink = diagnostic.Descriptor.HelpLinkUri,
                        IsWarningAsError = diagnostic.IsWarningAsError,
                        WarningLevel = diagnostic.WarningLevel,
                        CustomTags = string.Join(", ", diagnostic.Descriptor.CustomTags),
                        ProjectName = project.Name
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze project: {ProjectName}", project.Name);
            }
        }

        _logger.LogInformation("Found {ErrorCount} compilation issues", errors.Count);
        return errors;
    }

    /// <summary>
    /// Gets information about the loaded solution
    /// </summary>
    public async Task<Models.SolutionInfo?> GetSolutionInfoAsync()
    {
        if (_currentSolution == null)
        {
            _logger.LogWarning("No solution loaded");
            return null;
        }

        var solutionInfo = new Models.SolutionInfo
        {
            Name = Path.GetFileNameWithoutExtension(_currentSolution.FilePath) ?? "Unknown",
            FilePath = _currentSolution.FilePath ?? string.Empty,
            Projects = new List<Models.ProjectInfo>()
        };

        foreach (var project in _currentSolution.Projects)
        {
            try
            {
                var compilation = await project.GetCompilationAsync();
                var diagnostics = compilation?.GetDiagnostics() ?? ImmutableArray<Diagnostic>.Empty;
                
                var errorCount = diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error);
                var warningCount = diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning);

                var projectInfo = new Models.ProjectInfo
                {
                    Name = project.Name,
                    FilePath = project.FilePath ?? string.Empty,
                    TargetFramework = project.ParseOptions?.DocumentationMode.ToString() ?? "Unknown",
                    OutputType = project.CompilationOptions?.OutputKind.ToString() ?? "Unknown",
                    SourceFiles = project.Documents.Select(d => d.FilePath ?? string.Empty).ToList(),
                    References = project.MetadataReferences.Select(r => r.Display ?? string.Empty).ToList(),
                    PackageReferences = project.ProjectReferences.Select(r => r.ProjectId.ToString()).ToList(),
                    ErrorCount = errorCount,
                    WarningCount = warningCount,
                    HasCompilationErrors = errorCount > 0
                };

                solutionInfo.Projects.Add(projectInfo);
                solutionInfo.TotalErrors += errorCount;
                solutionInfo.TotalWarnings += warningCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get info for project: {ProjectName}", project.Name);
            }
        }

        solutionInfo.HasCompilationErrors = solutionInfo.TotalErrors > 0;
        return solutionInfo;
    }

    /// <summary>
    /// Analyzes a specific file for errors and warnings
    /// </summary>
    public async Task<List<CompilationError>> AnalyzeFileAsync(string filePath)
    {
        if (_currentSolution == null)
        {
            _logger.LogWarning("No solution loaded");
            return new List<CompilationError>();
        }

        var errors = new List<CompilationError>();

        // Find the document in the solution
        var document = _currentSolution.Projects
            .SelectMany(p => p.Documents)
            .FirstOrDefault(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

        if (document == null)
        {
            _logger.LogWarning("File not found in solution: {FilePath}", filePath);
            return errors;
        }

        try
        {
            var compilation = await document.Project.GetCompilationAsync();
            if (compilation == null) return errors;

            var syntaxTree = await document.GetSyntaxTreeAsync();
            if (syntaxTree == null) return errors;

            // Get diagnostics for this specific file
            var diagnostics = compilation.GetDiagnostics()
                .Where(d => d.Location.SourceTree == syntaxTree);

            foreach (var diagnostic in diagnostics)
            {
                var location = diagnostic.Location;
                var lineSpan = location.GetLineSpan();

                errors.Add(new CompilationError
                {
                    Id = diagnostic.Id,
                    Title = diagnostic.Descriptor.Title.ToString(),
                    Message = diagnostic.GetMessage(),
                    Severity = diagnostic.Severity,
                    FilePath = filePath,
                    StartLine = lineSpan.StartLinePosition.Line + 1,
                    StartColumn = lineSpan.StartLinePosition.Character + 1,
                    EndLine = lineSpan.EndLinePosition.Line + 1,
                    EndColumn = lineSpan.EndLinePosition.Character + 1,
                    Category = diagnostic.Descriptor.Category,
                    HelpLink = diagnostic.Descriptor.HelpLinkUri,
                    IsWarningAsError = diagnostic.IsWarningAsError,
                    WarningLevel = diagnostic.WarningLevel,
                    CustomTags = string.Join(", ", diagnostic.Descriptor.CustomTags),
                    ProjectName = document.Project.Name
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze file: {FilePath}", filePath);
        }

        return errors;
    }

    /// <summary>
    /// Gets code suggestions and analyzer recommendations from the loaded solution
    /// </summary>
    public async Task<List<CodeSuggestion>> GetCodeSuggestionsAsync(SuggestionAnalysisOptions? options = null)
    {
        if (_currentSolution == null)
        {
            _logger.LogWarning("No solution loaded for code suggestions analysis");
            return new List<CodeSuggestion>();
        }

        options ??= new SuggestionAnalysisOptions();
        var suggestions = new List<CodeSuggestion>();

        foreach (var project in _currentSolution.Projects)
        {
            try
            {
                _logger.LogDebug("Analyzing project for suggestions: {ProjectName}", project.Name);

                var compilation = await project.GetCompilationAsync();
                if (compilation == null) continue;

                // Get all diagnostics including analyzer suggestions
                var diagnostics = compilation.GetDiagnostics();

                foreach (var diagnostic in diagnostics)
                {
                    var suggestion = ConvertDiagnosticToSuggestion(diagnostic, project.Name);
                    if (suggestion != null && ShouldIncludeSuggestion(suggestion, options))
                    {
                        suggestions.Add(suggestion);
                    }
                }

                // Also get semantic model diagnostics for each document
                foreach (var document in project.Documents)
                {
                    try
                    {
                        var semanticModel = await document.GetSemanticModelAsync();
                        var syntaxTree = await document.GetSyntaxTreeAsync();

                        if (semanticModel != null && syntaxTree != null)
                        {
                            var semanticDiagnostics = semanticModel.GetDiagnostics();
                            foreach (var diagnostic in semanticDiagnostics)
                            {
                                var suggestion = ConvertDiagnosticToSuggestion(diagnostic, project.Name);
                                if (suggestion != null && ShouldIncludeSuggestion(suggestion, options))
                                {
                                    suggestions.Add(suggestion);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to analyze document for suggestions: {DocumentName}", document.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze project for suggestions: {ProjectName}", project.Name);
            }
        }

        // Remove duplicates and apply limits
        var uniqueSuggestions = suggestions
            .GroupBy(s => new { s.Id, s.FilePath, s.StartLine, s.StartColumn })
            .Select(g => g.First())
            .OrderByDescending(s => s.Priority)
            .ThenBy(s => s.Category)
            .Take(options.MaxSuggestions)
            .ToList();

        _logger.LogInformation("Found {SuggestionCount} code suggestions", uniqueSuggestions.Count);
        return uniqueSuggestions;
    }

    /// <summary>
    /// Gets code suggestions for a specific file
    /// </summary>
    public async Task<List<CodeSuggestion>> GetFileSuggestionsAsync(string filePath, SuggestionAnalysisOptions? options = null)
    {
        if (_currentSolution == null)
        {
            _logger.LogWarning("No solution loaded for file suggestions analysis");
            return new List<CodeSuggestion>();
        }

        options ??= new SuggestionAnalysisOptions();
        var suggestions = new List<CodeSuggestion>();

        // Find the document in the solution
        var document = _currentSolution.Projects
            .SelectMany(p => p.Documents)
            .FirstOrDefault(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

        if (document == null)
        {
            _logger.LogWarning("Document not found in solution: {FilePath}", filePath);
            return suggestions;
        }

        try
        {
            var compilation = await document.Project.GetCompilationAsync();
            var semanticModel = await document.GetSemanticModelAsync();
            var syntaxTree = await document.GetSyntaxTreeAsync();

            if (compilation != null && semanticModel != null && syntaxTree != null)
            {
                // Get compilation diagnostics for this file
                var compilationDiagnostics = compilation.GetDiagnostics()
                    .Where(d => d.Location.SourceTree == syntaxTree);

                foreach (var diagnostic in compilationDiagnostics)
                {
                    var suggestion = ConvertDiagnosticToSuggestion(diagnostic, document.Project.Name);
                    if (suggestion != null && ShouldIncludeSuggestion(suggestion, options))
                    {
                        suggestions.Add(suggestion);
                    }
                }

                // Get semantic model diagnostics
                var semanticDiagnostics = semanticModel.GetDiagnostics();
                foreach (var diagnostic in semanticDiagnostics)
                {
                    var suggestion = ConvertDiagnosticToSuggestion(diagnostic, document.Project.Name);
                    if (suggestion != null && ShouldIncludeSuggestion(suggestion, options))
                    {
                        suggestions.Add(suggestion);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze file for suggestions: {FilePath}", filePath);
        }

        return suggestions.Take(options.MaxSuggestions).ToList();
    }

    /// <summary>
    /// Converts a Roslyn diagnostic to a code suggestion
    /// </summary>
    private CodeSuggestion? ConvertDiagnosticToSuggestion(Diagnostic diagnostic, string projectName)
    {
        // Skip compilation errors - those are handled separately
        if (diagnostic.Severity == DiagnosticSeverity.Error)
            return null;

        // Skip hidden diagnostics unless they have useful suggestions
        if (diagnostic.Severity == DiagnosticSeverity.Hidden &&
            !diagnostic.Descriptor.CustomTags.Contains("Unnecessary"))
            return null;

        var location = diagnostic.Location;
        var lineSpan = location.GetLineSpan();

        var suggestion = new CodeSuggestion
        {
            Id = diagnostic.Id,
            Title = diagnostic.Descriptor.Title.ToString(),
            Description = diagnostic.GetMessage(),
            Category = CategorizeAnalyzerId(diagnostic.Id),
            Priority = MapSeverityToPriority(diagnostic.Severity),
            FilePath = location.SourceTree?.FilePath ?? "Unknown",
            StartLine = lineSpan.StartLinePosition.Line + 1,
            StartColumn = lineSpan.StartLinePosition.Character + 1,
            EndLine = lineSpan.EndLinePosition.Line + 1,
            EndColumn = lineSpan.EndLinePosition.Character + 1,
            OriginalCode = GetCodeFromLocation(location),
            Tags = diagnostic.Descriptor.CustomTags.ToList(),
            HelpLink = diagnostic.Descriptor.HelpLinkUri,
            ProjectName = projectName,
            CanAutoFix = diagnostic.Descriptor.CustomTags.Contains("Unnecessary") ||
                        diagnostic.Id.StartsWith("IDE"),
            Impact = DetermineImpact(diagnostic.Id, diagnostic.Descriptor.CustomTags)
        };

        return suggestion;
    }

    /// <summary>
    /// Determines if a suggestion should be included based on options
    /// </summary>
    private bool ShouldIncludeSuggestion(CodeSuggestion suggestion, SuggestionAnalysisOptions options)
    {
        // Check category filter
        if (!options.IncludedCategories.Contains(suggestion.Category))
            return false;

        // Check priority filter
        if (suggestion.Priority < options.MinimumPriority)
            return false;

        // Check auto-fix filter
        if (suggestion.CanAutoFix && !options.IncludeAutoFixable)
            return false;

        if (!suggestion.CanAutoFix && !options.IncludeManualFix)
            return false;

        // Check included analyzer IDs (if specified)
        if (options.IncludedAnalyzerIds.Count > 0 && !options.IncludedAnalyzerIds.Contains(suggestion.Id))
            return false;

        // Check excluded analyzer IDs
        if (options.ExcludedAnalyzerIds.Contains(suggestion.Id))
            return false;

        return true;
    }

    /// <summary>
    /// Categorizes an analyzer ID into a suggestion category
    /// </summary>
    private SuggestionCategory CategorizeAnalyzerId(string analyzerId)
    {
        return analyzerId switch
        {
            // IDE suggestions (style and modernization)
            var id when id.StartsWith("IDE0") => id switch
            {
                "IDE0001" or "IDE0002" or "IDE0003" or "IDE0004" or "IDE0005" => SuggestionCategory.Style,
                "IDE0007" or "IDE0008" or "IDE0009" or "IDE0010" => SuggestionCategory.Modernization,
                "IDE0011" or "IDE0016" or "IDE0017" or "IDE0018" => SuggestionCategory.Style,
                "IDE0019" or "IDE0020" or "IDE0021" or "IDE0022" => SuggestionCategory.Modernization,
                "IDE0025" or "IDE0026" or "IDE0027" or "IDE0028" => SuggestionCategory.Style,
                "IDE0029" or "IDE0030" or "IDE0031" or "IDE0032" => SuggestionCategory.Modernization,
                "IDE0036" or "IDE0037" or "IDE0039" or "IDE0040" => SuggestionCategory.Style,
                "IDE0041" or "IDE0042" or "IDE0044" or "IDE0045" => SuggestionCategory.Modernization,
                "IDE0046" or "IDE0047" or "IDE0048" or "IDE0049" => SuggestionCategory.Style,
                "IDE0050" or "IDE0051" or "IDE0052" or "IDE0053" => SuggestionCategory.Cleanup,
                "IDE0054" or "IDE0055" or "IDE0056" or "IDE0057" => SuggestionCategory.Modernization,
                "IDE0058" or "IDE0059" or "IDE0060" or "IDE0061" => SuggestionCategory.Cleanup,
                "IDE0062" or "IDE0063" or "IDE0064" or "IDE0065" => SuggestionCategory.Modernization,
                "IDE0066" or "IDE0070" or "IDE0071" or "IDE0072" => SuggestionCategory.Modernization,
                "IDE0073" or "IDE0074" or "IDE0075" or "IDE0076" => SuggestionCategory.Style,
                "IDE0077" or "IDE0078" or "IDE0079" or "IDE0080" => SuggestionCategory.Modernization,
                "IDE0081" or "IDE0082" or "IDE0083" or "IDE0084" => SuggestionCategory.Cleanup,
                "IDE0090" or "IDE0091" or "IDE0092" or "IDE0100" => SuggestionCategory.Modernization,
                "IDE0110" or "IDE0120" or "IDE0130" or "IDE0140" => SuggestionCategory.Modernization,
                "IDE0150" or "IDE0160" or "IDE0170" or "IDE0180" => SuggestionCategory.Modernization,
                "IDE0200" or "IDE0210" or "IDE0220" or "IDE0230" => SuggestionCategory.Modernization,
                "IDE0240" or "IDE0250" or "IDE0260" or "IDE0270" => SuggestionCategory.Modernization,
                "IDE0280" or "IDE0290" or "IDE0300" or "IDE0305" => SuggestionCategory.Modernization,
                _ => SuggestionCategory.Style
            },

            // Code Analysis rules
            var id when id.StartsWith("CA1") => SuggestionCategory.Design,
            var id when id.StartsWith("CA2") => SuggestionCategory.Reliability,
            var id when id.StartsWith("CA3") => SuggestionCategory.Security,
            var id when id.StartsWith("CA5") => SuggestionCategory.Security,

            // Performance rules
            var id when id.StartsWith("CA18") => SuggestionCategory.Performance,

            // Naming rules
            var id when id.StartsWith("CA17") => SuggestionCategory.Naming,

            // Documentation rules
            var id when id.StartsWith("CS1591") => SuggestionCategory.Documentation,

            // Default categorization
            _ => SuggestionCategory.BestPractices
        };
    }

    /// <summary>
    /// Maps diagnostic severity to suggestion priority
    /// </summary>
    private SuggestionPriority MapSeverityToPriority(DiagnosticSeverity severity)
    {
        return severity switch
        {
            DiagnosticSeverity.Error => SuggestionPriority.Critical,
            DiagnosticSeverity.Warning => SuggestionPriority.High,
            DiagnosticSeverity.Info => SuggestionPriority.Medium,
            DiagnosticSeverity.Hidden => SuggestionPriority.Low,
            _ => SuggestionPriority.Low
        };
    }

    /// <summary>
    /// Determines the impact of applying a suggestion
    /// </summary>
    private SuggestionImpact DetermineImpact(string analyzerId, IEnumerable<string> customTags)
    {
        // Performance-related suggestions have higher impact
        if (analyzerId.StartsWith("CA18") || customTags.Contains("Performance"))
            return SuggestionImpact.Significant;

        // Security suggestions are major
        if (analyzerId.StartsWith("CA3") || analyzerId.StartsWith("CA5") || customTags.Contains("Security"))
            return SuggestionImpact.Major;

        // Modernization can be moderate to significant
        if (analyzerId.StartsWith("IDE") && (analyzerId.Contains("90") || analyzerId.Contains("100")))
            return SuggestionImpact.Moderate;

        // Style and cleanup are usually minimal
        if (customTags.Contains("Unnecessary") || analyzerId.StartsWith("IDE0"))
            return SuggestionImpact.Small;

        return SuggestionImpact.Minimal;
    }

    /// <summary>
    /// Extracts code text from a diagnostic location
    /// </summary>
    private string GetCodeFromLocation(Location location)
    {
        try
        {
            if (location.SourceTree != null)
            {
                var sourceText = location.SourceTree.GetText();
                var span = location.SourceSpan;
                return sourceText.ToString(span);
            }
        }
        catch
        {
            // Ignore errors getting source text
        }

        return string.Empty;
    }

    public void Dispose()
    {
        _workspace?.Dispose();
    }
}
