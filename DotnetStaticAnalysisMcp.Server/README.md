# .NET Static Analysis MCP Server

This MCP (Model Context Protocol) server provides comprehensive static analysis capabilities for .NET solutions using Microsoft's Roslyn compiler platform. It allows AI agents to analyze .NET codebases without needing to build them, providing detailed information about compilation errors, warnings, project structure, and code symbols.

## Features

- **Load .NET Solutions**: Load and analyze entire .NET solution files (.sln)
- **Compilation Error Detection**: Get detailed compilation errors and warnings without building
- **Project Information**: Retrieve comprehensive information about projects in the solution
- **File-Level Analysis**: Analyze specific files for errors and warnings
- **Symbol Analysis**: Get detailed information about code symbols (classes, methods, properties, etc.)
- **Reference Tracking**: Find all references to symbols across the codebase

## Installation and Setup

1. **Build the server:**
   ```bash
   dotnet build MCP.Server
   ```

2. **Configure VS Code (or other MCP client):**

   Create or update your `.vscode/mcp.json` file:
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
           "/full/path/to/MCP.Server/MCP.Server.csproj"
         ]
       }
     }
   }
   ```

## Available Tools

### `LoadSolution`
Load a .NET solution file for analysis.

**Parameters:**
- `solutionPath` (string): Path to the .sln file to load

**Example:**
```json
{
  "solutionPath": "/path/to/your/solution.sln"
}
```

### `GetCompilationErrors`
Get all compilation errors and warnings from the loaded solution.

**Parameters:** None

**Returns:**
- List of compilation errors with detailed location and severity information
- Error and warning counts
- Project-specific error breakdown

### `GetSolutionInfo`
Get comprehensive information about the loaded solution.

**Parameters:** None

**Returns:**
- Solution metadata (name, path, projects)
- Project details (target framework, output type, source files, references)
- Compilation status for each project

### `AnalyzeFile`
Analyze a specific file for errors and warnings.

**Parameters:**
- `filePath` (string): Path to the file to analyze

**Example:**
```json
{
  "filePath": "/path/to/your/file.cs"
}
```

## Available Resources

This MCP server currently focuses on tools rather than resources. Future versions may include:
- Solution analysis summaries
- Project dependency graphs
- Code metrics and statistics

## Usage Examples

### Basic Workflow

1. **Load a solution:**
   ```json
   {
     "tool": "LoadSolution",
     "arguments": {
       "solutionPath": "/workspace/MyProject/MyProject.sln"
     }
   }
   ```

2. **Get compilation errors:**
   ```json
   {
     "tool": "GetCompilationErrors",
     "arguments": {}
   }
   ```

3. **Analyze specific file:**
   ```json
   {
     "tool": "AnalyzeFile",
     "arguments": {
       "filePath": "/workspace/MyProject/Program.cs"
     }
   }
   ```

### Error Response Format

Each compilation error includes:
- `Id`: Error/warning identifier (e.g., "CS0103")
- `Title`: Short description of the error
- `Message`: Detailed error message
- `Severity`: Error, Warning, Info, or Hidden
- `FilePath`: Path to the file containing the error
- `StartLine`/`EndLine`: Line numbers where the error occurs
- `StartColumn`/`EndColumn`: Column positions
- `Category`: Error category (e.g., "Compiler")
- `ProjectName`: Name of the project containing the error

3. **Test the server:**
   The server will automatically start when accessed by your MCP client (like VS Code with GitHub Copilot).

## Requirements

- .NET 9.0 or later
- Access to .NET solution files and source code
- Sufficient permissions to read project files and references

## Supported Project Types

- .NET Framework projects
- .NET Core/.NET 5+ projects
- .NET Standard libraries
- ASP.NET Core applications
- Console applications
- Class libraries
- Test projects

## Performance Considerations

- Large solutions may take time to load initially
- Compilation analysis is performed in-memory without building
- Error results are limited to 100 items by default for performance
- The server caches loaded solutions for subsequent analysis

## Error Handling

The server gracefully handles:
- Invalid solution paths
- Corrupted project files
- Missing references
- Syntax errors in source files
- Permission issues

All errors are logged and returned in a structured format for easy consumption by AI agents.

## Use Cases

This MCP server is ideal for:
- **Code Review Assistance**: Quickly identify compilation issues before building
- **Refactoring Support**: Understand code structure and dependencies
- **Documentation Generation**: Extract symbol information and project structure
- **Quality Analysis**: Identify common error patterns and code issues
- **Migration Planning**: Analyze existing codebases for compatibility issues
- **Educational Tools**: Help understand .NET project structure and common errors

## Technical Details

- Built on Microsoft.CodeAnalysis (Roslyn) 4.14.0
- Uses MSBuildWorkspace for solution loading
- Implements MCP protocol via mcpdotnet library
- Supports real-time analysis without compilation
- Thread-safe for concurrent analysis requests
