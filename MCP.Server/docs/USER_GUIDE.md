# .NET Static Analysis MCP Server - User Guide

## Overview

The .NET Static Analysis MCP Server provides AI agents with powerful static analysis capabilities for .NET solutions using Microsoft's Roslyn compiler platform. This allows agents to understand and analyze .NET codebases without needing to build them.

## Quick Start

### 1. Prerequisites

- .NET 9.0 SDK or later
- A .NET solution (.sln file) to analyze
- An MCP-compatible client (VS Code with GitHub Copilot, Claude Desktop, etc.)

### 2. Build the Server

```bash
cd MCP.Server
dotnet build
```

### 3. Configure Your MCP Client

#### For VS Code with GitHub Copilot

Create or update `.vscode/mcp.json` in your workspace:

```json
{
  "inputs": [],
  "servers": {
    "dotnet-static-analysis": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/absolute/path/to/MCP.Server/MCP.Server.csproj"
      ],
      "env": {}
    }
  }
}
```

#### For Claude Desktop

Add to your Claude Desktop configuration:

```json
{
  "mcpServers": {
    "dotnet-static-analysis": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/absolute/path/to/MCP.Server/MCP.Server.csproj"
      ]
    }
  }
}
```

### 4. Test the Connection

The server will automatically start when your MCP client connects. You should see log messages indicating successful startup.

## Available Tools

### LoadSolution

**Purpose**: Load a .NET solution file for analysis

**Parameters**:
- `solutionPath` (string): Absolute path to the .sln file

**Example Usage**:
```
Load the solution at /workspace/MyProject/MyProject.sln
```

**Response**:
```json
{
  "success": true,
  "message": "Solution loaded successfully"
}
```

### GetCompilationErrors

**Purpose**: Get all compilation errors and warnings from the loaded solution

**Parameters**: None

**Example Usage**:
```
Show me all compilation errors in the loaded solution
```

**Response**:
```json
{
  "success": true,
  "error_count": 3,
  "warning_count": 5,
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
      "ProjectName": "MyProject"
    }
  ]
}
```

### GetSolutionInfo

**Purpose**: Get comprehensive information about the loaded solution

**Parameters**: None

**Example Usage**:
```
Give me information about the solution structure
```

**Response**:
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
        "SourceFiles": ["/workspace/MyProject/MyProject.Core/Class1.cs"],
        "ErrorCount": 0,
        "WarningCount": 2,
        "HasCompilationErrors": false
      }
    ],
    "TotalErrors": 3,
    "TotalWarnings": 5,
    "HasCompilationErrors": true
  }
}
```

### AnalyzeFile

**Purpose**: Analyze a specific file for errors and warnings

**Parameters**:
- `filePath` (string): Absolute path to the .cs file to analyze

**Example Usage**:
```
Analyze the file /workspace/MyProject/Program.cs for errors
```

**Response**:
```json
{
  "success": true,
  "file_path": "/workspace/MyProject/Program.cs",
  "error_count": 1,
  "warning_count": 0,
  "errors": [
    {
      "Id": "CS0103",
      "Message": "The name 'undefinedVariable' does not exist in the current context",
      "Severity": "Error",
      "StartLine": 15,
      "StartColumn": 13
    }
  ]
}
```

## Common Workflows

### 1. Initial Solution Analysis

```
1. Load solution: "Load the solution at /workspace/MyProject/MyProject.sln"
2. Get overview: "Show me information about the solution structure"
3. Check errors: "Show me all compilation errors in the loaded solution"
```

### 2. File-Specific Analysis

```
1. Load solution (if not already loaded)
2. Analyze file: "Analyze /workspace/MyProject/Controllers/HomeController.cs for errors"
3. Get context: "What other files in the solution might be affected by errors in HomeController.cs?"
```

### 3. Error Investigation

```
1. Get all errors: "Show me all compilation errors"
2. Focus on specific error: "Analyze the file with CS0103 errors"
3. Get project context: "What projects are affected by compilation errors?"
```

## Error Types and Meanings

### Common Error IDs

- **CS0103**: Name does not exist in current context (missing using statements, typos)
- **CS0246**: Type or namespace not found (missing references, wrong namespace)
- **CS1002**: Syntax error (missing semicolons, brackets)
- **CS0161**: Not all code paths return a value
- **CS0029**: Cannot implicitly convert type

### Severity Levels

- **Error**: Prevents compilation, must be fixed
- **Warning**: Potential issues, compilation succeeds
- **Info**: Informational messages
- **Hidden**: Background analysis, usually not shown

## Troubleshooting

### Server Won't Start

1. **Check .NET Version**: Ensure .NET 9.0+ is installed
2. **Verify Paths**: Use absolute paths in MCP configuration
3. **Check Permissions**: Ensure read access to solution files
4. **Review Logs**: Check stderr output for error messages

### Solution Won't Load

1. **File Exists**: Verify the .sln file path is correct
2. **Valid Solution**: Ensure the solution file isn't corrupted
3. **Dependencies**: Check that all referenced projects exist
4. **MSBuild**: Ensure MSBuild can process the solution

### No Errors Returned

1. **Solution Loaded**: Verify LoadSolution returned success
2. **Valid Projects**: Check that projects in solution are valid
3. **Source Files**: Ensure projects contain .cs files
4. **Compilation**: Try building the solution manually to verify errors exist

## Performance Tips

### Large Solutions

- **Incremental Analysis**: Load solution once, then use AnalyzeFile for specific files
- **Error Limiting**: The server limits results to 100 errors for performance
- **Memory Usage**: Large solutions may require more memory

### Frequent Analysis

- **Keep Solution Loaded**: Avoid reloading the same solution repeatedly
- **File-Level Analysis**: Use AnalyzeFile for real-time feedback
- **Batch Requests**: Group related analysis requests together

## Integration Examples

### VS Code Extension

```typescript
// Example of calling MCP tools from VS Code extension
const result = await mcpClient.callTool('LoadSolution', {
  solutionPath: workspace.workspaceFolders[0].uri.fsPath + '/MySolution.sln'
});

if (result.success) {
  const errors = await mcpClient.callTool('GetCompilationErrors', {});
  // Process errors...
}
```

### AI Agent Prompts

```
"I need to analyze a .NET solution. First, load the solution at /workspace/MyProject/MyProject.sln, then show me all compilation errors and group them by project."

"Check the file /workspace/MyProject/Services/UserService.cs for any compilation errors and suggest fixes."

"What's the overall health of the solution? Show me error counts by project and identify the most problematic files."
```

## Best Practices

1. **Always Load First**: Call LoadSolution before other analysis tools
2. **Use Absolute Paths**: Relative paths may not resolve correctly
3. **Handle Errors Gracefully**: Check the success field in responses
4. **Batch Analysis**: Group related requests to minimize overhead
5. **Monitor Performance**: Large solutions may take time to load initially

## Limitations

- **Build Dependencies**: Cannot analyze solutions that require custom build steps
- **Generated Code**: May not see errors in code generated during build
- **External Tools**: Cannot analyze code that depends on external code generation
- **Platform Specific**: Some platform-specific code may not analyze correctly on different OS

## Next Steps

After mastering basic usage, explore:
- Advanced error analysis patterns
- Integration with CI/CD pipelines
- Custom analyzer integration
- Symbol and reference analysis (future features)
