using MCP.Server.Models;
using Microsoft.CodeAnalysis;
using System.Text.Json;

namespace MCP.IntegrationTests;

/// <summary>
/// Tests that exercise the REAL production model classes
/// These tests ensure the actual models work correctly and will show up in code coverage
/// </summary>
public class ProductionModelsTests
{
    [Fact]
    public void CompilationError_DefaultConstructor_CreatesValidInstance()
    {
        // Test the REAL production model
        var error = new CompilationError();

        Assert.NotNull(error);
        Assert.Equal(string.Empty, error.Id);
        Assert.Equal(string.Empty, error.Message);
        Assert.Equal(string.Empty, error.FilePath);
        Assert.Equal(string.Empty, error.ProjectName);
        Assert.Equal(string.Empty, error.Category);
        Assert.Equal(0, error.StartLine);
        Assert.Equal(0, error.EndLine);
        Assert.Equal(0, error.StartColumn);
        Assert.Equal(0, error.EndColumn);
    }

    [Fact]
    public void CompilationError_WithProperties_CanBeSetAndRetrieved()
    {
        // Test the REAL production model with all properties
        var error = new CompilationError
        {
            Id = "CS0103",
            Message = "The name 'test' does not exist in the current context",
            Severity = DiagnosticSeverity.Error,
            FilePath = "Program.cs",
            StartLine = 10,
            EndLine = 10,
            StartColumn = 5,
            EndColumn = 15,
            ProjectName = "TestProject",
            Category = "Compiler"
        };

        Assert.Equal("CS0103", error.Id);
        Assert.Equal("The name 'test' does not exist in the current context", error.Message);
        Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        Assert.Equal("Program.cs", error.FilePath);
        Assert.Equal(10, error.StartLine);
        Assert.Equal(10, error.EndLine);
        Assert.Equal(5, error.StartColumn);
        Assert.Equal(15, error.EndColumn);
        Assert.Equal("TestProject", error.ProjectName);
        Assert.Equal("Compiler", error.Category);
    }

    [Fact]
    public void SolutionInfo_DefaultConstructor_CreatesValidInstance()
    {
        // Test the REAL production model
        var solutionInfo = new MCP.Server.Models.SolutionInfo();

        Assert.NotNull(solutionInfo);
        Assert.Equal(string.Empty, solutionInfo.Name);
        Assert.Equal(string.Empty, solutionInfo.FilePath);
        Assert.False(solutionInfo.HasCompilationErrors);
        Assert.Equal(0, solutionInfo.TotalErrors);
        Assert.Equal(0, solutionInfo.TotalWarnings);
        Assert.NotNull(solutionInfo.Projects);
        Assert.Empty(solutionInfo.Projects);
    }

    [Fact]
    public void SolutionInfo_WithProperties_CanBeSetAndRetrieved()
    {
        // Test the REAL production model with all properties
        var solutionInfo = new MCP.Server.Models.SolutionInfo
        {
            Name = "TestSolution",
            FilePath = "TestSolution.sln",
            HasCompilationErrors = true,
            TotalErrors = 5,
            TotalWarnings = 3,
            Projects = new List<MCP.Server.Models.ProjectInfo>
            {
                new() { Name = "Project1" },
                new() { Name = "Project2" }
            }
        };

        Assert.Equal("TestSolution", solutionInfo.Name);
        Assert.Equal("TestSolution.sln", solutionInfo.FilePath);
        Assert.True(solutionInfo.HasCompilationErrors);
        Assert.Equal(5, solutionInfo.TotalErrors);
        Assert.Equal(3, solutionInfo.TotalWarnings);
        Assert.NotNull(solutionInfo.Projects);
        Assert.Equal(2, solutionInfo.Projects.Count);
        Assert.Equal("Project1", solutionInfo.Projects[0].Name);
        Assert.Equal("Project2", solutionInfo.Projects[1].Name);
    }

    [Fact]
    public void ProjectInfo_DefaultConstructor_CreatesValidInstance()
    {
        // Test the REAL production model
        var projectInfo = new MCP.Server.Models.ProjectInfo();

        Assert.NotNull(projectInfo);
        Assert.Equal(string.Empty, projectInfo.Name);
        Assert.Equal(string.Empty, projectInfo.FilePath);
        Assert.Equal(string.Empty, projectInfo.OutputType);
        Assert.False(projectInfo.HasCompilationErrors);
        Assert.Equal(0, projectInfo.ErrorCount);
        Assert.Equal(0, projectInfo.WarningCount);
    }

    [Fact]
    public void ProjectInfo_WithProperties_CanBeSetAndRetrieved()
    {
        // Test the REAL production model with all properties
        var projectInfo = new MCP.Server.Models.ProjectInfo
        {
            Name = "TestProject",
            FilePath = "TestProject.csproj",
            OutputType = "ConsoleApplication",
            HasCompilationErrors = true,
            ErrorCount = 3,
            WarningCount = 2
        };

        Assert.Equal("TestProject", projectInfo.Name);
        Assert.Equal("TestProject.csproj", projectInfo.FilePath);
        Assert.Equal("ConsoleApplication", projectInfo.OutputType);
        Assert.True(projectInfo.HasCompilationErrors);
        Assert.Equal(3, projectInfo.ErrorCount);
        Assert.Equal(2, projectInfo.WarningCount);
    }

    [Fact]
    public void CodeSuggestion_DefaultConstructor_CreatesValidInstance()
    {
        // Test the REAL production model
        var suggestion = new CodeSuggestion();

        Assert.NotNull(suggestion);
        Assert.Equal(string.Empty, suggestion.Id);
        Assert.Equal(string.Empty, suggestion.Title);
        Assert.Equal(string.Empty, suggestion.Description);
        Assert.Equal(SuggestionCategory.BestPractices, suggestion.Category);
        Assert.Equal(SuggestionPriority.Low, suggestion.Priority);
        Assert.Equal(SuggestionImpact.Small, suggestion.Impact);
        Assert.Equal(string.Empty, suggestion.FilePath);
        Assert.Equal(0, suggestion.StartLine);
        Assert.Equal(0, suggestion.EndLine);
        Assert.Equal(0, suggestion.StartColumn);
        Assert.Equal(0, suggestion.EndColumn);
        Assert.Equal(string.Empty, suggestion.OriginalCode);
        Assert.Equal(string.Empty, suggestion.SuggestedCode);
        Assert.False(suggestion.CanAutoFix);
        Assert.Equal(string.Empty, suggestion.HelpLink);
        Assert.Equal(string.Empty, suggestion.ProjectName);
        Assert.NotNull(suggestion.Tags);
        Assert.Empty(suggestion.Tags);
    }

    [Fact]
    public void CodeSuggestion_WithAllProperties_CanBeSetAndRetrieved()
    {
        // Test the REAL production model with all properties
        var suggestion = new CodeSuggestion
        {
            Id = "IDE0090",
            Title = "Use 'new(...)'",
            Description = "Use target-typed 'new' expression",
            Category = SuggestionCategory.Modernization,
            Priority = SuggestionPriority.Medium,
            Impact = SuggestionImpact.Small,
            FilePath = "Program.cs",
            StartLine = 10,
            EndLine = 10,
            StartColumn = 5,
            EndColumn = 25,
            OriginalCode = "new List<string>()",
            SuggestedCode = "new()",
            CanAutoFix = true,
            HelpLink = "https://docs.microsoft.com/dotnet/csharp/language-reference/operators/new-operator",
            ProjectName = "TestProject"
        };

        suggestion.Tags.Add("Style");
        suggestion.Tags.Add("Modernization");

        Assert.Equal("IDE0090", suggestion.Id);
        Assert.Equal("Use 'new(...)'", suggestion.Title);
        Assert.Equal("Use target-typed 'new' expression", suggestion.Description);
        Assert.Equal(SuggestionCategory.Modernization, suggestion.Category);
        Assert.Equal(SuggestionPriority.Medium, suggestion.Priority);
        Assert.Equal(SuggestionImpact.Small, suggestion.Impact);
        Assert.Equal("Program.cs", suggestion.FilePath);
        Assert.Equal(10, suggestion.StartLine);
        Assert.Equal(10, suggestion.EndLine);
        Assert.Equal(5, suggestion.StartColumn);
        Assert.Equal(25, suggestion.EndColumn);
        Assert.Equal("new List<string>()", suggestion.OriginalCode);
        Assert.Equal("new()", suggestion.SuggestedCode);
        Assert.True(suggestion.CanAutoFix);
        Assert.Equal("https://docs.microsoft.com/dotnet/csharp/language-reference/operators/new-operator", suggestion.HelpLink);
        Assert.Equal("TestProject", suggestion.ProjectName);
        Assert.NotNull(suggestion.Tags);
        Assert.Equal(2, suggestion.Tags.Count);
        Assert.Contains("Style", suggestion.Tags);
        Assert.Contains("Modernization", suggestion.Tags);
    }

    [Fact]
    public void SuggestionAnalysisOptions_DefaultConstructor_CreatesValidInstance()
    {
        // Test the REAL production model
        var options = new SuggestionAnalysisOptions();

        Assert.NotNull(options);
        Assert.NotNull(options.IncludedCategories);
        Assert.True(options.IncludedCategories.Count > 0); // Should have default categories
        Assert.Equal(SuggestionPriority.Low, options.MinimumPriority);
        Assert.Equal(100, options.MaxSuggestions);
        Assert.True(options.IncludeAutoFixable);
        Assert.True(options.IncludeManualFix);
        Assert.NotNull(options.IncludedAnalyzerIds);
        Assert.Empty(options.IncludedAnalyzerIds);
        Assert.NotNull(options.ExcludedAnalyzerIds);
        Assert.Empty(options.ExcludedAnalyzerIds);
    }

    [Fact]
    public void SuggestionAnalysisOptions_WithCustomSettings_CanBeSetAndRetrieved()
    {
        // Test the REAL production model with custom settings
        var options = new SuggestionAnalysisOptions
        {
            MinimumPriority = SuggestionPriority.High,
            MaxSuggestions = 50,
            IncludeAutoFixable = false,
            IncludeManualFix = true
        };

        options.IncludedCategories.Clear();
        options.IncludedCategories.Add(SuggestionCategory.Performance);
        options.IncludedCategories.Add(SuggestionCategory.Security);

        options.IncludedAnalyzerIds.Add("CA1822");
        options.IncludedAnalyzerIds.Add("IDE0090");

        options.ExcludedAnalyzerIds.Add("IDE0001");
        options.ExcludedAnalyzerIds.Add("CS1591");

        Assert.Equal(SuggestionPriority.High, options.MinimumPriority);
        Assert.Equal(50, options.MaxSuggestions);
        Assert.False(options.IncludeAutoFixable);
        Assert.True(options.IncludeManualFix);
        
        Assert.NotNull(options.IncludedCategories);
        Assert.Equal(2, options.IncludedCategories.Count);
        Assert.Contains(SuggestionCategory.Performance, options.IncludedCategories);
        Assert.Contains(SuggestionCategory.Security, options.IncludedCategories);

        Assert.NotNull(options.IncludedAnalyzerIds);
        Assert.Equal(2, options.IncludedAnalyzerIds.Count);
        Assert.Contains("CA1822", options.IncludedAnalyzerIds);
        Assert.Contains("IDE0090", options.IncludedAnalyzerIds);

        Assert.NotNull(options.ExcludedAnalyzerIds);
        Assert.Equal(2, options.ExcludedAnalyzerIds.Count);
        Assert.Contains("IDE0001", options.ExcludedAnalyzerIds);
        Assert.Contains("CS1591", options.ExcludedAnalyzerIds);
    }

    [Fact]
    public void SuggestionCategory_AllEnumValues_AreValid()
    {
        // Test that all enum values in the REAL production model are valid
        var categories = Enum.GetValues<SuggestionCategory>();
        
        Assert.True(categories.Length > 0);
        Assert.Contains(SuggestionCategory.Style, categories);
        Assert.Contains(SuggestionCategory.Performance, categories);
        Assert.Contains(SuggestionCategory.Modernization, categories);
        Assert.Contains(SuggestionCategory.BestPractices, categories);
        Assert.Contains(SuggestionCategory.Security, categories);
        Assert.Contains(SuggestionCategory.Reliability, categories);
        Assert.Contains(SuggestionCategory.Accessibility, categories);
        Assert.Contains(SuggestionCategory.Design, categories);
        Assert.Contains(SuggestionCategory.Naming, categories);
        Assert.Contains(SuggestionCategory.Documentation, categories);
        Assert.Contains(SuggestionCategory.Cleanup, categories);
    }

    [Fact]
    public void SuggestionPriority_AllEnumValues_AreValid()
    {
        // Test that all enum values in the REAL production model are valid
        var priorities = Enum.GetValues<SuggestionPriority>();
        
        Assert.True(priorities.Length > 0);
        Assert.Contains(SuggestionPriority.Low, priorities);
        Assert.Contains(SuggestionPriority.Medium, priorities);
        Assert.Contains(SuggestionPriority.High, priorities);
        Assert.Contains(SuggestionPriority.Critical, priorities);
    }

    [Fact]
    public void SuggestionImpact_AllEnumValues_AreValid()
    {
        // Test that all enum values in the REAL production model are valid
        var impacts = Enum.GetValues<SuggestionImpact>();
        
        Assert.True(impacts.Length > 0);
        Assert.Contains(SuggestionImpact.Minimal, impacts);
        Assert.Contains(SuggestionImpact.Small, impacts);
        Assert.Contains(SuggestionImpact.Moderate, impacts);
        Assert.Contains(SuggestionImpact.Significant, impacts);
        Assert.Contains(SuggestionImpact.Major, impacts);
    }

    [Fact]
    public void Models_CanBeSerializedToJson()
    {
        // Test that the REAL production models can be serialized to JSON
        var error = new CompilationError
        {
            Id = "CS0103",
            Message = "Test error",
            Severity = DiagnosticSeverity.Error,
            FilePath = "test.cs"
        };

        var suggestion = new CodeSuggestion
        {
            Id = "IDE0090",
            Title = "Use new(...)",
            Category = SuggestionCategory.Modernization,
            Priority = SuggestionPriority.Medium
        };

        var errorJson = JsonSerializer.Serialize(error);
        var suggestionJson = JsonSerializer.Serialize(suggestion);

        Assert.NotNull(errorJson);
        Assert.NotEmpty(errorJson);
        Assert.Contains("CS0103", errorJson);

        Assert.NotNull(suggestionJson);
        Assert.NotEmpty(suggestionJson);
        Assert.Contains("IDE0090", suggestionJson);
    }
}
