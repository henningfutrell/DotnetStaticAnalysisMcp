using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MCP.Server.Services;
using ModelContextProtocol.Server;

namespace MCP.Server;

internal class Program
{
    private static async Task Main(string[] args)
    {
        // Create host builder
        var builder = Host.CreateApplicationBuilder(args);

        // Configure logging to stderr for MCP compatibility
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(consoleLogOptions =>
        {
            // Configure all logs to go to stderr
            consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
        });

        // Register services
        builder.Services.AddSingleton<RoslynAnalysisService>();

        // Configure MCP server
        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly();

        // Build and run
        var host = builder.Build();

        await host.RunAsync();
    }
}
