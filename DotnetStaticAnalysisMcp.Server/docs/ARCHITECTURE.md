# .NET Static Analysis MCP Server - Architecture Documentation

## Overview

This document explains the internal architecture and design decisions of the .NET Static Analysis MCP Server. It covers how the system works, why certain choices were made, and how the components interact.

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    MCP Client                               │
│              (VS Code, Claude Desktop, etc.)               │
└─────────────────────┬───────────────────────────────────────┘
                      │ MCP Protocol (JSON-RPC over stdio)
                      │
┌─────────────────────▼───────────────────────────────────────┐
│                 MCP Server Host                             │
│  ┌─────────────────────────────────────────────────────────┐│
│  │            ModelContextProtocol.Server                 ││
│  │              (Microsoft Official SDK)                  ││
│  └─────────────────────┬───────────────────────────────────┘│
│                        │                                    │
│  ┌─────────────────────▼───────────────────────────────────┐│
│  │              DotNetAnalysisTools                        ││
│  │            (MCP Tool Implementations)                   ││
│  └─────────────────────┬───────────────────────────────────┘│
└──────────────────────┬─┴───────────────────────────────────────┘
                       │
┌──────────────────────▼───────────────────────────────────────┐
│              RoslynAnalysisService                          │
│  ┌─────────────────────────────────────────────────────────┐│
│  │            MSBuildWorkspace                             ││
│  │         (Microsoft.CodeAnalysis)                        ││
│  └─────────────────────┬───────────────────────────────────┘│
│                        │                                    │
│  ┌─────────────────────▼───────────────────────────────────┐│
│  │              Solution/Project                           ││
│  │               Analysis Engine                           ││
│  └─────────────────────┬───────────────────────────────────┘│
└──────────────────────┬─┴───────────────────────────────────────┘
                       │
┌──────────────────────▼───────────────────────────────────────┐
│                .NET Solution Files                          │
│              (.sln, .csproj, .cs files)                    │
└─────────────────────────────────────────────────────────────┘
```

## Core Components

### 1. MCP Server Host (Program.cs)

**Purpose**: Application entry point and dependency injection setup

**Key Responsibilities**:
- Configure the .NET Generic Host
- Set up logging to stderr (required for MCP)
- Register services with dependency injection
- Configure the MCP server with stdio transport
- Enable automatic tool discovery

**Implementation Details**:
```csharp
builder.Services
    .AddMcpServer()                    // Register MCP server services
    .WithStdioServerTransport()        // Use stdin/stdout for communication
    .WithToolsFromAssembly();          // Auto-discover tools via attributes
```

**Design Decisions**:
- Uses .NET Generic Host for robust application lifecycle management
- Logs to stderr to avoid interfering with MCP protocol on stdout
- Leverages dependency injection for clean separation of concerns

### 2. DotNetAnalysisTools (McpServerService.cs)

**Purpose**: MCP tool implementations that expose analysis capabilities

**Key Responsibilities**:
- Define MCP tools using `[McpServerTool]` attributes
- Handle parameter validation and error handling
- Serialize responses to JSON for MCP clients
- Coordinate with RoslynAnalysisService for actual analysis

**Tool Implementation Pattern**:
```csharp
[McpServerTool, Description("Tool description")]
public static async Task<string> ToolName(
    RoslynAnalysisService analysisService,
    [Description("Parameter description")] string parameter)
{
    try
    {
        var result = await analysisService.SomeOperation(parameter);
        return JsonSerializer.Serialize(new { success = true, data = result });
    }
    catch (Exception ex)
    {
        return JsonSerializer.Serialize(new { success = false, error = ex.Message });
    }
}
```

**Design Decisions**:
- Static methods for tool implementations (required by MCP SDK)
- Dependency injection of RoslynAnalysisService via method parameters
- Consistent error handling and response format
- JSON serialization for structured data exchange

### 3. RoslynAnalysisService

**Purpose**: Core analysis engine using Microsoft Roslyn compiler APIs

**Key Responsibilities**:
- Load and manage .NET solutions using MSBuildWorkspace
- Extract compilation diagnostics (errors, warnings, info)
- Provide solution and project metadata
- Perform file-level analysis
- Manage workspace lifecycle and memory

**Core Workflow**:
```
LoadSolutionAsync()
    ↓
MSBuildWorkspace.OpenSolutionAsync()
    ↓
Solution object with Projects
    ↓
GetCompilationAsync() for each Project
    ↓
Extract Diagnostics and Metadata
    ↓
Transform to Domain Models
```

**Key Methods**:

#### LoadSolutionAsync()
- Creates new MSBuildWorkspace
- Loads solution file using Roslyn APIs
- Validates solution structure
- Caches solution for subsequent operations

#### GetCompilationErrorsAsync()
- Iterates through all projects in solution
- Gets compilation for each project
- Extracts diagnostics (errors, warnings, info)
- Filters out hidden diagnostics
- Transforms to CompilationError models

#### GetSolutionInfoAsync()
- Extracts solution metadata
- Analyzes each project for structure information
- Counts errors and warnings per project
- Builds comprehensive solution overview

#### AnalyzeFileAsync()
- Finds document in loaded solution
- Gets compilation for the document's project
- Filters diagnostics to specific file
- Returns file-specific analysis results

**Design Decisions**:
- Single workspace instance per service lifetime
- Async operations throughout for non-blocking analysis
- Comprehensive error handling with logging
- Memory management through proper disposal
- Caching of loaded solutions for performance

### 4. Data Models

**Purpose**: Structured representation of analysis results

#### CompilationError
```csharp
public class CompilationError
{
    public string Id { get; set; }              // CS0103, CS0246, etc.
    public string Title { get; set; }           // Short description
    public string Message { get; set; }         // Full error message
    public DiagnosticSeverity Severity { get; set; }  // Error, Warning, Info
    public string FilePath { get; set; }        // Source file path
    public int StartLine { get; set; }          // 1-based line number
    public int StartColumn { get; set; }        // 1-based column number
    public string Category { get; set; }        // Compiler, Analyzer, etc.
    public string ProjectName { get; set; }     // Containing project
    // ... additional metadata
}
```

#### ProjectInfo & SolutionInfo
- Hierarchical representation of solution structure
- Compilation status and error counts
- File inventories and dependency information
- Target framework and output type metadata

**Design Decisions**:
- Rich metadata for comprehensive analysis
- 1-based line/column numbers (standard for editors)
- Separate models for different analysis levels
- Extensible design for future enhancements

## Data Flow

### 1. Solution Loading Flow

```
MCP Client Request
    ↓
DotNetAnalysisTools.LoadSolution()
    ↓
RoslynAnalysisService.LoadSolutionAsync()
    ↓
MSBuildWorkspace.OpenSolutionAsync()
    ↓
Roslyn Solution Object
    ↓
Cache in Service Instance
    ↓
Return Success/Failure
    ↓
JSON Response to MCP Client
```

### 2. Error Analysis Flow

```
MCP Client Request
    ↓
DotNetAnalysisTools.GetCompilationErrors()
    ↓
RoslynAnalysisService.GetCompilationErrorsAsync()
    ↓
For Each Project in Solution:
    ↓
    GetCompilationAsync()
    ↓
    compilation.GetDiagnostics()
    ↓
    Filter and Transform Diagnostics
    ↓
    Create CompilationError Objects
    ↓
Aggregate All Errors
    ↓
Return List<CompilationError>
    ↓
JSON Serialization
    ↓
MCP Client Response
```

## Performance Considerations

### Memory Management

**Challenge**: Large solutions can consume significant memory
**Solution**: 
- Single workspace instance with proper disposal
- Lazy loading of compilation objects
- Garbage collection friendly patterns

### Loading Performance

**Challenge**: Initial solution loading can be slow
**Solution**:
- Async operations prevent blocking
- Caching of loaded solutions
- Incremental analysis capabilities

### Response Size Limiting

**Challenge**: Large solutions may have thousands of errors
**Solution**:
- Limit error results to 100 items by default
- Provide summary statistics
- Enable file-level analysis for focused results

## Error Handling Strategy

### Layered Error Handling

1. **MCP Tool Level**: Catch all exceptions, return structured error responses
2. **Service Level**: Log errors, throw specific exceptions with context
3. **Roslyn Level**: Handle Roslyn-specific exceptions and workspace issues

### Error Response Format

```json
{
  "success": false,
  "error": "Human-readable error message",
  "details": "Additional technical details (optional)"
}
```

### Logging Strategy

- **Information**: Successful operations, performance metrics
- **Warning**: Recoverable issues, missing files
- **Error**: Failed operations, exceptions
- **Debug**: Detailed operation traces (development only)

## Extensibility Points

### Adding New Tools

1. Create static method in DotNetAnalysisTools
2. Add `[McpServerTool]` and `[Description]` attributes
3. Implement consistent error handling pattern
4. Add corresponding service method if needed

### Adding New Analysis Capabilities

1. Extend RoslynAnalysisService with new methods
2. Create appropriate data models
3. Add corresponding MCP tools
4. Update documentation

### Custom Analyzers

Future extension point for integrating custom Roslyn analyzers:
- Analyzer discovery and loading
- Custom diagnostic processing
- Configuration management

## Security Considerations

### File System Access

- Server only reads files, never writes
- Respects file system permissions
- No arbitrary code execution

### Input Validation

- Path validation to prevent directory traversal
- Solution file format validation
- Parameter sanitization in MCP tools

### Resource Limits

- Memory usage monitoring
- Timeout handling for long operations
- Graceful degradation for large solutions

## Testing Strategy

### Unit Testing

- Mock MSBuildWorkspace for isolated testing
- Test data model serialization/deserialization
- Validate error handling paths

### Integration Testing

- Test with real solution files
- Verify MCP protocol compliance
- Performance testing with large solutions

### End-to-End Testing

- Test with actual MCP clients
- Validate tool discovery and execution
- Verify error reporting accuracy

## Future Enhancements

### Planned Features

1. **Symbol Analysis**: Detailed symbol information and relationships
2. **Reference Tracking**: Find all references to symbols
3. **Code Metrics**: Complexity analysis and quality metrics
4. **Custom Analyzers**: Support for custom Roslyn analyzers
5. **Incremental Analysis**: Faster updates for file changes

### Architecture Evolution

- **Caching Layer**: Persistent caching for large solutions
- **Distributed Analysis**: Support for analyzing solutions across multiple processes
- **Real-time Updates**: File system watching for live analysis
- **Plugin System**: Extensible analyzer and tool system
