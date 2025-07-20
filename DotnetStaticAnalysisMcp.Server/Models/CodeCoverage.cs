using System.Text.Json.Serialization;

namespace DotnetStaticAnalysisMcp.Server.Models;

/// <summary>
/// Represents code coverage analysis results
/// </summary>
public class CoverageAnalysisResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime AnalysisTime { get; set; }
    public TimeSpan ExecutionDuration { get; set; }
    public CoverageSummary Summary { get; set; } = new();
    public List<ProjectCoverage> Projects { get; set; } = new();
    public TestExecutionSummary TestResults { get; set; } = new();
}

/// <summary>
/// Overall coverage summary statistics
/// </summary>
public class CoverageSummary
{
    public double LinesCoveredPercentage { get; set; }
    public double BranchesCoveredPercentage { get; set; }
    public double MethodsCoveredPercentage { get; set; }
    public double ClassesCoveredPercentage { get; set; }
    
    public int TotalLines { get; set; }
    public int CoveredLines { get; set; }
    public int UncoveredLines { get; set; }
    
    public int TotalBranches { get; set; }
    public int CoveredBranches { get; set; }
    public int UncoveredBranches { get; set; }
    
    public int TotalMethods { get; set; }
    public int CoveredMethods { get; set; }
    public int UncoveredMethods { get; set; }
    
    public int TotalClasses { get; set; }
    public int CoveredClasses { get; set; }
    public int UncoveredClasses { get; set; }
}

/// <summary>
/// Coverage information for a specific project
/// </summary>
public class ProjectCoverage
{
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectPath { get; set; } = string.Empty;
    public CoverageSummary Summary { get; set; } = new();
    public List<FileCoverage> Files { get; set; } = new();
    public List<ClassCoverage> Classes { get; set; } = new();
}

/// <summary>
/// Coverage information for a specific file
/// </summary>
public class FileCoverage
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public CoverageSummary Summary { get; set; } = new();
    public List<LineCoverage> Lines { get; set; } = new();
    public List<MethodCoverage> Methods { get; set; } = new();
}

/// <summary>
/// Coverage information for a specific class
/// </summary>
public class ClassCoverage
{
    public string ClassName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public CoverageSummary Summary { get; set; } = new();
    public List<MethodCoverage> Methods { get; set; } = new();
}

/// <summary>
/// Coverage information for a specific method
/// </summary>
public class MethodCoverage
{
    public string MethodName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public CoverageSummary Summary { get; set; } = new();
    public List<LineCoverage> Lines { get; set; } = new();
    public List<BranchCoverage> Branches { get; set; } = new();
    public bool IsFullyCovered => Summary.LinesCoveredPercentage >= 100.0;
    public bool IsPartiallyCovered => Summary.LinesCoveredPercentage > 0.0 && Summary.LinesCoveredPercentage < 100.0;
    public bool IsUncovered => Summary.LinesCoveredPercentage == 0.0;
}

/// <summary>
/// Coverage information for a specific line of code
/// </summary>
public class LineCoverage
{
    public int LineNumber { get; set; }
    public int HitCount { get; set; }
    public bool IsCovered => HitCount > 0;
    public string? SourceCode { get; set; }
    public CoverageStatus Status { get; set; }
}

/// <summary>
/// Coverage information for a specific branch
/// </summary>
public class BranchCoverage
{
    public int LineNumber { get; set; }
    public int BranchNumber { get; set; }
    public int HitCount { get; set; }
    public bool IsCovered => HitCount > 0;
    public string? Condition { get; set; }
    public BranchType Type { get; set; }
}

/// <summary>
/// Test execution summary
/// </summary>
public class TestExecutionSummary
{
    public int TotalTests { get; set; }
    public int PassedTests { get; set; }
    public int FailedTests { get; set; }
    public int SkippedTests { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public List<TestFailure> Failures { get; set; } = new();
    public bool AllTestsPassed => FailedTests == 0;
}

/// <summary>
/// Information about a test failure
/// </summary>
public class TestFailure
{
    public string TestName { get; set; } = string.Empty;
    public string TestClass { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
}

/// <summary>
/// Coverage status for a line of code
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CoverageStatus
{
    NotCoverable,
    Covered,
    Uncovered,
    PartiallyCovered
}

/// <summary>
/// Type of branch coverage
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BranchType
{
    Conditional,
    Switch,
    Loop,
    Exception,
    Return
}

/// <summary>
/// Options for coverage analysis
/// </summary>
public class CoverageAnalysisOptions
{
    public List<string> IncludedProjects { get; set; } = new();
    public List<string> ExcludedProjects { get; set; } = new();
    public List<string> IncludedTestProjects { get; set; } = new();
    public List<string> ExcludedFiles { get; set; } = new();
    public bool IncludeGeneratedCode { get; set; } = false;
    public bool CollectBranchCoverage { get; set; } = true;
    public bool CollectMethodCoverage { get; set; } = true;
    public int TimeoutMinutes { get; set; } = 10;
    public string OutputFormat { get; set; } = "json";
    public bool RunInParallel { get; set; } = true;
    public string? TestFilter { get; set; }
}

/// <summary>
/// Result of uncovered code analysis
/// </summary>
public class UncoveredCodeResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<UncoveredMethod> UncoveredMethods { get; set; } = new();
    public List<UncoveredLine> UncoveredLines { get; set; } = new();
    public List<UncoveredBranch> UncoveredBranches { get; set; } = new();
    public int TotalUncoveredItems => UncoveredMethods.Count + UncoveredLines.Count + UncoveredBranches.Count;
}

/// <summary>
/// Information about an uncovered method
/// </summary>
public class UncoveredMethod
{
    public string MethodName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public string Signature { get; set; } = string.Empty;
    public int LineCount { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Information about an uncovered line
/// </summary>
public class UncoveredLine
{
    public string FilePath { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public string? SourceCode { get; set; }
    public string? MethodName { get; set; }
    public string? ClassName { get; set; }
}

/// <summary>
/// Information about an uncovered branch
/// </summary>
public class UncoveredBranch
{
    public string FilePath { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public int BranchNumber { get; set; }
    public string? Condition { get; set; }
    public BranchType Type { get; set; }
    public string? MethodName { get; set; }
    public string? ClassName { get; set; }
}

/// <summary>
/// Result of coverage comparison
/// </summary>
public class CoverageComparisonResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public CoverageSummary BaselineCoverage { get; set; } = new();
    public CoverageSummary CurrentCoverage { get; set; } = new();
    public CoverageDelta Delta { get; set; } = new();
    public List<string> ImprovedFiles { get; set; } = new();
    public List<string> RegressedFiles { get; set; } = new();
    public List<string> NewlyUncoveredMethods { get; set; } = new();
    public List<string> NewlyCoveredMethods { get; set; } = new();
}

/// <summary>
/// Coverage delta between two analysis runs
/// </summary>
public class CoverageDelta
{
    public double LinesCoverageChange { get; set; }
    public double BranchesCoverageChange { get; set; }
    public double MethodsCoverageChange { get; set; }
    public double ClassesCoverageChange { get; set; }
    
    public int LinesChange { get; set; }
    public int BranchesChange { get; set; }
    public int MethodsChange { get; set; }
    public int ClassesChange { get; set; }
    
    public bool IsImprovement => LinesCoverageChange > 0;
    public bool IsRegression => LinesCoverageChange < 0;
    public bool IsUnchanged => Math.Abs(LinesCoverageChange) < 0.01;
}
