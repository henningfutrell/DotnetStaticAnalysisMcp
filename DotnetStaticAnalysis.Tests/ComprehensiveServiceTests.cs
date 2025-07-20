using DotnetStaticAnalysisMcp.Server.Services;
using DotnetStaticAnalysisMcp.Server.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using System.Text.Json;

namespace MCP.Tests;

/// <summary>
/// Comprehensive tests for all services to achieve high coverage
/// </summary>
public class ComprehensiveServiceTests
{
    private readonly Mock<ILogger<TelemetryService>> _mockTelemetryLogger;
    private readonly Mock<ILogger<RoslynAnalysisService>> _mockAnalysisLogger;
    private readonly Mock<ILogger<CodeCoverageService>> _mockCoverageLogger;
    private readonly TelemetryService _telemetryService;
    private readonly RoslynAnalysisService _analysisService;
    private readonly CodeCoverageService _coverageService;

    public ComprehensiveServiceTests()
    {
        _mockTelemetryLogger = new Mock<ILogger<TelemetryService>>();
        _mockAnalysisLogger = new Mock<ILogger<RoslynAnalysisService>>();
        _mockCoverageLogger = new Mock<ILogger<CodeCoverageService>>();
        
        _telemetryService = new TelemetryService(_mockTelemetryLogger.Object);
        _analysisService = new RoslynAnalysisService(_mockAnalysisLogger.Object);
        _coverageService = new CodeCoverageService(_mockCoverageLogger.Object, _analysisService, _telemetryService);
    }

    #region TelemetryService Tests

    [Fact]
    public void TelemetryService_Constructor_InitializesCorrectly()
    {
        // Arrange & Act
        var service = new TelemetryService(_mockTelemetryLogger.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void TelemetryService_LogTelemetry_WithValidData_DoesNotThrow()
    {
        // Arrange
        var eventName = "TestEvent";
        var data = new Dictionary<string, object> { ["key"] = "value" };

        // Act & Assert - Should not throw
        _telemetryService.LogTelemetry(eventName, data);
        Assert.True(true);
    }

    [Fact]
    public void TelemetryService_LogTelemetry_WithNullData_DoesNotThrow()
    {
        // Arrange
        var eventName = "TestEvent";

        // Act & Assert - Should not throw
        _telemetryService.LogTelemetry(eventName, new Dictionary<string, object>());
        Assert.True(true);
    }

    [Fact]
    public void TelemetryService_LogTelemetry_WithEmptyEventName_DoesNotThrow()
    {
        // Arrange
        var data = new Dictionary<string, object>();

        // Act & Assert - Should not throw
        _telemetryService.LogTelemetry("", data);
        Assert.True(true);
    }

    #endregion

    #region RoslynAnalysisService Tests

    [Fact]
    public void RoslynAnalysisService_Constructor_InitializesCorrectly()
    {
        // Arrange & Act
        var service = new RoslynAnalysisService(_mockAnalysisLogger.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public async Task RoslynAnalysisService_LoadSolutionAsync_WithNonExistentPath_ReturnsFalse()
    {
        // Arrange
        var nonExistentPath = "/path/that/does/not/exist.sln";

        // Act
        var result = await _analysisService.LoadSolutionAsync(nonExistentPath);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RoslynAnalysisService_GetSolutionInfoAsync_WithNoSolution_ReturnsNull()
    {
        // Act
        var result = await _analysisService.GetSolutionInfoAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RoslynAnalysisService_GetCompilationErrorsAsync_WithNoSolution_ReturnsEmptyList()
    {
        // Act
        var result = await _analysisService.GetCompilationErrorsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task RoslynAnalysisService_AnalyzeFileAsync_WithNoSolution_ReturnsEmptyList()
    {
        // Arrange
        var filePath = "/some/file.cs";

        // Act
        var result = await _analysisService.AnalyzeFileAsync(filePath);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task RoslynAnalysisService_GetCodeSuggestionsAsync_WithNoSolution_ReturnsEmptyList()
    {
        // Arrange
        var options = new SuggestionAnalysisOptions();

        // Act
        var result = await _analysisService.GetCodeSuggestionsAsync(options);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task RoslynAnalysisService_FindTypeUsagesAsync_WithNoSolution_ReturnsFailureResult()
    {
        // Arrange
        var typeName = "TestClass";
        var options = new TypeUsageAnalysisOptions();

        // Act
        var result = await _analysisService.FindTypeUsagesAsync(typeName, options);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal(typeName, result.TypeName);
        Assert.Equal(0, result.TotalUsages);
    }

    [Fact]
    public async Task RoslynAnalysisService_GetTypeDependenciesAsync_WithNoSolution_ReturnsFailureResult()
    {
        // Arrange
        var typeName = "TestClass";

        // Act
        var result = await _analysisService.GetTypeDependenciesAsync(typeName);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal(0, result.TotalDependencies);
    }

    [Fact]
    public void RoslynAnalysisService_Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var service = new RoslynAnalysisService(_mockAnalysisLogger.Object);

        // Act & Assert - Should not throw
        service.Dispose();
        service.Dispose();
        Assert.True(true);
    }

    [Fact]
    public async Task RoslynAnalysisService_FindMemberUsagesAsync_WithNoSolution_ReturnsFailureResult()
    {
        // Arrange
        var typeName = "TestClass";
        var memberName = "TestMethod";

        // Act
        var result = await _analysisService.FindMemberUsagesAsync(typeName, memberName);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal(0, result.TotalUsages);
    }

    [Fact]
    public async Task RoslynAnalysisService_GetFileSuggestionsAsync_WithValidParameters_ReturnsEmptyListWhenNoSolution()
    {
        // Arrange
        var filePath = "/some/file.cs";
        var options = new SuggestionAnalysisOptions
        {
            MaxSuggestions = 25
        };

        // Act
        var result = await _analysisService.GetFileSuggestionsAsync(filePath, options);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task RoslynAnalysisService_AnalyzeImpactScopeAsync_WithNoSolution_ReturnsFailureResult()
    {
        // Arrange
        var typeName = "TestClass";

        // Act
        var result = await _analysisService.AnalyzeImpactScopeAsync(typeName);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task RoslynAnalysisService_ValidateRenameSafetyAsync_WithNoSolution_ReturnsFailureResult()
    {
        // Arrange
        var currentName = "OldClass";
        var proposedName = "NewClass";

        // Act
        var result = await _analysisService.ValidateRenameSafetyAsync(currentName, proposedName);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal(currentName, result.CurrentName);
        Assert.Equal(proposedName, result.ProposedName);
    }

    [Fact]
    public async Task RoslynAnalysisService_PreviewRenameImpactAsync_WithNoSolution_ReturnsFailureResult()
    {
        // Arrange
        var currentName = "OldClass";
        var proposedName = "NewClass";

        // Act
        var result = await _analysisService.PreviewRenameImpactAsync(currentName, proposedName);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal(currentName, result.AnalyzedItem);
    }

    #endregion

    #region CodeCoverageService Tests

    [Fact]
    public void CodeCoverageService_Constructor_InitializesCorrectly()
    {
        // Arrange & Act
        var service = new CodeCoverageService(_mockCoverageLogger.Object, _analysisService, _telemetryService);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void CodeCoverageService_SetSolutionPath_WithValidPath_DoesNotThrow()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        
        try
        {
            // Act & Assert - Should not throw
            _coverageService.SetSolutionPath(tempFile);
            Assert.True(true);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void CodeCoverageService_SetSolutionPath_WithNullPath_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        _coverageService.SetSolutionPath(null!);
        Assert.True(true);
    }

    [Fact]
    public async Task CodeCoverageService_RunCoverageAnalysisAsync_WithNoSolution_ReturnsFailureResult()
    {
        // Act
        var result = await _coverageService.RunCoverageAnalysisAsync();

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("No solution loaded", result.ErrorMessage);
    }

    [Fact]
    public async Task CodeCoverageService_GetCoverageSummaryAsync_WithNoSolution_ReturnsEmptySummary()
    {
        // Act
        var result = await _coverageService.GetCoverageSummaryAsync();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<CoverageSummary>(result);
    }

    [Fact]
    public async Task CodeCoverageService_FindUncoveredCodeAsync_WithNoSolution_ReturnsFailureResult()
    {
        // Act
        var result = await _coverageService.FindUncoveredCodeAsync();

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task CodeCoverageService_GetMethodCoverageAsync_WithNoSolution_ReturnsNull()
    {
        // Arrange
        var className = "TestClass";
        var methodName = "TestMethod";

        // Act
        var result = await _coverageService.GetMethodCoverageAsync(className, methodName);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CodeCoverageService_CompareCoverageAsync_WithValidBaseline_ReturnsResult()
    {
        // Arrange
        var baseline = new CoverageAnalysisResult
        {
            Success = true,
            Summary = new CoverageSummary()
        };

        // Act
        var result = await _coverageService.CompareCoverageAsync(baseline);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<CoverageComparisonResult>(result);
    }

    [Fact]
    public async Task CodeCoverageService_RunCoverageAnalysisAsync_WithValidOptions_ReturnsResult()
    {
        // Arrange
        var options = new CoverageAnalysisOptions();

        // Act
        var result = await _coverageService.RunCoverageAnalysisAsync(options);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<CoverageAnalysisResult>(result);
    }

    [Fact]
    public void CodeCoverageService_SetSolutionPath_WithEmptyPath_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        _coverageService.SetSolutionPath("");
        Assert.True(true);
    }

    [Fact]
    public void CodeCoverageService_SetSolutionPath_WithWhitespacePath_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        _coverageService.SetSolutionPath("   ");
        Assert.True(true);
    }

    #endregion

    #region DotNetAnalysisTools Tests

    [Fact]
    public async Task DotNetAnalysisTools_GetServerVersion_ReturnsValidJson()
    {
        // Act
        var result = await DotNetAnalysisTools.GetServerVersion();

        // Assert
        Assert.NotNull(result);
        var json = JsonDocument.Parse(result);
        Assert.True(json.RootElement.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
        Assert.True(json.RootElement.TryGetProperty("version", out var version));
        Assert.NotNull(version.GetString());
    }

    [Fact]
    public async Task DotNetAnalysisTools_GetBasicDiagnostics_ReturnsValidJson()
    {
        // Act
        var result = await DotNetAnalysisTools.GetBasicDiagnostics();

        // Assert
        Assert.NotNull(result);
        var json = JsonDocument.Parse(result);
        Assert.True(json.RootElement.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Fact]
    public async Task DotNetAnalysisTools_GetDiagnostics_ReturnsValidJson()
    {
        // Act
        var result = await DotNetAnalysisTools.GetDiagnostics(true);

        // Assert
        Assert.NotNull(result);
        var json = JsonDocument.Parse(result);
        Assert.True(json.RootElement.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Fact]
    public void DotNetAnalysisTools_SetServiceProvider_WithValidProvider_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_telemetryService);
        services.AddSingleton(_analysisService);
        services.AddSingleton(_coverageService);
        var provider = services.BuildServiceProvider();

        // Act & Assert
        DotNetAnalysisTools.SetServiceProvider(provider);
        Assert.True(true);
    }

    [Fact]
    public async Task DotNetAnalysisTools_GetSuggestionCategories_ReturnsValidJson()
    {
        // Act
        var result = await DotNetAnalysisTools.GetSuggestionCategories();

        // Assert
        Assert.NotNull(result);
        var json = JsonDocument.Parse(result);
        Assert.True(json.RootElement.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
        Assert.True(json.RootElement.TryGetProperty("categories", out var categories));
        Assert.Equal(JsonValueKind.Array, categories.ValueKind);
    }

    #endregion

    #region Model Tests

    [Fact]
    public void CoverageAnalysisOptions_DefaultConstructor_CreatesValidInstance()
    {
        // Act
        var options = new CoverageAnalysisOptions();

        // Assert
        Assert.NotNull(options);
        Assert.True(options.CollectBranchCoverage);
        Assert.Equal(10, options.TimeoutMinutes);
    }

    [Fact]
    public void CoverageAnalysisResult_DefaultConstructor_CreatesValidInstance()
    {
        // Act
        var result = new CoverageAnalysisResult();

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.NotNull(result.Projects);
        Assert.Empty(result.Projects);
        Assert.NotNull(result.Summary);
    }

    [Fact]
    public void CoverageSummary_DefaultConstructor_CreatesValidInstance()
    {
        // Act
        var summary = new CoverageSummary();

        // Assert
        Assert.NotNull(summary);
        Assert.Equal(0, summary.LinesCoveredPercentage);
        Assert.Equal(0, summary.BranchesCoveredPercentage);
        Assert.Equal(0, summary.TotalLines);
        Assert.Equal(0, summary.CoveredLines);
    }

    [Fact]
    public void SuggestionAnalysisOptions_DefaultConstructor_CreatesValidInstance()
    {
        // Act
        var options = new SuggestionAnalysisOptions();

        // Assert
        Assert.NotNull(options);
        Assert.True(options.IncludeAutoFixable);
        Assert.True(options.IncludeManualFix);
        Assert.Equal(100, options.MaxSuggestions);
    }

    [Fact]
    public void TypeUsageAnalysisOptions_DefaultConstructor_CreatesValidInstance()
    {
        // Act
        var options = new TypeUsageAnalysisOptions();

        // Assert
        Assert.NotNull(options);
        Assert.Equal(1000, options.MaxResults);
        Assert.True(options.IncludeDocumentation);
    }

    [Fact]
    public void UncoveredCodeResult_DefaultConstructor_CreatesValidInstance()
    {
        // Act
        var result = new UncoveredCodeResult();

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.NotNull(result.UncoveredLines);
        Assert.NotNull(result.UncoveredMethods);
        Assert.NotNull(result.UncoveredBranches);
    }

    [Fact]
    public void UncoveredLine_Constructor_CreatesValidInstance()
    {
        // Act
        var line = new UncoveredLine
        {
            FilePath = "/test/file.cs",
            LineNumber = 42
        };

        // Assert
        Assert.NotNull(line);
        Assert.Equal("/test/file.cs", line.FilePath);
        Assert.Equal(42, line.LineNumber);
    }

    [Fact]
    public void UncoveredMethod_Constructor_CreatesValidInstance()
    {
        // Act
        var method = new UncoveredMethod
        {
            ClassName = "TestClass",
            MethodName = "TestMethod",
            FilePath = "/test/file.cs",
            StartLine = 10,
            EndLine = 20
        };

        // Assert
        Assert.NotNull(method);
        Assert.Equal("TestClass", method.ClassName);
        Assert.Equal("TestMethod", method.MethodName);
        Assert.Equal("/test/file.cs", method.FilePath);
        Assert.Equal(10, method.StartLine);
        Assert.Equal(20, method.EndLine);
    }

    [Fact]
    public void UncoveredBranch_Constructor_CreatesValidInstance()
    {
        // Act
        var branch = new UncoveredBranch
        {
            FilePath = "/test/file.cs",
            LineNumber = 15,
            BranchNumber = 1,
            Condition = "if (x > 0)"
        };

        // Assert
        Assert.NotNull(branch);
        Assert.Equal("/test/file.cs", branch.FilePath);
        Assert.Equal(15, branch.LineNumber);
        Assert.Equal(1, branch.BranchNumber);
        Assert.Equal("if (x > 0)", branch.Condition);
    }

    [Fact]
    public void TypeDependency_Constructor_CreatesValidInstance()
    {
        // Act
        var dependency = new TypeDependency
        {
            DependentType = "MyClass",
            DependencyType = "System.String",
            Kind = DependencyKind.Usage,
            Context = "Property type"
        };

        // Assert
        Assert.NotNull(dependency);
        Assert.Equal("MyClass", dependency.DependentType);
        Assert.Equal("System.String", dependency.DependencyType);
        Assert.Equal(DependencyKind.Usage, dependency.Kind);
        Assert.Equal("Property type", dependency.Context);
    }

    [Fact]
    public void TypeUsageReference_Constructor_CreatesValidInstance()
    {
        // Act
        var reference = new TypeUsageReference
        {
            FilePath = "/test/file.cs",
            StartLine = 25,
            StartColumn = 10,
            Context = "var str = new String();"
        };

        // Assert
        Assert.NotNull(reference);
        Assert.Equal("/test/file.cs", reference.FilePath);
        Assert.Equal(25, reference.StartLine);
        Assert.Equal(10, reference.StartColumn);
        Assert.Equal("var str = new String();", reference.Context);
    }

    [Fact]
    public void TestFailure_Constructor_CreatesValidInstance()
    {
        // Act
        var failure = new TestFailure
        {
            TestName = "TestMethod",
            ErrorMessage = "Assert failed",
            StackTrace = "at TestMethod() line 10"
        };

        // Assert
        Assert.NotNull(failure);
        Assert.Equal("TestMethod", failure.TestName);
        Assert.Equal("Assert failed", failure.ErrorMessage);
        Assert.Equal("at TestMethod() line 10", failure.StackTrace);
    }

    [Fact]
    public void SymbolInfo_Constructor_CreatesValidInstance()
    {
        // Act
        var symbol = new SymbolInfo
        {
            Name = "TestClass",
            Kind = Microsoft.CodeAnalysis.SymbolKind.NamedType,
            ContainingNamespace = "TestNamespace",
            FilePath = "/test/file.cs",
            StartLine = 1,
            EndLine = 100
        };

        // Assert
        Assert.NotNull(symbol);
        Assert.Equal("TestClass", symbol.Name);
        Assert.Equal(Microsoft.CodeAnalysis.SymbolKind.NamedType, symbol.Kind);
        Assert.Equal("TestNamespace", symbol.ContainingNamespace);
        Assert.Equal("/test/file.cs", symbol.FilePath);
        Assert.Equal(1, symbol.StartLine);
        Assert.Equal(100, symbol.EndLine);
    }

    [Fact]
    public void ReferenceLocation_Constructor_CreatesValidInstance()
    {
        // Act
        var location = new ReferenceLocation
        {
            FilePath = "/test/file.cs",
            StartLine = 42,
            StartColumn = 15,
            Context = "TestClass instance"
        };

        // Assert
        Assert.NotNull(location);
        Assert.Equal("/test/file.cs", location.FilePath);
        Assert.Equal(42, location.StartLine);
        Assert.Equal(15, location.StartColumn);
        Assert.Equal("TestClass instance", location.Context);
    }

    #endregion
}
