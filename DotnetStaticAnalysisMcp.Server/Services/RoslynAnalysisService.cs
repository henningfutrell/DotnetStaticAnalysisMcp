using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using System.Composition.Hosting;
using DotnetStaticAnalysisMcp.Server.Models;

namespace DotnetStaticAnalysisMcp.Server.Services;

/// <summary>
/// Service for performing Roslyn-based static analysis on .NET solutions and projects
/// </summary>
public class RoslynAnalysisService : IDisposable
{
    private readonly ILogger<RoslynAnalysisService> _logger;
    private readonly TelemetryService? _telemetryService;
    private MSBuildWorkspace? _workspace;
    private Solution? _currentSolution;

    public RoslynAnalysisService(ILogger<RoslynAnalysisService> logger, TelemetryService? telemetryService = null)
    {
        _logger = logger;
        _telemetryService = telemetryService;
        _logger.LogInformation("RoslynAnalysisService initialized with telemetry: {HasTelemetry}", telemetryService != null);
    }

    /// <summary>
    /// Loads a solution file for analysis
    /// </summary>
    public async Task<bool> LoadSolutionAsync(string solutionPath)
    {
        var telemetry = _telemetryService?.StartOperation("LoadSolution");
        var context = new Models.LogContext
        {
            Operation = "LoadSolution",
            SolutionPath = solutionPath
        };

        try
        {
            _logger.LogInformation("Loading solution: {SolutionPath} [CorrelationId: {CorrelationId}]",
                solutionPath, context.CorrelationId);

            // Also write to a simple debug file for visibility
            var debugLog = $"/tmp/mcp-debug-{DateTime.Now:yyyyMMdd}.log";
            File.AppendAllText(debugLog, $"[{DateTime.Now:HH:mm:ss}] Loading solution: {solutionPath}\n");

            // Validate solution file exists
            if (!File.Exists(solutionPath))
            {
                _logger.LogError("Solution file does not exist: {SolutionPath} [CorrelationId: {CorrelationId}]",
                    solutionPath, context.CorrelationId);
                telemetry?.Properties.Add("Error", "FileNotFound");
                _telemetryService?.FailOperation(telemetry!, new FileNotFoundException($"Solution file not found: {solutionPath}"));
                return false;
            }

            // Log environment diagnostics
            var msbuildDiagnostics = _telemetryService?.GetMSBuildDiagnostics();
            _logger.LogInformation("MSBuild Status: Registered={IsRegistered}, Path={MSBuildPath}, Version={Version} [CorrelationId: {CorrelationId}]",
                msbuildDiagnostics?.IsRegistered, msbuildDiagnostics?.MSBuildPath, msbuildDiagnostics?.MSBuildVersion, context.CorrelationId);

            // Dispose existing workspace if any
            _workspace?.Dispose();

            // Create new workspace with C# language support and enhanced logging
            _logger.LogInformation("Creating MSBuild workspace with C# support [CorrelationId: {CorrelationId}]", context.CorrelationId);

            // Create workspace with C# language services
            try
            {
                var services = MefHostServices.Create(MefHostServices.DefaultAssemblies);
                _workspace = MSBuildWorkspace.Create(new Dictionary<string, string>(), services);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to create workspace with MEF services, falling back to default: {Error}", ex.Message);
                _workspace = MSBuildWorkspace.Create();
            }

            _logger.LogInformation("MSBuild workspace created with C# language services [CorrelationId: {CorrelationId}]", context.CorrelationId);

            // Subscribe to workspace events for debugging
            _workspace.WorkspaceFailed += (sender, e) =>
            {
                _logger.LogWarning("Workspace failed: {Diagnostic} [CorrelationId: {CorrelationId}]",
                    e.Diagnostic, context.CorrelationId);
                msbuildDiagnostics?.WorkspaceFailures.Add(e.Diagnostic.ToString());
            };

            _logger.LogInformation("Loading solution with MSBuild workspace [CorrelationId: {CorrelationId}]", context.CorrelationId);

            // Load the solution
            var loadStart = DateTime.UtcNow;
            _currentSolution = await _workspace.OpenSolutionAsync(solutionPath);
            var loadDuration = DateTime.UtcNow - loadStart;

            var projectCount = _currentSolution.Projects.Count();
            _logger.LogInformation("Solution loaded in {LoadDurationMs}ms with {ProjectCount} projects [CorrelationId: {CorrelationId}]",
                loadDuration.TotalMilliseconds, projectCount, context.CorrelationId);

            // Debug logging
            File.AppendAllText(debugLog, $"[{DateTime.Now:HH:mm:ss}] Solution loaded with {projectCount} projects in {loadDuration.TotalMilliseconds}ms\n");

            // Log detailed project information
            var projectDetails = new List<object>();
            foreach (var project in _currentSolution.Projects)
            {
                var projectDetail = new
                {
                    Name = project.Name,
                    FilePath = project.FilePath,
                    DocumentCount = project.Documents.Count(),
                    Language = project.Language
                };
                projectDetails.Add(projectDetail);

                _logger.LogInformation("Found project: {ProjectName} at {ProjectPath} ({DocumentCount} documents, {Language}) [CorrelationId: {CorrelationId}]",
                    project.Name, project.FilePath, project.Documents.Count(), project.Language, context.CorrelationId);
            }

            // Log any workspace diagnostics
            foreach (var diagnostic in _workspace.Diagnostics)
            {
                _logger.LogWarning("Workspace diagnostic: {Diagnostic} [CorrelationId: {CorrelationId}]",
                    diagnostic, context.CorrelationId);
                msbuildDiagnostics?.WorkspaceDiagnostics.Add(diagnostic.ToString());
            }

            // Update telemetry
            var properties = new Dictionary<string, object>
            {
                ["SolutionPath"] = solutionPath,
                ["ProjectCount"] = projectCount,
                ["LoadDurationMs"] = loadDuration.TotalMilliseconds,
                ["ProjectDetails"] = projectDetails,
                ["WorkspaceDiagnosticCount"] = _workspace.Diagnostics.Count(),
                ["MSBuildDiagnostics"] = msbuildDiagnostics ?? new Models.MSBuildDiagnostics()
            };

            _telemetryService?.CompleteOperation(telemetry!, properties);
            _telemetryService?.UpdateSolutionStatus(solutionPath, projectCount, 0, 0, loadDuration);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load solution: {SolutionPath} [CorrelationId: {CorrelationId}] - {ErrorMessage}",
                solutionPath, context.CorrelationId, ex.Message);

            var errorProperties = new Dictionary<string, object>
            {
                ["SolutionPath"] = solutionPath,
                ["ErrorType"] = ex.GetType().Name,
                ["StackTrace"] = ex.StackTrace ?? ""
            };

            _telemetryService?.FailOperation(telemetry!, ex, errorProperties);
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

        _logger.LogInformation("Getting solution info for: {SolutionPath}", _currentSolution.FilePath);
        _logger.LogInformation("Solution has {ProjectCount} projects", _currentSolution.Projects.Count());

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
                _logger.LogInformation("Processing project: {ProjectName} at {ProjectPath}",
                    project.Name, project.FilePath);

                var compilation = await project.GetCompilationAsync();
                var diagnostics = compilation?.GetDiagnostics() ?? ImmutableArray<Diagnostic>.Empty;

                var errorCount = diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error);
                var warningCount = diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning);

                _logger.LogInformation("Project {ProjectName} has {ErrorCount} errors and {WarningCount} warnings",
                    project.Name, errorCount, warningCount);

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

    #region Type Analysis Methods

    /// <summary>
    /// Finds all usages of a specific type across the solution
    /// </summary>
    /// <param name="typeName">The name of the type to find</param>
    /// <param name="options">Analysis options</param>
    /// <returns>Type usage analysis result</returns>
    public async Task<TypeUsageAnalysisResult> FindTypeUsagesAsync(string typeName, TypeUsageAnalysisOptions? options = null)
    {
        var result = new TypeUsageAnalysisResult
        {
            TypeName = typeName
        };

        try
        {
            if (_currentSolution == null)
            {
                result.ErrorMessage = "No solution loaded";
                return result;
            }

            options ??= new TypeUsageAnalysisOptions();

            // Find the type symbol
            var typeSymbol = await FindTypeSymbolAsync(typeName);
            if (typeSymbol == null)
            {
                result.ErrorMessage = $"Type '{typeName}' not found in the solution";
                return result;
            }

            result.FullTypeName = typeSymbol.ToDisplayString();
            result.Namespace = typeSymbol.ContainingNamespace?.ToDisplayString();

            // Find all references to the type
            var references = await SymbolFinder.FindReferencesAsync(typeSymbol, _currentSolution);

            var usages = new List<TypeUsageReference>();
            var projectsWithUsages = new HashSet<string>();

            foreach (var reference in references)
            {
                foreach (var location in reference.Locations)
                {
                    if (location.Document == null) continue;

                    var project = location.Document.Project;
                    if (options.ExcludedProjects.Contains(project.Name)) continue;

                    projectsWithUsages.Add(project.Name);

                    var usage = await CreateTypeUsageReferenceAsync(location, typeSymbol);
                    if (usage != null && ShouldIncludeUsage(usage, options))
                    {
                        usages.Add(usage);
                    }
                }
            }

            // Apply filters and limits
            if (options.IncludedUsageKinds.Any())
            {
                usages = usages.Where(u => options.IncludedUsageKinds.Contains(u.UsageKind)).ToList();
            }

            if (usages.Count > options.MaxResults)
            {
                usages = usages.Take(options.MaxResults).ToList();
            }

            result.Usages = usages;
            result.TotalUsages = usages.Count;
            result.ProjectsWithUsages = projectsWithUsages.ToList();
            result.UsagesByKind = usages.GroupBy(u => u.UsageKind)
                .ToDictionary(g => g.Key, g => g.Count());
            result.Success = true;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding type usages for {TypeName}", typeName);
            result.ErrorMessage = ex.Message;
            return result;
        }
    }

    /// <summary>
    /// Finds all usages of a specific member (method, property, field, event)
    /// </summary>
    /// <param name="typeName">The containing type name</param>
    /// <param name="memberName">The member name</param>
    /// <returns>Member usage analysis result</returns>
    public async Task<MemberUsageAnalysisResult> FindMemberUsagesAsync(string typeName, string memberName)
    {
        var result = new MemberUsageAnalysisResult
        {
            ContainingType = typeName,
            MemberName = memberName
        };

        try
        {
            if (_currentSolution == null)
            {
                result.ErrorMessage = "No solution loaded";
                return result;
            }

            // Find the type symbol
            var typeSymbol = await FindTypeSymbolAsync(typeName);
            if (typeSymbol == null)
            {
                result.ErrorMessage = $"Type '{typeName}' not found";
                return result;
            }

            // Find the member symbol
            var memberSymbol = typeSymbol.GetMembers(memberName).FirstOrDefault();
            if (memberSymbol == null)
            {
                result.ErrorMessage = $"Member '{memberName}' not found in type '{typeName}'";
                return result;
            }

            result.MemberKind = memberSymbol.Kind.ToString();

            // Find all references to the member
            var references = await SymbolFinder.FindReferencesAsync(memberSymbol, _currentSolution);

            var usages = new List<MemberUsageReference>();
            var projectsWithUsages = new HashSet<string>();

            foreach (var reference in references)
            {
                foreach (var location in reference.Locations)
                {
                    if (location.Document == null) continue;

                    var project = location.Document.Project;
                    projectsWithUsages.Add(project.Name);

                    var usage = await CreateMemberUsageReferenceAsync(location, memberSymbol);
                    if (usage != null)
                    {
                        usages.Add(usage);
                    }
                }
            }

            result.Usages = usages;
            result.TotalUsages = usages.Count;
            result.ProjectsWithUsages = projectsWithUsages.ToList();
            result.UsagesByKind = usages.GroupBy(u => u.UsageKind)
                .ToDictionary(g => g.Key, g => g.Count());
            result.Success = true;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding member usages for {TypeName}.{MemberName}", typeName, memberName);
            result.ErrorMessage = ex.Message;
            return result;
        }
    }

    /// <summary>
    /// Finds all usages of a namespace
    /// </summary>
    /// <param name="namespaceName">The namespace to find</param>
    /// <returns>Type usage analysis result</returns>
    public async Task<TypeUsageAnalysisResult> FindNamespaceUsagesAsync(string namespaceName)
    {
        var result = new TypeUsageAnalysisResult
        {
            TypeName = namespaceName,
            FullTypeName = namespaceName
        };

        try
        {
            if (_currentSolution == null)
            {
                result.ErrorMessage = "No solution loaded";
                return result;
            }

            var usages = new List<TypeUsageReference>();
            var projectsWithUsages = new HashSet<string>();

            foreach (var project in _currentSolution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    var syntaxTree = await document.GetSyntaxTreeAsync();
                    if (syntaxTree == null) continue;

                    var root = await syntaxTree.GetRootAsync();
                    var usingDirectives = root.DescendantNodes().OfType<UsingDirectiveSyntax>();

                    foreach (var usingDirective in usingDirectives)
                    {
                        var nameText = usingDirective.Name?.ToString();
                        if (nameText == namespaceName || nameText?.StartsWith(namespaceName + ".") == true)
                        {
                            projectsWithUsages.Add(project.Name);

                            var lineSpan = usingDirective.GetLocation().GetLineSpan();
                            var usage = new TypeUsageReference
                            {
                                FilePath = document.FilePath ?? document.Name,
                                ProjectName = project.Name,
                                StartLine = lineSpan.StartLinePosition.Line + 1,
                                StartColumn = lineSpan.StartLinePosition.Character + 1,
                                EndLine = lineSpan.EndLinePosition.Line + 1,
                                EndColumn = lineSpan.EndLinePosition.Character + 1,
                                UsageKind = TypeUsageKind.UsingDirective,
                                Context = "Using directive",
                                CodeSnippet = usingDirective.ToString()
                            };

                            usages.Add(usage);
                        }
                    }
                }
            }

            result.Usages = usages;
            result.TotalUsages = usages.Count;
            result.ProjectsWithUsages = projectsWithUsages.ToList();
            result.UsagesByKind = usages.GroupBy(u => u.UsageKind)
                .ToDictionary(g => g.Key, g => g.Count());
            result.Success = true;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding namespace usages for {NamespaceName}", namespaceName);
            result.ErrorMessage = ex.Message;
            return result;
        }
    }

    /// <summary>
    /// Gets all types that a specific type depends on
    /// </summary>
    /// <param name="typeName">The type to analyze</param>
    /// <returns>Dependency analysis result</returns>
    public async Task<DependencyAnalysisResult> GetTypeDependenciesAsync(string typeName)
    {
        var result = new DependencyAnalysisResult
        {
            AnalyzedType = typeName
        };

        try
        {
            if (_currentSolution == null)
            {
                result.ErrorMessage = "No solution loaded";
                return result;
            }

            var typeSymbol = await FindTypeSymbolAsync(typeName);
            if (typeSymbol == null)
            {
                result.ErrorMessage = $"Type '{typeName}' not found";
                return result;
            }

            var dependencies = new List<TypeDependency>();

            // Analyze base types
            if (typeSymbol.BaseType != null && !typeSymbol.BaseType.SpecialType.HasFlag(SpecialType.System_Object))
            {
                dependencies.Add(new TypeDependency
                {
                    DependentType = typeName,
                    DependencyType = typeSymbol.BaseType.Name,
                    Kind = DependencyKind.Inheritance,
                    Context = "Base class"
                });
            }

            // Analyze implemented interfaces
            foreach (var interfaceType in typeSymbol.Interfaces)
            {
                dependencies.Add(new TypeDependency
                {
                    DependentType = typeName,
                    DependencyType = interfaceType.Name,
                    Kind = DependencyKind.Implementation,
                    Context = "Implemented interface"
                });
            }

            // Analyze members for composition/aggregation
            foreach (var member in typeSymbol.GetMembers())
            {
                if (member is IPropertySymbol property)
                {
                    var propertyTypeName = property.Type.Name;
                    if (!IsBuiltInType(property.Type))
                    {
                        dependencies.Add(new TypeDependency
                        {
                            DependentType = typeName,
                            DependencyType = propertyTypeName,
                            Kind = DependencyKind.Composition,
                            Context = $"Property: {property.Name}"
                        });
                    }
                }
                else if (member is IFieldSymbol field)
                {
                    var fieldTypeName = field.Type.Name;
                    if (!IsBuiltInType(field.Type))
                    {
                        dependencies.Add(new TypeDependency
                        {
                            DependentType = typeName,
                            DependencyType = fieldTypeName,
                            Kind = DependencyKind.Composition,
                            Context = $"Field: {field.Name}"
                        });
                    }
                }
            }

            result.Dependencies = dependencies;
            result.TotalDependencies = dependencies.Count;
            result.Success = true;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing dependencies for {TypeName}", typeName);
            result.ErrorMessage = ex.Message;
            return result;
        }
    }

    /// <summary>
    /// Gets all types that depend on a specific type
    /// </summary>
    /// <param name="typeName">The type to analyze</param>
    /// <returns>Dependency analysis result</returns>
    public async Task<DependencyAnalysisResult> GetTypeDependentsAsync(string typeName)
    {
        var result = new DependencyAnalysisResult
        {
            AnalyzedType = typeName
        };

        try
        {
            if (_currentSolution == null)
            {
                result.ErrorMessage = "No solution loaded";
                return result;
            }

            var typeSymbol = await FindTypeSymbolAsync(typeName);
            if (typeSymbol == null)
            {
                result.ErrorMessage = $"Type '{typeName}' not found";
                return result;
            }

            var dependents = new List<TypeDependency>();

            // Find all types that reference this type
            var references = await SymbolFinder.FindReferencesAsync(typeSymbol, _currentSolution);

            foreach (var reference in references)
            {
                foreach (var location in reference.Locations)
                {
                    if (location.Document == null) continue;

                    var semanticModel = await location.Document.GetSemanticModelAsync();
                    if (semanticModel == null) continue;

                    var syntaxTree = await location.Document.GetSyntaxTreeAsync();
                    if (syntaxTree == null) continue;

                    var root = await syntaxTree.GetRootAsync();
                    var node = root.FindNode(location.Location.SourceSpan);

                    // Find the containing type
                    var containingTypeNode = node.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
                    if (containingTypeNode != null)
                    {
                        var containingTypeSymbol = semanticModel.GetDeclaredSymbol(containingTypeNode);
                        if (containingTypeSymbol != null && containingTypeSymbol.Name != typeName)
                        {
                            var dependencyKind = DetermineDependencyKind(node, typeSymbol);

                            dependents.Add(new TypeDependency
                            {
                                DependentType = containingTypeSymbol.Name,
                                DependencyType = typeName,
                                Kind = dependencyKind,
                                Context = GetUsageContext(node)
                            });
                        }
                    }
                }
            }

            // Remove duplicates
            dependents = dependents
                .GroupBy(d => new { d.DependentType, d.DependencyType, d.Kind })
                .Select(g => g.First())
                .ToList();

            result.Dependents = dependents;
            result.TotalDependents = dependents.Count;
            result.Success = true;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing dependents for {TypeName}", typeName);
            result.ErrorMessage = ex.Message;
            return result;
        }
    }

    /// <summary>
    /// Analyzes the potential impact of changing a type
    /// </summary>
    /// <param name="typeName">The type to analyze</param>
    /// <returns>Impact analysis result</returns>
    public async Task<ImpactAnalysisResult> AnalyzeImpactScopeAsync(string typeName)
    {
        var result = new ImpactAnalysisResult
        {
            AnalyzedItem = typeName
        };

        try
        {
            var usageResult = await FindTypeUsagesAsync(typeName);
            if (!usageResult.Success)
            {
                result.ErrorMessage = usageResult.ErrorMessage;
                return result;
            }

            result.AffectedUsages = usageResult.Usages;
            result.AffectedProjects = usageResult.ProjectsWithUsages;

            // Determine impact scope
            if (usageResult.TotalUsages == 0)
            {
                result.Scope = ImpactScope.None;
            }
            else if (usageResult.ProjectsWithUsages.Count == 1)
            {
                var fileCount = usageResult.Usages.Select(u => u.FilePath).Distinct().Count();
                result.Scope = fileCount == 1 ? ImpactScope.SameFile : ImpactScope.SameProject;
            }
            else
            {
                result.Scope = usageResult.ProjectsWithUsages.Count > 1 ?
                    ImpactScope.MultipleProjets : ImpactScope.EntireSolution;
            }

            // Generate recommendations
            result.Recommendations = GenerateImpactRecommendations(usageResult);

            // Identify potential breaking changes
            result.PotentialBreakingChanges = IdentifyBreakingChanges(usageResult);

            result.Success = true;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing impact scope for {TypeName}", typeName);
            result.ErrorMessage = ex.Message;
            return result;
        }
    }

    /// <summary>
    /// Validates if renaming a type would be safe
    /// </summary>
    /// <param name="currentName">Current type name</param>
    /// <param name="proposedName">Proposed new name</param>
    /// <returns>Rename safety result</returns>
    public async Task<RenameSafetyResult> ValidateRenameSafetyAsync(string currentName, string proposedName)
    {
        var result = new RenameSafetyResult
        {
            CurrentName = currentName,
            ProposedName = proposedName
        };

        try
        {
            if (_currentSolution == null)
            {
                result.ErrorMessage = "No solution loaded";
                return result;
            }

            // Check if proposed name already exists
            var existingType = await FindTypeSymbolAsync(proposedName);
            if (existingType != null)
            {
                result.IsSafeToRename = false;
                result.Conflicts.Add($"Type '{proposedName}' already exists");
            }

            // Get all usages that would be affected
            var usageResult = await FindTypeUsagesAsync(currentName);
            if (usageResult.Success)
            {
                result.AffectedUsages = usageResult.Usages;

                // Check for potential naming conflicts in each usage location
                foreach (var usage in usageResult.Usages)
                {
                    // Additional conflict checking logic would go here
                    // For now, we'll assume it's safe if no type conflicts exist
                }
            }

            if (result.Conflicts.Count == 0)
            {
                result.IsSafeToRename = true;
            }

            result.Success = true;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating rename safety for {CurrentName} to {ProposedName}",
                currentName, proposedName);
            result.ErrorMessage = ex.Message;
            return result;
        }
    }

    /// <summary>
    /// Previews the impact of renaming a type
    /// </summary>
    /// <param name="currentName">Current type name</param>
    /// <param name="proposedName">Proposed new name</param>
    /// <returns>Impact analysis result</returns>
    public async Task<ImpactAnalysisResult> PreviewRenameImpactAsync(string currentName, string proposedName)
    {
        var result = new ImpactAnalysisResult
        {
            AnalyzedItem = $"{currentName} -> {proposedName}"
        };

        try
        {
            var usageResult = await FindTypeUsagesAsync(currentName);
            if (!usageResult.Success)
            {
                result.ErrorMessage = usageResult.ErrorMessage;
                return result;
            }

            result.AffectedUsages = usageResult.Usages;
            result.AffectedProjects = usageResult.ProjectsWithUsages;

            // Determine scope
            result.Scope = usageResult.ProjectsWithUsages.Count > 1 ?
                ImpactScope.MultipleProjets : ImpactScope.SameProject;

            // Generate specific recommendations for rename
            result.Recommendations.Add($"Rename will affect {usageResult.TotalUsages} locations across {usageResult.ProjectsWithUsages.Count} projects");
            result.Recommendations.Add("Consider using IDE refactoring tools for safe rename operations");

            if (usageResult.ProjectsWithUsages.Count > 1)
            {
                result.Recommendations.Add("Ensure all projects are rebuilt after rename");
            }

            result.Success = true;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing rename impact for {CurrentName} to {ProposedName}",
                currentName, proposedName);
            result.ErrorMessage = ex.Message;
            return result;
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Finds a type symbol by name in the current solution
    /// </summary>
    private async Task<INamedTypeSymbol?> FindTypeSymbolAsync(string typeName)
    {
        if (_currentSolution == null) return null;

        foreach (var project in _currentSolution.Projects)
        {
            var compilation = await project.GetCompilationAsync();
            if (compilation == null) continue;

            // Try exact match first
            var typeSymbol = compilation.GetTypeByMetadataName(typeName);
            if (typeSymbol != null) return typeSymbol;

            // Try searching by name in all namespaces
            var allTypes = compilation.GetSymbolsWithName(name => name == typeName, SymbolFilter.Type);
            var namedType = allTypes.OfType<INamedTypeSymbol>().FirstOrDefault();
            if (namedType != null) return namedType;
        }

        return null;
    }

    /// <summary>
    /// Creates a type usage reference from a reference location
    /// </summary>
    private async Task<TypeUsageReference?> CreateTypeUsageReferenceAsync(Microsoft.CodeAnalysis.FindSymbols.ReferenceLocation location, ISymbol typeSymbol)
    {
        try
        {
            if (location.Document == null) return null;

            var syntaxTree = await location.Document.GetSyntaxTreeAsync();
            if (syntaxTree == null) return null;

            var root = await syntaxTree.GetRootAsync();
            var node = root.FindNode(location.Location.SourceSpan);

            var lineSpan = location.Location.GetLineSpan();
            var usageKind = DetermineTypeUsageKind(node, typeSymbol);
            var context = GetUsageContext(node);
            var codeSnippet = GetSourceTextSnippet(syntaxTree, location.Location.SourceSpan);

            return new TypeUsageReference
            {
                FilePath = location.Document.FilePath ?? location.Document.Name,
                ProjectName = location.Document.Project.Name,
                StartLine = lineSpan.StartLinePosition.Line + 1,
                StartColumn = lineSpan.StartLinePosition.Character + 1,
                EndLine = lineSpan.EndLinePosition.Line + 1,
                EndColumn = lineSpan.EndLinePosition.Character + 1,
                UsageKind = usageKind,
                Context = context,
                CodeSnippet = codeSnippet,
                ContainingMember = GetContainingMember(node),
                ContainingType = GetContainingType(node)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error creating type usage reference");
            return null;
        }
    }

    /// <summary>
    /// Creates a member usage reference from a reference location
    /// </summary>
    private async Task<MemberUsageReference?> CreateMemberUsageReferenceAsync(Microsoft.CodeAnalysis.FindSymbols.ReferenceLocation location, ISymbol memberSymbol)
    {
        try
        {
            if (location.Document == null) return null;

            var syntaxTree = await location.Document.GetSyntaxTreeAsync();
            if (syntaxTree == null) return null;

            var root = await syntaxTree.GetRootAsync();
            var node = root.FindNode(location.Location.SourceSpan);

            var lineSpan = location.Location.GetLineSpan();
            var usageKind = DetermineMemberUsageKind(node, memberSymbol);
            var context = GetUsageContext(node);
            var codeSnippet = GetSourceTextSnippet(syntaxTree, location.Location.SourceSpan);

            return new MemberUsageReference
            {
                FilePath = location.Document.FilePath ?? location.Document.Name,
                ProjectName = location.Document.Project.Name,
                StartLine = lineSpan.StartLinePosition.Line + 1,
                StartColumn = lineSpan.StartLinePosition.Character + 1,
                EndLine = lineSpan.EndLinePosition.Line + 1,
                EndColumn = lineSpan.EndLinePosition.Character + 1,
                UsageKind = usageKind,
                Context = context,
                CodeSnippet = codeSnippet,
                ContainingMember = GetContainingMember(node),
                ContainingType = GetContainingType(node)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error creating member usage reference");
            return null;
        }
    }

    /// <summary>
    /// Determines the type of type usage based on syntax node
    /// </summary>
    private TypeUsageKind DetermineTypeUsageKind(SyntaxNode node, ISymbol typeSymbol)
    {
        return node switch
        {
            ClassDeclarationSyntax => TypeUsageKind.Declaration,
            InterfaceDeclarationSyntax => TypeUsageKind.Declaration,
            StructDeclarationSyntax => TypeUsageKind.Declaration,
            EnumDeclarationSyntax => TypeUsageKind.Declaration,
            ObjectCreationExpressionSyntax => TypeUsageKind.Instantiation,
            VariableDeclarationSyntax => TypeUsageKind.LocalVariable,
            PropertyDeclarationSyntax => TypeUsageKind.PropertyType,
            FieldDeclarationSyntax => TypeUsageKind.FieldType,
            ParameterSyntax => TypeUsageKind.MethodParameter,
            AttributeSyntax => TypeUsageKind.AttributeUsage,
            BaseListSyntax => DetermineBaseListUsageKind(node, typeSymbol),
            CastExpressionSyntax => TypeUsageKind.CastOperation,
            TypeOfExpressionSyntax => TypeUsageKind.TypeOfExpression,
            IsPatternExpressionSyntax => TypeUsageKind.IsExpression,
            BinaryExpressionSyntax when node.ToString().Contains(" as ") => TypeUsageKind.AsExpression,
            UsingDirectiveSyntax => TypeUsageKind.UsingDirective,
            _ => TypeUsageKind.FullyQualifiedReference
        };
    }

    /// <summary>
    /// Determines the type of member usage based on syntax node
    /// </summary>
    private MemberUsageKind DetermineMemberUsageKind(SyntaxNode node, ISymbol memberSymbol)
    {
        return node switch
        {
            InvocationExpressionSyntax => MemberUsageKind.MethodCall,
            MemberAccessExpressionSyntax when IsPropertyAccess(node, memberSymbol) => MemberUsageKind.PropertyAccess,
            AssignmentExpressionSyntax when IsPropertySet(node, memberSymbol) => MemberUsageKind.PropertySet,
            MemberAccessExpressionSyntax when IsFieldAccess(node, memberSymbol) => MemberUsageKind.FieldAccess,
            AssignmentExpressionSyntax when IsFieldSet(node, memberSymbol) => MemberUsageKind.FieldSet,
            _ => MemberUsageKind.MethodCall // Default fallback
        };
    }

    /// <summary>
    /// Additional helper methods for type analysis
    /// </summary>
    private TypeUsageKind DetermineBaseListUsageKind(SyntaxNode node, ISymbol typeSymbol)
    {
        // Simplified logic - in a real implementation, you'd check if it's inheritance vs interface implementation
        if (typeSymbol is INamedTypeSymbol namedType)
        {
            return namedType.TypeKind == TypeKind.Interface ? TypeUsageKind.ImplementedInterface : TypeUsageKind.BaseClass;
        }
        return TypeUsageKind.BaseClass;
    }

    private bool IsPropertyAccess(SyntaxNode node, ISymbol memberSymbol)
    {
        return memberSymbol.Kind == SymbolKind.Property && node.Parent is not AssignmentExpressionSyntax;
    }

    private bool IsPropertySet(SyntaxNode node, ISymbol memberSymbol)
    {
        return memberSymbol.Kind == SymbolKind.Property && node.Parent is AssignmentExpressionSyntax assignment &&
               assignment.Left.Contains(node);
    }

    private bool IsFieldAccess(SyntaxNode node, ISymbol memberSymbol)
    {
        return memberSymbol.Kind == SymbolKind.Field && node.Parent is not AssignmentExpressionSyntax;
    }

    private bool IsFieldSet(SyntaxNode node, ISymbol memberSymbol)
    {
        return memberSymbol.Kind == SymbolKind.Field && node.Parent is AssignmentExpressionSyntax assignment &&
               assignment.Left.Contains(node);
    }

    private string GetUsageContext(SyntaxNode node)
    {
        var parent = node.Parent;
        return parent switch
        {
            MethodDeclarationSyntax method => $"Method: {method.Identifier.ValueText}",
            PropertyDeclarationSyntax property => $"Property: {property.Identifier.ValueText}",
            FieldDeclarationSyntax => "Field declaration",
            ClassDeclarationSyntax classDecl => $"Class: {classDecl.Identifier.ValueText}",
            InterfaceDeclarationSyntax interfaceDecl => $"Interface: {interfaceDecl.Identifier.ValueText}",
            _ => parent?.GetType().Name ?? "Unknown context"
        };
    }

    private string? GetContainingMember(SyntaxNode node)
    {
        var memberNode = node.Ancestors().FirstOrDefault(n =>
            n is MethodDeclarationSyntax ||
            n is PropertyDeclarationSyntax ||
            n is FieldDeclarationSyntax ||
            n is ConstructorDeclarationSyntax);

        return memberNode switch
        {
            MethodDeclarationSyntax method => method.Identifier.ValueText,
            PropertyDeclarationSyntax property => property.Identifier.ValueText,
            ConstructorDeclarationSyntax => ".ctor",
            _ => null
        };
    }

    private string? GetContainingType(SyntaxNode node)
    {
        var typeNode = node.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
        return typeNode?.Identifier.ValueText;
    }

    private bool ShouldIncludeUsage(TypeUsageReference usage, TypeUsageAnalysisOptions options)
    {
        if (!options.IncludeDocumentation && usage.IsInDocumentation)
            return false;

        if (options.IncludedUsageKinds.Any() && !options.IncludedUsageKinds.Contains(usage.UsageKind))
            return false;

        return true;
    }

    private bool IsBuiltInType(ITypeSymbol type)
    {
        return type.SpecialType != SpecialType.None ||
               type.TypeKind == TypeKind.Enum ||
               type.ContainingNamespace?.ToDisplayString().StartsWith("System") == true;
    }

    private DependencyKind DetermineDependencyKind(SyntaxNode node, ISymbol typeSymbol)
    {
        return node.Parent switch
        {
            BaseListSyntax => (typeSymbol is INamedTypeSymbol namedType && namedType.TypeKind == TypeKind.Interface) ?
                DependencyKind.Implementation : DependencyKind.Inheritance,
            PropertyDeclarationSyntax => DependencyKind.Composition,
            FieldDeclarationSyntax => DependencyKind.Composition,
            ParameterSyntax => DependencyKind.Usage,
            AttributeSyntax => DependencyKind.Attribute,
            _ => DependencyKind.Usage
        };
    }

    private List<string> GenerateImpactRecommendations(TypeUsageAnalysisResult usageResult)
    {
        var recommendations = new List<string>();

        if (usageResult.TotalUsages > 50)
        {
            recommendations.Add("High usage count detected - consider careful planning before making changes");
        }

        if (usageResult.ProjectsWithUsages.Count > 1)
        {
            recommendations.Add("Type is used across multiple projects - coordinate changes carefully");
        }

        if (usageResult.UsagesByKind.ContainsKey(TypeUsageKind.BaseClass))
        {
            recommendations.Add("Type is used as base class - changes may affect inheritance hierarchy");
        }

        if (usageResult.UsagesByKind.ContainsKey(TypeUsageKind.ImplementedInterface))
        {
            recommendations.Add("Type is implemented as interface - changes may break implementations");
        }

        return recommendations;
    }

    private List<string> IdentifyBreakingChanges(TypeUsageAnalysisResult usageResult)
    {
        var breakingChanges = new List<string>();

        if (usageResult.UsagesByKind.ContainsKey(TypeUsageKind.BaseClass))
        {
            breakingChanges.Add("Changing base class structure may break derived classes");
        }

        if (usageResult.UsagesByKind.ContainsKey(TypeUsageKind.ImplementedInterface))
        {
            breakingChanges.Add("Changing interface may break implementing classes");
        }

        if (usageResult.UsagesByKind.ContainsKey(TypeUsageKind.MethodParameter))
        {
            breakingChanges.Add("Type is used in method signatures - changes may break callers");
        }

        return breakingChanges;
    }

    /// <summary>
    /// Gets a code snippet from the syntax tree at the specified span
    /// </summary>
    private string GetSourceTextSnippet(SyntaxTree syntaxTree, Microsoft.CodeAnalysis.Text.TextSpan span)
    {
        try
        {
            var sourceText = syntaxTree.GetText();
            return sourceText.GetSubText(span).ToString().Trim();
        }
        catch
        {
            return string.Empty;
        }
    }

    #endregion

    public void Dispose()
    {
        _workspace?.Dispose();
    }
}
