using Microsoft.Extensions.Logging;
using MCP.Server.Services;

namespace MCP.Tests;

/// <summary>
/// Simple tests to verify basic functionality
/// </summary>
public class SimpleTests
{
    [Test]
    public async Task RoslynAnalysisService_CanBeCreated()
    {
        // Arrange & Act
        var service = TestSetup.CreateAnalysisService();

        // Assert
        await Assert.That(service).IsNotNull();

        // Cleanup
        service.Dispose();
    }

    [Test]
    public async Task TestSolution_CanBeFound()
    {
        // Act & Assert
        var solutionExists = TestSetup.VerifyTestSolution();
        await Assert.That(solutionExists).IsTrue();
    }

    [Test]
    public async Task GetCompilationErrorsAsync_WithoutSolution_ReturnsEmptyList()
    {
        // Arrange
        var service = TestSetup.CreateAnalysisService();

        // Act
        var errors = await service.GetCompilationErrorsAsync();

        // Assert
        await Assert.That(errors).IsNotNull();
        await Assert.That(errors.Count).IsEqualTo(0);

        // Cleanup
        service.Dispose();
    }

    [Test]
    public async Task LoadSolution_InvalidPath_ReturnsFalse()
    {
        // Arrange
        var service = TestSetup.CreateAnalysisService();

        // Act
        var result = await service.LoadSolutionAsync("/invalid/path.sln");

        // Assert
        await Assert.That(result).IsFalse();

        // Cleanup
        service.Dispose();
    }

    [Test]
    public async Task DotNetAnalysisTools_LoadSolution_InvalidPath_ReturnsErrorJson()
    {
        // Arrange
        var service = TestSetup.CreateAnalysisService();

        // Act
        var result = await DotNetAnalysisTools.LoadSolution(service, "/invalid/path.sln");

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result).Contains("\"success\":false");

        // Cleanup
        service.Dispose();
    }

    [Test]
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
            await Assert.That(result).IsTrue();
        }
        finally
        {
            // Cleanup
            service.Dispose();
        }
    }

    [Test]
    public async Task LoadTestSolution_AndGetErrors_ReturnsExpectedErrors()
    {
        // Arrange & Act
        var (service, loaded) = await TestSetup.LoadTestSolutionAsync();

        try
        {
            await Assert.That(loaded).IsTrue();

            var errors = await service.GetCompilationErrorsAsync();

            // Assert
            await Assert.That(errors).IsNotNull();
            await Assert.That(errors.Count).IsGreaterThan(0);

            // Check for expected error types
            var errorIds = errors.Select(e => e.Id).ToList();
            Console.WriteLine($"Found error IDs: {string.Join(", ", errorIds)}");

            // We should have at least some compilation errors
            await Assert.That(errorIds.Any(id => id.StartsWith("CS"))).IsTrue();
        }
        finally
        {
            // Cleanup
            service.Dispose();
        }
    }
}
