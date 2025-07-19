using Microsoft.CodeAnalysis;

namespace MCP.Server.Models;

/// <summary>
/// Represents detailed information about a symbol in the code
/// </summary>
public class SymbolInfo
{
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public SymbolKind Kind { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string ContainingNamespace { get; set; } = string.Empty;
    public string ContainingType { get; set; } = string.Empty;
    public Accessibility DeclaredAccessibility { get; set; }
    public bool IsStatic { get; set; }
    public bool IsAbstract { get; set; }
    public bool IsVirtual { get; set; }
    public bool IsOverride { get; set; }
    public bool IsSealed { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public int StartLine { get; set; }
    public int StartColumn { get; set; }
    public int EndLine { get; set; }
    public int EndColumn { get; set; }
    public string Documentation { get; set; } = string.Empty;
    public List<string> Attributes { get; set; } = new();
    public List<ReferenceLocation> References { get; set; } = new();
}

/// <summary>
/// Represents a location where a symbol is referenced
/// </summary>
public class ReferenceLocation
{
    public string FilePath { get; set; } = string.Empty;
    public int StartLine { get; set; }
    public int StartColumn { get; set; }
    public int EndLine { get; set; }
    public int EndColumn { get; set; }
    public string Context { get; set; } = string.Empty;
    public bool IsDefinition { get; set; }
}
