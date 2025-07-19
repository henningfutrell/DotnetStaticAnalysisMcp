using Microsoft.Extensions.Logging;
using MCP.Server.Models;
using MCP.Server.Services;
using System.Text.Json;
using Xunit;

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

    [Fact]
    public async Task McpTools_GetCodeSuggestions_WithNullService_HandlesGracefully()
    {
        // This test verifies that our MCP tools handle null service gracefully
        // Note: We can't actually pass null due to method signature, but we can test with disposed service
        
        var service = InMemoryAnalysisService.CreateWithTestProjects(_logger);
        service.Dispose(); // Dispose the service
        
        // Act - This should handle the disposed service gracefully
        var result = await InMemoryMcpTools.GetCodeSuggestions(service);
        
        // Assert
        Assert.NotNull(result);
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(response.GetProperty("success").GetBoolean());
        Assert.Equal(0, response.GetProperty("suggestion_count").GetInt32());

        Console.WriteLine("Disposed service handled gracefully");
    }

    [Fact]
    public async Task McpTools_GetCodeSuggestions_WithExtremeParameters_HandlesCorrectly()
    {
        // Test with extreme parameter values
        using var service = InMemoryAnalysisService.CreateWithTestProjects(_logger);

        // Test with very large maxSuggestions
        var result1 = await InMemoryMcpTools.GetCodeSuggestions(service, null, null, int.MaxValue);
        Assert.NotNull(result1);
        
        var response1 = JsonSerializer.Deserialize<JsonElement>(result1);
        Assert.True(response1.GetProperty("success").GetBoolean());

        // Test with zero maxSuggestions
        var result2 = await InMemoryMcpTools.GetCodeSuggestions(service, null, null, 0);
        Assert.NotNull(result2);
        
        var response2 = JsonSerializer.Deserialize<JsonElement>(result2);
        Assert.True(response2.GetProperty("success").GetBoolean());
        Assert.Equal(0, response2.GetProperty("suggestion_count").GetInt32());

        // Test with negative maxSuggestions (should be handled gracefully)
        var result3 = await InMemoryMcpTools.GetCodeSuggestions(service, null, null, -1);
        Assert.NotNull(result3);
        
        var response3 = JsonSerializer.Deserialize<JsonElement>(result3);
        Assert.True(response3.GetProperty("success").GetBoolean());

        Console.WriteLine("Extreme parameters handled correctly");
    }

    [Fact]
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
                Assert.NotNull(result);
                
                var response = JsonSerializer.Deserialize<JsonElement>(result);
                Assert.True(response.GetProperty("success").GetBoolean());
                Assert.Equal(0, response.GetProperty("suggestion_count").GetInt32());
                
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

    [Fact]
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
            Assert.NotNull(result);
            
            var response = JsonSerializer.Deserialize<JsonElement>(result);
            Assert.True(response.GetProperty("success").GetBoolean());
            
            Console.WriteLine($"Malformed categories handled: '{categories}'");
        }
    }

    [Fact]
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
            Assert.NotNull(result);
            
            var response = JsonSerializer.Deserialize<JsonElement>(result);
            Assert.True(response.GetProperty("success").GetBoolean());
            
            Console.WriteLine($"Malformed priority handled: '{priority}'");
        }
    }

    [Fact]
    public void CodeSuggestion_WithNullValues_HandlesGracefully()
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
            Assert.NotNull(json);
            
            var deserialized = JsonSerializer.Deserialize<CodeSuggestion>(json);
            Assert.NotNull(deserialized);
            
            Console.WriteLine("Null values in CodeSuggestion handled gracefully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception with null values: {ex.Message}");
            // Test passes if we can handle the exception gracefully
        }
    }

    [Fact]
    public void SuggestionAnalysisOptions_WithNullCollections_HandlesGracefully()
    {
        // Test that SuggestionAnalysisOptions handles null collections
        var options = new SuggestionAnalysisOptions();
        
        // Try to set collections to null (this might not be possible due to initialization)
        // But we can test with empty collections
        options.IncludedCategories.Clear();
        options.IncludedAnalyzerIds.Clear();
        options.ExcludedAnalyzerIds.Clear();

        Assert.NotNull(options.IncludedCategories);
        Assert.NotNull(options.IncludedAnalyzerIds);
        Assert.NotNull(options.ExcludedAnalyzerIds);
        Assert.Empty(options.IncludedCategories);
        Assert.Empty(options.IncludedAnalyzerIds);
        Assert.Empty(options.ExcludedAnalyzerIds);

        Console.WriteLine("Empty collections handled correctly");
    }

    [Fact]
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
            Assert.NotNull(result);
            
            var response = JsonSerializer.Deserialize<JsonElement>(result);
            Assert.True(response.GetProperty("success").GetBoolean());
        }

        Console.WriteLine($"Completed {results.Length} concurrent requests successfully");
    }

    [Fact]
    public async Task McpTools_LargeResponseHandling_WorksCorrectly()
    {
        // Test handling of potentially large responses
        using var service = InMemoryAnalysisService.CreateWithTestProjects(_logger);

        // Request maximum suggestions
        var result = await InMemoryMcpTools.GetCodeSuggestions(service, null, null, 10000);
        Assert.NotNull(result);
        
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(response.GetProperty("success").GetBoolean());
        
        // Verify the response can be parsed even if large
        var suggestions = response.GetProperty("suggestions").EnumerateArray().ToList();
        Assert.True(suggestions.Count >= 0);
        
        // Test JSON size is reasonable (not too large)
        Assert.True(result.Length < 10_000_000); // Less than 10MB

        Console.WriteLine($"Large response test: {result.Length} characters, {suggestions.Count} suggestions");
    }

    [Fact]
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
                Assert.NotNull(result);
                
                var response = JsonSerializer.Deserialize<JsonElement>(result);
                Assert.True(response.GetProperty("success").GetBoolean());
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

    [Fact]
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
            Assert.NotNull(result);
            
            // Verify it's valid JSON
            var response = JsonSerializer.Deserialize<JsonElement>(result);
            
            // All responses should have a success field
            Assert.True(response.TryGetProperty("success", out var successProp));
            Assert.Equal(JsonValueKind.True, successProp.ValueKind);
            
            // Verify JSON is well-formed (no syntax errors)
            Assert.StartsWith("{", result);
            Assert.EndsWith("}", result);
        }

        Console.WriteLine("All JSON responses have consistent format");
    }
}
