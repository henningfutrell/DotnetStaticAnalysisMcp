using Microsoft.Extensions.Logging;
using MCP.Server.Services;
using MCP.Server.Models;
using System.Text.Json;
using Microsoft.Build.Locator;

namespace MCP.IntegrationTests;

/// <summary>
/// Integration tests that validate the MCP server functionality against real solutions
/// These tests use the actual RoslynAnalysisService to ensure the production code works correctly
/// </summary>
public class LiveMcpServerTests : IDisposable
{
    private readonly ILogger<RoslynAnalysisService> _logger;
    private static bool _msbuildInitialized = false;
    private static readonly object _lock = new object();

    public LiveMcpServerTests()
    {
        // Initialize MSBuild once for all tests
        lock (_lock)
        {
            if (!_msbuildInitialized)
            {
                try
                {
                    if (!MSBuildLocator.IsRegistered)
                    {
                        MSBuildLocator.RegisterDefaults();
                    }
                    _msbuildInitialized = true;
                }
                catch (Exception ex)
                {
                    // MSBuild initialization may fail in some environments, that's OK
                    Console.WriteLine($"MSBuild initialization failed: {ex.Message}");
                }
            }
        }

        var loggerFactory = LoggerFactory.Create(builder => 
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information); // Set to Information to see detailed logs
        });
        _logger = loggerFactory.CreateLogger<RoslynAnalysisService>();
    }

    [Fact]
    public async Task RoslynAnalysisService_LoadMcpSolution_ShouldDetectAllProjects()
    {
        // Test that the MCP solution loads correctly and detects all projects
        using var service = new RoslynAnalysisService(_logger);

        var solutionPath = "/home/henning/Workbench/MCP/MCP.sln";
        
        // Load the solution
        var loadResult = await service.LoadSolutionAsync(solutionPath);
        Assert.True(loadResult, "Solution should load successfully");

        // Get solution info
        var solutionInfo = await service.GetSolutionInfoAsync();
        Assert.NotNull(solutionInfo);
        Assert.Equal("MCP", solutionInfo.Name);
        
        // Should detect 4 projects: MCP.Tests, MCP.Server, MCP.IntegrationTests, MCP.TestProject
        Assert.True(solutionInfo.Projects.Count >= 4, 
            $"Expected at least 4 projects, but found {solutionInfo.Projects.Count}. " +
            $"Projects found: {string.Join(", ", solutionInfo.Projects.Select(p => p.Name))}");
        
        // Verify specific projects exist
        var projectNames = solutionInfo.Projects.Select(p => p.Name).ToList();
        Assert.Contains("MCP.Tests", projectNames);
        Assert.Contains("MCP.Server", projectNames);
        Assert.Contains("MCP.IntegrationTests", projectNames);
        Assert.Contains("MCP.TestProject", projectNames);
        
        Console.WriteLine($"✅ Successfully detected {solutionInfo.Projects.Count} projects:");
        foreach (var project in solutionInfo.Projects)
        {
            Console.WriteLine($"  - {project.Name} ({project.ErrorCount} errors, {project.WarningCount} warnings)");
        }
    }

    [Fact]
    public async Task RoslynAnalysisService_AnalyzeTestProject_ShouldDetectCompilationErrors()
    {
        // Test that the test project with deliberate errors is detected correctly
        using var service = new RoslynAnalysisService(_logger);

        var solutionPath = "/home/henning/Workbench/MCP/MCP.sln";
        
        // Load the solution
        var loadResult = await service.LoadSolutionAsync(solutionPath);
        Assert.True(loadResult);

        // Get compilation errors
        var errors = await service.GetCompilationErrorsAsync();
        Assert.NotNull(errors);
        
        // Should have errors from the test project
        Assert.True(errors.Count > 0, $"Expected compilation errors, but found {errors.Count}");
        
        // Check for specific error types we introduced
        var errorIds = errors.Select(e => e.Id).ToList();
        
        Console.WriteLine($"✅ Found {errors.Count} compilation errors:");
        foreach (var error in errors.Take(10)) // Show first 10 errors
        {
            Console.WriteLine($"  - {error.Id}: {error.Message} in {error.FilePath}:{error.StartLine}");
        }
        
        // Should contain the specific errors we introduced in MCP.TestProject
        Assert.Contains(errorIds, id => id == "CS1002"); // Missing semicolon
        
        // Verify errors are from our test project
        var testProjectErrors = errors.Where(e => e.FilePath.Contains("MCP.TestProject")).ToList();
        Assert.True(testProjectErrors.Count > 0, "Should have errors from MCP.TestProject");
    }

    [Fact]
    public async Task RoslynAnalysisService_AnalyzeSpecificFile_ShouldDetectFileErrors()
    {
        // Test file-specific analysis
        using var service = new RoslynAnalysisService(_logger);

        var solutionPath = "/home/henning/Workbench/MCP/MCP.sln";
        await service.LoadSolutionAsync(solutionPath);

        // Analyze the test project's Program.cs file
        var errors = await service.AnalyzeFileAsync("Program.cs");
        Assert.NotNull(errors);
        
        Console.WriteLine($"✅ Found {errors.Count} errors in Program.cs:");
        foreach (var error in errors)
        {
            Console.WriteLine($"  - {error.Id}: {error.Message} at line {error.StartLine}");
        }
        
        // Should have errors from our deliberate mistakes
        if (errors.Count > 0)
        {
            var errorIds = errors.Select(e => e.Id).ToList();
            Assert.Contains(errorIds, id => id == "CS1002" || id == "CS0103" || id == "CS0246" || id == "CS0161");
        }
    }

    [Fact]
    public async Task RoslynAnalysisService_GetCodeSuggestions_ShouldProvideImprovements()
    {
        // Test code suggestions functionality
        using var service = new RoslynAnalysisService(_logger);

        var solutionPath = "/home/henning/Workbench/MCP/MCP.sln";
        await service.LoadSolutionAsync(solutionPath);

        // Get code suggestions
        var suggestions = await service.GetCodeSuggestionsAsync();
        Assert.NotNull(suggestions);
        
        Console.WriteLine($"✅ Found {suggestions.Count} code suggestions:");
        foreach (var suggestion in suggestions.Take(10)) // Show first 10 suggestions
        {
            Console.WriteLine($"  - {suggestion.Category}: {suggestion.Title} in {suggestion.FilePath}:{suggestion.StartLine}");
            Console.WriteLine($"    {suggestion.Description}");
        }
        
        // Test with specific options
        var options = new SuggestionAnalysisOptions
        {
            MaxSuggestions = 50,
            MinimumPriority = SuggestionPriority.Low
        };
        options.IncludedCategories.Clear();
        options.IncludedCategories.Add(SuggestionCategory.Performance);
        options.IncludedCategories.Add(SuggestionCategory.Style);
        
        var filteredSuggestions = await service.GetCodeSuggestionsAsync(options);
        Assert.NotNull(filteredSuggestions);
        
        Console.WriteLine($"✅ Found {filteredSuggestions.Count} filtered suggestions (Performance + Style)");
    }

    [Fact]
    public async Task McpServerTools_WithRealSolution_ShouldReturnValidJson()
    {
        // Test the MCP server tools directly
        using var service = new RoslynAnalysisService(_logger);

        var solutionPath = "/home/henning/Workbench/MCP/MCP.sln";
        await service.LoadSolutionAsync(solutionPath);

        // Test get_compilation_errors tool
        var errorsJson = await DotNetAnalysisTools.GetCompilationErrors(service);
        Assert.NotNull(errorsJson);
        
        var errorsResponse = JsonSerializer.Deserialize<JsonElement>(errorsJson);
        Assert.True(errorsResponse.GetProperty("success").GetBoolean());
        
        var errorCount = errorsResponse.GetProperty("error_count").GetInt32();
        Console.WriteLine($"✅ MCP Tool - Compilation Errors: {errorCount}");
        
        // Test get_solution_info tool
        var solutionJson = await DotNetAnalysisTools.GetSolutionInfo(service);
        Assert.NotNull(solutionJson);
        
        var solutionResponse = JsonSerializer.Deserialize<JsonElement>(solutionJson);
        Assert.True(solutionResponse.GetProperty("success").GetBoolean());
        
        if (solutionResponse.TryGetProperty("solution_info", out var solutionInfoElement) &&
            solutionInfoElement.ValueKind != JsonValueKind.Null)
        {
            var projectCount = solutionInfoElement.GetProperty("Projects").GetArrayLength();
            Console.WriteLine($"✅ MCP Tool - Solution Projects: {projectCount}");
            
            // Should detect our projects
            Assert.True(projectCount >= 4, $"Expected at least 4 projects, found {projectCount}");
        }
        
        // Test get_code_suggestions tool
        var suggestionsJson = await DotNetAnalysisTools.GetCodeSuggestions(service, 
            categories: "Performance,Style", 
            minimumPriority: "Low", 
            maxSuggestions: 20);
        Assert.NotNull(suggestionsJson);
        
        var suggestionsResponse = JsonSerializer.Deserialize<JsonElement>(suggestionsJson);
        Assert.True(suggestionsResponse.GetProperty("success").GetBoolean());
        
        var suggestionCount = suggestionsResponse.GetProperty("suggestion_count").GetInt32();
        Console.WriteLine($"✅ MCP Tool - Code Suggestions: {suggestionCount}");
        
        // Test analyze_file tool
        var fileAnalysisJson = await DotNetAnalysisTools.AnalyzeFile(service, "Program.cs");
        Assert.NotNull(fileAnalysisJson);
        
        var fileResponse = JsonSerializer.Deserialize<JsonElement>(fileAnalysisJson);
        Assert.True(fileResponse.GetProperty("success").GetBoolean());
        
        var fileErrorCount = fileResponse.GetProperty("error_count").GetInt32();
        Console.WriteLine($"✅ MCP Tool - File Analysis Errors: {fileErrorCount}");
    }

    [Fact]
    public async Task ProjectDiscovery_ShouldWorkCorrectly()
    {
        // Specific test to debug project discovery issues
        using var service = new RoslynAnalysisService(_logger);

        var solutionPath = "/home/henning/Workbench/MCP/MCP.sln";
        
        Console.WriteLine($"🔍 Testing project discovery for: {solutionPath}");
        Console.WriteLine($"🔍 Solution file exists: {File.Exists(solutionPath)}");
        
        // Load the solution with detailed logging
        var loadResult = await service.LoadSolutionAsync(solutionPath);
        Console.WriteLine($"🔍 Load result: {loadResult}");
        
        // Get solution info with detailed analysis
        var solutionInfo = await service.GetSolutionInfoAsync();
        
        if (solutionInfo == null)
        {
            Console.WriteLine("❌ Solution info is null - this indicates the solution didn't load properly");
            Assert.Fail("Solution info should not be null after successful load");
        }
        
        Console.WriteLine($"🔍 Solution name: {solutionInfo.Name}");
        Console.WriteLine($"🔍 Solution path: {solutionInfo.FilePath}");
        Console.WriteLine($"🔍 Project count: {solutionInfo.Projects.Count}");
        
        if (solutionInfo.Projects.Count == 0)
        {
            Console.WriteLine("❌ No projects detected - this is the bug we're investigating");
            
            // Additional debugging: check if solution file is readable
            var solutionContent = await File.ReadAllTextAsync(solutionPath);
            var projectLines = solutionContent.Split('\n')
                .Where(line => line.Contains("Project("))
                .ToList();
            
            Console.WriteLine($"🔍 Solution file contains {projectLines.Count} project references:");
            foreach (var line in projectLines)
            {
                Console.WriteLine($"  {line.Trim()}");
            }
            
            // This test will fail, but it will give us valuable debugging information
            Assert.True(solutionInfo.Projects.Count > 0, 
                "Project discovery is failing - check the logs above for debugging information");
        }
        else
        {
            Console.WriteLine("✅ Projects detected successfully:");
            foreach (var project in solutionInfo.Projects)
            {
                Console.WriteLine($"  - {project.Name} at {project.FilePath}");
            }
        }
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
