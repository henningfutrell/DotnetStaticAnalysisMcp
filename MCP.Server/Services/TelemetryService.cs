using Microsoft.Extensions.Logging;
using MCP.Server.Models;
using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Build.Locator;

namespace MCP.Server.Services;

/// <summary>
/// Service for collecting telemetry, performance metrics, and diagnostic information
/// </summary>
public class TelemetryService
{
    private readonly ILogger<TelemetryService> _logger;
    private readonly ConcurrentDictionary<string, OperationTelemetry> _activeOperations = new();
    private readonly ConcurrentQueue<OperationTelemetry> _completedOperations = new();
    private readonly ServerStatus _serverStatus;
    private readonly object _statusLock = new();

    public TelemetryService(ILogger<TelemetryService> logger)
    {
        _logger = logger;
        _serverStatus = new ServerStatus
        {
            StartTime = DateTime.UtcNow
        };
        
        _logger.LogInformation("TelemetryService initialized at {StartTime}", _serverStatus.StartTime);
    }

    /// <summary>
    /// Start tracking an operation
    /// </summary>
    public OperationTelemetry StartOperation(string operationType, LogContext? context = null)
    {
        var telemetry = new OperationTelemetry
        {
            OperationType = operationType,
            StartTime = DateTime.UtcNow
        };

        _activeOperations[telemetry.OperationId] = telemetry;

        _logger.LogInformation("Started operation {OperationType} with ID {OperationId}",
            operationType, telemetry.OperationId);

        lock (_statusLock)
        {
            _serverStatus.RecentOperations.Insert(0, $"{DateTime.UtcNow:HH:mm:ss} - Started {operationType}");
            if (_serverStatus.RecentOperations.Count > 20)
            {
                _serverStatus.RecentOperations.RemoveAt(_serverStatus.RecentOperations.Count - 1);
            }
        }

        return telemetry;
    }

    /// <summary>
    /// Complete an operation with success
    /// </summary>
    public void CompleteOperation(OperationTelemetry telemetry, Dictionary<string, object>? additionalProperties = null)
    {
        telemetry.EndTime = DateTime.UtcNow;
        telemetry.IsSuccess = true;

        if (additionalProperties != null)
        {
            foreach (var prop in additionalProperties)
            {
                telemetry.Properties[prop.Key] = prop.Value;
            }
        }

        _activeOperations.TryRemove(telemetry.OperationId, out _);
        _completedOperations.Enqueue(telemetry);

        // Keep only last 100 completed operations
        while (_completedOperations.Count > 100)
        {
            _completedOperations.TryDequeue(out _);
        }

        _logger.LogInformation("Completed operation {OperationType} ({OperationId}) in {Duration}ms",
            telemetry.OperationType, telemetry.OperationId, telemetry.Duration?.TotalMilliseconds);

        lock (_statusLock)
        {
            _serverStatus.RecentOperations.Insert(0, 
                $"{DateTime.UtcNow:HH:mm:ss} - Completed {telemetry.OperationType} ({telemetry.Duration?.TotalMilliseconds:F1}ms)");
            if (_serverStatus.RecentOperations.Count > 20)
            {
                _serverStatus.RecentOperations.RemoveAt(_serverStatus.RecentOperations.Count - 1);
            }
        }
    }

    /// <summary>
    /// Complete an operation with failure
    /// </summary>
    public void FailOperation(OperationTelemetry telemetry, Exception exception, Dictionary<string, object>? additionalProperties = null)
    {
        telemetry.EndTime = DateTime.UtcNow;
        telemetry.IsSuccess = false;
        telemetry.ErrorMessage = exception.Message;

        if (additionalProperties != null)
        {
            foreach (var prop in additionalProperties)
            {
                telemetry.Properties[prop.Key] = prop.Value;
            }
        }

        _activeOperations.TryRemove(telemetry.OperationId, out _);
        _completedOperations.Enqueue(telemetry);

        _logger.LogError(exception, "Failed operation {OperationType} ({OperationId}) after {Duration}ms: {ErrorMessage}",
            telemetry.OperationType, telemetry.OperationId, telemetry.Duration?.TotalMilliseconds, exception.Message);

        lock (_statusLock)
        {
            _serverStatus.RecentOperations.Insert(0, 
                $"{DateTime.UtcNow:HH:mm:ss} - Failed {telemetry.OperationType}: {exception.Message}");
            if (_serverStatus.RecentOperations.Count > 20)
            {
                _serverStatus.RecentOperations.RemoveAt(_serverStatus.RecentOperations.Count - 1);
            }
        }
    }

    /// <summary>
    /// Update server status with solution information
    /// </summary>
    public void UpdateSolutionStatus(string? solutionPath, int projectCount, int errorCount, int warningCount, TimeSpan loadDuration)
    {
        lock (_statusLock)
        {
            _serverStatus.CurrentSolution = solutionPath;
            _serverStatus.ProjectCount = projectCount;
            _serverStatus.TotalErrors = errorCount;
            _serverStatus.TotalWarnings = warningCount;
            _serverStatus.LastSolutionLoad = DateTime.UtcNow;
            _serverStatus.LastLoadDuration = loadDuration;
        }

        _logger.LogInformation("Updated solution status: {SolutionPath}, {ProjectCount} projects, {ErrorCount} errors, {WarningCount} warnings",
            solutionPath, projectCount, errorCount, warningCount);
    }

    /// <summary>
    /// Get current server status
    /// </summary>
    public ServerStatus GetServerStatus()
    {
        lock (_statusLock)
        {
            // Update MSBuild status
            _serverStatus.MSBuildStatus = GetMSBuildDiagnostics();
            
            // Update performance metrics
            _serverStatus.PerformanceMetrics["ActiveOperations"] = _activeOperations.Count;
            _serverStatus.PerformanceMetrics["CompletedOperations"] = _completedOperations.Count;
            _serverStatus.PerformanceMetrics["UptimeMinutes"] = _serverStatus.Uptime.TotalMinutes;

            return _serverStatus;
        }
    }

    /// <summary>
    /// Get MSBuild diagnostic information
    /// </summary>
    public MSBuildDiagnostics GetMSBuildDiagnostics()
    {
        var diagnostics = new MSBuildDiagnostics
        {
            IsRegistered = MSBuildLocator.IsRegistered,
            CurrentDirectory = Directory.GetCurrentDirectory(),
            Timestamp = DateTime.UtcNow
        };

        // Collect relevant environment variables
        var envVars = Environment.GetEnvironmentVariables();
        foreach (var key in envVars.Keys.Cast<string>())
        {
            if (key.Contains("DOTNET", StringComparison.OrdinalIgnoreCase) ||
                key.Contains("MSBUILD", StringComparison.OrdinalIgnoreCase) ||
                key.Contains("NUGET", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("PATH", StringComparison.OrdinalIgnoreCase))
            {
                diagnostics.EnvironmentVariables[key] = envVars[key]?.ToString() ?? "";
            }
        }

        try
        {
            if (MSBuildLocator.IsRegistered)
            {
                var instances = MSBuildLocator.QueryVisualStudioInstances().ToList();
                if (instances.Any())
                {
                    var instance = instances.First();
                    diagnostics.MSBuildPath = instance.MSBuildPath;
                    diagnostics.MSBuildVersion = instance.Version.ToString();
                }
            }
        }
        catch (Exception ex)
        {
            diagnostics.WorkspaceFailures.Add($"Failed to query MSBuild instances: {ex.Message}");
        }

        return diagnostics;
    }

    /// <summary>
    /// Get recent operation telemetry
    /// </summary>
    public List<OperationTelemetry> GetRecentOperations(int count = 20)
    {
        var recent = new List<OperationTelemetry>();
        
        // Add active operations
        recent.AddRange(_activeOperations.Values.OrderByDescending(o => o.StartTime));
        
        // Add completed operations
        var completed = _completedOperations.ToArray().OrderByDescending(o => o.StartTime).Take(count - recent.Count);
        recent.AddRange(completed);

        return recent.Take(count).ToList();
    }

    /// <summary>
    /// Log structured telemetry data
    /// </summary>
    public void LogTelemetry(string operationType, Dictionary<string, object> properties, Dictionary<string, double>? metrics = null)
    {
        var telemetryData = new
        {
            OperationType = operationType,
            Timestamp = DateTime.UtcNow,
            Properties = properties,
            Metrics = metrics ?? new Dictionary<string, double>()
        };

        _logger.LogInformation("Telemetry: {OperationType} - {@TelemetryData}", operationType, telemetryData);
    }
}
