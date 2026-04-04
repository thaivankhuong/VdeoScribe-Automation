using Whiteboard.Cli.Contracts;
using Whiteboard.Cli.Models;
using Whiteboard.Core.Compilation;
using Whiteboard.Core.Validation;

namespace Whiteboard.Cli.Services;

public sealed class ScriptCompilationOrchestrator : IScriptCompilationOrchestrator
{
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
            return new CliScriptCompileCommandResult
            {
                Success = false,
                SpecOutputPath = request.SpecOutputPath,
                Issues = contractIssues
            };
        }

        var inputPath = Path.GetFullPath(request.InputPath);
        if (!File.Exists(inputPath))
        {
            return new CliScriptCompileCommandResult
            {
                Success = false,
                SpecOutputPath = Path.GetFullPath(request.SpecOutputPath),
                Issues =
                [
                    new ValidationIssue(
                        ValidationGate.Contract,
                        "$.inputPath",
                        ValidationSeverity.Error,
                        "script.contract.required",
                        $"Input script '{inputPath}' was not found.")
                ]
            };
        }

        var repoRoot = FindRepoRoot(inputPath);
        var compileResult = _scriptCompiler.Compile(
            File.ReadAllText(inputPath),
            inputPath,
            Path.Combine(repoRoot, ".planning", "templates", "index.json"),
            Path.Combine(repoRoot, ".planning", "script-compiler", "template-mappings.json"),
            Path.Combine(repoRoot, ".planning", "script-compiler", "governed-library.json"));

        var specOutputPath = Path.GetFullPath(request.SpecOutputPath);
        if (!compileResult.Success)
        {
            return new CliScriptCompileCommandResult
            {
                Success = false,
                ScriptId = compileResult.ScriptId,
                TemplateCount = compileResult.TemplateCount,
                SectionCount = compileResult.SectionCount,
                SpecOutputPath = specOutputPath,
                Issues = compileResult.Issues
            };
        }

        var outputDirectory = Path.GetDirectoryName(specOutputPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        File.WriteAllText(specOutputPath, compileResult.CanonicalJson);

        return new CliScriptCompileCommandResult
        {
            Success = true,
            ScriptId = compileResult.ScriptId,
            TemplateCount = compileResult.TemplateCount,
            SectionCount = compileResult.SectionCount,
            SpecOutputPath = specOutputPath,
            DeterministicKey = compileResult.DeterministicKey,
            Issues = []
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

        return ValidationIssueOrdering.Sort(issues);
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
