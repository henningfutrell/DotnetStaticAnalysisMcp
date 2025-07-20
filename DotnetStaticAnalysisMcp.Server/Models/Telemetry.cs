using System.Diagnostics;

namespace DotnetStaticAnalysisMcp.Server.Models;

/// <summary>
/// Telemetry data for tracking operation performance and context
/// </summary>
public class OperationTelemetry
{
    public string OperationId { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string OperationType { get; set; } = string.Empty;
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }
    public TimeSpan? Duration => EndTime - StartTime;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
    public Dictionary<string, double> Metrics { get; set; } = new();
}

/// <summary>
/// Structured logging context for correlation across operations
/// </summary>
public class LogContext
{
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Operation { get; set; } = string.Empty;
    public string? SolutionPath { get; set; }
    public string? ProjectName { get; set; }
    public string? FilePath { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// MSBuild workspace diagnostic information
/// </summary>
public class MSBuildDiagnostics
{
    public bool IsRegistered { get; set; }
    public string? MSBuildPath { get; set; }
    public string? MSBuildVersion { get; set; }
    public List<string> WorkspaceDiagnostics { get; set; } = new();
    public List<string> WorkspaceFailures { get; set; } = new();
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
    public string CurrentDirectory { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Solution loading telemetry and diagnostics
/// </summary>
public class SolutionLoadTelemetry
{
    public string SolutionPath { get; set; } = string.Empty;
    public bool LoadSuccess { get; set; }
    public TimeSpan LoadDuration { get; set; }
    public int ProjectCount { get; set; }
    public int TotalErrors { get; set; }
    public int TotalWarnings { get; set; }
    public List<ProjectLoadInfo> Projects { get; set; } = new();
    public List<string> LoadErrors { get; set; } = new();
    public MSBuildDiagnostics MSBuildInfo { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Individual project loading information
/// </summary>
public class ProjectLoadInfo
{
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public bool LoadSuccess { get; set; }
    public TimeSpan LoadDuration { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public int DocumentCount { get; set; }
    public List<string> LoadErrors { get; set; } = new();
}

/// <summary>
/// Real-time server status for diagnostic queries
/// </summary>
public class ServerStatus
{
    public DateTime StartTime { get; set; }
    public TimeSpan Uptime => DateTime.UtcNow - StartTime;
    public string? CurrentSolution { get; set; }
    public int ProjectCount { get; set; }
    public int TotalErrors { get; set; }
    public int TotalWarnings { get; set; }
    public DateTime? LastSolutionLoad { get; set; }
    public TimeSpan? LastLoadDuration { get; set; }
    public List<string> RecentOperations { get; set; } = new();
    public MSBuildDiagnostics MSBuildStatus { get; set; } = new();
    public Dictionary<string, object> PerformanceMetrics { get; set; } = new();
}

/// <summary>
/// Log entry for structured file logging
/// </summary>
public class StructuredLogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public LogContext Context { get; set; } = new();
    public OperationTelemetry? Telemetry { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}
