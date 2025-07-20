using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using DotnetStaticAnalysisMcp.Server.Services;
using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace DotnetStaticAnalysisMcp.Server.Services;

/// <summary>
/// MCP Tools for .NET Static Analysis using Roslyn
/// </summary>
[McpServerToolType]
public class DotNetAnalysisTools
{
    private static IServiceProvider? ServiceProvider { get; set; }

    public static void SetServiceProvider(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }
    /// <summary>
    /// Load a .NET solution file for analysis
    /// </summary>
    [McpServerTool]
    [Description("Load a .NET solution file for analysis")]
    public static async Task<string> LoadSolution(
        RoslynAnalysisService analysisService,
        [Description("Path to the .sln file to load")] string solutionPath)
    {
        try
        {
            var success = await analysisService.LoadSolutionAsync(solutionPath);
            var result = new { success, message = success ? "Solution loaded successfully" : "Failed to load solution" };
            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            var result = new { success = false, error = ex.Message };
            return JsonSerializer.Serialize(result);
        }
    }

    /// <summary>
    /// Get all compilation errors and warnings from the loaded solution
    /// </summary>
    [McpServerTool]
    [Description("Get all compilation errors and warnings from the loaded solution")]
    public static async Task<string> GetCompilationErrors(RoslynAnalysisService analysisService)
    {
        try
        {
            var errors = await analysisService.GetCompilationErrorsAsync();
            var result = new
            {
                success = true,
                error_count = errors.Count(e => e.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error),
                warning_count = errors.Count(e => e.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Warning),
                errors = errors.Take(100) // Limit to first 100 for performance
            };
            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            var result = new { success = false, error = ex.Message };
            return JsonSerializer.Serialize(result);
        }
    }

    /// <summary>
    /// Get information about the loaded solution
    /// </summary>
    [McpServerTool][Description("Get information about the loaded solution")]
    public static async Task<string> GetSolutionInfo(RoslynAnalysisService analysisService)
    {
        try
        {
            var solutionInfo = await analysisService.GetSolutionInfoAsync();
            var result = new { success = true, solution_info = solutionInfo };
            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            var result = new { success = false, error = ex.Message };
            return JsonSerializer.Serialize(result);
        }
    }

    /// <summary>
    /// Analyze a specific file for errors and warnings
    /// </summary>
    [McpServerTool][Description("Analyze a specific file for errors and warnings")]
    public static async Task<string> AnalyzeFile(
        RoslynAnalysisService analysisService,
        [Description("Path to the file to analyze")] string filePath)
    {
        try
        {
            var errors = await analysisService.AnalyzeFileAsync(filePath);
            var result = new
            {
                success = true,
                file_path = filePath,
                error_count = errors.Count(e => e.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error),
                warning_count = errors.Count(e => e.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Warning),
                errors = errors
            };
            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            var result = new { success = false, error = ex.Message };
            return JsonSerializer.Serialize(result);
        }
    }

    /// <summary>
    /// Get code suggestions and analyzer recommendations from the loaded solution
    /// </summary>
    [McpServerTool]
    [Description("Get code improvement suggestions and analyzer recommendations from the loaded solution")]
    public static async Task<string> GetCodeSuggestions(
        RoslynAnalysisService analysisService,
        [Description("Categories to include (comma-separated): Style, Performance, Modernization, BestPractices, Security, Reliability, Accessibility, Design, Naming, Documentation, Cleanup")]
        string? categories = null,
        [Description("Minimum priority level: Low, Medium, High, Critical")]
        string? minimumPriority = null,
        [Description("Maximum number of suggestions to return")]
        int maxSuggestions = 100,
        [Description("Include auto-fixable suggestions")]
        bool includeAutoFixable = true,
        [Description("Include suggestions requiring manual intervention")]
        bool includeManualFix = true)
    {
        try
        {
            var options = new Models.SuggestionAnalysisOptions
            {
                MaxSuggestions = maxSuggestions,
                IncludeAutoFixable = includeAutoFixable,
                IncludeManualFix = includeManualFix
            };

            // Parse categories
            if (!string.IsNullOrEmpty(categories))
            {
                options.IncludedCategories.Clear();
                var categoryNames = categories.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var categoryName in categoryNames)
                {
                    if (Enum.TryParse<Models.SuggestionCategory>(categoryName.Trim(), true, out var category))
                    {
                        options.IncludedCategories.Add(category);
                    }
                }
            }

            // Parse minimum priority
            if (!string.IsNullOrEmpty(minimumPriority))
            {
                if (Enum.TryParse<Models.SuggestionPriority>(minimumPriority.Trim(), true, out var priority))
                {
                    options.MinimumPriority = priority;
                }
            }

            var suggestions = await analysisService.GetCodeSuggestionsAsync(options);

            var result = new
            {
                success = true,
                suggestion_count = suggestions.Count,
                categories_analyzed = options.IncludedCategories.Select(c => c.ToString()).ToArray(),
                minimum_priority = options.MinimumPriority.ToString(),
                suggestions = suggestions.Select(s => new
                {
                    id = s.Id,
                    title = s.Title,
                    description = s.Description,
                    category = s.Category.ToString(),
                    priority = s.Priority.ToString(),
                    impact = s.Impact.ToString(),
                    file_path = s.FilePath,
                    start_line = s.StartLine,
                    start_column = s.StartColumn,
                    end_line = s.EndLine,
                    end_column = s.EndColumn,
                    original_code = s.OriginalCode,
                    suggested_code = s.SuggestedCode,
                    can_auto_fix = s.CanAutoFix,
                    tags = s.Tags,
                    help_link = s.HelpLink,
                    project_name = s.ProjectName
                })
            };

            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get code suggestions for a specific file
    /// </summary>
    [McpServerTool][Description("Get code improvement suggestions for a specific file")]
    public static async Task<string> GetFileSuggestions(
        RoslynAnalysisService analysisService,
        [Description("Absolute path to the file to analyze")] string filePath,
        [Description("Categories to include (comma-separated): Style, Performance, Modernization, BestPractices, Security, Reliability, Accessibility, Design, Naming, Documentation, Cleanup")]
        string? categories = null,
        [Description("Minimum priority level: Low, Medium, High, Critical")]
        string? minimumPriority = null,
        [Description("Maximum number of suggestions to return")]
        int maxSuggestions = 50)
    {
        try
        {
            var options = new Models.SuggestionAnalysisOptions
            {
                MaxSuggestions = maxSuggestions
            };

            // Parse categories
            if (!string.IsNullOrEmpty(categories))
            {
                options.IncludedCategories.Clear();
                var categoryNames = categories.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var categoryName in categoryNames)
                {
                    if (Enum.TryParse<Models.SuggestionCategory>(categoryName.Trim(), true, out var category))
                    {
                        options.IncludedCategories.Add(category);
                    }
                }
            }

            // Parse minimum priority
            if (!string.IsNullOrEmpty(minimumPriority))
            {
                if (Enum.TryParse<Models.SuggestionPriority>(minimumPriority.Trim(), true, out var priority))
                {
                    options.MinimumPriority = priority;
                }
            }

            var suggestions = await analysisService.GetFileSuggestionsAsync(filePath, options);

            var result = new
            {
                success = true,
                file_path = filePath,
                suggestion_count = suggestions.Count,
                suggestions = suggestions.Select(s => new
                {
                    id = s.Id,
                    title = s.Title,
                    description = s.Description,
                    category = s.Category.ToString(),
                    priority = s.Priority.ToString(),
                    impact = s.Impact.ToString(),
                    start_line = s.StartLine,
                    start_column = s.StartColumn,
                    end_line = s.EndLine,
                    end_column = s.EndColumn,
                    original_code = s.OriginalCode,
                    suggested_code = s.SuggestedCode,
                    can_auto_fix = s.CanAutoFix,
                    tags = s.Tags,
                    help_link = s.HelpLink
                })
            };

            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get comprehensive diagnostic information including server status, MSBuild state, and recent logs
    /// </summary>
    [McpServerTool][Description("Get comprehensive diagnostic information for debugging MCP server issues")]
    public static Task<string> GetDiagnostics([Description("Include recent log entries")] bool includeLogs = true)
    {
        try
        {
            var telemetryService = ServiceProvider?.GetService<TelemetryService>();
            var analysisService = ServiceProvider?.GetService<RoslynAnalysisService>();

            var serverStatus = telemetryService?.GetServerStatus();
            var msbuildDiagnostics = telemetryService?.GetMSBuildDiagnostics();
            var recentOperations = telemetryService?.GetRecentOperations(10);

            var diagnostics = new
            {
                success = true,
                timestamp = DateTime.UtcNow,
                server_status = new
                {
                    uptime_minutes = serverStatus?.Uptime.TotalMinutes ?? 0,
                    current_solution = serverStatus?.CurrentSolution,
                    project_count = serverStatus?.ProjectCount ?? 0,
                    total_errors = serverStatus?.TotalErrors ?? 0,
                    total_warnings = serverStatus?.TotalWarnings ?? 0,
                    last_solution_load = serverStatus?.LastSolutionLoad,
                    last_load_duration_ms = serverStatus?.LastLoadDuration?.TotalMilliseconds,
                    recent_operations = serverStatus?.RecentOperations ?? new List<string>(),
                    performance_metrics = serverStatus?.PerformanceMetrics ?? new Dictionary<string, object>()
                },
                msbuild_diagnostics = new
                {
                    is_registered = msbuildDiagnostics?.IsRegistered ?? false,
                    msbuild_path = msbuildDiagnostics?.MSBuildPath,
                    msbuild_version = msbuildDiagnostics?.MSBuildVersion,
                    current_directory = msbuildDiagnostics?.CurrentDirectory,
                    workspace_diagnostics = msbuildDiagnostics?.WorkspaceDiagnostics ?? new List<string>(),
                    workspace_failures = msbuildDiagnostics?.WorkspaceFailures ?? new List<string>(),
                    environment_variables = msbuildDiagnostics?.EnvironmentVariables ?? new Dictionary<string, string>()
                },
                environment = new
                {
                    process_id = Environment.ProcessId,
                    machine_name = Environment.MachineName,
                    user_name = Environment.UserName,
                    current_directory = Directory.GetCurrentDirectory(),
                    dotnet_version = Environment.Version.ToString(),
                    runtime_identifier = System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier,
                    framework_description = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                    process_architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
                    os_description = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                    working_set_mb = Environment.WorkingSet / 1024 / 1024,
                    gc_total_memory_mb = GC.GetTotalMemory(false) / 1024 / 1024
                },
                recent_operations = recentOperations?.Select(op => new
                {
                    operation_id = op.OperationId,
                    operation_type = op.OperationType,
                    start_time = op.StartTime,
                    end_time = op.EndTime,
                    duration_ms = op.Duration?.TotalMilliseconds,
                    is_success = op.IsSuccess,
                    error_message = op.ErrorMessage,
                    properties = op.Properties
                }).Cast<object>().ToList() ?? new List<object>(),
                log_file_info = GetLogFileInfo()
            };

            return Task.FromResult(JsonSerializer.Serialize(diagnostics, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message,
                stack_trace = ex.StackTrace,
                timestamp = DateTime.UtcNow
            }));
        }
    }

    private static object GetLogFileInfo()
    {
        try
        {
            var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".mcp", "logs");
            var logFile = Path.Combine(logDirectory, "dotnet-analysis.log");

            if (File.Exists(logFile))
            {
                var fileInfo = new FileInfo(logFile);
                var recentLines = new List<string>();

                try
                {
                    // Read last 20 lines of log file
                    var lines = File.ReadAllLines(logFile);
                    recentLines = lines.TakeLast(20).ToList();
                }
                catch (Exception ex)
                {
                    recentLines.Add($"Error reading log file: {ex.Message}");
                }

                return new
                {
                    log_file_path = logFile,
                    file_size_mb = fileInfo.Length / 1024.0 / 1024.0,
                    last_modified = fileInfo.LastWriteTime,
                    recent_log_entries = recentLines
                };
            }
            else
            {
                return new
                {
                    log_file_path = logFile,
                    file_exists = false,
                    message = "Log file does not exist"
                };
            }
        }
        catch (Exception ex)
        {
            return new
            {
                error = $"Failed to get log file info: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Get server version and build information to confirm updates
    /// </summary>
    [McpServerTool][Description("Get server version and build timestamp to verify server updates")]
    public static Task<string> GetServerVersion()
    {
        try
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var buildTime = File.GetLastWriteTime(assembly.Location);

            var version = new
            {
                success = true,
                server_name = "MCP .NET Analysis Server",
                version = "1.0.0",
                build_timestamp = buildTime,
                assembly_location = assembly.Location,
                current_time = DateTime.UtcNow,
                process_start_time = DateTime.UtcNow, // This will show when the process started
                test_marker = "ENHANCED_LOGGING_VERSION_20241220", // Change this to verify updates
                features = new[]
                {
                    "Enhanced Logging",
                    "Telemetry Service",
                    "Debug File Logging",
                    "MSBuild Diagnostics",
                    "Version Verification"
                }
            };

            return Task.FromResult(JsonSerializer.Serialize(version, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message,
                timestamp = DateTime.UtcNow
            }));
        }
    }

    /// <summary>
    /// Get basic environment diagnostics
    /// </summary>
    [McpServerTool][Description("Get basic environment and MSBuild diagnostics")]
    public static Task<string> GetBasicDiagnostics()
    {
        try
        {
            var diagnostics = new
            {
                success = true,
                timestamp = DateTime.UtcNow,
                environment = new
                {
                    current_directory = Directory.GetCurrentDirectory(),
                    process_id = Environment.ProcessId,
                    machine_name = Environment.MachineName,
                    user_name = Environment.UserName,
                    dotnet_version = Environment.Version.ToString(),
                    framework_description = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                    os_description = System.Runtime.InteropServices.RuntimeInformation.OSDescription
                },
                msbuild = new
                {
                    is_registered = Microsoft.Build.Locator.MSBuildLocator.IsRegistered,
                    instances = Microsoft.Build.Locator.MSBuildLocator.QueryVisualStudioInstances()
                        .Select(i => new { i.Name, Version = i.Version.ToString(), i.MSBuildPath })
                        .ToList()
                },
                environment_variables = Environment.GetEnvironmentVariables()
                    .Cast<System.Collections.DictionaryEntry>()
                    .Where(e => e.Key.ToString()?.Contains("DOTNET", StringComparison.OrdinalIgnoreCase) == true ||
                               e.Key.ToString()?.Contains("MSBUILD", StringComparison.OrdinalIgnoreCase) == true ||
                               e.Key.ToString()?.Equals("PATH", StringComparison.OrdinalIgnoreCase) == true)
                    .ToDictionary(e => e.Key.ToString() ?? "", e => e.Value?.ToString() ?? "")
            };

            return Task.FromResult(JsonSerializer.Serialize(diagnostics, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message,
                stack_trace = ex.StackTrace,
                timestamp = DateTime.UtcNow
            }));
        }
    }

    /// <summary>
    /// Get available suggestion categories and their descriptions
    /// </summary>
    [McpServerTool][Description("Get information about available code suggestion categories and configuration options")]
    public static Task<string> GetSuggestionCategories()
    {
        try
        {
            var categories = Enum.GetValues<Models.SuggestionCategory>()
                .Select(c => new
                {
                    name = c.ToString(),
                    description = GetCategoryDescription(c)
                })
                .ToArray();

            var priorities = Enum.GetValues<Models.SuggestionPriority>()
                .Select(p => new
                {
                    name = p.ToString(),
                    description = GetPriorityDescription(p)
                })
                .ToArray();

            var impacts = Enum.GetValues<Models.SuggestionImpact>()
                .Select(i => new
                {
                    name = i.ToString(),
                    description = GetImpactDescription(i)
                })
                .ToArray();

            // Add build timestamp to verify server updates
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var buildTime = File.GetLastWriteTime(assembly.Location);

            var result = new
            {
                success = true,
                categories = categories,
                priorities = priorities,
                impacts = impacts,
                default_options = new
                {
                    max_suggestions = 100,
                    include_auto_fixable = true,
                    include_manual_fix = true,
                    minimum_priority = "Low"
                },
                // Server version info to verify updates
                server_info = new
                {
                    build_timestamp = buildTime,
                    current_time = DateTime.UtcNow,
                    version_marker = "CSHARP_LANGUAGE_SUPPORT_20241220_1445", // Update this to verify changes
                    assembly_location = assembly.Location
                }
            };

            return Task.FromResult(JsonSerializer.Serialize(result));
        }
        catch (Exception ex)
        {
            return Task.FromResult(JsonSerializer.Serialize(new { success = false, error = ex.Message }));
        }
    }

    private static string GetCategoryDescription(Models.SuggestionCategory category)
    {
        return category switch
        {
            Models.SuggestionCategory.Style => "Code style and formatting improvements",
            Models.SuggestionCategory.Performance => "Performance optimizations and efficiency improvements",
            Models.SuggestionCategory.Modernization => "Updates to use newer language features and patterns",
            Models.SuggestionCategory.BestPractices => "General best practices and maintainability improvements",
            Models.SuggestionCategory.Security => "Security-related improvements and vulnerability fixes",
            Models.SuggestionCategory.Reliability => "Reliability and correctness improvements",
            Models.SuggestionCategory.Accessibility => "Accessibility and usability improvements",
            Models.SuggestionCategory.Design => "Design and architecture improvements",
            Models.SuggestionCategory.Naming => "Naming convention improvements",
            Models.SuggestionCategory.Documentation => "Documentation and comment improvements",
            Models.SuggestionCategory.Cleanup => "Unused code removal and cleanup",
            _ => "General code improvements"
        };
    }

    private static string GetPriorityDescription(Models.SuggestionPriority priority)
    {
        return priority switch
        {
            Models.SuggestionPriority.Low => "Optional improvements with minimal impact",
            Models.SuggestionPriority.Medium => "Recommended improvements for better code quality",
            Models.SuggestionPriority.High => "Important improvements that should be addressed",
            Models.SuggestionPriority.Critical => "Critical issues that need immediate attention",
            _ => "General priority"
        };
    }

    private static string GetImpactDescription(Models.SuggestionImpact impact)
    {
        return impact switch
        {
            Models.SuggestionImpact.Minimal => "Cosmetic changes with no functional impact",
            Models.SuggestionImpact.Small => "Small improvements in readability or maintainability",
            Models.SuggestionImpact.Moderate => "Moderate improvements in code quality",
            Models.SuggestionImpact.Significant => "Significant improvements in performance or correctness",
            Models.SuggestionImpact.Major => "Major improvements that affect application behavior",
            _ => "General impact"
        };
    }
}


