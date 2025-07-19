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

    public void Dispose()
    {
        _workspace?.Dispose();
    }
}
