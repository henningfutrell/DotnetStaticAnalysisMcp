# Changelog

All notable changes to the .NET Static Analysis MCP Server will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2024-12-20

### Added
- **Core MCP Server**: Complete Model Context Protocol server implementation
- **Solution Analysis**: Load and analyze .NET solution files with full project discovery
- **Compilation Error Detection**: Identify compilation errors with precise line and column locations
- **Project Insights**: Comprehensive project metadata, dependencies, and source file information
- **Diagnostic Tools**: Environment and MSBuild status checking for troubleshooting
- **Structured Logging**: JSON-formatted logging with Serilog for debugging and monitoring
- **Real-time Updates**: Hot reload support during development with `dotnet watch`
- **C# Language Support**: Full C# language services integration with Roslyn
- **Comprehensive Testing**: Unit tests, integration tests, and test projects with deliberate errors

### MCP Tools
- `load_solution` - Load a .NET solution file
- `get_solution_info` - Get detailed solution structure
- `get_compilation_errors` - Find compilation errors and warnings
- `analyze_file` - Analyze a specific file for issues
- `get_code_suggestions` - Get AI-powered code improvements (framework ready)
- `get_suggestion_categories` - List available suggestion categories
- `get_server_version` - Get server version and build information
- `get_basic_diagnostics` - Get environment and MSBuild diagnostics

### Technical Features
- **MSBuild Integration**: Full MSBuild workspace support with proper C# language services
- **Roslyn Analysis**: Deep code analysis using Microsoft Roslyn compiler platform
- **Telemetry Service**: Performance monitoring and operation tracking
- **Error Handling**: Comprehensive error handling with detailed diagnostics
- **Cross-platform**: Works on Windows, macOS, and Linux

### Documentation
- Comprehensive README with usage examples and troubleshooting
- MIT License for open source distribution
- Setup script for easy installation
- Example MCP configuration files
- API reference documentation

### Known Limitations
- Code suggestions framework is implemented but suggestion generation is not yet active
- Large solution loading may require performance tuning
- Requires .NET 9.0 SDK or later

## [1.1.0] - 2024-12-20

### Added - Type Analysis & Refactoring Support
- **Type Usage Discovery**: Find all references to types across the entire solution
  - `find_type_usages` - Comprehensive type reference analysis with 19 different usage kinds
  - `find_member_usages` - Track method, property, field, and event usage
  - `find_namespace_usages` - Find using statements and namespace references
- **Dependency Analysis**: Understand type relationships and dependencies
  - `get_type_dependencies` - Analyze what types a specific type depends on
  - `get_type_dependents` - Reverse dependency analysis
- **Safe Refactoring Validation**: Check rename safety and preview impacts
  - `validate_rename_safety` - Check if renaming would cause conflicts
  - `preview_rename_impact` - Show exactly what files would be affected
  - `analyze_impact_scope` - Analyze potential breaking changes
- **Comprehensive Analysis**: All-in-one type analysis
  - `get_type_analysis_summary` - Complete type analysis with summary statistics

### Technical Enhancements
- **Roslyn Semantic Analysis**: Uses true semantic analysis, not text matching
- **Cross-Project Support**: Handles references across multiple projects
- **Generic Type Support**: Correctly handles generic types and constraints
- **Precise Location Information**: File path, line number, column, and context
- **Usage Type Filtering**: Filter by declaration, instantiation, inheritance, etc.
- **Structured JSON Responses**: Consistent with existing MCP tool patterns

### Usage Kinds Supported
- Declaration, Instantiation, Method Parameters/Returns, Property/Field Types
- Generic Type Arguments, Base Classes, Implemented Interfaces
- Attribute Usage, Cast Operations, Type Checks (is/as/typeof)
- Using Directives, XML Documentation References

## [Unreleased]

### Planned Features
- **Active Code Suggestions**: Implement AI-powered code improvement suggestions
- **Performance Optimizations**: Optimize for large solution loading
- **Additional Language Support**: Support for VB.NET and F#
- **Custom Rules**: Allow custom analysis rules and configurations
- **Caching**: Implement intelligent caching for faster repeated analysis
- **Incremental Analysis**: Support for incremental file analysis
- **Code Metrics**: Add code complexity and quality metrics
- **Refactoring Suggestions**: Provide automated refactoring recommendations
