using Microsoft.Extensions.Logging;
using MCP.Server.Models;
using MCP.Server.Services;
using Xunit;
using Xunit.Abstractions;

namespace MCP.Tests.Services;

/// <summary>
/// Comprehensive tests for type analysis functionality
/// </summary>
public class TypeAnalysisServiceTests : IDisposable
{
    private readonly ILogger<RoslynAnalysisService> _logger;
    private readonly RoslynAnalysisService _analysisService;
    private readonly string _testSolutionPath;

    public TypeAnalysisServiceTests(ITestOutputHelper output)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddXUnit(output);
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        
        _logger = loggerFactory.CreateLogger<RoslynAnalysisService>();
        _analysisService = new RoslynAnalysisService(_logger);
        
        // Path to our test solution
        _testSolutionPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "TestData",
            "TypeAnalysisTestSolution",
            "TypeAnalysisTestSolution.sln"
        );
    }

    [Fact]
    public async Task LoadTestSolution_ShouldSucceed()
    {
        // Arrange & Act
        var result = await _analysisService.LoadSolutionAsync(_testSolutionPath);
        
        // Assert
        Assert.True(result, "Test solution should load successfully");
        
        var solutionInfo = await _analysisService.GetSolutionInfoAsync();
        Assert.NotNull(solutionInfo);
        Assert.True(solutionInfo.Projects.Count >= 2, "Should have at least 2 projects");
    }

    [Fact]
    public async Task FindTypeUsages_Customer_ShouldFindAllReferences()
    {
        // Arrange
        await _analysisService.LoadSolutionAsync(_testSolutionPath);
        
        // Act
        var result = await _analysisService.FindTypeUsagesAsync("Customer");
        
        // Assert
        Assert.True(result.Success);
        Assert.Equal("Customer", result.TypeName);
        Assert.True(result.TotalUsages > 0, "Should find Customer usages");
        
        // Should find various usage kinds
        Assert.Contains(TypeUsageKind.Declaration, result.UsagesByKind.Keys);
        Assert.Contains(TypeUsageKind.MethodParameter, result.UsagesByKind.Keys);
        Assert.Contains(TypeUsageKind.MethodReturnType, result.UsagesByKind.Keys);
        
        // Should find usages in multiple projects
        Assert.True(result.ProjectsWithUsages.Count >= 2, "Should find usages in multiple projects");
        
        // Verify specific usage details
        var declarationUsage = result.Usages.FirstOrDefault(u => u.UsageKind == TypeUsageKind.Declaration);
        Assert.NotNull(declarationUsage);
        Assert.True(declarationUsage.StartLine > 0);
        Assert.False(string.IsNullOrEmpty(declarationUsage.FilePath));
    }

    [Fact]
    public async Task FindTypeUsages_WithFullyQualifiedName_ShouldWork()
    {
        // Arrange
        await _analysisService.LoadSolutionAsync(_testSolutionPath);
        
        // Act
        var result = await _analysisService.FindTypeUsagesAsync("CoreLibrary.Models.Customer");
        
        // Assert
        Assert.True(result.Success);
        Assert.Equal("CoreLibrary.Models.Customer", result.FullTypeName);
        Assert.True(result.TotalUsages > 0);
    }

    [Fact]
    public async Task FindTypeUsages_Interface_ShouldFindImplementations()
    {
        // Arrange
        await _analysisService.LoadSolutionAsync(_testSolutionPath);
        
        // Act
        var result = await _analysisService.FindTypeUsagesAsync("ICustomer");
        
        // Assert
        Assert.True(result.Success);
        Assert.True(result.TotalUsages > 0);
        
        // Should find interface implementation
        Assert.Contains(TypeUsageKind.ImplementedInterface, result.UsagesByKind.Keys);
        
        var implementationUsage = result.Usages.FirstOrDefault(u => u.UsageKind == TypeUsageKind.ImplementedInterface);
        Assert.NotNull(implementationUsage);
        Assert.Contains("Customer", implementationUsage.Context);
    }

    [Fact]
    public async Task FindTypeUsages_Enum_ShouldFindAllUsages()
    {
        // Arrange
        await _analysisService.LoadSolutionAsync(_testSolutionPath);
        
        // Act
        var result = await _analysisService.FindTypeUsagesAsync("CustomerType");
        
        // Assert
        Assert.True(result.Success);
        Assert.True(result.TotalUsages > 0);
        
        // Should find enum usages in method parameters and comparisons
        var usageKinds = result.UsagesByKind.Keys.ToList();
        Assert.Contains(TypeUsageKind.MethodParameter, usageKinds);
    }

    [Fact]
    public async Task FindTypeUsages_Attribute_ShouldFindAttributeUsages()
    {
        // Arrange
        await _analysisService.LoadSolutionAsync(_testSolutionPath);
        
        // Act
        var result = await _analysisService.FindTypeUsagesAsync("CustomerRelatedAttribute");
        
        // Assert
        Assert.True(result.Success);
        Assert.True(result.TotalUsages > 0);
        
        // Should find attribute usages
        Assert.Contains(TypeUsageKind.AttributeUsage, result.UsagesByKind.Keys);
        
        var attributeUsage = result.Usages.FirstOrDefault(u => u.UsageKind == TypeUsageKind.AttributeUsage);
        Assert.NotNull(attributeUsage);
    }

    [Fact]
    public async Task FindTypeUsages_WithOptions_ShouldRespectFilters()
    {
        // Arrange
        await _analysisService.LoadSolutionAsync(_testSolutionPath);
        
        var options = new TypeUsageAnalysisOptions
        {
            IncludedUsageKinds = new List<TypeUsageKind> { TypeUsageKind.Declaration, TypeUsageKind.MethodParameter },
            MaxResults = 10
        };
        
        // Act
        var result = await _analysisService.FindTypeUsagesAsync("Customer", options);
        
        // Assert
        Assert.True(result.Success);
        Assert.True(result.Usages.Count <= 10);
        
        // Should only contain specified usage kinds
        foreach (var usage in result.Usages)
        {
            Assert.Contains(usage.UsageKind, options.IncludedUsageKinds);
        }
    }

    [Fact]
    public async Task FindTypeUsages_NonExistentType_ShouldReturnError()
    {
        // Arrange
        await _analysisService.LoadSolutionAsync(_testSolutionPath);
        
        // Act
        var result = await _analysisService.FindTypeUsagesAsync("NonExistentType");
        
        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Equal(0, result.TotalUsages);
    }

    [Fact]
    public async Task FindMemberUsages_Method_ShouldFindAllCalls()
    {
        // Arrange
        await _analysisService.LoadSolutionAsync(_testSolutionPath);
        
        // Act
        var result = await _analysisService.FindMemberUsagesAsync("Customer", "AddOrder");
        
        // Assert
        Assert.True(result.Success);
        Assert.Equal("AddOrder", result.MemberName);
        Assert.Equal("Customer", result.ContainingType);
        Assert.True(result.TotalUsages > 0);
        
        // Should find method calls
        Assert.Contains(MemberUsageKind.MethodCall, result.UsagesByKind.Keys);
        
        var methodCall = result.Usages.FirstOrDefault(u => u.UsageKind == MemberUsageKind.MethodCall);
        Assert.NotNull(methodCall);
        Assert.Contains("AddOrder", methodCall.CodeSnippet);
    }

    [Fact]
    public async Task FindMemberUsages_Property_ShouldFindAccessAndSet()
    {
        // Arrange
        await _analysisService.LoadSolutionAsync(_testSolutionPath);
        
        // Act
        var result = await _analysisService.FindMemberUsagesAsync("Customer", "Name");
        
        // Assert
        Assert.True(result.Success);
        Assert.True(result.TotalUsages > 0);
        
        // Should find both property access and set operations
        var usageKinds = result.UsagesByKind.Keys.ToList();
        Assert.True(usageKinds.Contains(MemberUsageKind.PropertyAccess) || 
                   usageKinds.Contains(MemberUsageKind.PropertySet));
    }

    [Fact]
    public async Task GetTypeDependencies_Customer_ShouldFindAllDependencies()
    {
        // Arrange
        await _analysisService.LoadSolutionAsync(_testSolutionPath);
        
        // Act
        var result = await _analysisService.GetTypeDependenciesAsync("Customer");
        
        // Assert
        Assert.True(result.Success);
        Assert.Equal("Customer", result.AnalyzedType);
        Assert.True(result.TotalDependencies > 0);
        
        // Should find dependencies on Address, Order, ICustomer, etc.
        var dependencyTypes = result.Dependencies.Select(d => d.DependencyType).ToList();
        Assert.Contains("Address", dependencyTypes);
        Assert.Contains("Order", dependencyTypes);
        Assert.Contains("ICustomer", dependencyTypes);
        
        // Should have different dependency kinds
        var dependencyKinds = result.Dependencies.Select(d => d.Kind).Distinct().ToList();
        Assert.Contains(DependencyKind.Implementation, dependencyKinds);
        Assert.Contains(DependencyKind.Composition, dependencyKinds);
    }

    [Fact]
    public async Task GetTypeDependents_Customer_ShouldFindDependentTypes()
    {
        // Arrange
        await _analysisService.LoadSolutionAsync(_testSolutionPath);
        
        // Act
        var result = await _analysisService.GetTypeDependentsAsync("Customer");
        
        // Assert
        Assert.True(result.Success);
        Assert.True(result.TotalDependents > 0);
        
        // Should find types that depend on Customer (like CustomerService)
        var dependentTypes = result.Dependents.Select(d => d.DependentType).ToList();
        Assert.Contains("CustomerService", dependentTypes);
    }

    [Fact]
    public async Task AnalyzeImpactScope_Customer_ShouldShowMultiProjectImpact()
    {
        // Arrange
        await _analysisService.LoadSolutionAsync(_testSolutionPath);
        
        // Act
        var result = await _analysisService.AnalyzeImpactScopeAsync("Customer");
        
        // Assert
        Assert.True(result.Success);
        Assert.Equal("Customer", result.AnalyzedItem);
        Assert.True(result.Scope >= ImpactScope.MultipleProjets);
        Assert.True(result.AffectedProjects.Count >= 2);
        Assert.True(result.AffectedUsages.Count > 0);
    }

    [Fact]
    public async Task ValidateRenameSafety_ValidRename_ShouldBeMarkedSafe()
    {
        // Arrange
        await _analysisService.LoadSolutionAsync(_testSolutionPath);
        
        // Act
        var result = await _analysisService.ValidateRenameSafetyAsync("Customer", "CustomerEntity");
        
        // Assert
        Assert.True(result.Success);
        Assert.True(result.IsSafeToRename);
        Assert.Equal("Customer", result.CurrentName);
        Assert.Equal("CustomerEntity", result.ProposedName);
        Assert.Empty(result.Conflicts);
    }

    [Fact]
    public async Task ValidateRenameSafety_ConflictingName_ShouldDetectConflict()
    {
        // Arrange
        await _analysisService.LoadSolutionAsync(_testSolutionPath);
        
        // Act - Try to rename Customer to Order (which already exists)
        var result = await _analysisService.ValidateRenameSafetyAsync("Customer", "Order");
        
        // Assert
        Assert.True(result.Success);
        Assert.False(result.IsSafeToRename);
        Assert.True(result.Conflicts.Count > 0);
        Assert.Contains("Order", result.Conflicts[0]);
    }

    [Fact]
    public async Task PreviewRenameImpact_ShouldShowAllAffectedLocations()
    {
        // Arrange
        await _analysisService.LoadSolutionAsync(_testSolutionPath);
        
        // Act
        var result = await _analysisService.PreviewRenameImpactAsync("Customer", "CustomerEntity");
        
        // Assert
        Assert.True(result.Success);
        Assert.True(result.AffectedUsages.Count > 0);
        
        // Should show specific file locations that would be affected
        foreach (var usage in result.AffectedUsages)
        {
            Assert.False(string.IsNullOrEmpty(usage.FilePath));
            Assert.True(usage.StartLine > 0);
            Assert.False(string.IsNullOrEmpty(usage.CodeSnippet));
        }
    }

    [Fact]
    public async Task FindNamespaceUsages_ShouldFindUsingStatements()
    {
        // Arrange
        await _analysisService.LoadSolutionAsync(_testSolutionPath);
        
        // Act
        var result = await _analysisService.FindNamespaceUsagesAsync("CoreLibrary.Models");
        
        // Assert
        Assert.True(result.Success);
        Assert.True(result.TotalUsages > 0);
        
        // Should find using statements
        Assert.Contains(TypeUsageKind.UsingDirective, result.UsagesByKind.Keys);
        
        var usingUsage = result.Usages.FirstOrDefault(u => u.UsageKind == TypeUsageKind.UsingDirective);
        Assert.NotNull(usingUsage);
        Assert.Contains("using", usingUsage.CodeSnippet);
    }

    public void Dispose()
    {
        _analysisService?.Dispose();
    }
}
