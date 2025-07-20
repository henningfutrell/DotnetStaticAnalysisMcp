# .NET Static Analysis MCP Server - Development Guide

## Development Environment Setup

### Prerequisites

- .NET 9.0 SDK or later
- Visual Studio 2022 or VS Code with C# extension
- Git for version control
- Optional: Docker for containerized development

### Getting Started

1. **Clone the repository**:
   ```bash
   git clone <repository-url>
   cd MCP
   ```

2. **Restore dependencies**:
   ```bash
   dotnet restore
   ```

3. **Build the solution**:
   ```bash
   dotnet build
   ```

4. **Run tests**:
   ```bash
   dotnet test
   ```

5. **Run the server**:
   ```bash
   dotnet run --project MCP.Server
   ```

## Project Structure

```
MCP/
├── MCP.Server/                 # Main MCP server project
│   ├── Models/                 # Data models
│   │   ├── CompilationError.cs
│   │   ├── ProjectInfo.cs
│   │   └── SymbolInfo.cs
│   ├── Services/               # Core services
│   │   ├── RoslynAnalysisService.cs
│   │   └── McpServerService.cs
│   ├── docs/                   # Documentation
│   ├── Program.cs              # Application entry point
│   └── MCP.Server.csproj       # Project file
├── MCP.Tests/                  # Test project
└── MCP.sln                     # Solution file
```

## Development Workflow

### Adding New MCP Tools

1. **Define the tool method** in `DotNetAnalysisTools`:
   ```csharp
   [McpServerTool, Description("Your tool description")]
   public static async Task<string> YourToolName(
       RoslynAnalysisService analysisService,
       [Description("Parameter description")] string parameter)
   {
       try
       {
           var result = await analysisService.YourAnalysisMethod(parameter);
           return JsonSerializer.Serialize(new { success = true, data = result });
       }
       catch (Exception ex)
       {
           return JsonSerializer.Serialize(new { success = false, error = ex.Message });
       }
   }
   ```

2. **Add corresponding service method** in `RoslynAnalysisService`:
   ```csharp
   public async Task<YourResultType> YourAnalysisMethod(string parameter)
   {
       if (_currentSolution == null)
       {
           _logger.LogWarning("No solution loaded");
           return new YourResultType();
       }

       // Implementation here
   }
   ```

3. **Create data models** if needed in `Models/` directory

4. **Add tests** in `MCP.Tests/`

5. **Update documentation**

### Adding New Analysis Capabilities

1. **Extend RoslynAnalysisService**:
   - Add new methods for specific analysis types
   - Use existing patterns for error handling and logging
   - Leverage Roslyn APIs for code analysis

2. **Create appropriate data models**:
   - Follow existing naming conventions
   - Include comprehensive metadata
   - Consider serialization requirements

3. **Add MCP tool wrapper**:
   - Follow the established pattern
   - Include proper parameter validation
   - Provide clear descriptions

### Code Style Guidelines

#### General Principles

- Follow Microsoft C# coding conventions
- Use async/await for I/O operations
- Implement proper error handling and logging
- Write comprehensive XML documentation

#### Naming Conventions

- **Classes**: PascalCase (e.g., `RoslynAnalysisService`)
- **Methods**: PascalCase (e.g., `LoadSolutionAsync`)
- **Properties**: PascalCase (e.g., `FilePath`)
- **Fields**: camelCase with underscore prefix (e.g., `_logger`)
- **Parameters**: camelCase (e.g., `solutionPath`)

#### Error Handling

```csharp
public async Task<ResultType> MethodName(string parameter)
{
    try
    {
        _logger.LogInformation("Starting operation: {Parameter}", parameter);
        
        // Validation
        if (string.IsNullOrEmpty(parameter))
        {
            throw new ArgumentException("Parameter cannot be null or empty", nameof(parameter));
        }

        // Implementation
        var result = await SomeOperation(parameter);
        
        _logger.LogInformation("Operation completed successfully");
        return result;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Operation failed: {Parameter}", parameter);
        throw;
    }
}
```

#### Logging Guidelines

- Use structured logging with parameters
- Log at appropriate levels (Debug, Information, Warning, Error)
- Include context information in log messages
- Avoid logging sensitive information

## Testing

### Unit Testing

Create unit tests for individual components:

```csharp
[Test]
public async Task LoadSolutionAsync_ValidPath_ReturnsTrue()
{
    // Arrange
    var service = new RoslynAnalysisService(_mockLogger.Object);
    var solutionPath = "/path/to/valid/solution.sln";

    // Act
    var result = await service.LoadSolutionAsync(solutionPath);

    // Assert
    Assert.IsTrue(result);
}
```

### Integration Testing

Test with real solution files:

```csharp
[Test]
public async Task GetCompilationErrors_RealSolution_ReturnsExpectedErrors()
{
    // Arrange
    var service = new RoslynAnalysisService(_logger);
    await service.LoadSolutionAsync(TestSolutionPath);

    // Act
    var errors = await service.GetCompilationErrorsAsync();

    // Assert
    Assert.IsNotNull(errors);
    Assert.IsTrue(errors.Count > 0);
}
```

### MCP Protocol Testing

Test MCP tool implementations:

```csharp
[Test]
public async Task LoadSolution_ValidPath_ReturnsSuccessJson()
{
    // Arrange
    var mockService = new Mock<RoslynAnalysisService>();
    mockService.Setup(s => s.LoadSolutionAsync(It.IsAny<string>()))
               .ReturnsAsync(true);

    // Act
    var result = await DotNetAnalysisTools.LoadSolution(mockService.Object, "/valid/path");

    // Assert
    var response = JsonSerializer.Deserialize<dynamic>(result);
    Assert.IsTrue(response.success);
}
```

## Debugging

### Local Debugging

1. **Set breakpoints** in your IDE
2. **Run with debugger**:
   ```bash
   dotnet run --project MCP.Server
   ```
3. **Use test MCP client** to send requests

### MCP Protocol Debugging

1. **Enable verbose logging**:
   ```csharp
   builder.Logging.SetMinimumLevel(LogLevel.Debug);
   ```

2. **Monitor stdio communication**:
   - All MCP communication happens over stdin/stdout
   - Logs go to stderr to avoid interference
   - Use tools like `tee` to capture communication

3. **Test with simple MCP client**:
   ```bash
   echo '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}' | dotnet run --project MCP.Server
   ```

### Common Issues

#### Solution Won't Load

- Check file paths are absolute
- Verify solution file format
- Ensure all referenced projects exist
- Check MSBuild compatibility

#### Tools Not Discovered

- Verify `[McpServerTool]` attributes
- Check method signatures match requirements
- Ensure assembly is included in tool discovery

#### Performance Issues

- Profile memory usage with large solutions
- Monitor async operation completion
- Check for resource leaks

## Performance Optimization

### Memory Management

```csharp
public void Dispose()
{
    _workspace?.Dispose();
    _workspace = null;
    _currentSolution = null;
}
```

### Async Best Practices

```csharp
// Good: Use ConfigureAwait(false) in library code
var result = await SomeAsyncOperation().ConfigureAwait(false);

// Good: Use async all the way down
public async Task<T> MethodAsync()
{
    return await SomeAsyncOperation();
}
```

### Caching Strategies

```csharp
private readonly ConcurrentDictionary<string, Solution> _solutionCache = new();

public async Task<Solution> GetOrLoadSolutionAsync(string path)
{
    return _solutionCache.GetOrAdd(path, async p => 
        await _workspace.OpenSolutionAsync(p));
}
```

## Deployment

### Local Deployment

1. **Publish self-contained**:
   ```bash
   dotnet publish -c Release --self-contained -r win-x64
   ```

2. **Update MCP configuration** with published executable path

### Docker Deployment

```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY publish/ .
ENTRYPOINT ["dotnet", "MCP.Server.dll"]
```

### CI/CD Pipeline

```yaml
# Example GitHub Actions workflow
name: Build and Test
on: [push, pull_request]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    - uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'
    - run: dotnet restore
    - run: dotnet build --no-restore
    - run: dotnet test --no-build
```

## Contributing

### Pull Request Process

1. **Fork the repository**
2. **Create feature branch**: `git checkout -b feature/your-feature`
3. **Make changes** following code style guidelines
4. **Add tests** for new functionality
5. **Update documentation** as needed
6. **Submit pull request** with clear description

### Code Review Checklist

- [ ] Code follows style guidelines
- [ ] Tests are included and passing
- [ ] Documentation is updated
- [ ] Error handling is appropriate
- [ ] Performance impact is considered
- [ ] Security implications are reviewed

## Release Process

### Version Management

- Use semantic versioning (MAJOR.MINOR.PATCH)
- Update version in project files
- Tag releases in Git

### Release Checklist

- [ ] All tests passing
- [ ] Documentation updated
- [ ] Performance benchmarks run
- [ ] Security review completed
- [ ] Release notes prepared

## Troubleshooting

### Build Issues

```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

### Runtime Issues

```bash
# Enable detailed logging
export DOTNET_ENVIRONMENT=Development
dotnet run --project MCP.Server
```

### MCP Communication Issues

```bash
# Test MCP protocol manually
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}' | dotnet run --project MCP.Server
```

## Resources

- [Microsoft Roslyn Documentation](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/)
- [MCP Protocol Specification](https://modelcontextprotocol.io/docs)
- [.NET Generic Host Documentation](https://docs.microsoft.com/en-us/dotnet/core/extensions/generic-host)
- [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
