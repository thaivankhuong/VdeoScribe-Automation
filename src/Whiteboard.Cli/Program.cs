using System;
using System.Globalization;
using Whiteboard.Cli.Contracts;
using Whiteboard.Cli.Models;
using Whiteboard.Cli.Services;
using Whiteboard.Core.Validation;

namespace Whiteboard.Cli;

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0 || HasHelpFlag(args))
            {
                PrintUsage();
                return args.Length == 0 ? 1 : 0;
            }

            var parser = new CliCommandParser();
            var command = parser.Parse(args);

            return command.Mode switch
            {
                CliCommandMode.Run => ExecuteRun(command.RunRequest!),
                CliCommandMode.Batch => ExecuteBatch(command.BatchRequest!),
                CliCommandMode.ScriptCompile => ExecuteScriptCompile(command.ScriptCompileRequest!),
                CliCommandMode.TemplateValidate => ExecuteTemplateValidate(command.TemplateValidateRequest!),
                CliCommandMode.TemplateInstantiate => ExecuteTemplateInstantiate(command.TemplateInstantiateRequest!),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"CLI error: {ex.Message}");
            return 1;
        }
    }

    private static int ExecuteRun(CliRunRequest request)
    {
        var orchestrator = new PipelineOrchestrator();
        var result = orchestrator.Run(request);

        Console.WriteLine($"Success: {result.Success}");
        Console.WriteLine($"Message: {result.Message}");
        Console.WriteLine($"SpecPath: {result.SpecPath}");
        Console.WriteLine($"RunMode: {(result.FrameIndex.HasValue ? "debug-frame" : "full-sequence")}");
        Console.WriteLine($"FrameIndex: {FormatOptionalFrameIndex(result.FrameIndex)}");
        Console.WriteLine($"FrameRange: {result.FirstFrameIndex}-{result.LastFrameIndex}");
        Console.WriteLine($"PlannedFrameCount: {result.PlannedFrameCount}");
        Console.WriteLine($"RenderedFrameCount: {result.RenderedFrameCount}");
        Console.WriteLine($"ProjectDurationSeconds: {result.ProjectDurationSeconds.ToString("0.######", CultureInfo.InvariantCulture)}");
        Console.WriteLine($"SceneCount: {result.SceneCount}");
        Console.WriteLine($"ObjectCount: {result.ObjectCount}");
        Console.WriteLine($"OperationCount: {result.OperationCount}");
        Console.WriteLine($"ExportedFrameCount: {result.ExportedFrameCount}");
        Console.WriteLine($"OutputPath: {result.OutputPath}");
        Console.WriteLine($"PlayableMediaPath: {result.PlayableMediaPath}");
        Console.WriteLine($"PlayableMediaStatus: {result.PlayableMediaStatus}");
        Console.WriteLine($"PlayableMediaAudioStatus: {result.PlayableMediaAudioStatus}");
        Console.WriteLine($"PlayableMediaAudioCueCount: {result.PlayableMediaAudioCueCount}");
        Console.WriteLine($"PlayableMediaByteCount: {result.PlayableMediaByteCount}");
        Console.WriteLine($"ExportStatus: {result.ExportStatus}");
        Console.WriteLine($"DeterministicKey: {result.DeterministicKey}");

        return result.Success ? 0 : 1;
    }

    private static int ExecuteTemplateValidate(CliTemplateValidateRequest request)
    {
        ITemplatePipelineOrchestrator orchestrator = new TemplatePipelineOrchestrator();
        var result = orchestrator.Validate(request);

        Console.WriteLine($"Success: {result.Success}");
        Console.WriteLine($"TemplateId: {result.TemplateId}");
        Console.WriteLine($"Version: {result.Version}");
        Console.WriteLine($"Status: {result.Status}");
        Console.WriteLine($"SlotValidationStatus: {result.SlotValidationStatus}");
        WriteIssues(result.Issues);

        return result.Success ? 0 : 1;
    }

    private static int ExecuteScriptCompile(CliScriptCompileCommandRequest request)
    {
        IScriptCompilationOrchestrator orchestrator = new ScriptCompilationOrchestrator();
        var result = orchestrator.Compile(request);

        Console.WriteLine($"Success: {result.Success}");
        Console.WriteLine($"ScriptId: {result.ScriptId}");
        Console.WriteLine($"TemplateCount: {result.TemplateCount}");
        Console.WriteLine($"SectionCount: {result.SectionCount}");
        Console.WriteLine($"SpecOutputPath: {result.SpecOutputPath}");
        Console.WriteLine($"ReportOutputPath: {result.ReportOutputPath}");
        Console.WriteLine($"DeterministicKey: {result.DeterministicKey}");
        WriteDiagnostics(result.Diagnostics);

        return result.Success ? 0 : 1;
    }

    private static int ExecuteTemplateInstantiate(CliTemplateInstantiateRequest request)
    {
        ITemplatePipelineOrchestrator orchestrator = new TemplatePipelineOrchestrator();
        var result = orchestrator.Instantiate(request);

        Console.WriteLine($"Success: {result.Success}");
        Console.WriteLine($"TemplateId: {result.TemplateId}");
        Console.WriteLine($"InstanceId: {result.InstanceId}");
        Console.WriteLine($"OutputPath: {result.OutputPath}");
        Console.WriteLine($"SlotValidationStatus: {result.SlotValidationStatus}");
        Console.WriteLine($"DeterministicKey: {result.DeterministicKey}");
        WriteIssues(result.Issues);

        return result.Success ? 0 : 1;
    }

    private static int ExecuteBatch(CliBatchRunRequest request)
    {
        var orchestrator = new BatchPipelineOrchestrator();
        var result = orchestrator.Run(request);

        Console.WriteLine($"Success: {result.Success}");
        Console.WriteLine($"JobCount: {result.JobCount}");
        Console.WriteLine($"SuccessCount: {result.SuccessCount}");
        Console.WriteLine($"FailureCount: {result.FailureCount}");
        Console.WriteLine($"SummaryOutputPath: {result.SummaryOutputPath}");
        Console.WriteLine($"DeterministicKey: {result.DeterministicKey}");

        return result.Success ? 0 : 1;
    }

    private static bool HasHelpFlag(string[] args)
    {
        foreach (var arg in args)
        {
            if (arg is "--help" or "-h")
            {
                return true;
            }
        }

        return false;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  whiteboard-cli run --spec <path> [--output <path>] [--frame-index <int>]");
        Console.WriteLine("  whiteboard-cli batch --manifest <path> --summary-output <path>");
        Console.WriteLine("  whiteboard-cli script compile --input <path> --spec-output <path> --report-output <path>");
        Console.WriteLine("  whiteboard-cli template validate --template <template-id> [--catalog <path>] [--slots <path>]");
        Console.WriteLine("  whiteboard-cli template instantiate --template <template-id> [--catalog <path>] --slots <path> --output <path> --instance-id <id> [--time-offset-seconds <double>] [--layer-offset <int>]");
        Console.WriteLine("  whiteboard-cli --spec <path> [--output <path>] [--frame-index <int>]  # legacy run shortcut");
        Console.WriteLine("Batch manifests stay file-driven: each Phase 19 job declares jobId, scriptPath, and outputPath, then reuses the existing script compiler plus render/export pipeline.");
        Console.WriteLine("Phase 20 optional QA gate fields: enforceDeterministicQaGates, defaultRegressionBaselinePath, and per-job regressionBaselinePath.");
        Console.WriteLine("Legacy batch specPath entries remain compatibility passthrough only; batch mode does not introduce prompts, editor flows, or interactive input.");
        Console.WriteLine("--frame-index is optional debug-only execution; default run semantics render the full planned sequence.");
        Console.WriteLine("Phase 8 scope: deterministic full-timeline packaging with optional playable-media encoding when WHITEBOARD_ENABLE_PLAYABLE_MEDIA=1 and WHITEBOARD_FFMPEG_PATH are configured.");
    }

    private static string FormatOptionalFrameIndex(int? frameIndex)
    {
        return frameIndex.HasValue
            ? frameIndex.Value.ToString(CultureInfo.InvariantCulture)
            : "<full-run>";
    }

    private static void WriteIssues(IReadOnlyList<ValidationIssue> issues)
    {
        foreach (var issue in issues)
        {
            Console.WriteLine($"Issue: [{issue.Gate}] {issue.Code} at {issue.Path}: {issue.Message}");
        }
    }

    private static void WriteDiagnostics(IReadOnlyList<Whiteboard.Core.Compilation.ScriptCompileDiagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
        {
            Console.WriteLine($"Issue: [{diagnostic.Gate}] {diagnostic.Code} at {diagnostic.Path}: {diagnostic.Message}");
        }
    }
}
