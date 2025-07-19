using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using MCP.Server.Models;
using System.Collections.Immutable;

namespace MCP.Tests;

/// <summary>
/// Enhanced analysis service that can work with in-memory workspaces for testing
/// </summary>
public class InMemoryAnalysisService : IDisposable
{
    private readonly ILogger<InMemoryAnalysisService> _logger;
    private AdhocWorkspace? _workspace;
    private Solution? _currentSolution;

    public InMemoryAnalysisService(ILogger<InMemoryAnalysisService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Load a pre-created in-memory workspace
    /// </summary>
    public bool LoadWorkspace(AdhocWorkspace workspace)
    {
        try
        {
            _logger.LogInformation("Loading in-memory workspace");
            
            _workspace = workspace;
            _currentSolution = workspace.CurrentSolution;
            
            _logger.LogInformation("Successfully loaded workspace with {ProjectCount} projects", 
                _currentSolution.Projects.Count());
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load workspace");
            return false;
        }
    }

    /// <summary>
    /// Gets all compilation errors and warnings from the loaded workspace
    /// </summary>
    public async Task<List<CompilationError>> GetCompilationErrorsAsync()
    {
        if (_currentSolution == null)
        {
            _logger.LogWarning("No workspace loaded");
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
                        FilePath = location.SourceTree?.FilePath ?? $"InMemory_{project.Name}_{diagnostic.Id}",
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
    public async Task<MCP.Server.Models.SolutionInfo?> GetSolutionInfoAsync()
    {
        if (_currentSolution == null)
        {
            _logger.LogWarning("No workspace loaded");
            return null;
        }

        var solutionInfo = new MCP.Server.Models.SolutionInfo
        {
            Name = "InMemoryTestSolution",
            FilePath = "InMemory://TestSolution",
            Projects = new List<MCP.Server.Models.ProjectInfo>()
        };

        foreach (var project in _currentSolution.Projects)
        {
            try
            {
                var compilation = await project.GetCompilationAsync();
                var diagnostics = compilation?.GetDiagnostics() ?? ImmutableArray<Diagnostic>.Empty;
                
                var errorCount = diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error);
                var warningCount = diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning);

                var projectInfo = new MCP.Server.Models.ProjectInfo
                {
                    Name = project.Name,
                    FilePath = $"InMemory://{project.Name}.csproj",
                    TargetFramework = "net9.0",
                    OutputType = project.CompilationOptions?.OutputKind.ToString() ?? "Unknown",
                    SourceFiles = project.Documents.Select(d => d.Name).ToList(),
                    References = project.MetadataReferences.Select(r => r.Display ?? "Unknown").ToList(),
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
    /// Analyzes a specific document by name for errors and warnings
    /// </summary>
    public async Task<List<CompilationError>> AnalyzeDocumentAsync(string documentName)
    {
        if (_currentSolution == null)
        {
            _logger.LogWarning("No workspace loaded");
            return new List<CompilationError>();
        }

        var errors = new List<CompilationError>();

        // Find the document in the solution
        var document = _currentSolution.Projects
            .SelectMany(p => p.Documents)
            .FirstOrDefault(d => string.Equals(d.Name, documentName, StringComparison.OrdinalIgnoreCase));

        if (document == null)
        {
            _logger.LogWarning("Document not found in workspace: {DocumentName}", documentName);
            return errors;
        }

        try
        {
            var compilation = await document.Project.GetCompilationAsync();
            if (compilation == null) return errors;

            var syntaxTree = await document.GetSyntaxTreeAsync();
            if (syntaxTree == null) return errors;

            // Get diagnostics for this specific document
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
                    FilePath = documentName,
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
            _logger.LogError(ex, "Failed to analyze document: {DocumentName}", documentName);
        }

        return errors;
    }

    /// <summary>
    /// Gets all documents in the workspace
    /// </summary>
    public IEnumerable<string> GetDocumentNames()
    {
        if (_currentSolution == null)
            return Enumerable.Empty<string>();

        return _currentSolution.Projects
            .SelectMany(p => p.Documents)
            .Select(d => d.Name);
    }

    /// <summary>
    /// Gets all project names in the workspace
    /// </summary>
    public IEnumerable<string> GetProjectNames()
    {
        if (_currentSolution == null)
            return Enumerable.Empty<string>();

        return _currentSolution.Projects.Select(p => p.Name);
    }

    /// <summary>
    /// Creates a workspace with specific error scenarios for testing
    /// </summary>
    public static InMemoryAnalysisService CreateWithErrors(ILogger<InMemoryAnalysisService> logger, params string[] errorCodes)
    {
        var service = new InMemoryAnalysisService(logger);
        var workspace = InMemoryProjectGenerator.CreateWorkspaceWithSpecificErrors(errorCodes);
        service.LoadWorkspace(workspace);
        return service;
    }

    /// <summary>
    /// Creates a workspace with the standard test projects
    /// </summary>
    public static InMemoryAnalysisService CreateWithTestProjects(ILogger<InMemoryAnalysisService> logger)
    {
        var service = new InMemoryAnalysisService(logger);
        var workspace = InMemoryProjectGenerator.CreateTestWorkspace();
        service.LoadWorkspace(workspace);
        return service;
    }

    public void Dispose()
    {
        _workspace?.Dispose();
        _workspace = null;
        _currentSolution = null;
    }
}
