using Microsoft.CodeAnalysis;

namespace DotnetStaticAnalysisMcp.Server.Models;

/// <summary>
/// Represents a compilation error or warning from Roslyn analysis
/// </summary>
public class CompilationError
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DiagnosticSeverity Severity { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public int StartLine { get; set; }
    public int StartColumn { get; set; }
    public int EndLine { get; set; }
    public int EndColumn { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? HelpLink { get; set; }
    public bool IsWarningAsError { get; set; }
    public int WarningLevel { get; set; }
    public string? CustomTags { get; set; }
    public string ProjectName { get; set; } = string.Empty;
}
