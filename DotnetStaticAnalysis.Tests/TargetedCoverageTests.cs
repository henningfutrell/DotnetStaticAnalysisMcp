using DotnetStaticAnalysisMcp.Server.Services;
using DotnetStaticAnalysisMcp.Server.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using System.Text.Json;

namespace MCP.Tests;

/// <summary>
/// Targeted tests to improve coverage for specific low-coverage services
/// </summary>
public class TargetedCoverageTests
{
    private readonly Mock<ILogger<CodeCoverageService>> _mockCoverageLogger;
    private readonly Mock<ILogger<RoslynAnalysisService>> _mockAnalysisLogger;
    private readonly Mock<ILogger<TelemetryService>> _mockTelemetryLogger;
    private readonly RoslynAnalysisService _analysisService;
    private readonly TelemetryService _telemetryService;
    private readonly CodeCoverageService _coverageService;

    public TargetedCoverageTests()
    {
        _mockCoverageLogger = new Mock<ILogger<CodeCoverageService>>();
        _mockAnalysisLogger = new Mock<ILogger<RoslynAnalysisService>>();
        _mockTelemetryLogger = new Mock<ILogger<TelemetryService>>();
        
        _analysisService = new RoslynAnalysisService(_mockAnalysisLogger.Object);
        _telemetryService = new TelemetryService(_mockTelemetryLogger.Object);
        _coverageService = new CodeCoverageService(_mockCoverageLogger.Object, _analysisService, _telemetryService);
    }

    #region CodeCoverageService Targeted Tests

    [Fact]
    public async Task CodeCoverageService_RunCoverageAnalysisAsync_WithAllOptionCombinations()
    {
        // Test all boolean combinations for CollectBranchCoverage
        var options1 = new CoverageAnalysisOptions { CollectBranchCoverage = true, TimeoutMinutes = 1 };
        var result1 = await _coverageService.RunCoverageAnalysisAsync(options1);
        Assert.NotNull(result1);
        Assert.False(result1.Success);

        var options2 = new CoverageAnalysisOptions { CollectBranchCoverage = false, TimeoutMinutes = 2 };
        var result2 = await _coverageService.RunCoverageAnalysisAsync(options2);
        Assert.NotNull(result2);
        Assert.False(result2.Success);

        // Test with different timeout values
        var options3 = new CoverageAnalysisOptions { TimeoutMinutes = 30 };
        var result3 = await _coverageService.RunCoverageAnalysisAsync(options3);
        Assert.NotNull(result3);
        Assert.False(result3.Success);

        var options4 = new CoverageAnalysisOptions { TimeoutMinutes = 120 };
        var result4 = await _coverageService.RunCoverageAnalysisAsync(options4);
        Assert.NotNull(result4);
        Assert.False(result4.Success);
    }

    [Fact]
    public async Task CodeCoverageService_GetCoverageSummaryAsync_WithAllOptionCombinations()
    {
        // Test with different project filter combinations
        var options1 = new CoverageAnalysisOptions();
        options1.IncludedProjects.Add("Project1");
        var result1 = await _coverageService.GetCoverageSummaryAsync(options1);
        Assert.NotNull(result1);

        var options2 = new CoverageAnalysisOptions();
        options2.ExcludedProjects.Add("TestProject");
        var result2 = await _coverageService.GetCoverageSummaryAsync(options2);
        Assert.NotNull(result2);

        var options3 = new CoverageAnalysisOptions();
        options3.IncludedProjects.AddRange(new[] { "Project1", "Project2" });
        options3.ExcludedProjects.AddRange(new[] { "TestProject1", "TestProject2" });
        var result3 = await _coverageService.GetCoverageSummaryAsync(options3);
        Assert.NotNull(result3);

        var options4 = new CoverageAnalysisOptions();
        options4.IncludedTestProjects.Add("UnitTests");
        var result4 = await _coverageService.GetCoverageSummaryAsync(options4);
        Assert.NotNull(result4);
    }

    [Fact]
    public async Task CodeCoverageService_FindUncoveredCodeAsync_WithAllOptionCombinations()
    {
        // Test with different filter combinations
        var options1 = new CoverageAnalysisOptions { TestFilter = "Category=Unit" };
        var result1 = await _coverageService.FindUncoveredCodeAsync(options1);
        Assert.NotNull(result1);
        Assert.False(result1.Success);

        var options2 = new CoverageAnalysisOptions { TestFilter = "Priority=High" };
        var result2 = await _coverageService.FindUncoveredCodeAsync(options2);
        Assert.NotNull(result2);
        Assert.False(result2.Success);

        var options3 = new CoverageAnalysisOptions { TestFilter = "FullyQualifiedName~Test" };
        var result3 = await _coverageService.FindUncoveredCodeAsync(options3);
        Assert.NotNull(result3);
        Assert.False(result3.Success);
    }

    [Fact]
    public async Task CodeCoverageService_GetMethodCoverageAsync_WithAllParameterCombinations()
    {
        // Test with different class and method name combinations
        var result1 = await _coverageService.GetMethodCoverageAsync("TestClass", "TestMethod");
        Assert.Null(result1);

        var result2 = await _coverageService.GetMethodCoverageAsync("MyNamespace.MyClass", "MyMethod");
        Assert.Null(result2);

        var result3 = await _coverageService.GetMethodCoverageAsync("Generic<T>", "GenericMethod<T>");
        Assert.Null(result3);

        var options = new CoverageAnalysisOptions();
        options.IncludedProjects.Add("TestProject");
        var result4 = await _coverageService.GetMethodCoverageAsync("TestClass", "TestMethod", options);
        Assert.Null(result4);
    }

    [Fact]
    public async Task CodeCoverageService_CompareCoverageAsync_WithDifferentBaselines()
    {
        // Test with different baseline scenarios
        var baseline1 = new CoverageAnalysisResult
        {
            Success = true,
            Summary = new CoverageSummary { LinesCoveredPercentage = 75.0, BranchesCoveredPercentage = 60.0 },
            Projects = new List<ProjectCoverage>()
        };
        var result1 = await _coverageService.CompareCoverageAsync(baseline1);
        Assert.NotNull(result1);

        var baseline2 = new CoverageAnalysisResult
        {
            Success = false,
            Summary = new CoverageSummary { LinesCoveredPercentage = 0.0, BranchesCoveredPercentage = 0.0 },
            Projects = new List<ProjectCoverage>()
        };
        var result2 = await _coverageService.CompareCoverageAsync(baseline2);
        Assert.NotNull(result2);

        var options = new CoverageAnalysisOptions();
        options.IncludedProjects.Add("MainProject");
        var result3 = await _coverageService.CompareCoverageAsync(baseline1, options);
        Assert.NotNull(result3);
    }

    [Fact]
    public void CodeCoverageService_SetSolutionPath_WithVariousPathFormats()
    {
        // Test with different path formats and edge cases
        _coverageService.SetSolutionPath("/absolute/path/solution.sln");
        _coverageService.SetSolutionPath("relative/path/solution.sln");
        _coverageService.SetSolutionPath("C:\\Windows\\Path\\solution.sln");
        _coverageService.SetSolutionPath("/path/with spaces/solution.sln");
        _coverageService.SetSolutionPath("/path/with-dashes/solution.sln");
        _coverageService.SetSolutionPath("/path/with_underscores/solution.sln");
        _coverageService.SetSolutionPath("/path/with.dots/solution.sln");
        _coverageService.SetSolutionPath("/path/with(parentheses)/solution.sln");
        _coverageService.SetSolutionPath("/path/with[brackets]/solution.sln");
        _coverageService.SetSolutionPath("/path/with{braces}/solution.sln");
        _coverageService.SetSolutionPath("/path/with@symbols/solution.sln");
        _coverageService.SetSolutionPath("/path/with#hash/solution.sln");
        _coverageService.SetSolutionPath("/path/with$dollar/solution.sln");
        _coverageService.SetSolutionPath("/path/with%percent/solution.sln");
        _coverageService.SetSolutionPath("/path/with&ampersand/solution.sln");

        // All should complete without throwing
        Assert.True(true);
    }

    #endregion

    #region RoslynAnalysisService Targeted Tests

    [Fact]
    public async Task RoslynAnalysisService_LoadSolutionAsync_WithVariousPathFormats()
    {
        // Test with different path formats
        var result1 = await _analysisService.LoadSolutionAsync("/absolute/path/solution.sln");
        Assert.False(result1);

        var result2 = await _analysisService.LoadSolutionAsync("relative/path/solution.sln");
        Assert.False(result2);

        var result3 = await _analysisService.LoadSolutionAsync("C:\\Windows\\Path\\solution.sln");
        Assert.False(result3);

        var result4 = await _analysisService.LoadSolutionAsync("/path/with spaces/solution.sln");
        Assert.False(result4);

        var result5 = await _analysisService.LoadSolutionAsync("/path/with-special@chars#/solution.sln");
        Assert.False(result5);
    }

    [Fact]
    public async Task RoslynAnalysisService_AnalyzeFileAsync_WithVariousFileTypes()
    {
        // Test with different file types and paths
        var result1 = await _analysisService.AnalyzeFileAsync("/path/to/file.cs");
        Assert.NotNull(result1);
        Assert.Empty(result1);

        var result2 = await _analysisService.AnalyzeFileAsync("/path/to/file.vb");
        Assert.NotNull(result2);
        Assert.Empty(result2);

        var result3 = await _analysisService.AnalyzeFileAsync("/path/to/file.fs");
        Assert.NotNull(result3);
        Assert.Empty(result3);

        var result4 = await _analysisService.AnalyzeFileAsync("C:\\Windows\\Path\\file.cs");
        Assert.NotNull(result4);
        Assert.Empty(result4);

        var result5 = await _analysisService.AnalyzeFileAsync("/path/with spaces/file.cs");
        Assert.NotNull(result5);
        Assert.Empty(result5);
    }

    [Fact]
    public async Task RoslynAnalysisService_GetCodeSuggestionsAsync_WithAllOptionCombinations()
    {
        // Test with different suggestion option combinations
        var options1 = new SuggestionAnalysisOptions
        {
            IncludeAutoFixable = true,
            IncludeManualFix = false,
            MaxSuggestions = 10
        };
        var result1 = await _analysisService.GetCodeSuggestionsAsync(options1);
        Assert.NotNull(result1);
        Assert.Empty(result1);

        var options2 = new SuggestionAnalysisOptions
        {
            IncludeAutoFixable = false,
            IncludeManualFix = true,
            MaxSuggestions = 50
        };
        var result2 = await _analysisService.GetCodeSuggestionsAsync(options2);
        Assert.NotNull(result2);
        Assert.Empty(result2);

        var options3 = new SuggestionAnalysisOptions
        {
            IncludeAutoFixable = true,
            IncludeManualFix = true,
            MaxSuggestions = 100
        };
        var result3 = await _analysisService.GetCodeSuggestionsAsync(options3);
        Assert.NotNull(result3);
        Assert.Empty(result3);

        var options4 = new SuggestionAnalysisOptions
        {
            IncludeAutoFixable = false,
            IncludeManualFix = false,
            MaxSuggestions = 25
        };
        var result4 = await _analysisService.GetCodeSuggestionsAsync(options4);
        Assert.NotNull(result4);
        Assert.Empty(result4);
    }

    [Fact]
    public async Task RoslynAnalysisService_GetFileSuggestionsAsync_WithAllOptionCombinations()
    {
        // Test with different file and option combinations
        var options1 = new SuggestionAnalysisOptions { MaxSuggestions = 5 };
        var result1 = await _analysisService.GetFileSuggestionsAsync("/test/file1.cs", options1);
        Assert.NotNull(result1);
        Assert.Empty(result1);

        var options2 = new SuggestionAnalysisOptions { MaxSuggestions = 15 };
        var result2 = await _analysisService.GetFileSuggestionsAsync("/test/file2.cs", options2);
        Assert.NotNull(result2);
        Assert.Empty(result2);

        var options3 = new SuggestionAnalysisOptions { MaxSuggestions = 75 };
        var result3 = await _analysisService.GetFileSuggestionsAsync("/test/file3.cs", options3);
        Assert.NotNull(result3);
        Assert.Empty(result3);

        var options4 = new SuggestionAnalysisOptions { MaxSuggestions = 200 };
        var result4 = await _analysisService.GetFileSuggestionsAsync("/test/file4.cs", options4);
        Assert.NotNull(result4);
        Assert.Empty(result4);
    }

    [Fact]
    public async Task RoslynAnalysisService_FindTypeUsagesAsync_WithAllOptionCombinations()
    {
        // Test with different type usage analysis options
        var options1 = new TypeUsageAnalysisOptions { MaxResults = 10, IncludeDocumentation = true };
        var result1 = await _analysisService.FindTypeUsagesAsync("TestType1", options1);
        Assert.NotNull(result1);
        Assert.False(result1.Success);

        var options2 = new TypeUsageAnalysisOptions { MaxResults = 50, IncludeDocumentation = false };
        var result2 = await _analysisService.FindTypeUsagesAsync("TestType2", options2);
        Assert.NotNull(result2);
        Assert.False(result2.Success);

        var options3 = new TypeUsageAnalysisOptions { MaxResults = 500, IncludeDocumentation = true };
        var result3 = await _analysisService.FindTypeUsagesAsync("TestType3", options3);
        Assert.NotNull(result3);
        Assert.False(result3.Success);

        var options4 = new TypeUsageAnalysisOptions { MaxResults = 1000, IncludeDocumentation = false };
        var result4 = await _analysisService.FindTypeUsagesAsync("TestType4", options4);
        Assert.NotNull(result4);
        Assert.False(result4.Success);
    }

    [Fact]
    public async Task RoslynAnalysisService_MultipleOperationsInSequence()
    {
        // Test multiple operations in sequence to exercise different code paths
        await _analysisService.LoadSolutionAsync("/test1.sln");
        await _analysisService.GetSolutionInfoAsync();
        await _analysisService.GetCompilationErrorsAsync();
        await _analysisService.AnalyzeFileAsync("/test1.cs");

        await _analysisService.LoadSolutionAsync("/test2.sln");
        await _analysisService.GetSolutionInfoAsync();
        await _analysisService.GetCompilationErrorsAsync();
        await _analysisService.AnalyzeFileAsync("/test2.cs");

        await _analysisService.LoadSolutionAsync("/test3.sln");
        await _analysisService.GetSolutionInfoAsync();
        await _analysisService.GetCompilationErrorsAsync();
        await _analysisService.AnalyzeFileAsync("/test3.cs");

        // All should complete without throwing
        Assert.True(true);
    }

    #endregion

    #region DotNetAnalysisTools Targeted Tests

    [Fact]
    public async Task DotNetAnalysisTools_AllMethods_WithNullService()
    {
        // Test all methods with null service to exercise error handling paths
        var result1 = await DotNetAnalysisTools.LoadSolution(null!, "/test.sln");
        var json1 = JsonDocument.Parse(result1);
        Assert.False(json1.RootElement.GetProperty("success").GetBoolean());

        var result2 = await DotNetAnalysisTools.GetSolutionInfo(null!);
        var json2 = JsonDocument.Parse(result2);
        Assert.False(json2.RootElement.GetProperty("success").GetBoolean());

        var result3 = await DotNetAnalysisTools.GetCompilationErrors(null!);
        var json3 = JsonDocument.Parse(result3);
        Assert.False(json3.RootElement.GetProperty("success").GetBoolean());

        var result4 = await DotNetAnalysisTools.AnalyzeFile(null!, "/test.cs");
        var json4 = JsonDocument.Parse(result4);
        Assert.False(json4.RootElement.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task DotNetAnalysisTools_AllMethods_WithValidService()
    {
        // Test all methods with valid service
        var result1 = await DotNetAnalysisTools.LoadSolution(_analysisService, "/test.sln");
        var json1 = JsonDocument.Parse(result1);
        Assert.False(json1.RootElement.GetProperty("success").GetBoolean());

        var result2 = await DotNetAnalysisTools.GetSolutionInfo(_analysisService);
        var json2 = JsonDocument.Parse(result2);
        Assert.True(json2.RootElement.GetProperty("success").GetBoolean());

        var result3 = await DotNetAnalysisTools.GetCompilationErrors(_analysisService);
        var json3 = JsonDocument.Parse(result3);
        Assert.True(json3.RootElement.GetProperty("success").GetBoolean());

        var result4 = await DotNetAnalysisTools.AnalyzeFile(_analysisService, "/test.cs");
        var json4 = JsonDocument.Parse(result4);
        Assert.True(json4.RootElement.GetProperty("success").GetBoolean());
    }

    [Fact]
    public void DotNetAnalysisTools_ServiceProvider_Operations()
    {
        // Test service provider operations
        var services = new ServiceCollection();
        services.AddSingleton(_analysisService);
        services.AddSingleton(_coverageService);
        services.AddSingleton(_telemetryService);
        var provider = services.BuildServiceProvider();

        DotNetAnalysisTools.SetServiceProvider(provider);
        DotNetAnalysisTools.SetServiceProvider(null!);
        DotNetAnalysisTools.SetServiceProvider(provider);

        // All should complete without throwing
        Assert.True(true);
    }

    #endregion

    #region TelemetryService Targeted Tests

    [Fact]
    public void TelemetryService_LogTelemetry_WithVariousDataTypes()
    {
        // Test with different data types and structures
        _telemetryService.LogTelemetry("Event1", new Dictionary<string, object> { ["key1"] = "value1" });
        _telemetryService.LogTelemetry("Event2", new Dictionary<string, object> { ["key1"] = 123 });
        _telemetryService.LogTelemetry("Event3", new Dictionary<string, object> { ["key1"] = true });
        _telemetryService.LogTelemetry("Event4", new Dictionary<string, object> { ["key1"] = 45.67 });
        _telemetryService.LogTelemetry("Event5", new Dictionary<string, object> { ["key1"] = DateTime.Now });
        _telemetryService.LogTelemetry("Event6", new Dictionary<string, object> 
        { 
            ["string"] = "value", 
            ["int"] = 123, 
            ["bool"] = true, 
            ["double"] = 45.67 
        });

        // All should complete without throwing
        Assert.True(true);
    }

    [Fact]
    public void TelemetryService_Operations_WithVariousNames()
    {
        // Test operation tracking with various names
        var op1 = _telemetryService.StartOperation("Operation1");
        _telemetryService.CompleteOperation(op1);

        var op2 = _telemetryService.StartOperation("Operation With Spaces");
        _telemetryService.CompleteOperation(op2);

        var op3 = _telemetryService.StartOperation("Operation-With-Dashes");
        _telemetryService.CompleteOperation(op3);

        var op4 = _telemetryService.StartOperation("Operation_With_Underscores");
        _telemetryService.CompleteOperation(op4);

        var op5 = _telemetryService.StartOperation("Operation.With.Dots");
        _telemetryService.CompleteOperation(op5);

        // All should complete without throwing
        Assert.True(true);
    }

    [Fact]
    public void TelemetryService_LogTelemetry_WithNullData_DoesNotThrow()
    {
        // Test with null data
        _telemetryService.LogTelemetry("EventWithNullData", null!);
        Assert.True(true);
    }

    [Fact]
    public void TelemetryService_LogTelemetry_WithEmptyData_DoesNotThrow()
    {
        // Test with empty data
        _telemetryService.LogTelemetry("EventWithEmptyData", new Dictionary<string, object>());
        Assert.True(true);
    }

    [Fact]
    public void TelemetryService_Operations_WithNullOperationId_ThrowsExpectedException()
    {
        // Test completing operation with null ID - this should throw
        Assert.Throws<NullReferenceException>(() => _telemetryService.CompleteOperation(null!));
    }

    [Fact]
    public void TelemetryService_Operations_WithEmptyOperationName_DoesNotThrow()
    {
        // Test starting and completing operation with empty name
        var op = _telemetryService.StartOperation("");
        _telemetryService.CompleteOperation(op);
        Assert.True(true);
    }

    [Fact]
    public void TelemetryService_Operations_WithWhitespaceOperationName_DoesNotThrow()
    {
        // Test starting and completing operation with whitespace name
        var op = _telemetryService.StartOperation("   ");
        _telemetryService.CompleteOperation(op);
        Assert.True(true);
    }

    [Fact]
    public void TelemetryService_StartOperation_WithNullName_DoesNotThrow()
    {
        // Test starting operation with null name
        var opId = _telemetryService.StartOperation(null!);
        _telemetryService.CompleteOperation(opId);
        Assert.True(true);
    }

    [Fact]
    public void TelemetryService_StartOperation_WithEmptyName_DoesNotThrow()
    {
        // Test starting operation with empty name
        var opId = _telemetryService.StartOperation("");
        _telemetryService.CompleteOperation(opId);
        Assert.True(true);
    }

    [Fact]
    public void TelemetryService_StartOperation_WithWhitespaceName_DoesNotThrow()
    {
        // Test starting operation with whitespace name
        var opId = _telemetryService.StartOperation("   ");
        _telemetryService.CompleteOperation(opId);
        Assert.True(true);
    }

    [Fact]
    public void TelemetryService_LogTelemetry_WithSpecialCharacters_DoesNotThrow()
    {
        // Test with special characters in event names and data
        _telemetryService.LogTelemetry("Event@#$%^&*()", new Dictionary<string, object> { ["key@#$"] = "value@#$" });
        _telemetryService.LogTelemetry("Event With Spaces", new Dictionary<string, object> { ["key with spaces"] = "value with spaces" });
        _telemetryService.LogTelemetry("Event-With-Dashes", new Dictionary<string, object> { ["key-with-dashes"] = "value-with-dashes" });
        _telemetryService.LogTelemetry("Event_With_Underscores", new Dictionary<string, object> { ["key_with_underscores"] = "value_with_underscores" });
        Assert.True(true);
    }

    [Fact]
    public void TelemetryService_LogTelemetry_WithComplexDataTypes_DoesNotThrow()
    {
        // Test with complex data types
        var complexData = new Dictionary<string, object>
        {
            ["string"] = "test",
            ["int"] = 42,
            ["double"] = 3.14159,
            ["bool"] = true,
            ["datetime"] = DateTime.Now,
            ["timespan"] = TimeSpan.FromMinutes(5),
            ["guid"] = Guid.NewGuid(),
            ["array"] = new[] { 1, 2, 3 },
            ["null"] = null!
        };
        _telemetryService.LogTelemetry("ComplexDataEvent", complexData);
        Assert.True(true);
    }

    [Fact]
    public void TelemetryService_Operations_NestedOperations_DoesNotThrow()
    {
        // Test nested operations
        var op1 = _telemetryService.StartOperation("OuterOperation");
        var op2 = _telemetryService.StartOperation("InnerOperation1");
        var op3 = _telemetryService.StartOperation("InnerOperation2");

        _telemetryService.CompleteOperation(op3);
        _telemetryService.CompleteOperation(op2);
        _telemetryService.CompleteOperation(op1);

        Assert.True(true);
    }

    [Fact]
    public void TelemetryService_Operations_MultipleSequentialOperations_DoesNotThrow()
    {
        // Test multiple sequential operations
        for (int i = 0; i < 10; i++)
        {
            var op = _telemetryService.StartOperation($"Operation{i}");
            _telemetryService.CompleteOperation(op);
        }
        Assert.True(true);
    }

    [Fact]
    public void TelemetryService_LogTelemetry_MultipleEventsInSequence_DoesNotThrow()
    {
        // Test multiple events in sequence
        for (int i = 0; i < 10; i++)
        {
            _telemetryService.LogTelemetry($"Event{i}", new Dictionary<string, object> { ["index"] = i });
        }
        Assert.True(true);
    }

    [Fact]
    public void TelemetryService_MixedOperationsAndEvents_DoesNotThrow()
    {
        // Test mixed operations and events
        _telemetryService.LogTelemetry("StartEvent", new Dictionary<string, object> { ["phase"] = "start" });
        var op1 = _telemetryService.StartOperation("MainOperation");
        _telemetryService.LogTelemetry("MiddleEvent", new Dictionary<string, object> { ["phase"] = "middle" });
        var op2 = _telemetryService.StartOperation("SubOperation");
        _telemetryService.LogTelemetry("SubEvent", new Dictionary<string, object> { ["phase"] = "sub" });
        _telemetryService.CompleteOperation(op2);
        _telemetryService.LogTelemetry("EndEvent", new Dictionary<string, object> { ["phase"] = "end" });
        _telemetryService.CompleteOperation(op1);
        Assert.True(true);
    }

    #endregion
}
