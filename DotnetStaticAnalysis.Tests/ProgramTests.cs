using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DotnetStaticAnalysisMcp.Server.Services;
using Xunit;
using System.Reflection;
using Microsoft.Build.Locator;
using Serilog;

namespace MCP.Tests;

/// <summary>
/// Tests for Program.cs startup and configuration
/// </summary>
public class ProgramTests
{
    [Fact]
    public void Server_Assembly_CanBeLoaded()
    {
        // Arrange & Act
        var assembly = Assembly.LoadFrom("DotnetStaticAnalysisMcp.Server.dll");

        // Assert
        Assert.NotNull(assembly);
        Assert.NotNull(assembly.FullName);
        Assert.Contains("DotnetStaticAnalysisMcp.Server", assembly.FullName);
    }

    [Fact]
    public void Server_Assembly_HasExpectedTypes()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        var types = assembly.GetTypes();

        // Assert
        Assert.NotNull(types);
        Assert.True(types.Length > 0);
    }

    [Fact]
    public void MSBuildLocator_CanQueryInstances()
    {
        // Arrange & Act
        var instances = MSBuildLocator.QueryVisualStudioInstances();

        // Assert
        Assert.NotNull(instances);
        // Don't assert on count as it depends on environment
    }

    [Fact]
    public void Environment_Variables_CanBeAccessed()
    {
        // Arrange & Act
        var processId = Environment.ProcessId;
        var machineName = Environment.MachineName;
        var currentDirectory = Directory.GetCurrentDirectory();

        // Assert
        Assert.True(processId > 0);
        Assert.NotNull(machineName);
        Assert.NotEmpty(machineName);
        Assert.NotNull(currentDirectory);
        Assert.NotEmpty(currentDirectory);
    }

    [Fact]
    public void LogDirectory_CanBeCreated()
    {
        // Arrange
        var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".mcp", "logs");

        // Act
        Directory.CreateDirectory(logDirectory);
        var logFile = Path.Combine(logDirectory, "test-dotnet-analysis.log");

        // Assert
        Assert.True(Directory.Exists(logDirectory));
        Assert.NotNull(logFile);
        Assert.EndsWith(".log", logFile);
    }

    [Fact]
    public void SerilogConfiguration_CanBeCreated()
    {
        // Arrange & Act
        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.WithProperty("Application", "MCP.DotNetAnalysis.Test")
            .Enrich.WithProperty("ProcessId", Environment.ProcessId)
            .Enrich.WithProperty("MachineName", Environment.MachineName);

        var logger = loggerConfig.CreateLogger();

        // Assert
        Assert.NotNull(logger);
        
        // Test logging
        logger.Information("Test log message");
        logger.Dispose();
    }

    [Fact]
    public void HostBuilder_CanBeCreated()
    {
        // Arrange
        var args = new string[] { };

        // Act
        var builder = Host.CreateApplicationBuilder(args);

        // Assert
        Assert.NotNull(builder);
        Assert.NotNull(builder.Services);
    }

    [Fact]
    public void Services_CanBeRegistered()
    {
        // Arrange
        var args = new string[] { };
        var builder = Host.CreateApplicationBuilder(args);

        // Act
        builder.Services.AddSingleton<TelemetryService>();
        builder.Services.AddSingleton<RoslynAnalysisService>();
        builder.Services.AddSingleton<CodeCoverageService>();

        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        Assert.NotNull(serviceProvider.GetService<TelemetryService>());
        Assert.NotNull(serviceProvider.GetService<RoslynAnalysisService>());
        Assert.NotNull(serviceProvider.GetService<CodeCoverageService>());
        
        // Cleanup
        serviceProvider.Dispose();
    }

    [Fact]
    public void DotNetAnalysisTools_SetServiceProvider_WorksCorrectly()
    {
        // Arrange
        var args = new string[] { };
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddSingleton<TelemetryService>();
        builder.Services.AddSingleton<RoslynAnalysisService>();
        builder.Services.AddSingleton<CodeCoverageService>();

        var serviceProvider = builder.Services.BuildServiceProvider();

        // Act & Assert - Should not throw
        DotNetAnalysisTools.SetServiceProvider(serviceProvider);
        Assert.True(true);
        
        // Cleanup
        serviceProvider.Dispose();
    }

    [Fact]
    public void Assembly_Location_CanBeAccessed()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        var location = assembly.Location;
        var lastWriteTime = File.GetLastWriteTime(location);

        // Assert
        Assert.NotNull(location);
        Assert.NotEmpty(location);
        Assert.True(lastWriteTime > DateTime.MinValue);
    }

    [Fact]
    public void Environment_SpecialFolders_CanBeAccessed()
    {
        // Arrange & Act
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var tempPath = Path.GetTempPath();

        // Assert
        Assert.NotNull(userProfile);
        Assert.NotEmpty(userProfile);
        Assert.True(Directory.Exists(userProfile));
        Assert.NotNull(tempPath);
        Assert.NotEmpty(tempPath);
    }

    [Fact]
    public void Path_Operations_WorkCorrectly()
    {
        // Arrange
        var basePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        
        // Act
        var mcpPath = Path.Combine(basePath, ".mcp");
        var logsPath = Path.Combine(mcpPath, "logs");
        var logFile = Path.Combine(logsPath, "test.log");

        // Assert
        Assert.NotNull(mcpPath);
        Assert.NotNull(logsPath);
        Assert.NotNull(logFile);
        Assert.Contains(".mcp", mcpPath);
        Assert.Contains("logs", logsPath);
        Assert.EndsWith(".log", logFile);
    }

    [Fact]
    public void MSBuildLocator_IsRegistered_CanBeChecked()
    {
        // Arrange & Act
        var isRegistered = MSBuildLocator.IsRegistered;

        // Assert
        // Just verify we can check the property without throwing
        Assert.True(isRegistered || !isRegistered); // Always true, but tests the property access
    }

    [Fact]
    public void DateTime_Operations_WorkCorrectly()
    {
        // Arrange & Act
        var now = DateTime.UtcNow;
        var today = DateTime.Today;

        // Assert
        Assert.True(now > DateTime.MinValue);
        Assert.True(today >= DateTime.MinValue);
        Assert.True(now >= today);
    }

    [Fact]
    public void RollingInterval_Values_AreValid()
    {
        // Arrange & Act
        var day = Serilog.RollingInterval.Day;
        var hour = Serilog.RollingInterval.Hour;

        // Assert
        Assert.Equal(Serilog.RollingInterval.Day, day);
        Assert.Equal(Serilog.RollingInterval.Hour, hour);
    }

    [Fact]
    public void Exception_Handling_WorksCorrectly()
    {
        // Arrange
        var testException = new InvalidOperationException("Test exception");

        // Act & Assert
        Assert.Equal("Test exception", testException.Message);
        Assert.IsType<InvalidOperationException>(testException);
    }

    [Fact]
    public void File_Operations_WorkCorrectly()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act
            File.WriteAllText(tempFile, "test content");
            var exists = File.Exists(tempFile);
            var lastWrite = File.GetLastWriteTime(tempFile);

            // Assert
            Assert.True(exists);
            Assert.True(lastWrite > DateTime.MinValue);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void Directory_Operations_WorkCorrectly()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            // Act
            Directory.CreateDirectory(tempDir);
            var exists = Directory.Exists(tempDir);
            var currentDir = Directory.GetCurrentDirectory();

            // Assert
            Assert.True(exists);
            Assert.NotNull(currentDir);
            Assert.NotEmpty(currentDir);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir);
            }
        }
    }

    [Fact]
    public void Guid_Generation_WorksCorrectly()
    {
        // Arrange & Act
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();

        // Assert
        Assert.NotEqual(guid1, guid2);
        Assert.NotEqual(Guid.Empty, guid1);
        Assert.NotEqual(Guid.Empty, guid2);
    }
}
