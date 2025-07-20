using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MCP.Server.Services;
using ModelContextProtocol.Server;
using Microsoft.Build.Locator;
using Serilog;
using Serilog.Formatting.Compact;

namespace MCP.Server;

internal class Program
{
    private static async Task Main(string[] args)
    {
        // Setup file-based logging first
        var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".mcp", "logs");
        Directory.CreateDirectory(logDirectory);
        var logFile = Path.Combine(logDirectory, "dotnet-analysis.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.WithProperty("Application", "MCP.DotNetAnalysis")
            .Enrich.WithProperty("ProcessId", Environment.ProcessId)
            .Enrich.WithProperty("MachineName", Environment.MachineName)
            .WriteTo.File(
                new CompactJsonFormatter(),
                logFile,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                buffered: false, // Immediate write for debugging
                shared: true)
            .CreateLogger();

        try
        {
            Log.Information("=== MCP .NET Analysis Server Starting ===");
            Log.Information("Process ID: {ProcessId}, Machine: {MachineName}", Environment.ProcessId, Environment.MachineName);
            Log.Information("Log file: {LogFile}", logFile);

            // Initialize MSBuild before anything else
            try
            {
                if (!MSBuildLocator.IsRegistered)
                {
                    Log.Information("Initializing MSBuild...");
                    Log.Information("Current directory: {CurrentDirectory}", Directory.GetCurrentDirectory());
                    Log.Information("DOTNET_ROOT: {DotNetRoot}", Environment.GetEnvironmentVariable("DOTNET_ROOT"));
                    Log.Information("PATH: {Path}", Environment.GetEnvironmentVariable("PATH"));

                    var instances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
                    Log.Information("Found {InstanceCount} MSBuild instances", instances.Length);
                    foreach (var instance in instances)
                    {
                        Log.Information("MSBuild instance: {Name} {Version} at {Path}",
                            instance.Name, instance.Version, instance.MSBuildPath);
                    }

                    MSBuildLocator.RegisterDefaults();
                    Log.Information("MSBuild initialized successfully");
                }
                else
                {
                    Log.Information("MSBuild already registered");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to initialize MSBuild: {ErrorMessage}", ex.Message);
                // Continue anyway - some functionality might still work
            }

            // Create host builder
            var builder = Host.CreateApplicationBuilder(args);

        // Configure Serilog for structured logging
        builder.Services.AddSerilog();

        // Register services
        builder.Services.AddSingleton<TelemetryService>();
        builder.Services.AddSingleton<RoslynAnalysisService>();

        // Configure MCP server
        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly();

        // Build and run
        var host = builder.Build();

        // Set service provider for MCP tools
        MCP.Server.Services.DotNetAnalysisTools.SetServiceProvider(host.Services);

        Log.Information("MCP .NET Analysis Server built successfully, starting host...");
        await host.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            throw;
        }
        finally
        {
            Log.Information("=== MCP .NET Analysis Server Shutting Down ===");
            Log.CloseAndFlush();
        }
    }
}
