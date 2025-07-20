using Microsoft.Build.Locator;
using Microsoft.Extensions.Logging;
using DotnetStaticAnalysisMcp.Server.Services;

namespace MCP.Tests;

/// <summary>
/// Test setup and utilities for MCP server tests
/// </summary>
public static class TestSetup
{
    private static bool _msbuildInitialized = false;
    private static readonly System.Threading.Lock _lock = new();

    /// <summary>
    /// Check if MSBuild has been initialized
    /// </summary>
    public static bool IsInitialized()
    {
        using (_lock.EnterScope())
        {
            return _msbuildInitialized;
        }
    }

    /// <summary>
    /// Initialize MSBuild for testing
    /// </summary>
    public static void InitializeMSBuild()
    {
        using (_lock.EnterScope())
        {
            if (!_msbuildInitialized)
            {
                try
                {
                    // Register MSBuild defaults
                    if (!MSBuildLocator.IsRegistered)
                    {
                        MSBuildLocator.RegisterDefaults();
                    }
                    _msbuildInitialized = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to initialize MSBuild: {ex.Message}");
                    // Continue anyway - some tests might still work
                }
            }
        }
    }

    /// <summary>
    /// Create a properly configured RoslynAnalysisService for testing
    /// </summary>
    public static RoslynAnalysisService CreateAnalysisService()
    {
        InitializeMSBuild();
        
        var loggerFactory = LoggerFactory.Create(builder => 
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning); // Reduce noise in tests
        });
        
        var logger = loggerFactory.CreateLogger<RoslynAnalysisService>();
        return new RoslynAnalysisService(logger);
    }

    /// <summary>
    /// Get the path to the test solution
    /// </summary>
    public static string GetTestSolutionPath()
    {
        // Try multiple possible locations
        var possiblePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "TestData", "TestSolution", "TestSolution.sln"),
            Path.Combine(Directory.GetCurrentDirectory(), "TestData", "TestSolution", "TestSolution.sln"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "TestSolution", "TestSolution.sln")
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                Console.WriteLine($"Found test solution at: {path}");
                return path;
            }
        }

        throw new FileNotFoundException($"Test solution not found. Searched paths: {string.Join(", ", possiblePaths)}");
    }

    /// <summary>
    /// Get the path to a test file
    /// </summary>
    public static string GetTestFilePath(string relativePath)
    {
        var solutionPath = GetTestSolutionPath();
        var solutionDir = Path.GetDirectoryName(solutionPath)!;
        return Path.Combine(solutionDir, relativePath);
    }

    /// <summary>
    /// Verify test solution exists and is valid
    /// </summary>
    public static bool VerifyTestSolution()
    {
        try
        {
            var solutionPath = GetTestSolutionPath();
            
            // Check if solution file exists
            if (!File.Exists(solutionPath))
            {
                Console.WriteLine($"Solution file not found: {solutionPath}");
                return false;
            }

            // Check if project files exist
            var solutionDir = Path.GetDirectoryName(solutionPath)!;
            var testProjectPath = Path.Combine(solutionDir, "TestProject", "TestProject.csproj");
            var testLibraryPath = Path.Combine(solutionDir, "TestLibrary", "TestLibrary.csproj");

            if (!File.Exists(testProjectPath))
            {
                Console.WriteLine($"TestProject not found: {testProjectPath}");
                return false;
            }

            if (!File.Exists(testLibraryPath))
            {
                Console.WriteLine($"TestLibrary not found: {testLibraryPath}");
                return false;
            }

            Console.WriteLine("Test solution verification passed");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test solution verification failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Create a test that loads the solution and returns analysis results
    /// </summary>
    public static async Task<(RoslynAnalysisService service, bool loaded)> LoadTestSolutionAsync()
    {
        var service = CreateAnalysisService();
        
        try
        {
            var solutionPath = GetTestSolutionPath();
            var loaded = await service.LoadSolutionAsync(solutionPath);
            
            if (!loaded)
            {
                Console.WriteLine("Failed to load test solution");
            }
            
            return (service, loaded);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception loading test solution: {ex.Message}");
            return (service, false);
        }
    }
}
