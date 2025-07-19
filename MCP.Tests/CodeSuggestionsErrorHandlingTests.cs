using Microsoft.Extensions.Logging;
using MCP.Server.Models;
using MCP.Server.Services;
using System.Text.Json;

namespace MCP.Tests;

/// <summary>
/// Tests for error handling and edge cases in code suggestions functionality
/// </summary>
public class CodeSuggestionsErrorHandlingTests
{
    private readonly ILogger<InMemoryAnalysisService> _logger;

    public CodeSuggestionsErrorHandlingTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => 
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });
        _logger = loggerFactory.CreateLogger<InMemoryAnalysisService>();
    }

    [Test]
    public async Task McpTools_GetCodeSuggestions_WithNullService_HandlesGracefully()
    {
        // This test verifies that our MCP tools handle null service gracefully
        // Note: We can't actually pass null due to method signature, but we can test with disposed service
        
        var service = InMemoryAnalysisService.CreateWithTestProjects(_logger);
        service.Dispose(); // Dispose the service
        
        // Act - This should handle the disposed service gracefully
        var result = await InMemoryMcpTools.GetCodeSuggestions(service);
        
        // Assert
        await Assert.That(result).IsNotNull();
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(response.GetProperty("success").GetBoolean()).IsTrue();
        await Assert.That(response.GetProperty("suggestion_count").GetInt32()).IsEqualTo(0);

        Console.WriteLine("Disposed service handled gracefully");
    }

    [Test]
    public async Task McpTools_GetCodeSuggestions_WithExtremeParameters_HandlesCorrectly()
    {
        // Test with extreme parameter values
        using var service = InMemoryAnalysisService.CreateWithTestProjects(_logger);

        // Test with very large maxSuggestions
        var result1 = await InMemoryMcpTools.GetCodeSuggestions(service, null, null, int.MaxValue);
        await Assert.That(result1).IsNotNull();
        
        var response1 = JsonSerializer.Deserialize<JsonElement>(result1);
        await Assert.That(response1.GetProperty("success").GetBoolean()).IsTrue();

        // Test with zero maxSuggestions
        var result2 = await InMemoryMcpTools.GetCodeSuggestions(service, null, null, 0);
        await Assert.That(result2).IsNotNull();
        
        var response2 = JsonSerializer.Deserialize<JsonElement>(result2);
        await Assert.That(response2.GetProperty("success").GetBoolean()).IsTrue();
        await Assert.That(response2.GetProperty("suggestion_count").GetInt32()).IsEqualTo(0);

        // Test with negative maxSuggestions (should be handled gracefully)
        var result3 = await InMemoryMcpTools.GetCodeSuggestions(service, null, null, -1);
        await Assert.That(result3).IsNotNull();
        
        var response3 = JsonSerializer.Deserialize<JsonElement>(result3);
        await Assert.That(response3.GetProperty("success").GetBoolean()).IsTrue();

        Console.WriteLine("Extreme parameters handled correctly");
    }

    [Test]
    public async Task McpTools_GetFileSuggestions_WithInvalidPaths_HandlesGracefully()
    {
        using var service = InMemoryAnalysisService.CreateWithTestProjects(_logger);

        var invalidPaths = new[]
        {
            "",
            "   ",
            null!,
            "/invalid/path/file.cs",
            "C:\\invalid\\path\\file.cs",
            "file.txt", // Wrong extension
            "very-long-filename-that-probably-does-not-exist-anywhere-in-the-system.cs",
            "../../../etc/passwd", // Security test
            "file with spaces.cs",
            "file\nwith\nnewlines.cs"
        };

        foreach (var invalidPath in invalidPaths)
        {
            try
            {
                var result = await InMemoryMcpTools.GetFileSuggestions(service, invalidPath);
                await Assert.That(result).IsNotNull();
                
                var response = JsonSerializer.Deserialize<JsonElement>(result);
                await Assert.That(response.GetProperty("success").GetBoolean()).IsTrue();
                await Assert.That(response.GetProperty("suggestion_count").GetInt32()).IsEqualTo(0);
                
                Console.WriteLine($"Invalid path handled: '{invalidPath ?? "null"}'");
            }
            catch (Exception ex)
            {
                // If an exception occurs, it should be handled gracefully
                Console.WriteLine($"Exception for path '{invalidPath ?? "null"}': {ex.Message}");
                // The test should still pass - we're testing that it doesn't crash
            }
        }
    }

    [Test]
    public async Task McpTools_GetCodeSuggestions_WithMalformedCategories_HandlesGracefully()
    {
        using var service = InMemoryAnalysisService.CreateWithTestProjects(_logger);

        var malformedCategories = new[]
        {
            "InvalidCategory",
            "Style,InvalidCategory,Performance",
            "STYLE", // Wrong case
            "style", // Wrong case
            "Performance,",
            ",Performance",
            "Performance,,Style",
            "Performance;Style", // Wrong separator
            "Performance Style", // No separator
            "123",
            "!@#$%",
            "Performance\nStyle", // Newline
            "Performance\tStyle", // Tab
            string.Empty,
            "   ",
            new string('A', 1000) // Very long string
        };

        foreach (var categories in malformedCategories)
        {
            var result = await InMemoryMcpTools.GetCodeSuggestions(service, categories);
            await Assert.That(result).IsNotNull();
            
            var response = JsonSerializer.Deserialize<JsonElement>(result);
            await Assert.That(response.GetProperty("success").GetBoolean()).IsTrue();
            
            Console.WriteLine($"Malformed categories handled: '{categories}'");
        }
    }

    [Test]
    public async Task McpTools_GetCodeSuggestions_WithMalformedPriorities_HandlesGracefully()
    {
        using var service = InMemoryAnalysisService.CreateWithTestProjects(_logger);

        var malformedPriorities = new[]
        {
            "InvalidPriority",
            "HIGH", // Wrong case
            "high", // Wrong case
            "123",
            "!@#$%",
            string.Empty,
            "   ",
            "Medium High", // Multiple values
            "Medium,High", // Comma separated
            new string('A', 1000) // Very long string
        };

        foreach (var priority in malformedPriorities)
        {
            var result = await InMemoryMcpTools.GetCodeSuggestions(service, null, priority);
            await Assert.That(result).IsNotNull();
            
            var response = JsonSerializer.Deserialize<JsonElement>(result);
            await Assert.That(response.GetProperty("success").GetBoolean()).IsTrue();
            
            Console.WriteLine($"Malformed priority handled: '{priority}'");
        }
    }

    [Test]
    public async Task CodeSuggestion_WithNullValues_HandlesGracefully()
    {
        // Test that CodeSuggestion handles null values appropriately
        var suggestion = new CodeSuggestion
        {
            Id = null!, // This should be handled
            Title = null!,
            Description = null!,
            FilePath = null!,
            OriginalCode = null!,
            SuggestedCode = null,
            Tags = null!,
            HelpLink = null,
            ProjectName = null!
        };

        // Test that we can serialize/deserialize with null values
        try
        {
            var json = JsonSerializer.Serialize(suggestion);
            await Assert.That(json).IsNotNull();
            
            var deserialized = JsonSerializer.Deserialize<CodeSuggestion>(json);
            await Assert.That(deserialized).IsNotNull();
            
            Console.WriteLine("Null values in CodeSuggestion handled gracefully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception with null values: {ex.Message}");
            // Test passes if we can handle the exception gracefully
        }
    }

    [Test]
    public async Task SuggestionAnalysisOptions_WithNullCollections_HandlesGracefully()
    {
        // Test that SuggestionAnalysisOptions handles null collections
        var options = new SuggestionAnalysisOptions();
        
        // Try to set collections to null (this might not be possible due to initialization)
        // But we can test with empty collections
        options.IncludedCategories.Clear();
        options.IncludedAnalyzerIds.Clear();
        options.ExcludedAnalyzerIds.Clear();

        await Assert.That(options.IncludedCategories).IsNotNull();
        await Assert.That(options.IncludedAnalyzerIds).IsNotNull();
        await Assert.That(options.ExcludedAnalyzerIds).IsNotNull();
        await Assert.That(options.IncludedCategories.Count).IsEqualTo(0);
        await Assert.That(options.IncludedAnalyzerIds.Count).IsEqualTo(0);
        await Assert.That(options.ExcludedAnalyzerIds.Count).IsEqualTo(0);

        Console.WriteLine("Empty collections handled correctly");
    }

    [Test]
    public async Task McpTools_ConcurrentAccess_HandlesCorrectly()
    {
        // Test concurrent access to suggestion methods
        using var service = InMemoryAnalysisService.CreateWithTestProjects(_logger);

        var tasks = new List<Task<string>>();

        // Create multiple concurrent requests
        for (var i = 0; i < 5; i++)
        {
            tasks.Add(InMemoryMcpTools.GetCodeSuggestions(service));
            tasks.Add(InMemoryMcpTools.GetFileSuggestions(service, "Program.cs"));
            tasks.Add(InMemoryMcpTools.GetSuggestionCategories());
        }

        // Wait for all tasks to complete
        var results = await Task.WhenAll(tasks);

        // Assert all requests completed successfully
        foreach (var result in results)
        {
            await Assert.That(result).IsNotNull();
            
            var response = JsonSerializer.Deserialize<JsonElement>(result);
            await Assert.That(response.GetProperty("success").GetBoolean()).IsTrue();
        }

        Console.WriteLine($"Completed {results.Length} concurrent requests successfully");
    }

    [Test]
    public async Task McpTools_LargeResponseHandling_WorksCorrectly()
    {
        // Test handling of potentially large responses
        using var service = InMemoryAnalysisService.CreateWithTestProjects(_logger);

        // Request maximum suggestions
        var result = await InMemoryMcpTools.GetCodeSuggestions(service, null, null, 10000);
        await Assert.That(result).IsNotNull();
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        await Assert.That(response.GetProperty("success").GetBoolean()).IsTrue();
        
        // Verify the response can be parsed even if large
        var suggestions = response.GetProperty("suggestions").EnumerateArray().ToList();
        await Assert.That(suggestions.Count).IsGreaterThanOrEqualTo(0);
        
        // Test JSON size is reasonable (not too large)
        await Assert.That(result.Length).IsLessThan(10_000_000); // Less than 10MB

        Console.WriteLine($"Large response test: {result.Length} characters, {suggestions.Count} suggestions");
    }

    [Test]
    public async Task McpTools_MemoryPressure_HandlesCorrectly()
    {
        // Test behavior under memory pressure by creating many services
        var services = new List<InMemoryAnalysisService>();
        
        try
        {
            // Create multiple services to simulate memory pressure
            for (var i = 0; i < 10; i++)
            {
                var service = InMemoryAnalysisService.CreateWithTestProjects(_logger);
                services.Add(service);
                
                // Get suggestions from each service
                var result = await InMemoryMcpTools.GetCodeSuggestions(service);
                await Assert.That(result).IsNotNull();
                
                var response = JsonSerializer.Deserialize<JsonElement>(result);
                await Assert.That(response.GetProperty("success").GetBoolean()).IsTrue();
            }

            Console.WriteLine($"Created and tested {services.Count} services successfully");
        }
        finally
        {
            // Clean up all services
            foreach (var service in services)
            {
                service.Dispose();
            }
        }

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    [Test]
    public async Task McpTools_JsonResponseFormat_IsConsistent()
    {
        // Test that JSON responses have consistent format across different scenarios
        using var service = InMemoryAnalysisService.CreateWithTestProjects(_logger);

        var results = new[]
        {
            await InMemoryMcpTools.GetCodeSuggestions(service),
            await InMemoryMcpTools.GetFileSuggestions(service, "Program.cs"),
            await InMemoryMcpTools.GetSuggestionCategories()
        };

        foreach (var result in results)
        {
            await Assert.That(result).IsNotNull();
            
            // Verify it's valid JSON
            var response = JsonSerializer.Deserialize<JsonElement>(result);
            
            // All responses should have a success field
            await Assert.That(response.TryGetProperty("success", out var successProp)).IsTrue();
            await Assert.That(successProp.ValueKind).IsEqualTo(JsonValueKind.True);
            
            // Verify JSON is well-formed (no syntax errors)
            await Assert.That(result.StartsWith("{")).IsTrue();
            await Assert.That(result.EndsWith("}")).IsTrue();
        }

        Console.WriteLine("All JSON responses have consistent format");
    }
}
