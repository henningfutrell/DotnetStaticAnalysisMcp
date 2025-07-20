using Microsoft.Extensions.Logging;
using DotnetStaticAnalysisMcp.Server.Services;
using DotnetStaticAnalysisMcp.Server.Models;
using System.Text.Json;
using Xunit;
using System.Xml.Linq;

namespace MCP.Tests;

/// <summary>
/// Focused tests to improve coverage of specific uncovered methods
/// </summary>
public class CoverageImprovementTests
{
    private readonly ILogger<CodeCoverageService> _logger;
    private readonly ILogger<RoslynAnalysisService> _analysisLogger;
    private readonly ILogger<TelemetryService> _telemetryLogger;

    public CoverageImprovementTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<CodeCoverageService>();
        _analysisLogger = loggerFactory.CreateLogger<RoslynAnalysisService>();
        _telemetryLogger = loggerFactory.CreateLogger<TelemetryService>();
    }

    [Fact]
    public void ParseTestResults_WithValidOutput_ParsesCorrectly()
    {
        // Arrange
        var analysisService = new RoslynAnalysisService(_analysisLogger);
        var telemetryService = new TelemetryService(_telemetryLogger);
        var coverageService = new CodeCoverageService(_logger, analysisService, telemetryService);

        var testOutput = new List<string>
        {
            "Starting test execution, please wait...",
            "Total tests: 18",
            "  Passed:   15",
            "  Failed:   2", 
            "  Skipped:  1",
            "Test Run Successful. Total: 18, Passed: 15, Failed: 2, Skipped: 1 - 00:00:05.123"
        };

        // Use reflection to access the private method
        var method = typeof(CodeCoverageService).GetMethod("ParseTestResults", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        try
        {
            // Act
            var result = (TestExecutionSummary?)method?.Invoke(coverageService, new object[] { testOutput });

            // Assert
            Assert.NotNull(result);
            Assert.Equal(18, result.TotalTests);
            Assert.Equal(15, result.PassedTests);
            Assert.Equal(2, result.FailedTests);
            Assert.Equal(1, result.SkippedTests);
            Assert.Equal(TimeSpan.FromSeconds(5.123), result.ExecutionTime);
        }
        finally
        {
            analysisService.Dispose();
        }
    }

    [Fact]
    public void ParseLine_WithValidXml_ReturnsLineCoverage()
    {
        // Arrange
        var analysisService = new RoslynAnalysisService(_analysisLogger);
        var telemetryService = new TelemetryService(_telemetryLogger);
        var coverageService = new CodeCoverageService(_logger, analysisService, telemetryService);

        var lineXml = XElement.Parse(@"<line number=""10"" hits=""5"" branch=""false""/>");
        var uncoveredLineXml = XElement.Parse(@"<line number=""11"" hits=""0"" branch=""true""/>");

        // Use reflection to access the private method
        var method = typeof(CodeCoverageService).GetMethod("ParseLine", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        try
        {
            // Act
            var coveredResult = (LineCoverage?)method?.Invoke(coverageService, new object[] { lineXml });
            var uncoveredResult = (LineCoverage?)method?.Invoke(coverageService, new object[] { uncoveredLineXml });

            // Assert
            Assert.NotNull(coveredResult);
            Assert.Equal(10, coveredResult.LineNumber);
            Assert.Equal(5, coveredResult.HitCount);
            Assert.Equal(CoverageStatus.Covered, coveredResult.Status);
            Assert.True(coveredResult.IsCovered);

            Assert.NotNull(uncoveredResult);
            Assert.Equal(11, uncoveredResult.LineNumber);
            Assert.Equal(0, uncoveredResult.HitCount);
            Assert.Equal(CoverageStatus.Uncovered, uncoveredResult.Status);
            Assert.False(uncoveredResult.IsCovered);
        }
        finally
        {
            analysisService.Dispose();
        }
    }

    [Fact]
    public void ParseMethod_WithValidXml_ReturnsMethodCoverage()
    {
        // Arrange
        var analysisService = new RoslynAnalysisService(_analysisLogger);
        var telemetryService = new TelemetryService(_telemetryLogger);
        var coverageService = new CodeCoverageService(_logger, analysisService, telemetryService);

        var methodXml = XElement.Parse(@"
<method name=""Add"" signature=""(int,int)"" line-rate=""1.0"" branch-rate=""1.0"">
  <lines>
    <line number=""5"" hits=""10"" branch=""false""/>
    <line number=""6"" hits=""10"" branch=""false""/>
    <line number=""7"" hits=""8"" branch=""true""/>
  </lines>
</method>");

        // Use reflection to access the private method
        var method = typeof(CodeCoverageService).GetMethod("ParseMethod", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        try
        {
            // Act
            var result = (MethodCoverage?)method?.Invoke(coverageService, new object[] { methodXml });

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Add", result.MethodName);
            Assert.Equal("(int,int)", result.Signature);
            Assert.Equal(100.0, result.Summary.LinesCoveredPercentage);
            Assert.Equal(100.0, result.Summary.BranchesCoveredPercentage);
            Assert.Equal(3, result.Lines.Count);

            var firstLine = result.Lines[0];
            Assert.Equal(5, firstLine.LineNumber);
            Assert.Equal(10, firstLine.HitCount);
            Assert.Equal(CoverageStatus.Covered, firstLine.Status);
        }
        finally
        {
            analysisService.Dispose();
        }
    }

    [Fact]
    public void ParseClass_WithValidXml_ReturnsClassCoverage()
    {
        // Arrange
        var analysisService = new RoslynAnalysisService(_analysisLogger);
        var telemetryService = new TelemetryService(_telemetryLogger);
        var coverageService = new CodeCoverageService(_logger, analysisService, telemetryService);

        var classXml = XElement.Parse(@"
<class name=""Calculator"" filename=""Calculator.cs"" line-rate=""0.85"" branch-rate=""0.75"">
  <methods>
    <method name=""Add"" signature=""(int,int)"" line-rate=""1.0"" branch-rate=""1.0"">
      <lines>
        <line number=""5"" hits=""10"" branch=""false""/>
        <line number=""6"" hits=""10"" branch=""false""/>
      </lines>
    </method>
  </methods>
</class>");

        // Use reflection to access the private method
        var method = typeof(CodeCoverageService).GetMethod("ParseClass", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        try
        {
            // Act
            var result = (ClassCoverage?)method?.Invoke(coverageService, new object[] { classXml });

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Calculator", result.ClassName);
            Assert.Equal("Calculator.cs", result.FilePath);
            Assert.Equal(85.0, result.Summary.LinesCoveredPercentage);
            Assert.Equal(75.0, result.Summary.BranchesCoveredPercentage);
            Assert.Single(result.Methods);

            var addMethod = result.Methods[0];
            Assert.Equal("Add", addMethod.MethodName);
            Assert.Equal("(int,int)", addMethod.Signature);
        }
        finally
        {
            analysisService.Dispose();
        }
    }

    [Fact]
    public void CalculateOverallSummary_WithMultipleProjects_CalculatesCorrectly()
    {
        // Arrange
        var analysisService = new RoslynAnalysisService(_analysisLogger);
        var telemetryService = new TelemetryService(_telemetryLogger);
        var coverageService = new CodeCoverageService(_logger, analysisService, telemetryService);

        var result = new CoverageAnalysisResult();
        
        // Add test projects with known coverage
        result.Projects.Add(new ProjectCoverage
        {
            ProjectName = "Project1",
            Summary = new CoverageSummary
            {
                TotalLines = 100,
                CoveredLines = 80,
                TotalMethods = 20,
                CoveredMethods = 15,
                TotalClasses = 5,
                CoveredClasses = 4,
                TotalBranches = 30,
                CoveredBranches = 20
            }
        });
        
        result.Projects.Add(new ProjectCoverage
        {
            ProjectName = "Project2", 
            Summary = new CoverageSummary
            {
                TotalLines = 200,
                CoveredLines = 120,
                TotalMethods = 40,
                CoveredMethods = 25,
                TotalClasses = 10,
                CoveredClasses = 7,
                TotalBranches = 60,
                CoveredBranches = 35
            }
        });

        // Use reflection to access the private method
        var method = typeof(CodeCoverageService).GetMethod("CalculateOverallSummary", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        try
        {
            // Act
            method!.Invoke(coverageService, new object[] { result });

            // Assert
            var summary = result.Summary;
            Assert.Equal(300, summary.TotalLines);
            Assert.Equal(200, summary.CoveredLines);
            Assert.Equal(100, summary.UncoveredLines);
            Assert.Equal(66.67, Math.Round(summary.LinesCoveredPercentage, 2));
            
            Assert.Equal(60, summary.TotalMethods);
            Assert.Equal(40, summary.CoveredMethods);
            Assert.Equal(20, summary.UncoveredMethods);
            Assert.Equal(66.67, Math.Round(summary.MethodsCoveredPercentage, 2));
        }
        finally
        {
            analysisService.Dispose();
        }
    }

    [Fact]
    public async Task CodeCoverageService_GetMethodCoverageAsync_WithValidParameters_ReturnsResult()
    {
        // Arrange
        var analysisService = new RoslynAnalysisService(_analysisLogger);
        var telemetryService = new TelemetryService(_telemetryLogger);
        var coverageService = new CodeCoverageService(_logger, analysisService, telemetryService);

        try
        {
            // Act
            var result = await coverageService.GetMethodCoverageAsync("TestClass", "TestMethod");

            // Assert
            Assert.NotNull(result);
        }
        finally
        {
            analysisService.Dispose();
        }
    }

    [Fact]
    public async Task CodeCoverageService_CompareCoverageAsync_WithValidBaseline_ReturnsComparison()
    {
        // Arrange
        var analysisService = new RoslynAnalysisService(_analysisLogger);
        var telemetryService = new TelemetryService(_telemetryLogger);
        var coverageService = new CodeCoverageService(_logger, analysisService, telemetryService);

        // Create a temporary baseline file
        var tempFile = Path.GetTempFileName();
        var baselineData = new CoverageAnalysisResult
        {
            Summary = new CoverageSummary
            {
                LinesCoveredPercentage = 50.0,
                TotalLines = 100,
                CoveredLines = 50
            }
        };
        await File.WriteAllTextAsync(tempFile, JsonSerializer.Serialize(baselineData));

        try
        {
            // Act
            var result = await coverageService.CompareCoverageAsync(baselineData);

            // Assert
            Assert.NotNull(result);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
                File.Delete(tempFile);
            analysisService.Dispose();
        }
    }

    [Fact]
    public async Task DotNetAnalysisTools_GetServerVersion_ReturnsValidJson()
    {
        // Act
        var result = await DotNetAnalysisTools.GetServerVersion();

        // Assert
        Assert.NotNull(result);
        var jsonDoc = JsonDocument.Parse(result);
        Assert.True(jsonDoc.RootElement.TryGetProperty("version", out var versionProp));
        Assert.True(jsonDoc.RootElement.TryGetProperty("build_timestamp", out var timestampProp));
        Assert.Contains("v1.1.0", versionProp.GetString());
    }

    [Fact]
    public async Task DotNetAnalysisTools_GetBasicDiagnostics_ReturnsValidJson()
    {
        // Act
        var result = await DotNetAnalysisTools.GetBasicDiagnostics();

        // Assert
        Assert.NotNull(result);
        var jsonDoc = JsonDocument.Parse(result);
        Assert.True(jsonDoc.RootElement.TryGetProperty("environment", out _));
        Assert.True(jsonDoc.RootElement.TryGetProperty("runtime", out _));
        Assert.True(jsonDoc.RootElement.TryGetProperty("memory", out _));
    }

    [Fact]
    public async Task DotNetAnalysisTools_GetDiagnostics_WithLogs_ReturnsValidJson()
    {
        // Act
        var result = await DotNetAnalysisTools.GetDiagnostics(includeLogs: true);

        // Assert
        Assert.NotNull(result);
        var jsonDoc = JsonDocument.Parse(result);
        Assert.True(jsonDoc.RootElement.TryGetProperty("environment", out _));
        Assert.True(jsonDoc.RootElement.TryGetProperty("logs", out _));
    }

    [Fact]
    public async Task DotNetAnalysisTools_GetDiagnostics_WithoutLogs_ReturnsValidJson()
    {
        // Act
        var result = await DotNetAnalysisTools.GetDiagnostics(includeLogs: false);

        // Assert
        Assert.NotNull(result);
        var jsonDoc = JsonDocument.Parse(result);
        Assert.True(jsonDoc.RootElement.TryGetProperty("environment", out _));
        // Should not include logs when includeLogs is false
    }

    [Fact]
    public void Environment_Variables_CanBeAccessed()
    {
        // Act - Test environment access like in Program.cs
        var processId = Environment.ProcessId;
        var machineName = Environment.MachineName;
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // Assert
        Assert.True(processId > 0);
        Assert.NotNull(machineName);
        Assert.NotNull(userProfile);
    }

    [Fact]
    public void Path_Operations_Work()
    {
        // Arrange & Act - Test path operations like in Program.cs
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var logDirectory = Path.Combine(userProfile, ".mcp", "logs");
        var logFile = Path.Combine(logDirectory, "dotnet-analysis.log");

        // Assert
        Assert.NotNull(userProfile);
        Assert.NotNull(logDirectory);
        Assert.NotNull(logFile);
        Assert.Contains(".mcp", logDirectory);
        Assert.Contains("dotnet-analysis.log", logFile);
    }
}
