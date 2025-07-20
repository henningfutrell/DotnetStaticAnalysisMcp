using System.Text.Json.Serialization;

namespace DotnetStaticAnalysisMcp.Server.Models;

/// <summary>
/// Represents a type usage reference found in the codebase
/// </summary>
public class TypeUsageReference
{
    public string FilePath { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public int StartLine { get; set; }
    public int StartColumn { get; set; }
    public int EndLine { get; set; }
    public int EndColumn { get; set; }
    public TypeUsageKind UsageKind { get; set; }
    public string Context { get; set; } = string.Empty;
    public string CodeSnippet { get; set; } = string.Empty;
    public bool IsInDocumentation { get; set; }
    public string? ContainingMember { get; set; }
    public string? ContainingType { get; set; }
}

/// <summary>
/// Types of type usage
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TypeUsageKind
{
    Declaration,
    Instantiation,
    MethodParameter,
    MethodReturnType,
    PropertyType,
    FieldType,
    GenericTypeArgument,
    BaseClass,
    ImplementedInterface,
    AttributeUsage,
    CastOperation,
    TypeOfExpression,
    IsExpression,
    AsExpression,
    UsingDirective,
    FullyQualifiedReference,
    XmlDocumentation,
    LocalVariable,
    EventType
}

/// <summary>
/// Result of type usage analysis
/// </summary>
public class TypeUsageAnalysisResult
{
    public bool Success { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string FullTypeName { get; set; } = string.Empty;
    public string? Namespace { get; set; }
    public int TotalUsages { get; set; }
    public List<TypeUsageReference> Usages { get; set; } = new();
    public Dictionary<TypeUsageKind, int> UsagesByKind { get; set; } = new();
    public List<string> ProjectsWithUsages { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Represents a member usage reference
/// </summary>
public class MemberUsageReference
{
    public string FilePath { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public int StartLine { get; set; }
    public int StartColumn { get; set; }
    public int EndLine { get; set; }
    public int EndColumn { get; set; }
    public MemberUsageKind UsageKind { get; set; }
    public string Context { get; set; } = string.Empty;
    public string CodeSnippet { get; set; } = string.Empty;
    public string? ContainingMember { get; set; }
    public string? ContainingType { get; set; }
}

/// <summary>
/// Types of member usage
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MemberUsageKind
{
    MethodCall,
    PropertyAccess,
    PropertySet,
    FieldAccess,
    FieldSet,
    EventSubscription,
    EventUnsubscription,
    MethodGroup,
    Override,
    Implementation,
    XmlDocumentation
}

/// <summary>
/// Result of member usage analysis
/// </summary>
public class MemberUsageAnalysisResult
{
    public bool Success { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string ContainingType { get; set; } = string.Empty;
    public string MemberKind { get; set; } = string.Empty;
    public int TotalUsages { get; set; }
    public List<MemberUsageReference> Usages { get; set; } = new();
    public Dictionary<MemberUsageKind, int> UsagesByKind { get; set; } = new();
    public List<string> ProjectsWithUsages { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Dependency relationship between types
/// </summary>
public class TypeDependency
{
    public string DependentType { get; set; } = string.Empty;
    public string DependencyType { get; set; } = string.Empty;
    public DependencyKind Kind { get; set; }
    public string? Context { get; set; }
    public List<TypeUsageReference> References { get; set; } = new();
}

/// <summary>
/// Types of dependencies between types
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DependencyKind
{
    Inheritance,
    Implementation,
    Composition,
    Aggregation,
    Usage,
    GenericConstraint,
    Attribute
}

/// <summary>
/// Result of dependency analysis
/// </summary>
public class DependencyAnalysisResult
{
    public bool Success { get; set; }
    public string AnalyzedType { get; set; } = string.Empty;
    public List<TypeDependency> Dependencies { get; set; } = new();
    public List<TypeDependency> Dependents { get; set; } = new();
    public int TotalDependencies { get; set; }
    public int TotalDependents { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Impact analysis for potential changes
/// </summary>
public class ImpactAnalysisResult
{
    public bool Success { get; set; }
    public string AnalyzedItem { get; set; } = string.Empty;
    public ImpactScope Scope { get; set; }
    public List<string> AffectedProjects { get; set; } = new();
    public List<TypeUsageReference> AffectedUsages { get; set; } = new();
    public List<string> PotentialBreakingChanges { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Scope of impact for changes
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ImpactScope
{
    None,
    SameFile,
    SameProject,
    MultipleProjets,
    EntireSolution
}

/// <summary>
/// Result of rename safety validation
/// </summary>
public class RenameSafetyResult
{
    public bool Success { get; set; }
    public bool IsSafeToRename { get; set; }
    public string CurrentName { get; set; } = string.Empty;
    public string ProposedName { get; set; } = string.Empty;
    public List<string> Conflicts { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<TypeUsageReference> AffectedUsages { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Options for type usage analysis
/// </summary>
public class TypeUsageAnalysisOptions
{
    public bool IncludeDocumentation { get; set; } = true;
    public bool IncludeGeneratedCode { get; set; } = false;
    public List<TypeUsageKind> IncludedUsageKinds { get; set; } = new();
    public List<string> ExcludedProjects { get; set; } = new();
    public bool GroupByProject { get; set; } = false;
    public int MaxResults { get; set; } = 1000;
}
