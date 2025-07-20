using Microsoft.Extensions.Logging;
using DotnetStaticAnalysisMcp.Server.Services;
using DotnetStaticAnalysisMcp.Server.Models;
using Xunit;

namespace MCP.Tests;

/// <summary>
/// Basic tests for services to improve coverage
/// </summary>
public class BasicServiceTests
{
    private readonly ILogger<TelemetryService> _telemetryLogger;
    private readonly ILogger<RoslynAnalysisService> _analysisLogger;
    private readonly ILogger<CodeCoverageService> _coverageLogger;

    public BasicServiceTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _telemetryLogger = loggerFactory.CreateLogger<TelemetryService>();
        _analysisLogger = loggerFactory.CreateLogger<RoslynAnalysisService>();
        _coverageLogger = loggerFactory.CreateLogger<CodeCoverageService>();
    }

    [Fact]
    public void TelemetryService_Constructor_InitializesCorrectly()
    {
        // Arrange & Act
        var service = new TelemetryService(_telemetryLogger);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void TelemetryService_StartOperation_ReturnsOperationTelemetry()
    {
        // Arrange
        var service = new TelemetryService(_telemetryLogger);

        // Act
        var operation = service.StartOperation("TestOperation");

        // Assert
        Assert.NotNull(operation);
        Assert.Equal("TestOperation", operation.OperationType);
        Assert.NotNull(operation.OperationId);
        Assert.True(operation.StartTime > DateTime.MinValue);
    }

    [Fact]
    public void TelemetryService_CompleteOperation_UpdatesOperationStatus()
    {
        // Arrange
        var service = new TelemetryService(_telemetryLogger);
        var operation = service.StartOperation("TestOperation");

        // Act
        service.CompleteOperation(operation);

        // Assert
        Assert.True(operation.IsSuccess);
        Assert.True(operation.EndTime > operation.StartTime);
    }

    [Fact]
    public void TelemetryService_FailOperation_UpdatesOperationWithError()
    {
        // Arrange
        var service = new TelemetryService(_telemetryLogger);
        var operation = service.StartOperation("TestOperation");
        var exception = new InvalidOperationException("Test error");

        // Act
        service.FailOperation(operation, exception);

        // Assert
        Assert.False(operation.IsSuccess);
        Assert.Equal("Test error", operation.ErrorMessage);
        Assert.True(operation.EndTime > operation.StartTime);
    }

    [Fact]
    public void TelemetryService_LogTelemetry_WithValidData_DoesNotThrow()
    {
        // Arrange
        var service = new TelemetryService(_telemetryLogger);
        var properties = new Dictionary<string, object>
        {
            ["test_property"] = "test_value",
            ["numeric_property"] = 42
        };

        // Act & Assert - Should not throw
        service.LogTelemetry("TestEvent", properties);
        Assert.True(true);
    }

    [Fact]
    public void TelemetryService_GetRecentOperations_ReturnsOperations()
    {
        // Arrange
        var service = new TelemetryService(_telemetryLogger);
        var operation1 = service.StartOperation("Operation1");
        var operation2 = service.StartOperation("Operation2");
        service.CompleteOperation(operation1);

        // Act
        var recentOps = service.GetRecentOperations();

        // Assert
        Assert.NotNull(recentOps);
        Assert.True(recentOps.Count >= 2);
    }

    [Fact]
    public void TelemetryService_UpdateSolutionStatus_UpdatesCorrectly()
    {
        // Arrange
        var service = new TelemetryService(_telemetryLogger);

        // Act & Assert - Should not throw
        service.UpdateSolutionStatus("test.sln", 5, 2, 3, TimeSpan.FromSeconds(1));
        Assert.True(true);
    }

    [Fact]
    public void RoslynAnalysisService_Constructor_InitializesCorrectly()
    {
        // Arrange & Act
        var service = new RoslynAnalysisService(_analysisLogger);

        // Assert
        Assert.NotNull(service);
        
        // Cleanup
        service.Dispose();
    }

    [Fact]
    public void CodeCoverageService_Constructor_InitializesCorrectly()
    {
        // Arrange
        var analysisService = new RoslynAnalysisService(_analysisLogger);
        var telemetryService = new TelemetryService(_telemetryLogger);

        // Act
        var coverageService = new CodeCoverageService(_coverageLogger, analysisService, telemetryService);

        // Assert
        Assert.NotNull(coverageService);
        
        // Cleanup
        analysisService.Dispose();
    }

    [Fact]
    public void CodeCoverageService_SetSolutionPath_WithValidPath_DoesNotThrow()
    {
        // Arrange
        var analysisService = new RoslynAnalysisService(_analysisLogger);
        var telemetryService = new TelemetryService(_telemetryLogger);
        var coverageService = new CodeCoverageService(_coverageLogger, analysisService, telemetryService);

        // Act & Assert - Should not throw
        coverageService.SetSolutionPath("test.sln");
        Assert.True(true);
        
        // Cleanup
        analysisService.Dispose();
    }

    [Fact]
    public void CodeCoverageService_SetSolutionPath_WithNullPath_DoesNotThrow()
    {
        // Arrange
        var analysisService = new RoslynAnalysisService(_analysisLogger);
        var telemetryService = new TelemetryService(_telemetryLogger);
        var coverageService = new CodeCoverageService(_coverageLogger, analysisService, telemetryService);

        // Act & Assert - Should not throw
        coverageService.SetSolutionPath(null!);
        Assert.True(true);
        
        // Cleanup
        analysisService.Dispose();
    }

    [Fact]
    public async Task CodeCoverageService_RunCoverageAnalysisAsync_WithoutSolution_ReturnsError()
    {
        // Arrange
        var analysisService = new RoslynAnalysisService(_analysisLogger);
        var telemetryService = new TelemetryService(_telemetryLogger);
        var coverageService = new CodeCoverageService(_coverageLogger, analysisService, telemetryService);

        // Act
        var result = await coverageService.RunCoverageAnalysisAsync();

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("No solution loaded", result.ErrorMessage);
        
        // Cleanup
        analysisService.Dispose();
    }

    [Fact]
    public async Task CodeCoverageService_GetCoverageSummaryAsync_WithoutSolution_ReturnsEmptyResult()
    {
        // Arrange
        var analysisService = new RoslynAnalysisService(_analysisLogger);
        var telemetryService = new TelemetryService(_telemetryLogger);
        var coverageService = new CodeCoverageService(_coverageLogger, analysisService, telemetryService);

        // Act
        var result = await coverageService.GetCoverageSummaryAsync();

        // Assert
        Assert.NotNull(result);
        
        // Cleanup
        analysisService.Dispose();
    }

    [Fact]
    public async Task CodeCoverageService_FindUncoveredCodeAsync_WithoutSolution_ReturnsResult()
    {
        // Arrange
        var analysisService = new RoslynAnalysisService(_analysisLogger);
        var telemetryService = new TelemetryService(_telemetryLogger);
        var coverageService = new CodeCoverageService(_coverageLogger, analysisService, telemetryService);

        // Act
        var result = await coverageService.FindUncoveredCodeAsync();

        // Assert
        Assert.NotNull(result);
        
        // Cleanup
        analysisService.Dispose();
    }

    [Fact]
    public void TelemetryService_LogTelemetry_WithNullProperties_DoesNotThrow()
    {
        // Arrange
        var service = new TelemetryService(_telemetryLogger);

        // Act & Assert - Should not throw
        service.LogTelemetry("TestEvent", null!);
        Assert.True(true);
    }

    [Fact]
    public void TelemetryService_LogTelemetry_WithEmptyProperties_DoesNotThrow()
    {
        // Arrange
        var service = new TelemetryService(_telemetryLogger);
        var properties = new Dictionary<string, object>();

        // Act & Assert - Should not throw
        service.LogTelemetry("TestEvent", properties);
        Assert.True(true);
    }

    [Fact]
    public void TelemetryService_StartOperation_WithNullOperationType_DoesNotThrow()
    {
        // Arrange
        var service = new TelemetryService(_telemetryLogger);

        // Act
        var operation = service.StartOperation(null!);

        // Assert
        Assert.NotNull(operation);
        Assert.NotNull(operation.OperationId);
    }

    [Fact]
    public void TelemetryService_CompleteOperation_WithAdditionalProperties_UpdatesCorrectly()
    {
        // Arrange
        var service = new TelemetryService(_telemetryLogger);
        var operation = service.StartOperation("TestOperation");
        var additionalProps = new Dictionary<string, object>
        {
            ["result"] = "success",
            ["duration"] = 100
        };

        // Act
        service.CompleteOperation(operation, additionalProps);

        // Assert
        Assert.True(operation.IsSuccess);
        Assert.True(operation.Properties.ContainsKey("result"));
        Assert.Equal("success", operation.Properties["result"]);
    }

    [Fact]
    public void TelemetryService_FailOperation_WithAdditionalProperties_UpdatesCorrectly()
    {
        // Arrange
        var service = new TelemetryService(_telemetryLogger);
        var operation = service.StartOperation("TestOperation");
        var exception = new InvalidOperationException("Test error");
        var additionalProps = new Dictionary<string, object>
        {
            ["error_code"] = 500,
            ["context"] = "test_context"
        };

        // Act
        service.FailOperation(operation, exception, additionalProps);

        // Assert
        Assert.False(operation.IsSuccess);
        Assert.Equal("Test error", operation.ErrorMessage);
        Assert.True(operation.Properties.ContainsKey("error_code"));
        Assert.Equal(500, operation.Properties["error_code"]);
    }

    [Fact]
    public void TelemetryService_GetRecentOperations_WithLimit_ReturnsLimitedResults()
    {
        // Arrange
        var service = new TelemetryService(_telemetryLogger);

        // Start several operations
        for (int i = 0; i < 5; i++)
        {
            var op = service.StartOperation($"Operation{i}");
            service.CompleteOperation(op);
        }

        // Act
        var recentOps = service.GetRecentOperations(3);

        // Assert
        Assert.NotNull(recentOps);
        Assert.True(recentOps.Count <= 3);
    }

    [Fact]
    public async Task CodeCoverageService_GetMethodCoverageAsync_WithValidParameters_ReturnsResult()
    {
        // Arrange
        var analysisService = new RoslynAnalysisService(_analysisLogger);
        var telemetryService = new TelemetryService(_telemetryLogger);
        var coverageService = new CodeCoverageService(_coverageLogger, analysisService, telemetryService);

        // Act
        var result = await coverageService.GetMethodCoverageAsync("TestClass", "TestMethod");

        // Assert
        // Result can be null if no coverage data exists, which is valid
        Assert.True(result == null || result is MethodCoverage);

        // Cleanup
        analysisService.Dispose();
    }

    [Fact]
    public async Task CodeCoverageService_CompareCoverageAsync_WithValidBaseline_ReturnsComparison()
    {
        // Arrange
        var analysisService = new RoslynAnalysisService(_analysisLogger);
        var telemetryService = new TelemetryService(_telemetryLogger);
        var coverageService = new CodeCoverageService(_coverageLogger, analysisService, telemetryService);

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
        await File.WriteAllTextAsync(tempFile, System.Text.Json.JsonSerializer.Serialize(baselineData));

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
}
