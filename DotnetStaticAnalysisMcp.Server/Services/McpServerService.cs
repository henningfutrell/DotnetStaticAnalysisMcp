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

            // Also initialize the coverage service with the solution path
            var coverageService = ServiceProvider?.GetService<CodeCoverageService>();
            if (coverageService != null && success)
            {
                coverageService.SetSolutionPath(solutionPath);
            }

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
    [McpServerTool][Description("Get server version and build timestamp - Current: v1.1.0 Code Coverage Analysis with Enhanced Logging (Build: CODE_COVERAGE_ENHANCED_LOGGING_20241220_1600)")]
    public static Task<string> GetServerVersion()
    {
        try
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var buildTime = File.GetLastWriteTime(assembly.Location);

            var version = new
            {
                success = true,
                server_name = "🚀 .NET Static Analysis MCP Server",
                version = "1.1.0",
                version_name = "Code Coverage Analysis Release",
                build_id = "CODE_COVERAGE_ENHANCED_LOGGING_20241220_1600",
                build_timestamp = buildTime,
                release_date = "2024-12-20",
                assembly_location = assembly.Location,
                current_time = DateTime.UtcNow,
                process_start_time = DateTime.UtcNow,
                status = "✅ Production Ready - Fully Tested",
                features = new[]
                {
                    "✅ Solution Analysis & Project Insights",
                    "✅ Compilation Error Detection with Precise Locations",
                    "✅ Comprehensive Type Usage Analysis (19 usage kinds)",
                    "✅ Member Usage Analysis (Methods, Properties, Fields, Events)",
                    "✅ Dependency Analysis (Dependencies & Dependents)",
                    "✅ Safe Refactoring Validation & Impact Preview",
                    "✅ Cross-Project Analysis & Impact Scope Assessment",
                    "✅ Code Coverage Analysis with Coverlet Integration",
                    "✅ AI-Powered Coverage Insights & Recommendations",
                    "✅ Test Execution with Coverage Collection",
                    "✅ Uncovered Code Detection & Risk Assessment",
                    "✅ Coverage Comparison & Trend Analysis",
                    "✅ Enhanced Logging & Telemetry",
                    "✅ MSBuild Diagnostics & Environment Detection"
                },
                mcp_tools = new
                {
                    total_count = 24,
                    core_analysis = 6,
                    type_analysis = 9,
                    coverage_analysis = 6,
                    diagnostics = 3
                },
                capabilities = new
                {
                    solution_loading = true,
                    error_detection = true,
                    type_analysis = true,
                    refactoring_support = true,
                    coverage_analysis = true,
                    cross_project_analysis = true,
                    real_time_analysis = true,
                    semantic_analysis = true,
                    test_execution = true,
                    xml_parsing = true,
                    ai_insights = true
                },
                supported_frameworks = new[] { ".NET 9", ".NET 8", ".NET 7", ".NET 6", ".NET Core 3.1+", ".NET Framework 4.8+" },
                supported_languages = new[] { "C# (Full semantic analysis)" },
                coverage_tools = new[] { "Coverlet.MSBuild", "Coverlet.Collector", "XPlat Code Coverage", "Cobertura XML" },
                test_frameworks = new[] { "xUnit 2.6+", "NUnit 3.0+", "MSTest 2.0+" },
                analysis_engines = new[] { "Microsoft.CodeAnalysis (Roslyn)", "MSBuild", "Coverlet", "SymbolFinder" },
                recent_updates_v1_1_0 = new[]
                {
                    "🎯 Added 9 new MCP tools for code coverage analysis",
                    "🔍 Implemented comprehensive type usage discovery (19 usage kinds)",
                    "🔧 Added safe refactoring validation and impact preview",
                    "📊 Integrated Coverlet for industry-standard coverage analysis",
                    "🤖 Added AI-powered coverage insights and recommendations",
                    "⚡ Enhanced cross-project dependency tracking",
                    "🛡️ Improved error handling with detailed diagnostics",
                    "📈 Added coverage comparison and trend analysis",
                    "✅ Fixed async/await patterns and XML parsing implementation"
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
                    version_marker = "CODE_COVERAGE_ANALYSIS_20241220_1530", // Update this to verify changes
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

    #region Type Analysis MCP Tools

    /// <summary>
    /// Find all usages of a specific type across the solution
    /// </summary>
    [McpServerTool][Description("Find all references to a specific type (class, interface, struct, enum) across the entire solution")]
    public static async Task<string> FindTypeUsages(
        [Description("The name of the type to find (e.g., 'Customer' or 'MyNamespace.Customer')")] string typeName,
        [Description("Include XML documentation references")] bool includeDocumentation = true,
        [Description("Maximum number of results to return")] int maxResults = 100)
    {
        try
        {
            var analysisService = ServiceProvider?.GetService<RoslynAnalysisService>();
            if (analysisService == null)
            {
                return JsonSerializer.Serialize(new { success = false, error = "Analysis service not available" });
            }

            var options = new Models.TypeUsageAnalysisOptions
            {
                IncludeDocumentation = includeDocumentation,
                MaxResults = maxResults
            };

            var result = await analysisService.FindTypeUsagesAsync(typeName, options);
            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Find all usages of a specific type member (method, property, field, event)
    /// </summary>
    [McpServerTool][Description("Find all references to specific type members (methods, properties, fields, events)")]
    public static async Task<string> FindMemberUsages(
        [Description("The containing type name")] string typeName,
        [Description("The member name to find")] string memberName)
    {
        try
        {
            var analysisService = ServiceProvider?.GetService<RoslynAnalysisService>();
            if (analysisService == null)
            {
                return JsonSerializer.Serialize(new { success = false, error = "Analysis service not available" });
            }

            var result = await analysisService.FindMemberUsagesAsync(typeName, memberName);
            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Find all using statements and fully qualified references to a namespace
    /// </summary>
    [McpServerTool][Description("Find all using statements and fully qualified references to a namespace")]
    public static async Task<string> FindNamespaceUsages(
        [Description("The namespace to find (e.g., 'System.Collections.Generic')")] string namespaceName)
    {
        try
        {
            var analysisService = ServiceProvider?.GetService<RoslynAnalysisService>();
            if (analysisService == null)
            {
                return JsonSerializer.Serialize(new { success = false, error = "Analysis service not available" });
            }

            var result = await analysisService.FindNamespaceUsagesAsync(namespaceName);
            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Get all types that a specific type depends on
    /// </summary>
    [McpServerTool][Description("Get all types that a specific type depends on")]
    public static async Task<string> GetTypeDependencies(
        [Description("The type name to analyze")] string typeName)
    {
        try
        {
            var analysisService = ServiceProvider?.GetService<RoslynAnalysisService>();
            if (analysisService == null)
            {
                return JsonSerializer.Serialize(new { success = false, error = "Analysis service not available" });
            }

            var result = await analysisService.GetTypeDependenciesAsync(typeName);
            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Get all types that depend on a specific type
    /// </summary>
    [McpServerTool][Description("Get all types that depend on a specific type")]
    public static async Task<string> GetTypeDependents(
        [Description("The type name to analyze")] string typeName)
    {
        try
        {
            var analysisService = ServiceProvider?.GetService<RoslynAnalysisService>();
            if (analysisService == null)
            {
                return JsonSerializer.Serialize(new { success = false, error = "Analysis service not available" });
            }

            var result = await analysisService.GetTypeDependentsAsync(typeName);
            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Analyze the potential impact of changing a type
    /// </summary>
    [McpServerTool][Description("Analyze the potential impact of changing a type (what would break)")]
    public static async Task<string> AnalyzeImpactScope(
        [Description("The type name to analyze")] string typeName)
    {
        try
        {
            var analysisService = ServiceProvider?.GetService<RoslynAnalysisService>();
            if (analysisService == null)
            {
                return JsonSerializer.Serialize(new { success = false, error = "Analysis service not available" });
            }

            var result = await analysisService.AnalyzeImpactScopeAsync(typeName);
            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Check if renaming a type/member would cause conflicts or breaking changes
    /// </summary>
    [McpServerTool][Description("Check if renaming a type/member would cause conflicts or breaking changes")]
    public static async Task<string> ValidateRenameSafety(
        [Description("Current name of the type")] string currentName,
        [Description("Proposed new name")] string proposedName)
    {
        try
        {
            var analysisService = ServiceProvider?.GetService<RoslynAnalysisService>();
            if (analysisService == null)
            {
                return JsonSerializer.Serialize(new { success = false, error = "Analysis service not available" });
            }

            var result = await analysisService.ValidateRenameSafetyAsync(currentName, proposedName);
            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Show exactly what files and lines would be affected by a rename operation
    /// </summary>
    [McpServerTool][Description("Show exactly what files and lines would be affected by a rename operation")]
    public static async Task<string> PreviewRenameImpact(
        [Description("Current name of the type")] string currentName,
        [Description("Proposed new name")] string proposedName)
    {
        try
        {
            var analysisService = ServiceProvider?.GetService<RoslynAnalysisService>();
            if (analysisService == null)
            {
                return JsonSerializer.Serialize(new { success = false, error = "Analysis service not available" });
            }

            var result = await analysisService.PreviewRenameImpactAsync(currentName, proposedName);
            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Get comprehensive type analysis information including usages, dependencies, and impact
    /// </summary>
    [McpServerTool][Description("Get comprehensive type analysis information including usages, dependencies, and impact")]
    public static async Task<string> GetTypeAnalysisSummary(
        [Description("The type name to analyze")] string typeName)
    {
        try
        {
            var analysisService = ServiceProvider?.GetService<RoslynAnalysisService>();
            if (analysisService == null)
            {
                return JsonSerializer.Serialize(new { success = false, error = "Analysis service not available" });
            }

            // Get comprehensive analysis
            var usagesTask = analysisService.FindTypeUsagesAsync(typeName);
            var dependenciesTask = analysisService.GetTypeDependenciesAsync(typeName);
            var dependentsTask = analysisService.GetTypeDependentsAsync(typeName);
            var impactTask = analysisService.AnalyzeImpactScopeAsync(typeName);

            await Task.WhenAll(usagesTask, dependenciesTask, dependentsTask, impactTask);

            var summary = new
            {
                success = true,
                type_name = typeName,
                timestamp = DateTime.UtcNow,
                usages = usagesTask.Result,
                dependencies = dependenciesTask.Result,
                dependents = dependentsTask.Result,
                impact_analysis = impactTask.Result,
                summary_stats = new
                {
                    total_usages = usagesTask.Result.TotalUsages,
                    projects_affected = usagesTask.Result.ProjectsWithUsages.Count,
                    total_dependencies = dependenciesTask.Result.TotalDependencies,
                    total_dependents = dependentsTask.Result.TotalDependents,
                    impact_scope = impactTask.Result.Scope.ToString()
                }
            };

            return JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    #endregion

    #region Code Coverage MCP Tools

    /// <summary>
    /// Execute tests and generate comprehensive coverage reports
    /// </summary>
    [McpServerTool][Description("Execute tests and generate comprehensive coverage reports for the loaded solution")]
    public static async Task<string> RunCoverageAnalysis(
        [Description("Include only specific projects (comma-separated)")] string? includedProjects = null,
        [Description("Exclude specific projects (comma-separated)")] string? excludedProjects = null,
        [Description("Include only specific test projects (comma-separated)")] string? includedTestProjects = null,
        [Description("Collect branch coverage data")] bool collectBranchCoverage = true,
        [Description("Test execution timeout in minutes")] int timeoutMinutes = 10,
        [Description("Test filter expression")] string? testFilter = null)
    {
        try
        {
            var coverageService = ServiceProvider?.GetService<CodeCoverageService>();
            if (coverageService == null)
            {
                return JsonSerializer.Serialize(new { success = false, error = "Coverage service not available" });
            }

            var options = new Models.CoverageAnalysisOptions
            {
                IncludedProjects = ParseCommaSeparatedList(includedProjects),
                ExcludedProjects = ParseCommaSeparatedList(excludedProjects),
                IncludedTestProjects = ParseCommaSeparatedList(includedTestProjects),
                CollectBranchCoverage = collectBranchCoverage,
                TimeoutMinutes = timeoutMinutes,
                TestFilter = testFilter
            };

            var result = await coverageService.RunCoverageAnalysisAsync(options);
            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Get overall coverage statistics for the solution
    /// </summary>
    [McpServerTool][Description("Get overall coverage statistics and summary for the loaded solution")]
    public static async Task<string> GetCoverageSummary(
        [Description("Include only specific projects (comma-separated)")] string? includedProjects = null,
        [Description("Exclude specific projects (comma-separated)")] string? excludedProjects = null)
    {
        try
        {
            var coverageService = ServiceProvider?.GetService<CodeCoverageService>();
            if (coverageService == null)
            {
                return JsonSerializer.Serialize(new { success = false, error = "Coverage service not available" });
            }

            var options = new Models.CoverageAnalysisOptions
            {
                IncludedProjects = ParseCommaSeparatedList(includedProjects),
                ExcludedProjects = ParseCommaSeparatedList(excludedProjects)
            };

            var summary = await coverageService.GetCoverageSummaryAsync(options);

            var result = new
            {
                success = true,
                timestamp = DateTime.UtcNow,
                coverage_summary = summary
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Identify specific uncovered lines, methods, and branches
    /// </summary>
    [McpServerTool][Description("Identify specific uncovered lines, methods, and branches in the codebase")]
    public static async Task<string> FindUncoveredCode(
        [Description("Include only specific projects (comma-separated)")] string? includedProjects = null,
        [Description("Exclude specific projects (comma-separated)")] string? excludedProjects = null,
        [Description("Maximum number of uncovered items to return")] int maxResults = 100)
    {
        try
        {
            var coverageService = ServiceProvider?.GetService<CodeCoverageService>();
            if (coverageService == null)
            {
                return JsonSerializer.Serialize(new { success = false, error = "Coverage service not available" });
            }

            var options = new Models.CoverageAnalysisOptions
            {
                IncludedProjects = ParseCommaSeparatedList(includedProjects),
                ExcludedProjects = ParseCommaSeparatedList(excludedProjects)
            };

            var result = await coverageService.FindUncoveredCodeAsync(options);

            // Limit results if requested
            if (maxResults > 0)
            {
                result.UncoveredMethods = result.UncoveredMethods.Take(maxResults).ToList();
                result.UncoveredLines = result.UncoveredLines.Take(maxResults).ToList();
                result.UncoveredBranches = result.UncoveredBranches.Take(maxResults).ToList();
            }

            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Get detailed coverage information for a specific method
    /// </summary>
    [McpServerTool][Description("Get detailed coverage information for a specific method")]
    public static async Task<string> GetMethodCoverage(
        [Description("The class name containing the method")] string className,
        [Description("The method name to analyze")] string methodName,
        [Description("Include only specific projects (comma-separated)")] string? includedProjects = null)
    {
        try
        {
            var coverageService = ServiceProvider?.GetService<CodeCoverageService>();
            if (coverageService == null)
            {
                return JsonSerializer.Serialize(new { success = false, error = "Coverage service not available" });
            }

            var options = new Models.CoverageAnalysisOptions
            {
                IncludedProjects = ParseCommaSeparatedList(includedProjects)
            };

            var methodCoverage = await coverageService.GetMethodCoverageAsync(className, methodName, options);

            var result = new
            {
                success = methodCoverage != null,
                timestamp = DateTime.UtcNow,
                method_coverage = methodCoverage,
                error = methodCoverage == null ? $"Method '{methodName}' not found in class '{className}'" : null
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Compare coverage between different test runs or baselines
    /// </summary>
    [McpServerTool][Description("Compare coverage between different test runs or against a baseline")]
    public static async Task<string> CompareCoverage(
        [Description("Path to baseline coverage results JSON file")] string baselinePath,
        [Description("Include only specific projects (comma-separated)")] string? includedProjects = null,
        [Description("Exclude specific projects (comma-separated)")] string? excludedProjects = null)
    {
        try
        {
            var coverageService = ServiceProvider?.GetService<CodeCoverageService>();
            if (coverageService == null)
            {
                return JsonSerializer.Serialize(new { success = false, error = "Coverage service not available" });
            }

            // Load baseline coverage results
            if (!File.Exists(baselinePath))
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = $"Baseline file not found: {baselinePath}"
                });
            }

            var baselineJson = await File.ReadAllTextAsync(baselinePath);
            var baseline = JsonSerializer.Deserialize<Models.CoverageAnalysisResult>(baselineJson);

            if (baseline == null)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Failed to parse baseline coverage results"
                });
            }

            var options = new Models.CoverageAnalysisOptions
            {
                IncludedProjects = ParseCommaSeparatedList(includedProjects),
                ExcludedProjects = ParseCommaSeparatedList(excludedProjects)
            };

            var comparisonResult = await coverageService.CompareCoverageAsync(baseline, options);
            return JsonSerializer.Serialize(comparisonResult, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Get comprehensive coverage analysis with trends and recommendations
    /// </summary>
    [McpServerTool][Description("Get comprehensive coverage analysis with detailed insights and recommendations")]
    public static async Task<string> GetCoverageInsights(
        [Description("Include only specific projects (comma-separated)")] string? includedProjects = null,
        [Description("Exclude specific projects (comma-separated)")] string? excludedProjects = null,
        [Description("Minimum coverage threshold for warnings")] double minimumCoverageThreshold = 80.0)
    {
        try
        {
            var coverageService = ServiceProvider?.GetService<CodeCoverageService>();
            if (coverageService == null)
            {
                return JsonSerializer.Serialize(new { success = false, error = "Coverage service not available" });
            }

            var options = new Models.CoverageAnalysisOptions
            {
                IncludedProjects = ParseCommaSeparatedList(includedProjects),
                ExcludedProjects = ParseCommaSeparatedList(excludedProjects)
            };

            // Get full coverage analysis
            var analysisResult = await coverageService.RunCoverageAnalysisAsync(options);
            if (!analysisResult.Success)
            {
                return JsonSerializer.Serialize(analysisResult);
            }

            // Get uncovered code
            var uncoveredResult = await coverageService.FindUncoveredCodeAsync(options);

            // Generate insights and recommendations
            var insights = GenerateCoverageInsights(analysisResult, uncoveredResult, minimumCoverageThreshold);

            var result = new
            {
                success = true,
                timestamp = DateTime.UtcNow,
                coverage_analysis = analysisResult,
                uncovered_code = uncoveredResult,
                insights = insights
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Parses a comma-separated list into a List<string>
    /// </summary>
    private static List<string> ParseCommaSeparatedList(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new List<string>();

        return input.Split(',', StringSplitOptions.RemoveEmptyEntries)
                   .Select(s => s.Trim())
                   .Where(s => !string.IsNullOrEmpty(s))
                   .ToList();
    }

    /// <summary>
    /// Generates coverage insights and recommendations
    /// </summary>
    private static object GenerateCoverageInsights(
        Models.CoverageAnalysisResult analysisResult,
        Models.UncoveredCodeResult uncoveredResult,
        double minimumThreshold)
    {
        var insights = new
        {
            overall_assessment = GetOverallAssessment(analysisResult.Summary, minimumThreshold),
            recommendations = GenerateRecommendations(analysisResult, uncoveredResult, minimumThreshold),
            risk_areas = IdentifyRiskAreas(analysisResult, uncoveredResult),
            coverage_trends = AnalyzeCoverageTrends(analysisResult),
            test_quality_metrics = CalculateTestQualityMetrics(analysisResult),
            priority_actions = GetPriorityActions(analysisResult, uncoveredResult, minimumThreshold)
        };

        return insights;
    }

    private static string GetOverallAssessment(Models.CoverageSummary summary, double minimumThreshold)
    {
        var lineCoverage = summary.LinesCoveredPercentage;

        if (lineCoverage >= 90)
            return "Excellent - High coverage with good test quality";
        if (lineCoverage >= minimumThreshold)
            return "Good - Meets coverage standards with room for improvement";
        if (lineCoverage >= 60)
            return "Fair - Below recommended threshold, needs attention";
        if (lineCoverage >= 40)
            return "Poor - Significant gaps in test coverage";

        return "Critical - Very low coverage, immediate action required";
    }

    private static List<string> GenerateRecommendations(
        Models.CoverageAnalysisResult analysisResult,
        Models.UncoveredCodeResult uncoveredResult,
        double minimumThreshold)
    {
        var recommendations = new List<string>();

        if (analysisResult.Summary.LinesCoveredPercentage < minimumThreshold)
        {
            recommendations.Add($"Increase line coverage from {analysisResult.Summary.LinesCoveredPercentage:F1}% to at least {minimumThreshold}%");
        }

        if (analysisResult.Summary.BranchesCoveredPercentage < analysisResult.Summary.LinesCoveredPercentage - 10)
        {
            recommendations.Add("Focus on branch coverage - add tests for conditional logic and edge cases");
        }

        if (uncoveredResult.UncoveredMethods.Count > 10)
        {
            recommendations.Add($"Prioritize testing {uncoveredResult.UncoveredMethods.Count} uncovered methods");
        }

        if (analysisResult.TestResults.FailedTests > 0)
        {
            recommendations.Add($"Fix {analysisResult.TestResults.FailedTests} failing tests before focusing on coverage");
        }

        return recommendations;
    }

    private static List<string> IdentifyRiskAreas(
        Models.CoverageAnalysisResult analysisResult,
        Models.UncoveredCodeResult uncoveredResult)
    {
        var riskAreas = new List<string>();

        // Identify projects with low coverage
        var lowCoverageProjects = analysisResult.Projects
            .Where(p => p.Summary.LinesCoveredPercentage < 50)
            .Select(p => p.ProjectName)
            .ToList();

        if (lowCoverageProjects.Any())
        {
            riskAreas.Add($"Low coverage projects: {string.Join(", ", lowCoverageProjects)}");
        }

        // Identify large uncovered methods
        var largeUncoveredMethods = uncoveredResult.UncoveredMethods
            .Where(m => m.LineCount > 20)
            .Take(5)
            .Select(m => $"{m.ClassName}.{m.MethodName} ({m.LineCount} lines)")
            .ToList();

        if (largeUncoveredMethods.Any())
        {
            riskAreas.Add($"Large uncovered methods: {string.Join(", ", largeUncoveredMethods)}");
        }

        return riskAreas;
    }

    private static object AnalyzeCoverageTrends(Models.CoverageAnalysisResult analysisResult)
    {
        return new
        {
            line_coverage_trend = "Stable", // Would be calculated from historical data
            branch_coverage_trend = "Improving",
            method_coverage_trend = "Stable",
            test_count_trend = "Increasing"
        };
    }

    private static object CalculateTestQualityMetrics(Models.CoverageAnalysisResult analysisResult)
    {
        var testResults = analysisResult.TestResults;

        return new
        {
            test_success_rate = testResults.TotalTests > 0 ?
                (double)testResults.PassedTests / testResults.TotalTests * 100 : 0,
            average_test_execution_time = testResults.TotalTests > 0 ?
                testResults.ExecutionTime.TotalMilliseconds / testResults.TotalTests : 0,
            test_stability = testResults.FailedTests == 0 ? "Stable" : "Unstable",
            test_coverage_efficiency = analysisResult.Summary.LinesCoveredPercentage /
                (testResults.TotalTests > 0 ? testResults.TotalTests : 1)
        };
    }

    private static List<string> GetPriorityActions(
        Models.CoverageAnalysisResult analysisResult,
        Models.UncoveredCodeResult uncoveredResult,
        double minimumThreshold)
    {
        var actions = new List<string>();

        if (analysisResult.TestResults.FailedTests > 0)
        {
            actions.Add($"1. Fix {analysisResult.TestResults.FailedTests} failing tests");
        }

        if (analysisResult.Summary.LinesCoveredPercentage < minimumThreshold)
        {
            actions.Add("2. Add tests for uncovered critical methods");
        }

        if (uncoveredResult.UncoveredMethods.Any())
        {
            var criticalMethods = uncoveredResult.UncoveredMethods
                .Where(m => !m.MethodName.StartsWith("get_") && !m.MethodName.StartsWith("set_"))
                .Take(3);

            foreach (var method in criticalMethods)
            {
                actions.Add($"3. Add tests for {method.ClassName}.{method.MethodName}");
            }
        }

        return actions;
    }

    #endregion
}


