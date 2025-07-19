namespace MCP.Server.Models;

/// <summary>
/// Represents information about a project in the solution
/// </summary>
public class ProjectInfo
{
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string TargetFramework { get; set; } = string.Empty;
    public string OutputType { get; set; } = string.Empty;
    public List<string> SourceFiles { get; set; } = new();
    public List<string> References { get; set; } = new();
    public List<string> PackageReferences { get; set; } = new();
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public bool HasCompilationErrors { get; set; }
}

/// <summary>
/// Represents information about the entire solution
/// </summary>
public class SolutionInfo
{
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public List<ProjectInfo> Projects { get; set; } = new();
    public int TotalErrors { get; set; }
    public int TotalWarnings { get; set; }
    public bool HasCompilationErrors { get; set; }
}
