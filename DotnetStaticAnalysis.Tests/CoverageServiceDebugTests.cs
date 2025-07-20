using DotnetStaticAnalysisMcp.Server.Services;
using DotnetStaticAnalysisMcp.Server.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.Reflection;

namespace MCP.Tests;

/// <summary>
/// Debug tests to understand why coverage analysis is not working
/// </summary>
public class CoverageServiceDebugTests
{
    private readonly Mock<ILogger<CodeCoverageService>> _mockLogger;
    private readonly Mock<ILogger<RoslynAnalysisService>> _mockAnalysisLogger;
    private readonly Mock<ILogger<TelemetryService>> _mockTelemetryLogger;
    private readonly CodeCoverageService _coverageService;
    private readonly RoslynAnalysisService _analysisService;
    private readonly TelemetryService _telemetryService;

    public CoverageServiceDebugTests()
    {
        _mockLogger = new Mock<ILogger<CodeCoverageService>>();
        _mockAnalysisLogger = new Mock<ILogger<RoslynAnalysisService>>();
        _mockTelemetryLogger = new Mock<ILogger<TelemetryService>>();
        
        _analysisService = new RoslynAnalysisService(_mockAnalysisLogger.Object);
        _telemetryService = new TelemetryService(_mockTelemetryLogger.Object);
        _coverageService = new CodeCoverageService(_mockLogger.Object, _analysisService, _telemetryService);
    }

    [Fact]
    public async Task DebugTestProjectDiscovery()
    {
        // Set the solution path to the actual MCP solution
        var solutionPath = "/home/henning/Workbench/MCP/DotnetStaticAnalysisMcp.sln";
        _coverageService.SetSolutionPath(solutionPath);

        // Use reflection to call the private GetTestProjectsAsync method
        var method = typeof(CodeCoverageService).GetMethod("GetTestProjectsAsync", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        Assert.NotNull(method);

        var options = new CoverageAnalysisOptions();
        var operationId = Guid.NewGuid().ToString();

        // Call the private method
        var result = await (Task<List<string>>)method.Invoke(_coverageService, new object[] { options, operationId })!;

        // Debug output
        Console.WriteLine($"Found {result?.Count ?? 0} test projects:");
        if (result != null)
        {
            foreach (var project in result)
            {
                Console.WriteLine($"  - {project}");
            }
        }

        // We should find at least 2 test projects
        Assert.True(result?.Count >= 2, $"Expected at least 2 test projects, but found {result?.Count ?? 0}");

        // Check that our known test projects are found
        var testProjectNames = result?.Select(p => Path.GetFileNameWithoutExtension(p)).ToList() ?? new List<string>();
        Assert.Contains("DotnetStaticAnalysis.Tests", testProjectNames);
        Assert.Contains("DotnetStaticAnalysisMcp.IntegrationTests", testProjectNames);
    }

    [Fact]
    public async Task DebugIsTestProjectMethod()
    {
        // Test the IsTestProjectAsync method directly
        var method = typeof(CodeCoverageService).GetMethod("IsTestProjectAsync", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        Assert.NotNull(method);

        var operationId = Guid.NewGuid().ToString();

        // Test our known test projects
        var testProjects = new[]
        {
            "/home/henning/Workbench/MCP/DotnetStaticAnalysis.Tests/DotnetStaticAnalysis.Tests.csproj",
            "/home/henning/Workbench/MCP/DotnetStaticAnalysisMcp.IntegrationTests/DotnetStaticAnalysisMcp.IntegrationTests.csproj"
        };

        foreach (var projectPath in testProjects)
        {
            if (File.Exists(projectPath))
            {
                var result = await (Task<bool>)method.Invoke(_coverageService, new object[] { projectPath, operationId })!;
                Console.WriteLine($"IsTestProject({Path.GetFileName(projectPath)}) = {result}");
                Assert.True(result, $"Project {projectPath} should be detected as a test project");
            }
            else
            {
                Console.WriteLine($"Project file does not exist: {projectPath}");
            }
        }

        // Test the main server project (should NOT be a test project)
        var serverProject = "/home/henning/Workbench/MCP/DotnetStaticAnalysisMcp.Server/DotnetStaticAnalysisMcp.Server.csproj";
        if (File.Exists(serverProject))
        {
            var result = await (Task<bool>)method.Invoke(_coverageService, new object[] { serverProject, operationId })!;
            Console.WriteLine($"IsTestProject({Path.GetFileName(serverProject)}) = {result}");
            Assert.False(result, $"Project {serverProject} should NOT be detected as a test project");
        }
    }

    [Fact]
    public async Task DebugCoverageAnalysisStep()
    {
        // Set the solution path
        var solutionPath = "/home/henning/Workbench/MCP/DotnetStaticAnalysisMcp.sln";
        _coverageService.SetSolutionPath(solutionPath);

        // Try to run coverage analysis with detailed logging
        var options = new CoverageAnalysisOptions
        {
            TimeoutMinutes = 2, // Short timeout for debugging
            CollectBranchCoverage = true
        };

        var result = await _coverageService.RunCoverageAnalysisAsync(options);

        Console.WriteLine($"Coverage analysis result:");
        Console.WriteLine($"  Success: {result.Success}");
        Console.WriteLine($"  Error: {result.ErrorMessage}");
        Console.WriteLine($"  Projects found: {result.Projects.Count}");
        Console.WriteLine($"  Test results: {result.TestResults.TotalTests} total tests");

        // Even if it fails, we want to understand why
        if (!result.Success)
        {
            Console.WriteLine($"Analysis failed with error: {result.ErrorMessage}");
        }
    }

    [Fact]
    public void DebugSolutionPathSetting()
    {
        var solutionPath = "/home/henning/Workbench/MCP/DotnetStaticAnalysisMcp.sln";
        
        // Verify the solution file exists
        Assert.True(File.Exists(solutionPath), $"Solution file should exist at {solutionPath}");
        
        // Set the solution path
        _coverageService.SetSolutionPath(solutionPath);
        
        // Use reflection to check the private field
        var field = typeof(CodeCoverageService).GetField("_currentSolutionPath",
            BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(field);
        var currentPath = (string?)field.GetValue(_coverageService);
        
        Console.WriteLine($"Current solution path set to: {currentPath}");
        Assert.Equal(solutionPath, currentPath);
        
        // Check solution directory
        var solutionDir = Path.GetDirectoryName(solutionPath);
        Console.WriteLine($"Solution directory: {solutionDir}");
        Assert.True(Directory.Exists(solutionDir), $"Solution directory should exist: {solutionDir}");
        
        // List all .csproj files in the solution directory
        var projectFiles = Directory.GetFiles(solutionDir, "*.csproj", SearchOption.AllDirectories);
        Console.WriteLine($"Found {projectFiles.Length} project files:");
        foreach (var file in projectFiles)
        {
            Console.WriteLine($"  - {file}");
        }
        
        Assert.True(projectFiles.Length >= 3, $"Should find at least 3 project files, found {projectFiles.Length}");
    }

    [Fact]
    public async Task DebugRunCoverageForSingleProject()
    {
        // Test running coverage for a single test project
        var testProjectPath = "/home/henning/Workbench/MCP/DotnetStaticAnalysis.Tests/DotnetStaticAnalysis.Tests.csproj";
        
        if (!File.Exists(testProjectPath))
        {
            Console.WriteLine($"Test project not found: {testProjectPath}");
            return;
        }

        // Use reflection to call the private RunCoverageForProjectAsync method
        var method = typeof(CodeCoverageService).GetMethod("RunCoverageForProjectAsync", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        Assert.NotNull(method);

        var options = new CoverageAnalysisOptions
        {
            TimeoutMinutes = 5,
            CollectBranchCoverage = true
        };

        // Call the private method
        var result = await (Task<dynamic>)method.Invoke(_coverageService, new object[] { testProjectPath, options })!;

        Console.WriteLine($"Single project coverage result:");
        Console.WriteLine($"  Success: {result.Success}");
        Console.WriteLine($"  Error: {result.ErrorMessage}");
        Console.WriteLine($"  Coverage files: {result.CoverageFiles?.Count ?? 0}");
        Console.WriteLine($"  Total tests: {result.TestSummary?.TotalTests ?? 0}");

        if (!result.Success)
        {
            Console.WriteLine($"Single project analysis failed: {result.ErrorMessage}");
        }
    }
}
