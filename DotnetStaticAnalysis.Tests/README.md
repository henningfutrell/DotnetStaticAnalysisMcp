# MCP Server Tests

This directory contains comprehensive tests for the .NET Static Analysis MCP Server. The tests are organized into different categories to ensure thorough coverage of all functionality.

## Test Structure

### Test Categories

1. **Unit Tests** (`Tests.cs` - `RoslynAnalysisServiceTests`)
   - Tests individual components in isolation
   - Focuses on the core `RoslynAnalysisService` functionality
   - Uses mocking where appropriate

2. **Integration Tests** (`Tests2.cs` - `McpToolsTests`)
   - Tests MCP tool implementations end-to-end
   - Validates JSON serialization and response formats
   - Tests the integration between MCP tools and analysis service

3. **Performance Tests** (`Tests3.cs` - `PerformanceAndIntegrationTests`)
   - Validates performance characteristics
   - Tests memory usage and disposal
   - Validates specific error detection capabilities

### Test Data

The `TestData/TestSolution/` directory contains a sample .NET solution with intentional compilation errors for testing:

- **TestSolution.sln**: Main solution file with 2 projects
- **TestProject/**: Console application with multiple error types
- **TestLibrary/**: Class library with syntax errors and valid code

#### Intentional Errors in Test Data

The test solution contains these specific errors for validation:

1. **CS0103**: Undeclared variable (`undeclaredVariable` in Program.cs)
2. **CS0246**: Unknown type (`UnknownType` in Program.cs)
3. **CS0161**: Not all code paths return a value (`GetValue()` method in Program.cs)
4. **CS1002**: Syntax error (missing semicolon in Calculator.cs)
5. **CS0168**: Unused variable warnings (various locations)

## Running Tests

### Prerequisites

- .NET 9.0 SDK
- All dependencies restored (`dotnet restore`)

### Run All Tests

```bash
dotnet test MCP.Tests
```

### Run Specific Test Categories

```bash
# Unit tests only
dotnet test MCP.Tests --filter "FullyQualifiedName~RoslynAnalysisServiceTests"

# Integration tests only
dotnet test MCP.Tests --filter "FullyQualifiedName~McpToolsTests"

# Performance tests only
dotnet test MCP.Tests --filter "FullyQualifiedName~PerformanceAndIntegrationTests"
```

### Run with Verbose Output

```bash
dotnet test MCP.Tests --verbosity normal
```

## Test Coverage

### RoslynAnalysisService Tests

- ✅ Solution loading (valid and invalid paths)
- ✅ Compilation error extraction
- ✅ Solution information retrieval
- ✅ File-specific analysis
- ✅ Error handling for unloaded solutions
- ✅ Resource disposal

### MCP Tools Tests

- ✅ LoadSolution tool (success and error cases)
- ✅ GetCompilationErrors tool (with and without loaded solution)
- ✅ GetSolutionInfo tool (with and without loaded solution)
- ✅ AnalyzeFile tool (valid files and non-existent files)
- ✅ JSON response format validation
- ✅ Error response handling

### Performance Tests

- ✅ Solution loading performance (< 10 seconds)
- ✅ Error analysis performance (< 5 seconds)
- ✅ Caching effectiveness (subsequent calls faster)
- ✅ Memory usage validation (< 100MB increase)
- ✅ Resource cleanup verification

### Error Detection Tests

- ✅ CS0103 (undeclared variable) detection
- ✅ CS0246 (unknown type) detection
- ✅ CS0161 (missing return) detection
- ✅ CS1002 (syntax error) detection
- ✅ Error location accuracy (line/column numbers)
- ✅ Project-specific error attribution

## Test Data Validation

The tests validate that the analysis correctly identifies:

1. **Error Types**: Specific C# compiler error codes
2. **Error Locations**: Accurate line and column numbers
3. **Project Attribution**: Errors correctly attributed to projects
4. **File Isolation**: File-specific analysis returns only relevant errors
5. **Solution Structure**: Correct project count and metadata

## Performance Benchmarks

Expected performance characteristics:

- **Solution Loading**: < 10 seconds for small solutions
- **Error Analysis**: < 5 seconds for full solution analysis
- **File Analysis**: < 1 second for individual file analysis
- **Memory Usage**: < 100MB increase during analysis
- **Subsequent Calls**: Faster due to caching

## Troubleshooting Tests

### Common Issues

1. **Test Data Not Found**
   ```
   Solution: Ensure TestData directory is copied to output
   Check: MCP.Tests.csproj includes TestData files
   ```

2. **Performance Tests Failing**
   ```
   Solution: Run on a machine with adequate resources
   Note: Performance thresholds may need adjustment for slower systems
   ```

3. **Compilation Errors in Test Data**
   ```
   Expected: Test data intentionally contains compilation errors
   Solution: This is normal and required for testing
   ```

### Debug Test Execution

```bash
# Run with detailed output
dotnet test MCP.Tests --logger "console;verbosity=detailed"

# Run specific test
dotnet test MCP.Tests --filter "LoadSolution_ValidSolution_ReturnsTrue"
```

## Adding New Tests

### Test Naming Convention

```csharp
[Test]
public async Task MethodName_Scenario_ExpectedResult()
{
    // Arrange
    // Act  
    // Assert
}
```

### Test Categories

- **Unit Tests**: Test individual methods in isolation
- **Integration Tests**: Test component interactions
- **Performance Tests**: Validate timing and resource usage
- **Validation Tests**: Verify specific error detection

### Test Data Guidelines

- Use the existing TestSolution for consistency
- Add new files to TestData if needed
- Ensure intentional errors are well-documented
- Update this README when adding new test scenarios

## Continuous Integration

These tests are designed to run in CI/CD environments:

- No external dependencies required
- Self-contained test data
- Deterministic results
- Performance thresholds suitable for CI

### CI Configuration Example

```yaml
- name: Run Tests
  run: dotnet test MCP.Tests --logger trx --results-directory TestResults
  
- name: Publish Test Results
  uses: dorny/test-reporter@v1
  if: always()
  with:
    name: Test Results
    path: TestResults/*.trx
    reporter: dotnet-trx
```

## Test Framework

The tests use **TUnit** testing framework:

- Modern async/await support
- Attribute-based test configuration
- Rich assertion library
- Performance testing capabilities

### TUnit Features Used

- `[Test]` attribute for test methods
- `await Assert.That()` for async assertions
- Automatic test discovery
- Built-in performance measurement

## Future Test Enhancements

Planned test additions:

1. **Stress Tests**: Large solution handling
2. **Concurrency Tests**: Multiple simultaneous analysis requests
3. **Error Recovery Tests**: Handling corrupted solution files
4. **Custom Analyzer Tests**: When custom analyzers are added
5. **Symbol Analysis Tests**: When symbol analysis is implemented

## Contributing

When adding new functionality:

1. Add corresponding unit tests
2. Add integration tests for MCP tools
3. Add performance tests if applicable
4. Update test data if new error scenarios needed
5. Update this documentation
