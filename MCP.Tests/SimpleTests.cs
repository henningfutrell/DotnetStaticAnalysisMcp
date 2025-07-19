using Microsoft.Extensions.Logging;
using MCP.Server.Services;
using Xunit;

namespace MCP.Tests;

/// <summary>
/// Simple tests to verify basic functionality
/// </summary>
public class SimpleTests
{
    [Fact]
    public void RoslynAnalysisService_CanBeCreated()
    {
        // Arrange & Act
        var service = TestSetup.CreateAnalysisService();

        // Assert
        Assert.NotNull(service);

        // Cleanup
        service.Dispose();
    }

    [Fact]
    public void TestSolution_CanBeFound()
    {
        // Act & Assert
        var solutionExists = TestSetup.VerifyTestSolution();
        Assert.True(solutionExists);
    }

    [Fact]
    public async Task GetCompilationErrorsAsync_WithoutSolution_ReturnsEmptyList()
    {
        // Arrange
        var service = TestSetup.CreateAnalysisService();

        // Act
        var errors = await service.GetCompilationErrorsAsync();

        // Assert
        Assert.NotNull(errors);
        Assert.Empty(errors);

        // Cleanup
        service.Dispose();
    }

    [Fact]
    public async Task LoadSolution_InvalidPath_ReturnsFalse()
    {
        // Arrange
        var service = TestSetup.CreateAnalysisService();

        // Act
        var result = await service.LoadSolutionAsync("/invalid/path.sln");

        // Assert
        Assert.False(result);

        // Cleanup
        service.Dispose();
    }

    [Fact]
    public async Task DotNetAnalysisTools_LoadSolution_InvalidPath_ReturnsErrorJson()
    {
        // Arrange
        var service = TestSetup.CreateAnalysisService();

        // Act
        var result = await DotNetAnalysisTools.LoadSolution(service, "/invalid/path.sln");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("\"success\":false", result);

        // Cleanup
        service.Dispose();
    }

    [Fact]
    public async Task LoadSolution_ValidTestSolution_ReturnsTrue()
    {
        // Arrange
        var service = TestSetup.CreateAnalysisService();

        try
        {
            var solutionPath = TestSetup.GetTestSolutionPath();

            // Act
            var result = await service.LoadSolutionAsync(solutionPath);

            // Assert
            Assert.True(result);
        }
        finally
        {
            // Cleanup
            service.Dispose();
        }
    }

    [Fact]
    public async Task LoadTestSolution_AndGetErrors_ReturnsExpectedErrors()
    {
        // Arrange & Act
        var (service, loaded) = await TestSetup.LoadTestSolutionAsync();

        try
        {
            Assert.True(loaded);

            var errors = await service.GetCompilationErrorsAsync();

            // Assert
            Assert.NotNull(errors);
            Assert.True(errors.Count > 0);

            // Check for expected error types
            var errorIds = errors.Select(e => e.Id).ToList();
            Console.WriteLine($"Found error IDs: {string.Join(", ", errorIds)}");

            // We should have at least some compilation errors
            Assert.Contains(errorIds, id => id.StartsWith("CS"));
        }
        finally
        {
            // Cleanup
            service.Dispose();
        }
    }
}
