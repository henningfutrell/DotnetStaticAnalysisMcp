# .NET Static Analysis MCP Server

This MCP (Model Context Protocol) server provides comprehensive static analysis and code coverage capabilities for .NET solutions using Microsoft's Roslyn compiler platform and Coverlet. It allows AI agents to analyze .NET codebases, execute tests with coverage collection, and provide intelligent insights without manual intervention.

## Features

### 🔍 **Static Analysis**
- **Load .NET Solutions**: Load and analyze entire .NET solution files (.sln)
- **Compilation Error Detection**: Get detailed compilation errors and warnings without building
- **Project Information**: Retrieve comprehensive information about projects in the solution
- **File-Level Analysis**: Analyze specific files for errors and warnings
- **Symbol Analysis**: Get detailed information about code symbols (classes, methods, properties, etc.)
- **Reference Tracking**: Find all references to symbols across the codebase

### 🎯 **Type & Dependency Analysis**
- **Type Usage Discovery**: Find all usages of types across projects (19 different usage kinds)
- **Member Usage Analysis**: Track method, property, field, and event usage
- **Dependency Mapping**: Analyze dependencies and dependents between types
- **Safe Refactoring**: Validate rename operations and preview impact
- **Cross-Project Analysis**: Understand relationships across solution boundaries

### 📊 **Code Coverage Analysis**
- **Test Execution with Coverage**: Automated test running with Coverlet integration
- **Comprehensive Metrics**: Line, method, class, and branch coverage analysis
- **Uncovered Code Detection**: Precise identification of untested code areas
- **AI-Powered Insights**: Intelligent recommendations for coverage improvement
- **Coverage Comparison**: Baseline comparison and trend analysis
- **Multi-Framework Support**: xUnit, NUnit, MSTest compatibility

## Installation and Setup

1. **Build the server:**
   ```bash
   dotnet build DotnetStaticAnalysisMcp.Server
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
           "/full/path/to/DotnetStaticAnalysisMcp.Server/DotnetStaticAnalysisMcp.Server.csproj"
         ]
       }
     }
   }
   ```

## Available Tools

The server provides **24 comprehensive MCP tools** organized into four categories:

### 🔧 **Core Analysis Tools (6 tools)**

#### `load_solution`
Load a .NET solution file for analysis.
- **Parameters:** `solutionPath` (string) - Path to the .sln file
- **Returns:** Success status and solution loading confirmation

#### `get_compilation_errors`
Get all compilation errors and warnings from the loaded solution.
- **Parameters:** None
- **Returns:** Detailed compilation errors with location, severity, and project information

#### `get_solution_info`
Get comprehensive information about the loaded solution.
- **Parameters:** None
- **Returns:** Solution metadata, project details, target frameworks, and source files

#### `analyze_file`
Analyze a specific file for errors and warnings.
- **Parameters:** `filePath` (string) - Path to the file to analyze
- **Returns:** File-specific compilation errors and warnings

#### `get_server_version`
Get server version and build information.
- **Parameters:** None
- **Returns:** Version info, features, capabilities, and recent updates

#### `get_diagnostics`
Get comprehensive diagnostic information for debugging.
- **Parameters:** `includeLogs` (bool, optional) - Include recent log entries
- **Returns:** Environment info, loaded solutions, and diagnostic data

### 🎯 **Type Analysis Tools (9 tools)**

#### `find_type_usages`
Find all references to a specific type across the entire solution.
- **Parameters:** `typeName` (string), `maxResults` (int, optional), `includeDocumentation` (bool, optional)
- **Returns:** Comprehensive list of type usages with 19 different usage kinds

#### `find_member_usages`
Find all references to specific type members (methods, properties, fields, events).
- **Parameters:** `typeName` (string), `memberName` (string)
- **Returns:** Detailed member usage analysis across projects

#### `find_namespace_usages`
Find all using statements and fully qualified references to a namespace.
- **Parameters:** `namespaceName` (string)
- **Returns:** Namespace usage patterns and import statements

#### `get_type_analysis_summary`
Get comprehensive type analysis including usages, dependencies, and impact.
- **Parameters:** `typeName` (string)
- **Returns:** Complete type analysis with usage patterns and relationships

#### `get_type_dependencies`
Get all types that a specific type depends on.
- **Parameters:** `typeName` (string)
- **Returns:** Dependency tree and type relationships

#### `get_type_dependents`
Get all types that depend on a specific type.
- **Parameters:** `typeName` (string)
- **Returns:** Reverse dependency analysis

#### `validate_rename_safety`
Check if renaming a type/member would cause conflicts or breaking changes.
- **Parameters:** `currentName` (string), `proposedName` (string)
- **Returns:** Safety validation and potential conflict analysis

#### `preview_rename_impact`
Show exactly what files and lines would be affected by a rename operation.
- **Parameters:** `currentName` (string), `proposedName` (string)
- **Returns:** Detailed impact preview with file and line information

#### `analyze_impact_scope`
Analyze the potential impact of changing a type (what would break).
- **Parameters:** `typeName` (string)
- **Returns:** Comprehensive impact analysis and risk assessment

### 📊 **Code Coverage Analysis Tools (6 tools)**

#### `run_coverage_analysis`
Execute tests and generate comprehensive coverage reports for the loaded solution.
- **Parameters:**
  - `collectBranchCoverage` (bool, optional) - Collect branch coverage data
  - `timeoutMinutes` (int, optional) - Test execution timeout
  - `testFilter` (string, optional) - Test filter expression
  - `includedProjects` (string, optional) - Include specific projects (comma-separated)
  - `excludedProjects` (string, optional) - Exclude specific projects (comma-separated)
- **Returns:** Complete coverage analysis with line, method, class, and branch metrics

#### `get_coverage_summary`
Get overall coverage statistics and summary for the loaded solution.
- **Parameters:**
  - `includedProjects` (string, optional) - Include only specific projects
  - `excludedProjects` (string, optional) - Exclude specific projects
- **Returns:** High-level coverage metrics and project breakdown

#### `find_uncovered_code`
Identify specific uncovered lines, methods, and branches in the codebase.
- **Parameters:**
  - `maxResults` (int, optional) - Maximum number of uncovered items to return
  - `includedProjects` (string, optional) - Include only specific projects
- **Returns:** Precise list of uncovered code areas with line numbers and context

#### `get_method_coverage`
Get detailed coverage information for a specific method.
- **Parameters:** `className` (string), `methodName` (string)
- **Returns:** Method-specific coverage analysis with line-by-line details

#### `compare_coverage`
Compare coverage between different test runs or against a baseline.
- **Parameters:** `baselinePath` (string) - Path to baseline coverage results JSON file
- **Returns:** Coverage comparison analysis with trends and changes

#### `get_coverage_insights`
Get comprehensive coverage analysis with detailed insights and AI-powered recommendations.
- **Parameters:** `minimumCoverageThreshold` (number, optional) - Minimum coverage threshold for warnings
- **Returns:** AI-powered insights, recommendations, risk assessment, and priority actions

### 💡 **Code Suggestions Tools (3 tools)**

#### `get_code_suggestions`
Get code improvement suggestions and analyzer recommendations from the loaded solution.
- **Parameters:**
  - `categories` (string, optional) - Categories to include (Style, Performance, etc.)
  - `minimumPriority` (string, optional) - Minimum priority level (Low, Medium, High, Critical)
  - `maxSuggestions` (int, optional) - Maximum number of suggestions to return
- **Returns:** Comprehensive code improvement suggestions with categories and priorities

#### `get_file_suggestions`
Get code improvement suggestions for a specific file.
- **Parameters:**
  - `filePath` (string) - Absolute path to the file to analyze
  - `categories` (string, optional) - Categories to include
  - `minimumPriority` (string, optional) - Minimum priority level
- **Returns:** File-specific improvement suggestions and recommendations

#### `get_suggestion_categories`
Get information about available code suggestion categories and configuration options.
- **Parameters:** None
- **Returns:** Available categories, priority levels, and configuration options

## Available Resources

This MCP server currently focuses on tools rather than resources. Future versions may include:
- Solution analysis summaries
- Project dependency graphs
- Code metrics and statistics

## Usage Examples

### 🚀 **Basic Analysis Workflow**

1. **Load a solution:**
   ```bash
   load_solution solutionPath="/workspace/MyProject/MyProject.sln"
   ```

2. **Get compilation errors:**
   ```bash
   get_compilation_errors
   ```

3. **Get solution information:**
   ```bash
   get_solution_info
   ```

### 🎯 **Type Analysis Workflow**

1. **Find all usages of a type:**
   ```bash
   find_type_usages typeName="Customer" maxResults=50
   ```

2. **Analyze type dependencies:**
   ```bash
   get_type_dependencies typeName="OrderService"
   ```

3. **Validate safe refactoring:**
   ```bash
   validate_rename_safety currentName="OldClassName" proposedName="NewClassName"
   ```

### 📊 **Code Coverage Analysis Workflow**

1. **Run comprehensive coverage analysis:**
   ```bash
   run_coverage_analysis collectBranchCoverage=true timeoutMinutes=5
   ```

2. **Get coverage summary:**
   ```bash
   get_coverage_summary
   ```

3. **Find uncovered code:**
   ```bash
   find_uncovered_code maxResults=20
   ```

4. **Get AI-powered coverage insights:**
   ```bash
   get_coverage_insights minimumCoverageThreshold=80.0
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

### **Runtime Requirements**
- .NET 9.0 or later (recommended) / .NET 8.0+ (minimum)
- Access to .NET solution files and source code
- Sufficient permissions to read project files and references

### **Code Coverage Requirements**
- Test projects with supported frameworks (xUnit, NUnit, MSTest)
- Coverlet packages for coverage collection
- Test execution permissions

### **Observability**
- Serilog for structured logging
- File system access for log files (`~/.mcp/logs/`)
- Telemetry service for operation tracking

## Supported Project Types

### **Analysis Support**
- .NET Framework 4.8+ projects
- .NET Core 3.1+ / .NET 5+ / .NET 6+ / .NET 7+ / .NET 8+ / .NET 9+ projects
- .NET Standard 2.0+ libraries
- ASP.NET Core applications
- Console applications
- Class libraries
- WPF/WinForms applications
- Blazor applications

### **Test Framework Support**
- **xUnit** 2.6+ (recommended)
- **NUnit** 3.0+
- **MSTest** 2.0+
- **Coverlet** integration for coverage collection

## Performance Considerations

### **Analysis Performance**
- Large solutions may take 2-10 seconds to load initially
- Compilation analysis is performed in-memory without building
- Type analysis is real-time with semantic caching
- Error results are limited to 100 items by default for performance

### **Coverage Analysis Performance**
- Test execution time: 30 seconds to 5 minutes (depends on test suite size)
- Coverage parsing: < 1 second for typical projects
- Parallel test execution supported for faster analysis
- Configurable timeouts prevent hanging operations

### **Memory & Caching**
- Solution workspace cached for subsequent analysis
- Coverage results cached until next test run
- Optimized for enterprise-scale solutions
- Automatic cleanup of temporary coverage files

## Error Handling

The server gracefully handles:
- Invalid solution paths
- Corrupted project files
- Missing references
- Syntax errors in source files
- Permission issues

All errors are logged and returned in a structured format for easy consumption by AI agents.

## Use Cases

### **🔍 Static Analysis Use Cases**
- **Code Review Assistance**: Quickly identify compilation issues before building
- **Refactoring Support**: Understand code structure and dependencies with safe rename validation
- **Documentation Generation**: Extract symbol information and project structure
- **Quality Analysis**: Identify common error patterns and code issues
- **Migration Planning**: Analyze existing codebases for compatibility issues

### **🎯 Type Analysis Use Cases**
- **Safe Refactoring**: Validate renames and preview impact before making changes
- **Dependency Analysis**: Understand type relationships and coupling
- **Impact Assessment**: Analyze what would break when changing types
- **Code Navigation**: Find all usages of types and members across projects
- **Architecture Review**: Understand cross-project dependencies

### **📊 Coverage Analysis Use Cases**
- **Test Quality Assessment**: Measure and improve test coverage
- **CI/CD Integration**: Automated coverage validation in build pipelines
- **Risk Assessment**: Identify untested critical code paths
- **Coverage Trends**: Track coverage improvements over time
- **Test Gap Analysis**: Find specific areas needing more tests

### **💡 Code Quality Use Cases**
- **Educational Tools**: Help understand .NET project structure and best practices
- **Code Modernization**: Identify opportunities for code improvements
- **Performance Optimization**: Find performance bottlenecks and suggestions
- **Security Analysis**: Identify potential security issues and vulnerabilities

## Technical Details

### **Core Technologies**
- **Microsoft.CodeAnalysis (Roslyn)** 4.14.0+ - Semantic analysis engine
- **MSBuildWorkspace** - Solution and project loading
- **Coverlet** - Industry-standard .NET code coverage
- **Serilog** - Structured logging with JSON output
- **mcpdotnet** - MCP protocol implementation

### **Architecture**
- **Real-time Analysis**: No compilation required for most operations
- **Thread-safe**: Concurrent analysis requests supported
- **Async/Await**: Non-blocking operations throughout
- **Caching**: Intelligent caching for performance
- **Telemetry**: Comprehensive operation tracking and diagnostics

### **Coverage Integration**
- **XPlat Code Coverage**: Cross-platform coverage collection
- **Cobertura XML**: Industry-standard coverage format
- **Multi-Framework**: xUnit, NUnit, MSTest support
- **Branch Coverage**: Conditional logic and decision point analysis
- **Hit Count Analysis**: Execution frequency tracking

## Observability & Monitoring

### **📊 Structured Logging**
The server provides comprehensive observability through Serilog with structured JSON logging:

```bash
# Monitor logs in real-time
tail -f ~/.mcp/logs/dotnet-analysis*.log | jq '.'

# Filter for coverage-related events
tail -f ~/.mcp/logs/dotnet-analysis*.log | jq 'select(.Message | contains("Coverage"))'

# Filter for specific operation IDs
tail -f ~/.mcp/logs/dotnet-analysis*.log | jq 'select(.Properties.operation_id)'
```

### **🔍 Diagnostic Tools**
- **`get_diagnostics`**: Comprehensive diagnostic information with recent logs
- **`get_server_version`**: Version info, features, and recent updates
- **Operation Tracking**: Unique operation IDs for tracing requests
- **Performance Metrics**: Execution times and resource usage

### **📈 Telemetry Events**
Key telemetry events tracked:
- `CodeCoverageService.RunCoverageAnalysis.Started` - Coverage analysis initiation
- `CodeCoverageService.GetTestProjects.ProjectFilesFound` - Test project discovery
- `CodeCoverageService.IsTestProject.Analysis` - Test project validation
- `TypeAnalysis.FindUsages.Completed` - Type usage analysis completion
- `Solution.Loaded` - Solution loading events

### **🛡️ Error Handling & Recovery**
- Comprehensive error logging with stack traces
- Graceful degradation when projects can't be analyzed
- Clear error messages for troubleshooting
- Automatic cleanup of temporary files
- Timeout protection for long-running operations

## Version Information

### **Current Version: v1.1.0 - Code Coverage Analysis with Enhanced Logging**
**Build ID:** `CODE_COVERAGE_ENHANCED_LOGGING_20241220_1600`
**Release Date:** December 20, 2024

### **🎯 Recent Updates (v1.1.0)**
- ✅ **Added 9 new MCP tools** for comprehensive code coverage analysis
- ✅ **Implemented comprehensive type usage discovery** with 19 different usage kinds
- ✅ **Added safe refactoring validation** and impact preview tools
- ✅ **Integrated Coverlet** for industry-standard coverage analysis
- ✅ **Added AI-powered coverage insights** and recommendations
- ✅ **Enhanced cross-project dependency tracking** and analysis
- ✅ **Improved error handling** with detailed diagnostics and logging
- ✅ **Added coverage comparison** and trend analysis capabilities
- ✅ **Fixed async/await patterns** and XML parsing implementation
- ✅ **Enhanced observability** with structured logging and telemetry

### **📊 Tool Count Summary**
- **Total MCP Tools:** 24
- **Core Analysis Tools:** 6
- **Type Analysis Tools:** 9
- **Coverage Analysis Tools:** 6
- **Code Suggestions Tools:** 3

### **🚀 Production Ready Features**
- ✅ **Enterprise-grade observability** with comprehensive logging
- ✅ **Real-time analysis** with semantic caching for performance
- ✅ **Cross-platform compatibility** (Windows, macOS, Linux)
- ✅ **Multi-framework support** (.NET Framework, .NET Core, .NET 5+)
- ✅ **Robust error handling** with graceful degradation
- ✅ **Comprehensive test coverage** with self-analyzing capabilities

---

**🎉 The .NET Static Analysis MCP Server is now production-ready with comprehensive code coverage analysis, type analysis, and AI-powered insights!**
