# .NET Static Analysis MCP Server

A powerful Model Context Protocol (MCP) server that provides real-time static analysis for .NET solutions. This server integrates with MCP-compatible chat clients to offer comprehensive code analysis, compilation error detection, and project insights directly within your conversation interface.

## Features

- **🔍 Solution Analysis**: Load and analyze entire .NET solutions with detailed project information
- **⚠️ Compilation Error Detection**: Identify compilation errors with precise line and column locations
- **📊 Project Insights**: Get comprehensive project metadata, dependencies, and source file information
- **🛠️ Diagnostic Tools**: Environment and MSBuild status checking for troubleshooting
- **📝 Structured Logging**: Comprehensive logging with JSON format for debugging and monitoring
- **🔄 Real-time Updates**: Hot reload support during development with `dotnet watch`

## Quick Start

### Prerequisites

- .NET 9.0 SDK or later
- An MCP-compatible chat client (Claude Desktop, Cline, etc.)

### Installation

1. **Clone the repository**:
   ```bash
   git clone <repository-url>
   cd MCP
   ```

2. **Build the server**:
   ```bash
   dotnet build MCP.Server
   ```

3. **Configure your MCP client** by adding this to your MCP configuration file:

   **For Development (with hot reload)**:
   ```json
   {
     "mcpServers": {
       "dotnet-analysis": {
         "command": "dotnet",
         "args": ["watch", "run", "--project", "/path/to/MCP/MCP.Server"],
         "env": {}
       }
     }
   }
   ```

   **For Production**:
   ```json
   {
     "mcpServers": {
       "dotnet-analysis": {
         "command": "dotnet",
         "args": ["run", "--project", "/path/to/MCP/MCP.Server"],
         "env": {}
       }
     }
   }
   ```

4. **Restart your MCP client** to load the server

## Usage

Once configured, you can use these commands in your MCP-compatible chat:

### Core Analysis Commands

#### Load a Solution
```
Load the solution at /path/to/your/solution.sln
```
Uses the `load_solution` tool to analyze a .NET solution file.

#### Get Solution Information
```
Show me the structure of the loaded solution
```
Uses the `get_solution_info` tool to display projects, dependencies, and metadata.

#### Check for Compilation Errors
```
Are there any compilation errors in the current solution?
```
Uses the `get_compilation_errors` tool to find and report all compilation issues.

#### Analyze Specific Files
```
Analyze the file Program.cs for errors
```
Uses the `analyze_file` tool to check a specific file for compilation issues.

### Diagnostic Commands

#### Check Server Status
```
What's the current status of the .NET analysis server?
```
Uses diagnostic tools to show server health, MSBuild status, and environment information.

#### Get Code Suggestions
```
Give me code improvement suggestions for performance and style
```
Uses the `get_code_suggestions` tool to provide AI-powered code improvements.

## Available MCP Tools

| Tool | Description | Parameters |
|------|-------------|------------|
| `load_solution` | Load a .NET solution file | `solutionPath` (string) |
| `get_solution_info` | Get detailed solution structure | None |
| `get_compilation_errors` | Find compilation errors and warnings | None |
| `analyze_file` | Analyze a specific file | `filePath` (string) |
| `get_code_suggestions` | Get AI-powered code improvements | `categories`, `minimumPriority`, `maxSuggestions` |
| `get_suggestion_categories` | List available suggestion categories | None |
| `get_server_version` | Get server version and build info | None |
| `get_basic_diagnostics` | Get environment diagnostics | None |

## Configuration Files

### MCP Client Configuration Locations

- **Claude Desktop**: `~/.config/claude/claude_desktop_config.json`
- **Cline**: Usually in VS Code settings or `.cline/config.json`
- **Other MCP clients**: Check their documentation

### Example Complete Configuration

```json
{
  "mcpServers": {
    "dotnet-analysis": {
      "command": "dotnet",
      "args": ["watch", "run", "--project", "/home/user/projects/MCP/MCP.Server"],
      "env": {
        "DOTNET_CLI_TELEMETRY_OPTOUT": "1"
      }
    }
  }
}
```

## Logging and Debugging

The server provides comprehensive logging for troubleshooting:

### Log Files
- **Structured logs**: `~/.mcp/logs/dotnet-analysis*.log` (JSON format)
- **Debug logs**: `/tmp/mcp-debug-*.log` (simple text format)

### Viewing Logs
```bash
# View recent structured logs
tail -f ~/.mcp/logs/dotnet-analysis$(date +%Y%m%d).log | jq

# View debug logs
tail -f /tmp/mcp-debug-$(date +%Y%m%d).log
```

### Common Issues

#### Server Not Starting
1. Check that .NET 9.0 SDK is installed: `dotnet --version`
2. Verify the project path in your MCP configuration
3. Check MCP client logs for error messages

#### No Projects Detected
1. Ensure the solution file exists and is valid
2. Check that MSBuild is properly installed
3. Use the diagnostic tools to check MSBuild status

#### Compilation Errors Not Found
1. Verify the solution loads successfully first
2. Check that the projects use supported .NET versions
3. Ensure all NuGet packages are restored: `dotnet restore`

## Development

### Project Structure
```
MCP/
├── MCP.Server/              # Main MCP server application
│   ├── Models/              # Data models for analysis results
│   ├── Services/            # Core analysis and MCP services
│   └── Program.cs           # Server entry point
├── MCP.Tests/               # Unit and integration tests
├── MCP.IntegrationTests/    # Integration tests
├── MCP.TestProject/         # Test project with deliberate errors
└── README.md               # This file
```

### Building from Source
```bash
# Restore dependencies
dotnet restore

# Build all projects
dotnet build

# Run tests
dotnet test

# Run the server locally
dotnet run --project MCP.Server
```

### Running Tests
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test MCP.IntegrationTests

# Run with verbose output
dotnet test --verbosity normal
```

## Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/amazing-feature`
3. Make your changes and add tests
4. Ensure all tests pass: `dotnet test`
5. Commit your changes: `git commit -m 'Add amazing feature'`
6. Push to the branch: `git push origin feature/amazing-feature`
7. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

- **Issues**: Report bugs and request features on GitHub Issues
- **Discussions**: Join the conversation in GitHub Discussions
- **Documentation**: Check the wiki for additional documentation

## Example Usage Scenarios

### Scenario 1: Code Review Assistant
```
Load the solution at /home/user/MyProject/MyProject.sln and check for any compilation errors. Then analyze the main Program.cs file and suggest performance improvements.
```

### Scenario 2: Project Health Check
```
What's the current status of the loaded solution? Show me the project structure and any issues that need attention.
```

### Scenario 3: Debugging Build Issues
```
I'm getting build errors. Can you load my solution and tell me exactly what's wrong and where?
```

### Scenario 4: Code Quality Analysis
```
Give me code suggestions for style and best practices improvements with high priority items first.
```

## Advanced Configuration

### Environment Variables
You can customize the server behavior with environment variables:

```json
{
  "mcpServers": {
    "dotnet-analysis": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/MCP/MCP.Server"],
      "env": {
        "DOTNET_CLI_TELEMETRY_OPTOUT": "1",
        "MSBUILD_EXE_PATH": "/custom/path/to/msbuild",
        "DOTNET_ROOT": "/custom/dotnet/root"
      }
    }
  }
}
```

### Performance Tuning
For large solutions, you may want to adjust these settings:

- **Memory**: Increase available memory for the .NET process
- **Timeout**: Adjust solution loading timeouts for very large codebases
- **Logging**: Reduce logging verbosity in production

### Security Considerations
- The server runs with the same permissions as your MCP client
- Only load solutions from trusted sources
- Be cautious when analyzing solutions with custom MSBuild targets

## Troubleshooting Guide

### Problem: "MSBuild not found"
**Solution**: 
1. Install .NET SDK (not just runtime)
2. Ensure `dotnet` is in your PATH
3. Try running `dotnet --info` to verify installation

### Problem: "Solution loads but shows 0 projects"
**Solution**:
1. Check that the solution file is valid
2. Ensure all referenced projects exist
3. Run `dotnet restore` on the solution
4. Check the diagnostic logs for MSBuild errors

### Problem: "Server not responding"
**Solution**:
1. Check MCP client logs for connection errors
2. Verify the server process is running
3. Try restarting the MCP client
4. Check firewall/antivirus blocking the process

### Problem: "Compilation errors not detected"
**Solution**:
1. Ensure the solution loads successfully first
2. Check that projects target supported .NET versions
3. Verify all NuGet packages are restored
4. Try building the solution manually with `dotnet build`

## API Reference

### Response Formats

All MCP tools return JSON responses with this general structure:

```json
{
  "success": true,
  "timestamp": "2024-12-20T15:30:00Z",
  "data": { /* tool-specific data */ }
}
```

### Error Handling

When errors occur, responses include detailed error information:

```json
{
  "success": false,
  "error": "Error message",
  "error_code": "ERROR_CODE",
  "timestamp": "2024-12-20T15:30:00Z",
  "details": { /* additional error context */ }
}
```

## Acknowledgments

- Built with [Roslyn](https://github.com/dotnet/roslyn) for .NET code analysis
- Uses [Model Context Protocol](https://modelcontextprotocol.io/) for chat integration
- Powered by [MSBuild](https://github.com/dotnet/msbuild) for project loading
- Logging provided by [Serilog](https://serilog.net/)
- Testing framework: [xUnit](https://xunit.net/)
