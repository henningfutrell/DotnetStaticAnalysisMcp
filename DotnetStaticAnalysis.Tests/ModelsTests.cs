using DotnetStaticAnalysisMcp.Server.Models;
using System.Text.Json;
using Xunit;

namespace MCP.Tests;

/// <summary>
/// Tests for model classes to improve coverage
/// </summary>
public class ModelsTests
{
    [Fact]
    public void CoverageAnalysisOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new CoverageAnalysisOptions();

        // Assert
        Assert.Equal(10, options.TimeoutMinutes);
        Assert.True(options.CollectBranchCoverage);
        Assert.NotNull(options.IncludedProjects);
        Assert.NotNull(options.ExcludedProjects);
        Assert.NotNull(options.IncludedTestProjects);
        Assert.Empty(options.IncludedProjects);
        Assert.Empty(options.ExcludedProjects);
        Assert.Empty(options.IncludedTestProjects);
    }

    [Fact]
    public void CoverageAnalysisOptions_SetProperties_WorksCorrectly()
    {
        // Arrange
        var options = new CoverageAnalysisOptions();

        // Act
        options.TimeoutMinutes = 5;
        options.CollectBranchCoverage = false;
        options.TestFilter = "Category=Unit";
        options.IncludedProjects.Add("Project1");
        options.ExcludedProjects.Add("Project2");
        options.IncludedTestProjects.Add("TestProject1");

        // Assert
        Assert.Equal(5, options.TimeoutMinutes);
        Assert.False(options.CollectBranchCoverage);
        Assert.Equal("Category=Unit", options.TestFilter);
        Assert.Contains("Project1", options.IncludedProjects);
        Assert.Contains("Project2", options.ExcludedProjects);
        Assert.Contains("TestProject1", options.IncludedTestProjects);
    }

    [Fact]
    public void CoverageAnalysisResult_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var result = new CoverageAnalysisResult();

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.Projects);
        Assert.Empty(result.Projects);
        Assert.NotNull(result.Summary);
        Assert.Equal(0, result.Summary.TotalLines);
        Assert.Equal(0, result.Summary.CoveredLines);
        Assert.Equal(0.0, result.Summary.LinesCoveredPercentage);
    }

    [Fact]
    public void CoverageAnalysisResult_SetProperties_WorksCorrectly()
    {
        // Arrange
        var result = new CoverageAnalysisResult();
        var project = new ProjectCoverage { ProjectName = "TestProject" };

        // Act
        result.Success = true;
        result.ErrorMessage = "Test error";
        result.Projects.Add(project);
        result.Summary.TotalLines = 100;
        result.Summary.CoveredLines = 75;
        result.Summary.LinesCoveredPercentage = 75.0;

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Test error", result.ErrorMessage);
        Assert.Single(result.Projects);
        Assert.Equal("TestProject", result.Projects[0].ProjectName);
        Assert.Equal(100, result.Summary.TotalLines);
        Assert.Equal(75, result.Summary.CoveredLines);
        Assert.Equal(75.0, result.Summary.LinesCoveredPercentage);
    }

    [Fact]
    public void ProjectCoverage_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var project = new ProjectCoverage();

        // Assert
        Assert.Equal(string.Empty, project.ProjectName);
        Assert.Equal(string.Empty, project.ProjectPath);
        Assert.NotNull(project.Files);
        Assert.NotNull(project.Classes);
        Assert.NotNull(project.Summary);
        Assert.Empty(project.Files);
        Assert.Empty(project.Classes);
    }

    [Fact]
    public void ProjectCoverage_SetProperties_WorksCorrectly()
    {
        // Arrange
        var project = new ProjectCoverage();
        var file = new FileCoverage { FilePath = "test.cs" };
        var classItem = new ClassCoverage { ClassName = "TestClass" };

        // Act
        project.ProjectName = "TestProject";
        project.ProjectPath = "/path/to/project";
        project.Files.Add(file);
        project.Classes.Add(classItem);

        // Assert
        Assert.Equal("TestProject", project.ProjectName);
        Assert.Equal("/path/to/project", project.ProjectPath);
        Assert.Single(project.Files);
        Assert.Single(project.Classes);
        Assert.Equal("test.cs", project.Files[0].FilePath);
        Assert.Equal("TestClass", project.Classes[0].ClassName);
    }

    [Fact]
    public void FileCoverage_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var file = new FileCoverage();

        // Assert
        Assert.Equal(string.Empty, file.FilePath);
        Assert.NotNull(file.Lines);
        Assert.NotNull(file.Methods);
        Assert.NotNull(file.Summary);
        Assert.Empty(file.Lines);
        Assert.Empty(file.Methods);
    }

    [Fact]
    public void ClassCoverage_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var classItem = new ClassCoverage();

        // Assert
        Assert.Equal(string.Empty, classItem.ClassName);
        Assert.Equal(string.Empty, classItem.FilePath);
        Assert.NotNull(classItem.Methods);
        Assert.NotNull(classItem.Summary);
        Assert.Empty(classItem.Methods);
    }

    [Fact]
    public void MethodCoverage_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var method = new MethodCoverage();

        // Assert
        Assert.Equal(string.Empty, method.MethodName);
        Assert.Equal(string.Empty, method.Signature);
        Assert.NotNull(method.Lines);
        Assert.NotNull(method.Summary);
        Assert.Empty(method.Lines);
    }

    [Fact]
    public void LineCoverage_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var line = new LineCoverage();

        // Assert
        Assert.Equal(0, line.LineNumber);
        Assert.Equal(0, line.HitCount);
        Assert.Equal(CoverageStatus.NotCoverable, line.Status);
    }

    [Fact]
    public void LineCoverage_IsCovered_Property_WorksCorrectly()
    {
        // Arrange
        var coveredLine = new LineCoverage { Status = CoverageStatus.Covered, HitCount = 5 };
        var uncoveredLine = new LineCoverage { Status = CoverageStatus.Uncovered, HitCount = 0 };
        var partialLine = new LineCoverage { Status = CoverageStatus.PartiallyCovered, HitCount = 2 };

        // Act & Assert
        Assert.True(coveredLine.IsCovered);
        Assert.False(uncoveredLine.IsCovered);
        Assert.True(partialLine.IsCovered);
    }

    [Fact]
    public void CoverageSummary_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var summary = new CoverageSummary();

        // Assert
        Assert.Equal(0, summary.TotalLines);
        Assert.Equal(0, summary.CoveredLines);
        Assert.Equal(0, summary.UncoveredLines);
        Assert.Equal(0.0, summary.LinesCoveredPercentage);
        Assert.Equal(0, summary.TotalMethods);
        Assert.Equal(0, summary.CoveredMethods);
        Assert.Equal(0, summary.UncoveredMethods);
        Assert.Equal(0.0, summary.MethodsCoveredPercentage);
        Assert.Equal(0, summary.TotalClasses);
        Assert.Equal(0, summary.CoveredClasses);
        Assert.Equal(0, summary.UncoveredClasses);
        Assert.Equal(0.0, summary.ClassesCoveredPercentage);
        Assert.Equal(0.0, summary.BranchesCoveredPercentage);
    }

    [Fact]
    public void TestExecutionSummary_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var summary = new TestExecutionSummary();

        // Assert
        Assert.Equal(0, summary.TotalTests);
        Assert.Equal(0, summary.PassedTests);
        Assert.Equal(0, summary.FailedTests);
        Assert.Equal(0, summary.SkippedTests);
        Assert.Equal(TimeSpan.Zero, summary.ExecutionTime);
    }

    [Fact]
    public void CoverageAnalysisOptions_Serialization_WorksCorrectly()
    {
        // Arrange
        var options = new CoverageAnalysisOptions
        {
            TimeoutMinutes = 5,
            CollectBranchCoverage = false,
            TestFilter = "Category=Unit"
        };
        options.IncludedProjects.Add("Project1");

        // Act
        var json = JsonSerializer.Serialize(options);
        var deserialized = JsonSerializer.Deserialize<CoverageAnalysisOptions>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(5, deserialized.TimeoutMinutes);
        Assert.False(deserialized.CollectBranchCoverage);
        Assert.Equal("Category=Unit", deserialized.TestFilter);
        Assert.Contains("Project1", deserialized.IncludedProjects);
    }
}
