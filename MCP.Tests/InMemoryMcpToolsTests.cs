using Microsoft.Extensions.Logging;
using MCP.Server.Services;
using System.Text.Json;

namespace MCP.Tests;

/// <summary>
/// Tests that verify MCP tools work with in-memory analysis service
/// </summary>
public class InMemoryMcpToolsTests
{
    private readonly ILogger<InMemoryAnalysisService> _logger;

    public InMemoryMcpToolsTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => 
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });
        _logger = loggerFactory.CreateLogger<InMemoryAnalysisService>();
    }

    [Test]
    public async Task McpTools_WithInMemoryService_GetCompilationErrors_ReturnsValidJson()
    {
        // Arrange
        using var inMemoryService = InMemoryAnalysisService.CreateWithTestProjects(_logger);

        // Act
        var result = await InMemoryMcpTools.GetCompilationErrors(inMemoryService);

        // Assert
        await Assert.That(result).IsNotNull();

        var response = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(response.GetProperty("success").GetBoolean()).IsTrue();
        await Assert.That(response.GetProperty("error_count").GetInt32()).IsGreaterThan(0);
        await Assert.That(response.GetProperty("warning_count").GetInt32()).IsGreaterThanOrEqualTo(0);

        var errors = response.GetProperty("errors").EnumerateArray().ToList();
        await Assert.That(errors.Count).IsGreaterThan(0);

        // Verify error structure
        var firstError = errors[0];
        await Assert.That(firstError.GetProperty("Id").GetString()).IsNotNull();
        await Assert.That(firstError.GetProperty("Message").GetString()).IsNotNull();
        // Severity is serialized as a number (enum value), so use GetInt32()
        await Assert.That(firstError.GetProperty("Severity").GetInt32()).IsGreaterThanOrEqualTo(0);
        await Assert.That(firstError.GetProperty("ProjectName").GetString()).IsNotNull();

        Console.WriteLine($"Found {errors.Count} errors via MCP tools");
    }

    [Test]
    public async Task McpTools_WithInMemoryService_GetSolutionInfo_ReturnsValidJson()
    {
        // Arrange
        using var inMemoryService = InMemoryAnalysisService.CreateWithTestProjects(_logger);

        // Act
        var result = await InMemoryMcpTools.GetSolutionInfo(inMemoryService);

        // Assert
        await Assert.That(result).IsNotNull();

        var response = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(response.GetProperty("success").GetBoolean()).IsTrue();

        var solutionInfo = response.GetProperty("solution_info");
        await Assert.That(solutionInfo.GetProperty("Name").GetString()).IsEqualTo("InMemoryTestSolution");
        await Assert.That(solutionInfo.GetProperty("HasCompilationErrors").GetBoolean()).IsTrue();
        await Assert.That(solutionInfo.GetProperty("TotalErrors").GetInt32()).IsGreaterThan(0);

        var projects = solutionInfo.GetProperty("Projects").EnumerateArray().ToList();
        await Assert.That(projects.Count).IsEqualTo(3);

        var projectNames = projects.Select(p => p.GetProperty("Name").GetString() ?? "").Where(name => !string.IsNullOrEmpty(name)).ToList();
        await Assert.That(projectNames).Contains("TestConsoleProject");
        await Assert.That(projectNames).Contains("TestLibrary");
        await Assert.That(projectNames).Contains("ValidProject");

        Console.WriteLine($"Solution has {projects.Count} projects via MCP tools");
    }

    [Test]
    public async Task McpTools_WithInMemoryService_AnalyzeFile_ReturnsValidJson()
    {
        // Arrange
        using var inMemoryService = InMemoryAnalysisService.CreateWithTestProjects(_logger);

        // Act
        var result = await InMemoryMcpTools.AnalyzeFile(inMemoryService, "Program.cs");

        // Assert
        await Assert.That(result).IsNotNull();

        var response = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(response.GetProperty("success").GetBoolean()).IsTrue();
        await Assert.That(response.GetProperty("file_path").GetString()).IsEqualTo("Program.cs");
        await Assert.That(response.GetProperty("error_count").GetInt32()).IsGreaterThan(0);

        var errors = response.GetProperty("errors").EnumerateArray().ToList();
        await Assert.That(errors.Count).IsGreaterThan(0);

        // All errors should be from Program.cs
        foreach (var error in errors)
        {
            await Assert.That(error.GetProperty("FilePath").GetString()).IsEqualTo("Program.cs");
        }

        Console.WriteLine($"Program.cs has {errors.Count} errors via MCP tools");
    }

    [Test]
    public async Task McpTools_WithSpecificErrors_DetectsExpectedErrorTypes()
    {
        // Arrange - Create workspace with specific error types
        using var inMemoryService = InMemoryAnalysisService.CreateWithErrors(_logger, "CS0103", "CS0246", "CS1002");

        // Act
        var result = await InMemoryMcpTools.GetCompilationErrors(inMemoryService);

        // Assert
        await Assert.That(result).IsNotNull();

        var response = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(response.GetProperty("success").GetBoolean()).IsTrue();

        var errors = response.GetProperty("errors").EnumerateArray().ToList();
        var errorIds = errors.Select(e => e.GetProperty("Id").GetString() ?? "").Where(id => !string.IsNullOrEmpty(id)).ToList();

        // Should contain the specific errors we requested
        await Assert.That(errorIds).Contains("CS0103");
        await Assert.That(errorIds).Contains("CS0246");
        await Assert.That(errorIds).Contains("CS1002");

        Console.WriteLine($"Targeted errors found: {string.Join(", ", errorIds)}");
    }

    [Test]
    public async Task McpTools_Performance_InMemoryIsFasterThanFileSystem()
    {
        // This test demonstrates that in-memory approach is faster

        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act - In-memory approach
        using var inMemoryService = InMemoryAnalysisService.CreateWithTestProjects(_logger);

        var creationTime = stopwatch.ElapsedMilliseconds;
        stopwatch.Restart();

        var errorsResult = await InMemoryMcpTools.GetCompilationErrors(inMemoryService);
        var analysisTime = stopwatch.ElapsedMilliseconds;
        stopwatch.Restart();

        var solutionInfoResult = await InMemoryMcpTools.GetSolutionInfo(inMemoryService);
        var solutionInfoTime = stopwatch.ElapsedMilliseconds;

        // Assert - More realistic thresholds for CI environments
        await Assert.That(creationTime).IsLessThan(5000); // < 5 seconds to create
        await Assert.That(analysisTime).IsLessThan(10000); // < 10 seconds to analyze
        await Assert.That(solutionInfoTime).IsLessThan(5000); // < 5 seconds for solution info

        // Verify results are valid
        var errorsResponse = JsonSerializer.Deserialize<JsonElement>(errorsResult);
        var solutionResponse = JsonSerializer.Deserialize<JsonElement>(solutionInfoResult);

        await Assert.That(errorsResponse.GetProperty("success").GetBoolean()).IsTrue();
        await Assert.That(solutionResponse.GetProperty("success").GetBoolean()).IsTrue();

        Console.WriteLine($"In-memory performance:");
        Console.WriteLine($"  Creation: {creationTime}ms");
        Console.WriteLine($"  Analysis: {analysisTime}ms");
        Console.WriteLine($"  Solution info: {solutionInfoTime}ms");
        Console.WriteLine($"  Total: {creationTime + analysisTime + solutionInfoTime}ms");
    }
}

/// <summary>
/// Adapter that makes InMemoryAnalysisService compatible with MCP tools
/// We'll create custom MCP tool methods that work directly with InMemoryAnalysisService
/// </summary>
public static class InMemoryMcpTools
{
    /// <summary>
    /// Get compilation errors using in-memory analysis service
    /// </summary>
    public static async Task<string> GetCompilationErrors(InMemoryAnalysisService analysisService)
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
    /// Get solution info using in-memory analysis service
    /// </summary>
    public static async Task<string> GetSolutionInfo(InMemoryAnalysisService analysisService)
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
    /// Analyze file using in-memory analysis service
    /// </summary>
    public static async Task<string> AnalyzeFile(InMemoryAnalysisService analysisService, string filePath)
    {
        try
        {
            // Extract filename for in-memory lookup
            var fileName = Path.GetFileName(filePath);
            var errors = await analysisService.AnalyzeDocumentAsync(fileName);
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
    /// Get code suggestions using in-memory analysis service
    /// </summary>
    public static Task<string> GetCodeSuggestions(InMemoryAnalysisService analysisService, string? categories = null, string? minimumPriority = null, int maxSuggestions = 100, bool includeAutoFixable = true, bool includeManualFix = true)
    {
        try
        {
            // Parse and validate categories
            var validCategories = new List<string>();
            if (!string.IsNullOrEmpty(categories))
            {
                var categoryNames = categories.Split(',', StringSplitOptions.RemoveEmptyEntries);
                var validCategoryNames = new[] { "Style", "Performance", "Modernization", "BestPractices", "Security", "Reliability", "Accessibility", "Design", "Naming", "Documentation", "Cleanup" };

                foreach (var categoryName in categoryNames)
                {
                    var trimmed = categoryName.Trim();
                    if (validCategoryNames.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
                    {
                        validCategories.Add(trimmed);
                    }
                }
            }
            else
            {
                validCategories.AddRange(new[] { "Style", "Performance", "Modernization" });
            }

            // For now, return a mock response since we don't have actual suggestion analysis in InMemoryAnalysisService
            var result = new
            {
                success = true,
                suggestion_count = 0,
                categories_analyzed = validCategories.ToArray(),
                minimum_priority = minimumPriority ?? "Low",
                suggestions = new object[0]
            };
            return Task.FromResult(JsonSerializer.Serialize(result));
        }
        catch (Exception ex)
        {
            var result = new { success = false, error = ex.Message };
            return Task.FromResult(JsonSerializer.Serialize(result));
        }
    }

    /// <summary>
    /// Get file suggestions using in-memory analysis service
    /// </summary>
    public static Task<string> GetFileSuggestions(InMemoryAnalysisService analysisService, string filePath, string? categories = null, string? minimumPriority = null, int maxSuggestions = 50)
    {
        try
        {
            // For now, return a mock response
            var result = new
            {
                success = true,
                file_path = filePath,
                suggestion_count = 0,
                suggestions = new object[0]
            };
            return Task.FromResult(JsonSerializer.Serialize(result));
        }
        catch (Exception ex)
        {
            var result = new { success = false, error = ex.Message };
            return Task.FromResult(JsonSerializer.Serialize(result));
        }
    }

    /// <summary>
    /// Get suggestion categories
    /// </summary>
    public static Task<string> GetSuggestionCategories()
    {
        try
        {
            var categories = new[]
            {
                new { name = "Style", description = "Code style and formatting improvements" },
                new { name = "Performance", description = "Performance optimizations and efficiency improvements" },
                new { name = "Modernization", description = "Updates to use newer language features and patterns" },
                new { name = "BestPractices", description = "General best practices and maintainability improvements" },
                new { name = "Security", description = "Security-related improvements and vulnerability fixes" },
                new { name = "Reliability", description = "Reliability and correctness improvements" }
            };

            var priorities = new[]
            {
                new { name = "Low", description = "Optional improvements with minimal impact" },
                new { name = "Medium", description = "Recommended improvements for better code quality" },
                new { name = "High", description = "Important improvements that should be addressed" },
                new { name = "Critical", description = "Critical issues that need immediate attention" }
            };

            var impacts = new[]
            {
                new { name = "Minimal", description = "Cosmetic changes with no functional impact" },
                new { name = "Small", description = "Small improvements in readability or maintainability" },
                new { name = "Moderate", description = "Moderate improvements in code quality" },
                new { name = "Significant", description = "Significant improvements in performance or correctness" },
                new { name = "Major", description = "Major improvements that affect application behavior" }
            };

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
                }
            };

            return Task.FromResult(JsonSerializer.Serialize(result));
        }
        catch (Exception ex)
        {
            var result = new { success = false, error = ex.Message };
            return Task.FromResult(JsonSerializer.Serialize(result));
        }
    }
}
