# .NET Static Analysis MCP Server - API Reference

## Overview

This document provides detailed API reference for all MCP tools, data models, and response formats provided by the .NET Static Analysis MCP Server.

## MCP Tools

### LoadSolution

Load a .NET solution file for analysis.

**Method**: `LoadSolution`

**Parameters**:
| Name | Type | Required | Description |
|------|------|----------|-------------|
| `solutionPath` | string | Yes | Absolute path to the .sln file to load |

**Request Example**:
```json
{
  "tool": "LoadSolution",
  "arguments": {
    "solutionPath": "/workspace/MyProject/MyProject.sln"
  }
}
```

**Response Format**:
```json
{
  "success": boolean,
  "message": string
}
```

**Success Response Example**:
```json
{
  "success": true,
  "message": "Solution loaded successfully"
}
```

**Error Response Example**:
```json
{
  "success": false,
  "error": "Solution file not found: /invalid/path/solution.sln"
}
```

**Error Conditions**:
- File not found
- Invalid solution format
- Permission denied
- MSBuild compatibility issues

---

### GetCompilationErrors

Get all compilation errors and warnings from the loaded solution.

**Method**: `GetCompilationErrors`

**Parameters**: None

**Request Example**:
```json
{
  "tool": "GetCompilationErrors",
  "arguments": {}
}
```

**Response Format**:
```json
{
  "success": boolean,
  "error_count": number,
  "warning_count": number,
  "errors": CompilationError[]
}
```

**Success Response Example**:
```json
{
  "success": true,
  "error_count": 2,
  "warning_count": 3,
  "errors": [
    {
      "Id": "CS0103",
      "Title": "The name 'xyz' does not exist in the current context",
      "Message": "The name 'undefinedVariable' does not exist in the current context",
      "Severity": "Error",
      "FilePath": "/workspace/MyProject/Program.cs",
      "StartLine": 15,
      "StartColumn": 13,
      "EndLine": 15,
      "EndColumn": 29,
      "Category": "Compiler",
      "HelpLink": null,
      "IsWarningAsError": false,
      "WarningLevel": 0,
      "CustomTags": "",
      "ProjectName": "MyProject"
    }
  ]
}
```

**Error Response Example**:
```json
{
  "success": false,
  "error": "No solution loaded. Call LoadSolution first."
}
```

**Notes**:
- Results are limited to 100 errors for performance
- Hidden diagnostics are filtered out unless they are errors
- Errors are returned in the order they appear in the compilation

---

### GetSolutionInfo

Get comprehensive information about the loaded solution.

**Method**: `GetSolutionInfo`

**Parameters**: None

**Request Example**:
```json
{
  "tool": "GetSolutionInfo",
  "arguments": {}
}
```

**Response Format**:
```json
{
  "success": boolean,
  "solution_info": SolutionInfo
}
```

**Success Response Example**:
```json
{
  "success": true,
  "solution_info": {
    "Name": "MyProject",
    "FilePath": "/workspace/MyProject/MyProject.sln",
    "Projects": [
      {
        "Name": "MyProject.Core",
        "FilePath": "/workspace/MyProject/MyProject.Core/MyProject.Core.csproj",
        "TargetFramework": "net9.0",
        "OutputType": "Library",
        "SourceFiles": [
          "/workspace/MyProject/MyProject.Core/Models/User.cs",
          "/workspace/MyProject/MyProject.Core/Services/UserService.cs"
        ],
        "References": [
          "System.Runtime",
          "Microsoft.Extensions.DependencyInjection"
        ],
        "PackageReferences": [
          "Newtonsoft.Json"
        ],
        "ErrorCount": 0,
        "WarningCount": 2,
        "HasCompilationErrors": false
      }
    ],
    "TotalErrors": 2,
    "TotalWarnings": 5,
    "HasCompilationErrors": true
  }
}
```

**Error Response Example**:
```json
{
  "success": false,
  "error": "No solution loaded. Call LoadSolution first."
}
```

---

### AnalyzeFile

Analyze a specific file for errors and warnings.

**Method**: `AnalyzeFile`

**Parameters**:
| Name | Type | Required | Description |
|------|------|----------|-------------|
| `filePath` | string | Yes | Absolute path to the .cs file to analyze |

**Request Example**:
```json
{
  "tool": "AnalyzeFile",
  "arguments": {
    "filePath": "/workspace/MyProject/Controllers/HomeController.cs"
  }
}
```

**Response Format**:
```json
{
  "success": boolean,
  "file_path": string,
  "error_count": number,
  "warning_count": number,
  "errors": CompilationError[]
}
```

**Success Response Example**:
```json
{
  "success": true,
  "file_path": "/workspace/MyProject/Controllers/HomeController.cs",
  "error_count": 1,
  "warning_count": 0,
  "errors": [
    {
      "Id": "CS0246",
      "Title": "The type or namespace name 'xyz' could not be found",
      "Message": "The type or namespace name 'InvalidType' could not be found (are you missing a using directive or an assembly reference?)",
      "Severity": "Error",
      "FilePath": "/workspace/MyProject/Controllers/HomeController.cs",
      "StartLine": 8,
      "StartColumn": 9,
      "EndLine": 8,
      "EndColumn": 20,
      "Category": "Compiler",
      "ProjectName": "MyProject.Web"
    }
  ]
}
```

**Error Response Example**:
```json
{
  "success": false,
  "error": "File not found in loaded solution: /invalid/path/file.cs"
}
```

**Error Conditions**:
- No solution loaded
- File not found in solution
- File is not a C# source file

## Data Models

### CompilationError

Represents a compilation error, warning, or informational message.

```typescript
interface CompilationError {
  Id: string;                    // Error code (e.g., "CS0103", "CS0246")
  Title: string;                 // Short description of the error
  Message: string;               // Full error message
  Severity: DiagnosticSeverity;  // "Error", "Warning", "Info", "Hidden"
  FilePath: string;              // Absolute path to the source file
  StartLine: number;             // 1-based line number where error starts
  StartColumn: number;           // 1-based column number where error starts
  EndLine: number;               // 1-based line number where error ends
  EndColumn: number;             // 1-based column number where error ends
  Category: string;              // Error category (e.g., "Compiler")
  HelpLink?: string;             // URL to help documentation (optional)
  IsWarningAsError: boolean;     // Whether warning is treated as error
  WarningLevel: number;          // Warning level (0-4)
  CustomTags: string;            // Comma-separated custom tags
  ProjectName: string;           // Name of the containing project
}
```

**DiagnosticSeverity Values**:
- `"Error"`: Compilation error that prevents building
- `"Warning"`: Potential issue that doesn't prevent building
- `"Info"`: Informational message
- `"Hidden"`: Background analysis result

### ProjectInfo

Represents information about a project in the solution.

```typescript
interface ProjectInfo {
  Name: string;                  // Project name
  FilePath: string;              // Absolute path to .csproj file
  TargetFramework: string;       // Target framework (e.g., "net9.0")
  OutputType: string;            // Output type (e.g., "Library", "Exe")
  SourceFiles: string[];         // List of source file paths
  References: string[];          // List of assembly references
  PackageReferences: string[];   // List of NuGet package references
  ErrorCount: number;            // Number of compilation errors
  WarningCount: number;          // Number of compilation warnings
  HasCompilationErrors: boolean; // Whether project has any errors
}
```

### SolutionInfo

Represents information about the entire solution.

```typescript
interface SolutionInfo {
  Name: string;                  // Solution name
  FilePath: string;              // Absolute path to .sln file
  Projects: ProjectInfo[];       // List of projects in solution
  TotalErrors: number;           // Total errors across all projects
  TotalWarnings: number;         // Total warnings across all projects
  HasCompilationErrors: boolean; // Whether solution has any errors
}
```

## Error Handling

### Standard Error Response

All tools return a consistent error response format when operations fail:

```json
{
  "success": false,
  "error": "Human-readable error message"
}
```

### Common Error Messages

| Error Message | Cause | Solution |
|---------------|-------|----------|
| "No solution loaded" | Tool called before LoadSolution | Call LoadSolution first |
| "Solution file not found" | Invalid file path | Check file path and permissions |
| "File not found in loaded solution" | File not part of solution | Verify file is in a project |
| "Failed to load solution" | MSBuild/Roslyn error | Check solution format and dependencies |

## Performance Considerations

### Response Limits

- **GetCompilationErrors**: Limited to 100 errors for performance
- **Large Solutions**: Initial loading may take several seconds
- **Memory Usage**: Large solutions consume more memory

### Optimization Tips

1. **Load Once**: Load solution once, then use other tools multiple times
2. **File Analysis**: Use AnalyzeFile for focused analysis instead of GetCompilationErrors
3. **Error Filtering**: Filter results on client side if needed

## Version Compatibility

### MCP Protocol

- **Supported Version**: 2024-11-05
- **Transport**: stdio (stdin/stdout)
- **Encoding**: UTF-8 JSON-RPC

### .NET Compatibility

- **Server Runtime**: .NET 9.0+
- **Analyzed Solutions**: .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5.0+
- **Project Types**: Console, Library, Web, Test projects

### Roslyn Compatibility

- **Microsoft.CodeAnalysis**: 4.14.0+
- **MSBuild**: 17.0+
- **Language Versions**: C# 1.0 through C# 13.0

## Rate Limiting

Currently, no rate limiting is implemented. Consider implementing client-side throttling for:
- Rapid successive calls to GetCompilationErrors
- Frequent solution reloading
- Large batch analysis operations

## Security Considerations

### File Access

- Server only reads files, never writes
- Respects file system permissions
- No arbitrary code execution
- Path traversal protection

### Input Validation

- All file paths are validated
- Solution files are parsed safely
- No user code execution during analysis

## Future API Extensions

### Planned Tools

- `GetSymbolInfo`: Detailed symbol analysis
- `FindReferences`: Find all references to a symbol
- `GetCodeMetrics`: Code complexity and quality metrics
- `RunCustomAnalyzers`: Execute custom Roslyn analyzers

### Planned Data Models

- `SymbolInfo`: Detailed symbol information
- `ReferenceLocation`: Symbol reference locations
- `CodeMetrics`: Quality and complexity metrics
