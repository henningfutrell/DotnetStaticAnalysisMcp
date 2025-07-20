using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using DotnetStaticAnalysisMcp.Server.Models;
using System.Text.Json;
using Xunit;

namespace MCP.Tests;

/// <summary>
/// Simple in-memory tests using CSharpCompilation directly (no workspace dependencies)
/// </summary>
public class SimpleInMemoryTests
{
    [Fact]
    public void DirectCompilation_WithErrors_DetectsExpectedErrors()
    {
        // Arrange - Create C# code with known errors
        var sourceCode = @"
using System;

namespace TestProject
{
    class Program
    {
        static void Main()
        {
            // CS0103: undeclared variable
            var result = undeclaredVariable + 5;
            
            // CS0246: unknown type
            UnknownType unknown = new UnknownType();
            
            Console.WriteLine(""Hello World"");
        }
        
        // CS0161: not all code paths return a value
        static int GetValue()
        {
            var random = new Random();
            if (random.Next(0, 2) == 0)
            {
                return 42;
            }
            // Missing return statement
        }
    }
}";

        // Act - Compile with Roslyn directly
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            GetBasicReferences(),
            new CSharpCompilationOptions(OutputKind.ConsoleApplication));

        var diagnostics = compilation.GetDiagnostics();

        // Assert
        Assert.True(diagnostics.Length > 0);

        var errorIds = diagnostics.Select(d => d.Id).ToList();
        Console.WriteLine($"Found error IDs: {string.Join(", ", errorIds)}");

        // Check for expected errors
        Assert.Contains("CS0103", errorIds); // undeclared variable
        Assert.Contains("CS0246", errorIds); // unknown type
        Assert.Contains("CS0161", errorIds); // not all code paths return
    }

    [Fact]
    public void DirectCompilation_SyntaxError_DetectsCS1002()
    {
        // Arrange - Create C# code with syntax error
        var sourceCode = @"

namespace TestProject
{
    class Calculator
    {
        public void BrokenMethod()
        {
            var x = 5
            Console.WriteLine(x);
        }
    }
}";

        // Act
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            GetBasicReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var diagnostics = compilation.GetDiagnostics();

        // Assert
        Assert.True(diagnostics.Length > 0);

        var errorIds = diagnostics.Select(d => d.Id).ToList();
        Console.WriteLine($"Syntax error IDs: {string.Join(", ", errorIds)}");

        Assert.Contains("CS1002", errorIds); // Missing semicolon
    }

    [Fact]
    public void DirectCompilation_ValidCode_NoErrors()
    {
        // Arrange - Create valid C# code
        var sourceCode = @"

namespace TestProject
{
    public class ValidClass
    {
        public string Name { get; set; } = string.Empty;
        
        public int Value { get; set; }
        
        public ValidClass()
        {
        }
        
        public ValidClass(string name, int value)
        {
            Name = name;
            Value = value;
        }
        
        public string GetDescription()
        {
            return $""Name: {Name}, Value: {Value}"";
        }
    }
}";

        // Act
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            GetBasicReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var diagnostics = compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();

        // Assert
        Assert.Empty(diagnostics);
        Console.WriteLine("Valid code compiled without errors");
    }

    [Fact]
    public void CompilationErrorsToModels_ConvertsCorrectly()
    {
        // Arrange
        var sourceCode = @"
class Test 
{ 
    void Method() 
    { 
        var x = undeclaredVar; 
    } 
}";

        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            GetBasicReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var diagnostics = compilation.GetDiagnostics();

        // Act - Convert to our model format
        var errors = new List<CompilationError>();
        foreach (var diagnostic in diagnostics)
        {
            var location = diagnostic.Location;
            var lineSpan = location.GetLineSpan();

            errors.Add(new CompilationError
            {
                Id = diagnostic.Id,
                Title = diagnostic.Descriptor.Title.ToString(),
                Message = diagnostic.GetMessage(),
                Severity = diagnostic.Severity,
                FilePath = "InMemoryTest.cs",
                StartLine = lineSpan.StartLinePosition.Line + 1,
                StartColumn = lineSpan.StartLinePosition.Character + 1,
                EndLine = lineSpan.EndLinePosition.Line + 1,
                EndColumn = lineSpan.EndLinePosition.Character + 1,
                Category = diagnostic.Descriptor.Category,
                HelpLink = diagnostic.Descriptor.HelpLinkUri,
                IsWarningAsError = diagnostic.IsWarningAsError,
                WarningLevel = diagnostic.WarningLevel,
                CustomTags = string.Join(", ", diagnostic.Descriptor.CustomTags),
                ProjectName = "InMemoryProject"
            });
        }

        // Assert
        Assert.True(errors.Count > 0);
        
        var firstError = errors[0];
        Assert.Equal("CS0103", firstError.Id);
        Assert.Contains("undeclaredVar", firstError.Message);
        Assert.True(firstError.StartLine > 0);
        Assert.True(firstError.StartColumn > 0);
        Assert.Equal("InMemoryProject", firstError.ProjectName);

        Console.WriteLine($"Converted {errors.Count} diagnostics to CompilationError models");
    }

    [Fact]
    public void McpJsonResponse_WithDirectCompilation_ReturnsValidFormat()
    {
        // Arrange
        var sourceCode = @"
class Test 
{ 
    void Method() 
    { 
        var x = undeclaredVar;
        UnknownType y = new UnknownType();
    } 
}";

        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            GetBasicReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var diagnostics = compilation.GetDiagnostics();

        // Convert to our models
        var errors = diagnostics.Select(d => new CompilationError
        {
            Id = d.Id,
            Message = d.GetMessage(),
            Severity = d.Severity,
            FilePath = "Test.cs",
            StartLine = d.Location.GetLineSpan().StartLinePosition.Line + 1,
            StartColumn = d.Location.GetLineSpan().StartLinePosition.Character + 1,
            ProjectName = "TestProject"
        }).ToList();

        // Act - Create MCP-style JSON response
        var response = new
        {
            success = true,
            error_count = errors.Count(e => e.Severity == DiagnosticSeverity.Error),
            warning_count = errors.Count(e => e.Severity == DiagnosticSeverity.Warning),
            errors = errors
        };

        var json = JsonSerializer.Serialize(response);

        // Assert
        Assert.NotNull(json);
        
        var parsed = JsonSerializer.Deserialize<JsonElement>(json);
        Assert.True(parsed.GetProperty("success").GetBoolean());
        Assert.True(parsed.GetProperty("error_count").GetInt32() > 0);
        
        var errorsArray = parsed.GetProperty("errors").EnumerateArray().ToList();
        Assert.True(errorsArray.Count > 0);

        Console.WriteLine($"Generated MCP JSON response with {errorsArray.Count} errors");
        Console.WriteLine($"JSON length: {json.Length} characters");
    }

    [Fact]
    public void MultipleCompilations_Performance_CompletesQuickly()
    {
        // Arrange
        var sourceCodes = new[]
        {
            "using System; class Test1 { void Method() { var x = undeclaredVar; } }",
            "using System; class Test2 { void Method() { UnknownType y; } }",
            "using System; class Test3 { int Method() { if (true) return 1; } }", // CS0161
            "using System; class Test4 { void Method() { var x = 5 Console.WriteLine(x); } }" // CS1002
        };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var allErrors = new List<CompilationError>();
        
        foreach (var sourceCode in sourceCodes)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            var compilation = CSharpCompilation.Create(
                $"TestAssembly_{allErrors.Count}",
                new[] { syntaxTree },
                GetBasicReferences(),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var diagnostics = compilation.GetDiagnostics();
            
            foreach (var diagnostic in diagnostics)
            {
                allErrors.Add(new CompilationError
                {
                    Id = diagnostic.Id,
                    Message = diagnostic.GetMessage(),
                    Severity = diagnostic.Severity,
                    FilePath = $"Test{allErrors.Count}.cs",
                    ProjectName = $"Project{allErrors.Count}"
                });
            }
        }

        stopwatch.Stop();

        // Assert
        Assert.True(allErrors.Count > 0);
        Assert.True(stopwatch.ElapsedMilliseconds < 5000); // Should complete in < 5 seconds

        var errorIds = allErrors.Select(e => e.Id).Distinct().ToList();
        Assert.Contains("CS0103", errorIds);
        Assert.Contains("CS0246", errorIds);

        Console.WriteLine($"Processed {sourceCodes.Length} compilations in {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Found {allErrors.Count} total errors with IDs: {string.Join(", ", errorIds)}");
    }

    /// <summary>
    /// Gets basic metadata references needed for C# compilation
    /// </summary>
    private static MetadataReference[] GetBasicReferences()
    {
        return new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly.Location),
        };
    }
}
