using Microsoft.Extensions.Logging;
using MCP.Server.Services;
using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace MCP.Server.Services;

/// <summary>
/// MCP Tools for .NET Static Analysis using Roslyn
/// </summary>
[McpServerToolType]
public class DotNetAnalysisTools
{
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
}


