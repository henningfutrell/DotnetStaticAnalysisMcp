using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace MCP.Tests;

/// <summary>
/// Generates in-memory Roslyn projects and solutions for testing without file system dependencies
/// </summary>
public static class InMemoryProjectGenerator
{
    /// <summary>
    /// Creates a workspace with predefined test projects containing known compilation errors
    /// </summary>
    public static AdhocWorkspace CreateTestWorkspace()
    {
        var workspace = new AdhocWorkspace();

        // Add basic references needed for C# compilation
        var references = GetBasicReferences();

        // Create projects with different error scenarios
        var consoleProject = CreateConsoleProjectWithErrors(workspace, references);
        var libraryProject = CreateLibraryProjectWithErrors(workspace, references);
        var validProject = CreateValidProject(workspace, references);

        // Add project reference from console to library
        var consoleProjectWithRef = consoleProject.AddProjectReference(
            new ProjectReference(libraryProject.Id));
        workspace.TryApplyChanges(consoleProjectWithRef.Solution);

        return workspace;
    }

    /// <summary>
    /// Creates a console project with specific compilation errors for testing
    /// </summary>
    private static Project CreateConsoleProjectWithErrors(AdhocWorkspace workspace, ImmutableArray<MetadataReference> references)
    {
        var projectId = ProjectId.CreateNewId();
        var projectInfo = ProjectInfo.Create(
            projectId,
            VersionStamp.Create(),
            "TestConsoleProject",
            "TestConsoleProject",
            LanguageNames.CSharp,
            compilationOptions: new CSharpCompilationOptions(OutputKind.ConsoleApplication),
            parseOptions: new CSharpParseOptions(LanguageVersion.Latest),
            metadataReferences: references);

        var project = workspace.AddProject(projectInfo);

        // Add Program.cs with multiple error types
        var programCode = @"
using System;
using TestLibrary;

namespace TestConsoleProject
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(""Hello, World!"");
            
            // CS0103: The name 'undeclaredVariable' does not exist in the current context
            var result = undeclaredVariable + 5;
            
            // CS0246: The type or namespace name 'UnknownType' could not be found
            UnknownType unknown = new UnknownType();
            
            var calculator = new Calculator();
            var sum = calculator.Add(10, 20);
            Console.WriteLine($""Sum: {sum}"");
            
            // CS0161: Not all code paths return a value
            var value = GetValue();
            Console.WriteLine(value);
        }
        
        static int GetValue()
        {
            var random = new Random();
            if (random.Next(0, 2) == 0)
            {
                return 42;
            }
            // Missing return statement for else case - CS0161
        }
    }
}";

        var programDocument = DocumentInfo.Create(
            DocumentId.CreateNewId(projectId),
            "Program.cs",
            sourceCodeKind: SourceCodeKind.Regular,
            loader: TextLoader.From(TextAndVersion.Create(SourceText.From(programCode), VersionStamp.Create())));

        var solution = project.Solution.AddDocument(programDocument);
        workspace.TryApplyChanges(solution);

        return workspace.CurrentSolution.GetProject(projectId)!;
    }

    /// <summary>
    /// Creates a library project with syntax errors and warnings
    /// </summary>
    private static Project CreateLibraryProjectWithErrors(AdhocWorkspace workspace, ImmutableArray<MetadataReference> references)
    {
        var projectId = ProjectId.CreateNewId();
        var projectInfo = ProjectInfo.Create(
            projectId,
            VersionStamp.Create(),
            "TestLibrary",
            "TestLibrary",
            LanguageNames.CSharp,
            compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            parseOptions: new CSharpParseOptions(LanguageVersion.Latest),
            metadataReferences: references);

        var project = workspace.AddProject(projectInfo);

        // Add Calculator.cs with syntax error
        var calculatorCode = @"
using System;

namespace TestLibrary
{
    public class Calculator
    {
        public int Add(int a, int b)
        {
            return a + b;
        }
        
        public int Subtract(int a, int b)
        {
            return a - b;
        }
        
        // CS0168: Variable is declared but never used (warning)
        public void DoSomething(int unusedParameter)
        {
            Console.WriteLine(""Doing something..."");
        }
        
        // CS1002: Syntax error - missing semicolon
        public void BrokenMethod()
        {
            var x = 5
            Console.WriteLine(x);
        }
    }
}";

        var calculatorDocument = DocumentInfo.Create(
            DocumentId.CreateNewId(projectId),
            "Calculator.cs",
            sourceCodeKind: SourceCodeKind.Regular,
            loader: TextLoader.From(TextAndVersion.Create(SourceText.From(calculatorCode), VersionStamp.Create())));

        var solutionWithCalculator = project.Solution.AddDocument(calculatorDocument);
        workspace.TryApplyChanges(solutionWithCalculator);
        project = solutionWithCalculator.GetProject(projectId)!;

        // Add MathHelper.cs with additional errors
        var mathHelperCode = @"
namespace TestLibrary
{
    public class MathHelper
    {
        // CS0111: Type already defines a member (if we had duplicate)
        // CS0029: Cannot implicitly convert type
        public string Calculate()
        {
            int number = ""not a number""; // CS0029: Cannot convert string to int
            return number;
        }
        
        // CS0120: An object reference is required for the non-static field
        public static void StaticMethod()
        {
            var helper = new MathHelper();
            var result = NonStaticField; // CS0120 if NonStaticField exists
        }
        
        private int NonStaticField = 42;
    }
}";

        var mathHelperDocument = DocumentInfo.Create(
            DocumentId.CreateNewId(projectId),
            "MathHelper.cs",
            sourceCodeKind: SourceCodeKind.Regular,
            loader: TextLoader.From(TextAndVersion.Create(SourceText.From(mathHelperCode), VersionStamp.Create())));

        var solutionWithMathHelper = project.Solution.AddDocument(mathHelperDocument);
        workspace.TryApplyChanges(solutionWithMathHelper);

        return workspace.CurrentSolution.GetProject(projectId)!;
    }

    /// <summary>
    /// Creates a valid project with no compilation errors
    /// </summary>
    private static Project CreateValidProject(AdhocWorkspace workspace, ImmutableArray<MetadataReference> references)
    {
        var projectId = ProjectId.CreateNewId();
        var projectInfo = ProjectInfo.Create(
            projectId,
            VersionStamp.Create(),
            "ValidProject",
            "ValidProject",
            LanguageNames.CSharp,
            compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            parseOptions: new CSharpParseOptions(LanguageVersion.Latest),
            metadataReferences: references);

        var project = workspace.AddProject(projectInfo);

        // Add ValidClass.cs with no errors
        var validClassCode = @"
using System;

namespace ValidProject
{
    /// <summary>
    /// A valid class with no compilation errors
    /// </summary>
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
        
        public override string ToString()
        {
            return GetDescription();
        }
    }
}";

        var validClassDocument = DocumentInfo.Create(
            DocumentId.CreateNewId(projectId),
            "ValidClass.cs",
            sourceCodeKind: SourceCodeKind.Regular,
            loader: TextLoader.From(TextAndVersion.Create(SourceText.From(validClassCode), VersionStamp.Create())));

        var solutionWithValidClass = project.Solution.AddDocument(validClassDocument);
        workspace.TryApplyChanges(solutionWithValidClass);

        return workspace.CurrentSolution.GetProject(projectId)!;
    }

    /// <summary>
    /// Gets basic metadata references needed for C# compilation
    /// </summary>
    private static ImmutableArray<MetadataReference> GetBasicReferences()
    {
        var references = new List<MetadataReference>
        {
            // Core .NET references
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly.Location),
        };

        // Add System.Runtime if available
        try
        {
            var systemRuntime = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "System.Runtime");
            if (systemRuntime != null)
            {
                references.Add(MetadataReference.CreateFromFile(systemRuntime.Location));
            }
        }
        catch
        {
            // Ignore if System.Runtime can't be loaded
        }

        return references.ToImmutableArray();
    }

    /// <summary>
    /// Creates a workspace with a specific error scenario for targeted testing
    /// </summary>
    public static AdhocWorkspace CreateWorkspaceWithSpecificErrors(params string[] errorCodes)
    {
        var workspace = new AdhocWorkspace();
        var references = GetBasicReferences();

        var projectId = ProjectId.CreateNewId();
        var projectInfo = ProjectInfo.Create(
            projectId,
            VersionStamp.Create(),
            "TargetedErrorProject",
            "TargetedErrorProject",
            LanguageNames.CSharp,
            compilationOptions: new CSharpCompilationOptions(OutputKind.ConsoleApplication),
            parseOptions: new CSharpParseOptions(LanguageVersion.Latest),
            metadataReferences: references);

        var project = workspace.AddProject(projectInfo);

        var codeBuilder = new System.Text.StringBuilder();
        codeBuilder.AppendLine("using System;");
        codeBuilder.AppendLine("namespace TargetedErrorProject {");
        codeBuilder.AppendLine("class Program {");
        codeBuilder.AppendLine("static void Main() {");

        foreach (var errorCode in errorCodes)
        {
            codeBuilder.AppendLine(GetCodeForError(errorCode));
        }

        codeBuilder.AppendLine("}}}");

        var document = DocumentInfo.Create(
            DocumentId.CreateNewId(projectId),
            "Program.cs",
            sourceCodeKind: SourceCodeKind.Regular,
            loader: TextLoader.From(TextAndVersion.Create(SourceText.From(codeBuilder.ToString()), VersionStamp.Create())));

        var solutionWithDocument = project.Solution.AddDocument(document);
        workspace.TryApplyChanges(solutionWithDocument);

        return workspace;
    }

    /// <summary>
    /// Gets C# code that will generate a specific compiler error
    /// </summary>
    private static string GetCodeForError(string errorCode)
    {
        return errorCode switch
        {
            "CS0103" => "var x = undeclaredVariable;", // Undeclared variable
            "CS0246" => "UnknownType y = new UnknownType();", // Unknown type
            "CS0161" => "int GetValue() { if (true) return 1; }", // Not all code paths return
            "CS1002" => "var z = 5", // Missing semicolon
            "CS0029" => "int number = \"string\";", // Cannot convert type
            "CS0120" => "Console.WriteLine(instanceField);", // Object reference required
            _ => "// Unknown error code"
        };
    }
}
