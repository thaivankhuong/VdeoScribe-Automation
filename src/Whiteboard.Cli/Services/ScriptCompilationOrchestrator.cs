using System.Text.Json;
using Whiteboard.Cli.Contracts;
using Whiteboard.Cli.Models;
using Whiteboard.Core.Compilation;
using Whiteboard.Core.Validation;

namespace Whiteboard.Cli.Services;

public sealed class ScriptCompilationOrchestrator : IScriptCompilationOrchestrator
{
    private static readonly JsonSerializerOptions ReportSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly IScriptCompiler _scriptCompiler;

    public ScriptCompilationOrchestrator(IScriptCompiler? scriptCompiler = null)
    {
        _scriptCompiler = scriptCompiler ?? new ScriptCompiler();
    }

    public CliScriptCompileCommandResult Compile(CliScriptCompileCommandRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var contractIssues = ValidateRequest(request);
        if (contractIssues.Count > 0)
        {
            var diagnostics = ToDiagnostics(contractIssues);
            WriteReport(request.ReportOutputPath, BuildFallbackReport(request, diagnostics));
            return new CliScriptCompileCommandResult
            {
                Success = false,
                SpecOutputPath = Path.GetFullPath(request.SpecOutputPath),
                ReportOutputPath = Path.GetFullPath(request.ReportOutputPath),
                Diagnostics = diagnostics,
                Issues = contractIssues
            };
        }

        var inputPath = Path.GetFullPath(request.InputPath);
        var specOutputPath = Path.GetFullPath(request.SpecOutputPath);
        var reportOutputPath = Path.GetFullPath(request.ReportOutputPath);

        if (!File.Exists(inputPath))
        {
            var issues = ValidationIssueOrdering.Sort(
            [
                new ValidationIssue(
                    ValidationGate.Contract,
                    "$.inputPath",
                    ValidationSeverity.Error,
                    "script.contract.required",
                    $"Input script '{inputPath}' was not found.")
            ]);
            var diagnostics = ToDiagnostics(issues);
            WriteReport(request.ReportOutputPath, BuildFallbackReport(request, diagnostics));

            return new CliScriptCompileCommandResult
            {
                Success = false,
                SpecOutputPath = specOutputPath,
                ReportOutputPath = reportOutputPath,
                Diagnostics = diagnostics,
                Issues = issues
            };
        }

        var repoRoot = FindRepoRoot(inputPath);
        var compileResult = _scriptCompiler.Compile(
            File.ReadAllText(inputPath),
            inputPath,
            Path.Combine(repoRoot, ".planning", "templates", "index.json"),
            Path.Combine(repoRoot, ".planning", "script-compiler", "template-mappings.json"),
            Path.Combine(repoRoot, ".planning", "script-compiler", "governed-library.json"));

        WriteReport(request.ReportOutputPath, compileResult.Report);

        if (!compileResult.Success)
        {
            return new CliScriptCompileCommandResult
            {
                Success = false,
                ScriptId = compileResult.ScriptId,
                TemplateCount = compileResult.TemplateCount,
                SectionCount = compileResult.SectionCount,
                SpecOutputPath = specOutputPath,
                ReportOutputPath = reportOutputPath,
                DeterministicKey = compileResult.DeterministicKey,
                Diagnostics = compileResult.Diagnostics,
                Issues = compileResult.Issues
            };
        }

        var outputDirectory = Path.GetDirectoryName(specOutputPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        File.WriteAllText(specOutputPath, compileResult.SpecOutputJson);

        return new CliScriptCompileCommandResult
        {
            Success = true,
            ScriptId = compileResult.ScriptId,
            TemplateCount = compileResult.TemplateCount,
            SectionCount = compileResult.SectionCount,
            SpecOutputPath = specOutputPath,
            ReportOutputPath = reportOutputPath,
            DeterministicKey = compileResult.DeterministicKey,
            Diagnostics = compileResult.Diagnostics,
            Issues = []
        };
    }

    private static ScriptCompileReport BuildFallbackReport(
        CliScriptCompileCommandRequest request,
        IReadOnlyList<ScriptCompileDiagnostic> diagnostics)
    {
        return new ScriptCompileReport
        {
            Script = new ScriptCompileReportScript
            {
                SourcePath = Path.GetFullPath(request.InputPath)
            },
            Spec = new ScriptCompileReportSpec
            {
                Success = false,
                SpecOutputGenerated = false
            },
            Diagnostics = diagnostics
        };
    }

    private static IReadOnlyList<ValidationIssue> ValidateRequest(CliScriptCompileCommandRequest request)
    {
        var issues = new List<ValidationIssue>();

        if (string.IsNullOrWhiteSpace(request.InputPath))
        {
            issues.Add(new ValidationIssue(
                ValidationGate.Contract,
                "$.inputPath",
                ValidationSeverity.Error,
                "script.contract.required",
                "InputPath is required."));
        }

        if (string.IsNullOrWhiteSpace(request.SpecOutputPath))
        {
            issues.Add(new ValidationIssue(
                ValidationGate.Contract,
                "$.specOutputPath",
                ValidationSeverity.Error,
                "script.contract.required",
                "SpecOutputPath is required."));
        }

        if (string.IsNullOrWhiteSpace(request.ReportOutputPath))
        {
            issues.Add(new ValidationIssue(
                ValidationGate.Contract,
                "$.reportOutputPath",
                ValidationSeverity.Error,
                "script.contract.required",
                "ReportOutputPath is required."));
        }

        return ValidationIssueOrdering.Sort(issues);
    }

    private static IReadOnlyList<ScriptCompileDiagnostic> ToDiagnostics(IReadOnlyList<ValidationIssue> issues)
    {
        return ScriptCompileDiagnostic.Sort(
            issues.Select(issue => new ScriptCompileDiagnostic
            {
                Severity = issue.Severity.ToString().ToLowerInvariant(),
                Code = issue.Code,
                Message = issue.Message,
                Path = issue.Path,
                Gate = issue.Gate.ToString().ToLowerInvariant()
            }));
    }

    private static void WriteReport(string reportOutputPath, ScriptCompileReport report)
    {
        var resolvedPath = Path.GetFullPath(reportOutputPath);
        var outputDirectory = Path.GetDirectoryName(resolvedPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        File.WriteAllText(resolvedPath, JsonSerializer.Serialize(report, ReportSerializerOptions));
    }

    private static string FindRepoRoot(string path)
    {
        var current = new DirectoryInfo(Path.GetDirectoryName(Path.GetFullPath(path)) ?? Environment.CurrentDirectory);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, ".planning")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return Environment.CurrentDirectory;
    }
}
