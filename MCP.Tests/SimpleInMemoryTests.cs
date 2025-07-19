using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using MCP.Server.Models;
using System.Text.Json;

namespace MCP.Tests;

/// <summary>
/// Simple in-memory tests using CSharpCompilation directly (no workspace dependencies)
/// </summary>
public class SimpleInMemoryTests
{
    [Test]
    public async Task DirectCompilation_WithErrors_DetectsExpectedErrors()
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
        await Assert.That(diagnostics.Length).IsGreaterThan(0);

        var errorIds = diagnostics.Select(d => d.Id).ToList();
        Console.WriteLine($"Found error IDs: {string.Join(", ", errorIds)}");

        // Check for expected errors
        await Assert.That(errorIds).Contains("CS0103"); // undeclared variable
        await Assert.That(errorIds).Contains("CS0246"); // unknown type
        await Assert.That(errorIds).Contains("CS0161"); // not all code paths return
    }

    [Test]
    public async Task DirectCompilation_SyntaxError_DetectsCS1002()
    {
        // Arrange - Create C# code with syntax error
        var sourceCode = @"
using System;

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
        await Assert.That(diagnostics.Length).IsGreaterThan(0);

        var errorIds = diagnostics.Select(d => d.Id).ToList();
        Console.WriteLine($"Syntax error IDs: {string.Join(", ", errorIds)}");

        await Assert.That(errorIds).Contains("CS1002"); // Missing semicolon
    }

    [Test]
    public async Task DirectCompilation_ValidCode_NoErrors()
    {
        // Arrange - Create valid C# code
        var sourceCode = @"
using System;

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
        await Assert.That(diagnostics.Length).IsEqualTo(0);
        Console.WriteLine("Valid code compiled without errors");
    }

    [Test]
    public async Task CompilationErrorsToModels_ConvertsCorrectly()
    {
        // Arrange
        var sourceCode = @"
using System;
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
        await Assert.That(errors.Count).IsGreaterThan(0);
        
        var firstError = errors[0];
        await Assert.That(firstError.Id).IsEqualTo("CS0103");
        await Assert.That(firstError.Message).Contains("undeclaredVar");
        await Assert.That(firstError.StartLine).IsGreaterThan(0);
        await Assert.That(firstError.StartColumn).IsGreaterThan(0);
        await Assert.That(firstError.ProjectName).IsEqualTo("InMemoryProject");

        Console.WriteLine($"Converted {errors.Count} diagnostics to CompilationError models");
    }

    [Test]
    public async Task McpJsonResponse_WithDirectCompilation_ReturnsValidFormat()
    {
        // Arrange
        var sourceCode = @"
using System;
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
        await Assert.That(json).IsNotNull();
        
        var parsed = JsonSerializer.Deserialize<JsonElement>(json);
        await Assert.That(parsed.GetProperty("success").GetBoolean()).IsTrue();
        await Assert.That(parsed.GetProperty("error_count").GetInt32()).IsGreaterThan(0);
        
        var errorsArray = parsed.GetProperty("errors").EnumerateArray().ToList();
        await Assert.That(errorsArray.Count).IsGreaterThan(0);

        Console.WriteLine($"Generated MCP JSON response with {errorsArray.Count} errors");
        Console.WriteLine($"JSON length: {json.Length} characters");
    }

    [Test]
    public async Task MultipleCompilations_Performance_CompletesQuickly()
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
        await Assert.That(allErrors.Count).IsGreaterThan(0);
        await Assert.That(stopwatch.ElapsedMilliseconds).IsLessThan(5000); // Should complete in < 5 seconds

        var errorIds = allErrors.Select(e => e.Id).Distinct().ToList();
        await Assert.That(errorIds).Contains("CS0103");
        await Assert.That(errorIds).Contains("CS0246");

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
