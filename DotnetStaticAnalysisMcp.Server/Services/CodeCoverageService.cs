using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using DotnetStaticAnalysisMcp.Server.Models;
using DotnetStaticAnalysisMcp.Server.Services;

namespace DotnetStaticAnalysisMcp.Server.Services;

/// <summary>
/// Service for analyzing code coverage using Coverlet and .NET test tools
/// </summary>
public class CodeCoverageService
{
    private readonly ILogger<CodeCoverageService> _logger;
    private readonly RoslynAnalysisService _analysisService;
    private readonly TelemetryService _telemetryService;
    private string? _currentSolutionPath;

    public CodeCoverageService(
        ILogger<CodeCoverageService> logger,
        RoslynAnalysisService analysisService,
        TelemetryService telemetryService)
    {
        _logger = logger;
        _analysisService = analysisService;
        _telemetryService = telemetryService;

        _logger.LogInformation("CodeCoverageService initialized");
        _telemetryService.LogTelemetry("CodeCoverageService.Initialized", new Dictionary<string, object>
        {
            ["timestamp"] = DateTime.UtcNow,
            ["service_version"] = "1.1.0"
        });
    }

    /// <summary>
    /// Sets the current solution path for coverage analysis
    /// </summary>
    public void SetSolutionPath(string solutionPath)
    {
        _logger.LogInformation("Setting solution path for coverage analysis: {SolutionPath}", solutionPath);

        if (string.IsNullOrWhiteSpace(solutionPath))
        {
            _logger.LogWarning("Solution path is null or empty");
            _telemetryService.LogTelemetry("CodeCoverageService.SetSolutionPath.InvalidPath", new Dictionary<string, object>
            {
                ["path"] = solutionPath ?? "null",
                ["timestamp"] = DateTime.UtcNow
            });
            return;
        }

        if (!File.Exists(solutionPath))
        {
            _logger.LogWarning("Solution file does not exist: {SolutionPath}", solutionPath);
            _telemetryService.LogTelemetry("CodeCoverageService.SetSolutionPath.FileNotFound", new Dictionary<string, object>
            {
                ["path"] = solutionPath,
                ["timestamp"] = DateTime.UtcNow
            });
            return;
        }

        _currentSolutionPath = solutionPath;
        _logger.LogInformation("Solution path set successfully: {SolutionPath}", solutionPath);
        _telemetryService.LogTelemetry("CodeCoverageService.SetSolutionPath.Success", new Dictionary<string, object>
        {
            ["path"] = solutionPath,
            ["directory"] = Path.GetDirectoryName(solutionPath) ?? "unknown",
            ["timestamp"] = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Runs coverage analysis for the current solution
    /// </summary>
    public async Task<CoverageAnalysisResult> RunCoverageAnalysisAsync(CoverageAnalysisOptions? options = null)
    {
        var result = new CoverageAnalysisResult
        {
            AnalysisTime = DateTime.UtcNow
        };

        var stopwatch = Stopwatch.StartNew();
        var operationId = Guid.NewGuid().ToString();

        try
        {
            _logger.LogInformation("Starting coverage analysis operation {OperationId}", operationId);
            _telemetryService.LogTelemetry("CodeCoverageService.RunCoverageAnalysis.Started", new Dictionary<string, object>
            {
                ["operation_id"] = operationId,
                ["timestamp"] = DateTime.UtcNow,
                ["solution_path"] = _currentSolutionPath ?? "null"
            });

            if (string.IsNullOrEmpty(_currentSolutionPath))
            {
                var errorMsg = "No solution loaded. Please load a solution first.";
                _logger.LogError("Coverage analysis failed: {Error}", errorMsg);
                _telemetryService.LogTelemetry("CodeCoverageService.RunCoverageAnalysis.NoSolution", new Dictionary<string, object>
                {
                    ["operation_id"] = operationId,
                    ["error"] = errorMsg,
                    ["timestamp"] = DateTime.UtcNow
                });
                result.ErrorMessage = errorMsg;
                return result;
            }

            options ??= new CoverageAnalysisOptions();

            _logger.LogInformation("Starting coverage analysis for solution: {SolutionPath} with options: {@Options}",
                _currentSolutionPath, options);

            _telemetryService.LogTelemetry("CodeCoverageService.RunCoverageAnalysis.OptionsSet", new Dictionary<string, object>
            {
                ["operation_id"] = operationId,
                ["solution_path"] = _currentSolutionPath,
                ["timeout_minutes"] = options.TimeoutMinutes,
                ["collect_branch_coverage"] = options.CollectBranchCoverage,
                ["included_projects_count"] = options.IncludedProjects.Count,
                ["excluded_projects_count"] = options.ExcludedProjects.Count,
                ["included_test_projects_count"] = options.IncludedTestProjects.Count,
                ["timestamp"] = DateTime.UtcNow
            });

            // Get test projects from the solution
            _logger.LogInformation("Discovering test projects for operation {OperationId}", operationId);
            var testProjects = await GetTestProjectsAsync(options, operationId);

            _logger.LogInformation("Found {Count} test projects for operation {OperationId}: {Projects}",
                testProjects.Count, operationId, string.Join(", ", testProjects.Select(Path.GetFileName)));

            _telemetryService.LogTelemetry("CodeCoverageService.RunCoverageAnalysis.TestProjectsDiscovered", new Dictionary<string, object>
            {
                ["operation_id"] = operationId,
                ["test_projects_count"] = testProjects.Count,
                ["test_projects"] = testProjects.Select(Path.GetFileName).ToArray(),
                ["timestamp"] = DateTime.UtcNow
            });

            if (!testProjects.Any())
            {
                var errorMsg = "No test projects found in the solution.";
                _logger.LogWarning("Coverage analysis failed for operation {OperationId}: {Error}", operationId, errorMsg);
                _telemetryService.LogTelemetry("CodeCoverageService.RunCoverageAnalysis.NoTestProjects", new Dictionary<string, object>
                {
                    ["operation_id"] = operationId,
                    ["error"] = errorMsg,
                    ["solution_path"] = _currentSolutionPath,
                    ["timestamp"] = DateTime.UtcNow
                });
                result.ErrorMessage = errorMsg;
                return result;
            }

            // Run tests with coverage collection
            var coverageResults = new List<string>();
            var testResults = new TestExecutionSummary();

            foreach (var testProject in testProjects)
            {
                _logger.LogInformation("Running coverage analysis for test project: {ProjectPath}", testProject);

                var projectResult = await RunCoverageForProjectAsync(testProject, options);
                if (projectResult.Success)
                {
                    coverageResults.AddRange(projectResult.CoverageFiles);
                    
                    // Aggregate test results
                    testResults.TotalTests += projectResult.TestSummary.TotalTests;
                    testResults.PassedTests += projectResult.TestSummary.PassedTests;
                    testResults.FailedTests += projectResult.TestSummary.FailedTests;
                    testResults.SkippedTests += projectResult.TestSummary.SkippedTests;
                    testResults.ExecutionTime = testResults.ExecutionTime.Add(projectResult.TestSummary.ExecutionTime);
                    testResults.Failures.AddRange(projectResult.TestSummary.Failures);
                }
                else
                {
                    _logger.LogWarning("Coverage analysis failed for project {ProjectPath}: {Error}", 
                        testProject, projectResult.ErrorMessage);
                }
            }

            // Parse coverage results
            if (coverageResults.Any())
            {
                await ParseCoverageResultsAsync(result, coverageResults, options);
                result.TestResults = testResults;
                result.Success = true;
            }
            else
            {
                result.ErrorMessage = "No coverage data was generated.";
            }

            stopwatch.Stop();
            result.ExecutionDuration = stopwatch.Elapsed;

            _logger.LogInformation("Coverage analysis completed in {Duration}ms. Success: {Success}", 
                stopwatch.ElapsedMilliseconds, result.Success);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.ExecutionDuration = stopwatch.Elapsed;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error during coverage analysis");
            return result;
        }
    }

    /// <summary>
    /// Gets coverage summary for the current solution
    /// </summary>
    public async Task<CoverageSummary> GetCoverageSummaryAsync(CoverageAnalysisOptions? options = null)
    {
        var analysisResult = await RunCoverageAnalysisAsync(options);
        return analysisResult.Success ? analysisResult.Summary : new CoverageSummary();
    }

    /// <summary>
    /// Finds uncovered code in the solution
    /// </summary>
    public async Task<UncoveredCodeResult> FindUncoveredCodeAsync(CoverageAnalysisOptions? options = null)
    {
        var result = new UncoveredCodeResult();

        try
        {
            var analysisResult = await RunCoverageAnalysisAsync(options);
            if (!analysisResult.Success)
            {
                result.ErrorMessage = analysisResult.ErrorMessage;
                return result;
            }

            // Extract uncovered items from coverage results
            foreach (var project in analysisResult.Projects)
            {
                foreach (var file in project.Files)
                {
                    // Find uncovered lines
                    var uncoveredLines = file.Lines.Where(l => !l.IsCovered).ToList();
                    foreach (var line in uncoveredLines)
                    {
                        result.UncoveredLines.Add(new UncoveredLine
                        {
                            FilePath = file.FilePath,
                            LineNumber = line.LineNumber,
                            SourceCode = line.SourceCode
                        });
                    }

                    // Find uncovered methods
                    var uncoveredMethods = file.Methods.Where(m => m.IsUncovered).ToList();
                    foreach (var method in uncoveredMethods)
                    {
                        result.UncoveredMethods.Add(new UncoveredMethod
                        {
                            MethodName = method.MethodName,
                            ClassName = method.ClassName,
                            FilePath = file.FilePath,
                            StartLine = method.StartLine,
                            EndLine = method.EndLine,
                            Signature = method.Signature,
                            LineCount = method.EndLine - method.StartLine + 1
                        });
                    }
                }
            }

            result.Success = true;
            return result;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error finding uncovered code");
            return result;
        }
    }

    /// <summary>
    /// Gets coverage details for a specific method
    /// </summary>
    public async Task<MethodCoverage?> GetMethodCoverageAsync(string className, string methodName, CoverageAnalysisOptions? options = null)
    {
        try
        {
            var analysisResult = await RunCoverageAnalysisAsync(options);
            if (!analysisResult.Success)
            {
                return null;
            }

            // Search for the method across all projects and files
            foreach (var project in analysisResult.Projects)
            {
                foreach (var file in project.Files)
                {
                    var method = file.Methods.FirstOrDefault(m => 
                        m.ClassName.Equals(className, StringComparison.OrdinalIgnoreCase) &&
                        m.MethodName.Equals(methodName, StringComparison.OrdinalIgnoreCase));
                    
                    if (method != null)
                    {
                        return method;
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting method coverage for {ClassName}.{MethodName}", className, methodName);
            return null;
        }
    }

    /// <summary>
    /// Compares coverage between two analysis runs
    /// </summary>
    public async Task<CoverageComparisonResult> CompareCoverageAsync(
        CoverageAnalysisResult baseline, 
        CoverageAnalysisOptions? options = null)
    {
        var result = new CoverageComparisonResult();

        try
        {
            var currentAnalysis = await RunCoverageAnalysisAsync(options);
            if (!currentAnalysis.Success)
            {
                result.ErrorMessage = currentAnalysis.ErrorMessage;
                return result;
            }

            result.BaselineCoverage = baseline.Summary;
            result.CurrentCoverage = currentAnalysis.Summary;

            // Calculate deltas
            result.Delta = new CoverageDelta
            {
                LinesCoverageChange = currentAnalysis.Summary.LinesCoveredPercentage - baseline.Summary.LinesCoveredPercentage,
                BranchesCoverageChange = currentAnalysis.Summary.BranchesCoveredPercentage - baseline.Summary.BranchesCoveredPercentage,
                MethodsCoverageChange = currentAnalysis.Summary.MethodsCoveredPercentage - baseline.Summary.MethodsCoveredPercentage,
                ClassesCoverageChange = currentAnalysis.Summary.ClassesCoveredPercentage - baseline.Summary.ClassesCoveredPercentage,
                
                LinesChange = currentAnalysis.Summary.CoveredLines - baseline.Summary.CoveredLines,
                BranchesChange = currentAnalysis.Summary.CoveredBranches - baseline.Summary.CoveredBranches,
                MethodsChange = currentAnalysis.Summary.CoveredMethods - baseline.Summary.CoveredMethods,
                ClassesChange = currentAnalysis.Summary.CoveredClasses - baseline.Summary.CoveredClasses
            };

            // Find improved and regressed files
            CompareFilesCoverage(baseline, currentAnalysis, result);

            result.Success = true;
            return result;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error comparing coverage");
            return result;
        }
    }

    #region Private Helper Methods

    private async Task<List<string>> GetTestProjectsAsync(CoverageAnalysisOptions options, string operationId)
    {
        var testProjects = new List<string>();

        try
        {
            _logger.LogInformation("Starting test project discovery for operation {OperationId}", operationId);

            if (string.IsNullOrEmpty(_currentSolutionPath))
            {
                _logger.LogWarning("Current solution path is null or empty for operation {OperationId}", operationId);
                return testProjects;
            }

            var solutionDir = Path.GetDirectoryName(_currentSolutionPath);
            if (solutionDir == null)
            {
                _logger.LogWarning("Could not determine solution directory for operation {OperationId}", operationId);
                return testProjects;
            }

            _logger.LogInformation("Searching for project files in directory: {SolutionDir} for operation {OperationId}",
                solutionDir, operationId);

            // Find all .csproj files in the solution directory
            var projectFiles = Directory.GetFiles(solutionDir, "*.csproj", SearchOption.AllDirectories);
            _logger.LogInformation("Found {Count} project files in solution directory for operation {OperationId}: {Files}",
                projectFiles.Length, operationId, string.Join(", ", projectFiles.Select(Path.GetFileName)));

            _telemetryService.LogTelemetry("CodeCoverageService.GetTestProjects.ProjectFilesFound", new Dictionary<string, object>
            {
                ["operation_id"] = operationId,
                ["solution_directory"] = solutionDir,
                ["project_files_count"] = projectFiles.Length,
                ["project_files"] = projectFiles.Select(Path.GetFileName).ToArray(),
                ["timestamp"] = DateTime.UtcNow
            });

            foreach (var projectFile in projectFiles)
            {
                var projectName = Path.GetFileNameWithoutExtension(projectFile);
                _logger.LogInformation("Evaluating project: {ProjectName} at {ProjectFile} for operation {OperationId}",
                    projectName, projectFile, operationId);

                // Skip excluded projects
                if (options.ExcludedProjects.Contains(projectName))
                {
                    _logger.LogInformation("Skipping excluded project: {ProjectName} for operation {OperationId}",
                        projectName, operationId);
                    continue;
                }

                // Include specific test projects if specified
                if (options.IncludedTestProjects.Any() && !options.IncludedTestProjects.Contains(projectName))
                {
                    _logger.LogInformation("Skipping project not in included list: {ProjectName} for operation {OperationId}",
                        projectName, operationId);
                    continue;
                }

                // Check if it's a test project (contains test-related packages or naming patterns)
                _logger.LogInformation("Checking if {ProjectName} is a test project for operation {OperationId}",
                    projectName, operationId);
                var isTestProject = await IsTestProjectAsync(projectFile, operationId);
                _logger.LogInformation("Project {ProjectName} is test project: {IsTestProject} for operation {OperationId}",
                    projectName, isTestProject, operationId);

                if (isTestProject)
                {
                    testProjects.Add(projectFile);
                    _logger.LogInformation("✅ Added test project: {ProjectFile} for operation {OperationId}",
                        projectFile, operationId);
                    _telemetryService.LogTelemetry("CodeCoverageService.GetTestProjects.TestProjectAdded", new Dictionary<string, object>
                    {
                        ["operation_id"] = operationId,
                        ["project_name"] = projectName,
                        ["project_file"] = projectFile,
                        ["timestamp"] = DateTime.UtcNow
                    });
                }
                else
                {
                    _logger.LogInformation("❌ Project {ProjectName} is not a test project for operation {OperationId}",
                        projectName, operationId);
                }
            }

            return testProjects;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting test projects");
            return testProjects;
        }
    }

    private async Task<bool> IsTestProjectAsync(string projectPath, string operationId)
    {
        try
        {
            _logger.LogInformation("Analyzing project file for test indicators: {ProjectPath} for operation {OperationId}",
                projectPath, operationId);

            if (!File.Exists(projectPath))
            {
                _logger.LogWarning("Project file does not exist: {ProjectPath} for operation {OperationId}",
                    projectPath, operationId);
                return false;
            }

            var content = await File.ReadAllTextAsync(projectPath);
            _logger.LogDebug("Project file content length: {Length} characters for {ProjectPath} operation {OperationId}",
                content.Length, projectPath, operationId);

            // Check for test framework packages (not just coverage packages)
            var testFrameworkPackages = new[]
            {
                "Microsoft.NET.Test.Sdk",
                "xunit",
                "NUnit",
                "MSTest"
            };

            var foundTestFrameworkPackages = new List<string>();
            foreach (var package in testFrameworkPackages)
            {
                if (content.Contains(package, StringComparison.OrdinalIgnoreCase))
                {
                    foundTestFrameworkPackages.Add(package);
                    _logger.LogInformation("Found test framework package '{Package}' in {ProjectPath} for operation {OperationId}",
                        package, projectPath, operationId);
                }
            }

            // Check for coverage packages separately (these don't make a project a test project)
            var coveragePackages = new[] { "coverlet" };
            var foundCoveragePackages = new List<string>();
            foreach (var package in coveragePackages)
            {
                if (content.Contains(package, StringComparison.OrdinalIgnoreCase))
                {
                    foundCoveragePackages.Add(package);
                    _logger.LogInformation("Found coverage package '{Package}' in {ProjectPath} for operation {OperationId}",
                        package, projectPath, operationId);
                }
            }

            // Check for IsTestProject property
            var hasIsTestProject = content.Contains("<IsTestProject>true</IsTestProject>", StringComparison.OrdinalIgnoreCase);
            if (hasIsTestProject)
            {
                _logger.LogInformation("Found IsTestProject=true in {ProjectPath} for operation {OperationId}",
                    projectPath, operationId);
            }

            // Check naming convention
            var projectName = Path.GetFileNameWithoutExtension(projectPath);
            var hasTestInName = projectName.Contains("Test", StringComparison.OrdinalIgnoreCase);
            if (hasTestInName)
            {
                _logger.LogInformation("Project name contains 'Test': {ProjectName} for operation {OperationId}",
                    projectName, operationId);
            }

            // A project is a test project if it has test framework packages, IsTestProject=true, or Test in the name
            // Coverage packages alone don't make it a test project
            var isTestProject = foundTestFrameworkPackages.Any() || hasIsTestProject || hasTestInName;

            _logger.LogInformation("Test project analysis result for {ProjectPath} operation {OperationId}: {IsTestProject} " +
                "(TestFrameworkPackages: {TestFrameworkPackages}, CoveragePackages: {CoveragePackages}, IsTestProject: {HasIsTestProject}, NameContainsTest: {HasTestInName})",
                projectPath, operationId, isTestProject, string.Join(", ", foundTestFrameworkPackages), string.Join(", ", foundCoveragePackages), hasIsTestProject, hasTestInName);

            _telemetryService.LogTelemetry("CodeCoverageService.IsTestProject.Analysis", new Dictionary<string, object>
            {
                ["operation_id"] = operationId,
                ["project_path"] = projectPath,
                ["project_name"] = projectName,
                ["is_test_project"] = isTestProject,
                ["found_test_framework_packages"] = foundTestFrameworkPackages.ToArray(),
                ["found_coverage_packages"] = foundCoveragePackages.ToArray(),
                ["has_is_test_project"] = hasIsTestProject,
                ["has_test_in_name"] = hasTestInName,
                ["content_length"] = content.Length,
                ["timestamp"] = DateTime.UtcNow
            });

            return isTestProject;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing project file {ProjectPath} for operation {OperationId}: {Error}",
                projectPath, operationId, ex.Message);
            _telemetryService.LogTelemetry("CodeCoverageService.IsTestProject.Error", new Dictionary<string, object>
            {
                ["operation_id"] = operationId,
                ["project_path"] = projectPath,
                ["error"] = ex.Message,
                ["timestamp"] = DateTime.UtcNow
            });
            return false;
        }
    }

    private async Task<ProjectCoverageResult> RunCoverageForProjectAsync(string projectPath, CoverageAnalysisOptions options)
    {
        var result = new ProjectCoverageResult();

        try
        {
            var projectDir = Path.GetDirectoryName(projectPath);
            if (projectDir == null)
            {
                result.ErrorMessage = $"Could not determine directory for project: {projectPath}";
                return result;
            }

            var outputDir = Path.Combine(projectDir, "TestResults");
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, true);
            }
            Directory.CreateDirectory(outputDir);

            // Simplified dotnet test command with coverage
            var arguments = new List<string>
            {
                "test",
                $"\"{projectPath}\"",
                "--collect:\"XPlat Code Coverage\"",
                $"--results-directory \"{outputDir}\"",
                "--verbosity:normal"
            };

            if (!string.IsNullOrEmpty(options.TestFilter))
            {
                arguments.Add($"--filter \"{options.TestFilter}\"");
            }

            // Execute the test command
            var processInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = string.Join(" ", arguments),
                WorkingDirectory = projectDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _logger.LogInformation("Running command: dotnet {Arguments} in directory {WorkingDirectory}",
                string.Join(" ", arguments), projectDir);

            using var process = new Process { StartInfo = processInfo };
            var output = new List<string>();
            var errors = new List<string>();

            process.OutputDataReceived += (_, e) => { if (e.Data != null) output.Add(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data != null) errors.Add(e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var timeout = TimeSpan.FromMinutes(options.TimeoutMinutes);
            var completed = await Task.Run(() => process.WaitForExit((int)timeout.TotalMilliseconds));

            if (!completed)
            {
                process.Kill();
                result.ErrorMessage = $"Test execution timed out after {options.TimeoutMinutes} minutes";
                return result;
            }

            // Log all output for debugging
            _logger.LogInformation("Test output: {Output}", string.Join(Environment.NewLine, output));
            if (errors.Any())
            {
                _logger.LogWarning("Test errors: {Errors}", string.Join(Environment.NewLine, errors));
            }

            if (process.ExitCode != 0)
            {
                result.ErrorMessage = $"Test execution failed with exit code {process.ExitCode}. Output: {string.Join(Environment.NewLine, output)}. Errors: {string.Join(Environment.NewLine, errors)}";
                return result;
            }

            // Parse test results from output
            result.TestSummary = ParseTestResults(output);

            // Find coverage files - look for Cobertura XML files
            var coverageFiles = Directory.GetFiles(outputDir, "coverage.cobertura.xml", SearchOption.AllDirectories);
            if (!coverageFiles.Any())
            {
                // Also try looking for other coverage file patterns
                var allFiles = Directory.GetFiles(outputDir, "*", SearchOption.AllDirectories);
                _logger.LogInformation("Files found in output directory: {Files}", string.Join(", ", allFiles));

                coverageFiles = Directory.GetFiles(outputDir, "*.xml", SearchOption.AllDirectories)
                    .Where(f => f.Contains("coverage", StringComparison.OrdinalIgnoreCase))
                    .ToArray();
            }

            result.CoverageFiles.AddRange(coverageFiles);
            result.Success = true;

            _logger.LogInformation("Found {Count} coverage files: {Files}",
                coverageFiles.Length, string.Join(", ", coverageFiles));

            return result;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error running coverage for project {ProjectPath}", projectPath);
            return result;
        }
    }



    private TestExecutionSummary ParseTestResults(List<string> output)
    {
        var summary = new TestExecutionSummary();

        try
        {
            // Parse dotnet test output for test statistics
            foreach (var line in output)
            {
                if (line.Contains("Total tests:"))
                {
                    var match = Regex.Match(line, @"Total tests:\s*(\d+)");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out var total))
                        summary.TotalTests = total;
                }
                else if (line.Contains("Passed:"))
                {
                    var match = Regex.Match(line, @"Passed:\s*(\d+)");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out var passed))
                        summary.PassedTests = passed;
                }
                else if (line.Contains("Failed:"))
                {
                    var match = Regex.Match(line, @"Failed:\s*(\d+)");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out var failed))
                        summary.FailedTests = failed;
                }
                else if (line.Contains("Skipped:"))
                {
                    var match = Regex.Match(line, @"Skipped:\s*(\d+)");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out var skipped))
                        summary.SkippedTests = skipped;
                }
                else if (line.Contains("Test Run Successful") || line.Contains("Test Run Failed"))
                {
                    var timeMatch = Regex.Match(line, @"(\d+:\d+:\d+\.\d+)");
                    if (timeMatch.Success && TimeSpan.TryParse(timeMatch.Groups[1].Value, out var duration))
                        summary.ExecutionTime = duration;
                }
            }

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing test results");
            return summary;
        }
    }

    private async Task ParseCoverageResultsAsync(CoverageAnalysisResult result, List<string> coverageFiles, CoverageAnalysisOptions options)
    {
        try
        {
            foreach (var coverageFile in coverageFiles)
            {
                await ParseCoberturaFileAsync(result, coverageFile, options);
            }

            // Calculate overall summary
            CalculateOverallSummary(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing coverage results");
            throw;
        }
    }

    private async Task ParseCoberturaFileAsync(CoverageAnalysisResult result, string coverageFile, CoverageAnalysisOptions options)
    {
        try
        {
            _logger.LogInformation("Parsing coverage file: {CoverageFile}", coverageFile);

            var content = await File.ReadAllTextAsync(coverageFile);
            var doc = XDocument.Parse(content);
            var coverage = doc.Root;

            if (coverage == null || coverage.Name != "coverage")
            {
                _logger.LogWarning("Invalid Cobertura XML format in file: {CoverageFile}", coverageFile);
                return;
            }

            // Parse overall coverage metrics
            var lineRate = double.Parse(coverage.Attribute("line-rate")?.Value ?? "0");
            var branchRate = double.Parse(coverage.Attribute("branch-rate")?.Value ?? "0");
            var linesValid = int.Parse(coverage.Attribute("lines-valid")?.Value ?? "0");
            var linesCovered = int.Parse(coverage.Attribute("lines-covered")?.Value ?? "0");
            var branchesValid = int.Parse(coverage.Attribute("branches-valid")?.Value ?? "0");
            var branchesCovered = int.Parse(coverage.Attribute("branches-covered")?.Value ?? "0");

            // Parse packages (projects)
            var packages = coverage.Element("packages")?.Elements("package") ?? Enumerable.Empty<XElement>();

            foreach (var package in packages)
            {
                var projectCoverage = ParsePackage(package, coverageFile);
                if (projectCoverage != null)
                {
                    result.Projects.Add(projectCoverage);
                }
            }

            _logger.LogInformation("Successfully parsed coverage file with {ProjectCount} projects", result.Projects.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Cobertura file {CoverageFile}", coverageFile);
            throw;
        }
    }

    private ProjectCoverage? ParsePackage(XElement package, string coverageFilePath)
    {
        try
        {
            var packageName = package.Attribute("name")?.Value ?? "Unknown";
            var lineRate = double.Parse(package.Attribute("line-rate")?.Value ?? "0");
            var branchRate = double.Parse(package.Attribute("branch-rate")?.Value ?? "0");

            var projectCoverage = new ProjectCoverage
            {
                ProjectName = packageName,
                ProjectPath = coverageFilePath,
                Summary = new CoverageSummary
                {
                    LinesCoveredPercentage = lineRate * 100,
                    BranchesCoveredPercentage = branchRate * 100
                }
            };

            // Parse classes
            var classes = package.Element("classes")?.Elements("class") ?? Enumerable.Empty<XElement>();

            foreach (var classElement in classes)
            {
                var classCoverage = ParseClass(classElement);
                if (classCoverage != null)
                {
                    projectCoverage.Classes.Add(classCoverage);

                    // Also add file coverage
                    var fileCoverage = new FileCoverage
                    {
                        FilePath = classCoverage.FilePath,
                        FileName = Path.GetFileName(classCoverage.FilePath),
                        Summary = classCoverage.Summary,
                        Methods = classCoverage.Methods
                    };

                    // Parse lines for this class
                    var lines = classElement.Element("lines")?.Elements("line") ?? Enumerable.Empty<XElement>();
                    foreach (var line in lines)
                    {
                        var lineCoverage = ParseLine(line);
                        if (lineCoverage != null)
                        {
                            fileCoverage.Lines.Add(lineCoverage);
                        }
                    }

                    projectCoverage.Files.Add(fileCoverage);
                }
            }

            // Calculate summary statistics
            CalculateProjectSummary(projectCoverage);

            return projectCoverage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing package in Cobertura file");
            return null;
        }
    }

    private void CalculateOverallSummary(CoverageAnalysisResult result)
    {
        if (!result.Projects.Any())
            return;

        var summary = result.Summary;

        // Calculate weighted averages based on project sizes
        var totalLines = result.Projects.Sum(p => p.Summary.TotalLines);
        var totalBranches = result.Projects.Sum(p => p.Summary.TotalBranches);
        var totalMethods = result.Projects.Sum(p => p.Summary.TotalMethods);
        var totalClasses = result.Projects.Sum(p => p.Summary.TotalClasses);

        summary.TotalLines = totalLines;
        summary.TotalBranches = totalBranches;
        summary.TotalMethods = totalMethods;
        summary.TotalClasses = totalClasses;

        summary.CoveredLines = result.Projects.Sum(p => p.Summary.CoveredLines);
        summary.CoveredBranches = result.Projects.Sum(p => p.Summary.CoveredBranches);
        summary.CoveredMethods = result.Projects.Sum(p => p.Summary.CoveredMethods);
        summary.CoveredClasses = result.Projects.Sum(p => p.Summary.CoveredClasses);

        summary.UncoveredLines = summary.TotalLines - summary.CoveredLines;
        summary.UncoveredBranches = summary.TotalBranches - summary.CoveredBranches;
        summary.UncoveredMethods = summary.TotalMethods - summary.CoveredMethods;
        summary.UncoveredClasses = summary.TotalClasses - summary.CoveredClasses;

        summary.LinesCoveredPercentage = totalLines > 0 ? (double)summary.CoveredLines / totalLines * 100 : 0;
        summary.BranchesCoveredPercentage = totalBranches > 0 ? (double)summary.CoveredBranches / totalBranches * 100 : 0;
        summary.MethodsCoveredPercentage = totalMethods > 0 ? (double)summary.CoveredMethods / totalMethods * 100 : 0;
        summary.ClassesCoveredPercentage = totalClasses > 0 ? (double)summary.CoveredClasses / totalClasses * 100 : 0;
    }

    private void CompareFilesCoverage(CoverageAnalysisResult baseline, CoverageAnalysisResult current, CoverageComparisonResult result)
    {
        // Compare file-level coverage between baseline and current
        var baselineFiles = baseline.Projects.SelectMany(p => p.Files).ToDictionary(f => f.FilePath, f => f);
        var currentFiles = current.Projects.SelectMany(p => p.Files).ToDictionary(f => f.FilePath, f => f);

        foreach (var kvp in currentFiles)
        {
            var filePath = kvp.Key;
            var currentFile = kvp.Value;

            if (baselineFiles.TryGetValue(filePath, out var baselineFile))
            {
                var coverageChange = currentFile.Summary.LinesCoveredPercentage - baselineFile.Summary.LinesCoveredPercentage;

                if (coverageChange > 0.1) // Improved by more than 0.1%
                {
                    result.ImprovedFiles.Add(filePath);
                }
                else if (coverageChange < -0.1) // Regressed by more than 0.1%
                {
                    result.RegressedFiles.Add(filePath);
                }
            }
        }
    }

    private ClassCoverage? ParseClass(XElement classElement)
    {
        try
        {
            var className = classElement.Attribute("name")?.Value ?? "Unknown";
            var filename = classElement.Attribute("filename")?.Value ?? "";
            var lineRate = double.Parse(classElement.Attribute("line-rate")?.Value ?? "0");
            var branchRate = double.Parse(classElement.Attribute("branch-rate")?.Value ?? "0");

            var classCoverage = new ClassCoverage
            {
                ClassName = className,
                FilePath = filename,
                Summary = new CoverageSummary
                {
                    LinesCoveredPercentage = lineRate * 100,
                    BranchesCoveredPercentage = branchRate * 100
                }
            };

            // Parse methods
            var methods = classElement.Element("methods")?.Elements("method") ?? Enumerable.Empty<XElement>();
            foreach (var method in methods)
            {
                var methodCoverage = ParseMethod(method);
                if (methodCoverage != null)
                {
                    classCoverage.Methods.Add(methodCoverage);
                }
            }

            return classCoverage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing class in Cobertura file");
            return null;
        }
    }

    private MethodCoverage? ParseMethod(XElement methodElement)
    {
        try
        {
            var methodName = methodElement.Attribute("name")?.Value ?? "Unknown";
            var signature = methodElement.Attribute("signature")?.Value ?? "";
            var lineRate = double.Parse(methodElement.Attribute("line-rate")?.Value ?? "0");
            var branchRate = double.Parse(methodElement.Attribute("branch-rate")?.Value ?? "0");

            var methodCoverage = new MethodCoverage
            {
                MethodName = methodName,
                Signature = signature,
                Summary = new CoverageSummary
                {
                    LinesCoveredPercentage = lineRate * 100,
                    BranchesCoveredPercentage = branchRate * 100
                }
            };

            // Parse lines for this method
            var lines = methodElement.Element("lines")?.Elements("line") ?? Enumerable.Empty<XElement>();
            foreach (var line in lines)
            {
                var lineCoverage = ParseLine(line);
                if (lineCoverage != null)
                {
                    methodCoverage.Lines.Add(lineCoverage);
                }
            }

            return methodCoverage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing method in Cobertura file");
            return null;
        }
    }

    private LineCoverage? ParseLine(XElement lineElement)
    {
        try
        {
            var lineNumber = int.Parse(lineElement.Attribute("number")?.Value ?? "0");
            var hits = int.Parse(lineElement.Attribute("hits")?.Value ?? "0");
            var branch = bool.Parse(lineElement.Attribute("branch")?.Value ?? "false");

            return new LineCoverage
            {
                LineNumber = lineNumber,
                HitCount = hits,
                Status = hits > 0 ? CoverageStatus.Covered : CoverageStatus.Uncovered
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing line in Cobertura file");
            return null;
        }
    }

    private void CalculateProjectSummary(ProjectCoverage projectCoverage)
    {
        if (!projectCoverage.Files.Any())
            return;

        var allLines = projectCoverage.Files.SelectMany(f => f.Lines).ToList();
        var allMethods = projectCoverage.Files.SelectMany(f => f.Methods).ToList();

        projectCoverage.Summary.TotalLines = allLines.Count;
        projectCoverage.Summary.CoveredLines = allLines.Count(l => l.IsCovered);
        projectCoverage.Summary.UncoveredLines = projectCoverage.Summary.TotalLines - projectCoverage.Summary.CoveredLines;

        projectCoverage.Summary.TotalMethods = allMethods.Count;
        projectCoverage.Summary.CoveredMethods = allMethods.Count(m => m.Summary.LinesCoveredPercentage > 0);
        projectCoverage.Summary.UncoveredMethods = projectCoverage.Summary.TotalMethods - projectCoverage.Summary.CoveredMethods;

        projectCoverage.Summary.TotalClasses = projectCoverage.Classes.Count;
        projectCoverage.Summary.CoveredClasses = projectCoverage.Classes.Count(c => c.Summary.LinesCoveredPercentage > 0);
        projectCoverage.Summary.UncoveredClasses = projectCoverage.Summary.TotalClasses - projectCoverage.Summary.CoveredClasses;

        if (projectCoverage.Summary.TotalLines > 0)
        {
            projectCoverage.Summary.LinesCoveredPercentage = (double)projectCoverage.Summary.CoveredLines / projectCoverage.Summary.TotalLines * 100;
        }

        if (projectCoverage.Summary.TotalMethods > 0)
        {
            projectCoverage.Summary.MethodsCoveredPercentage = (double)projectCoverage.Summary.CoveredMethods / projectCoverage.Summary.TotalMethods * 100;
        }

        if (projectCoverage.Summary.TotalClasses > 0)
        {
            projectCoverage.Summary.ClassesCoveredPercentage = (double)projectCoverage.Summary.CoveredClasses / projectCoverage.Summary.TotalClasses * 100;
        }
    }

    #endregion

    /// <summary>
    /// Helper class for project coverage results
    /// </summary>
    private class ProjectCoverageResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> CoverageFiles { get; set; } = new();
        public TestExecutionSummary TestSummary { get; set; } = new();
    }
}
