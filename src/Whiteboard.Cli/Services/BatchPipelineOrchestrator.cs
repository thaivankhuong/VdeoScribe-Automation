using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Whiteboard.Cli.Contracts;
using Whiteboard.Cli.Models;

namespace Whiteboard.Cli.Services;

public sealed class BatchPipelineOrchestrator : IBatchPipelineOrchestrator
{
    private static readonly JsonSerializerOptions SummaryJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly IScriptCompilationOrchestrator _scriptCompilationOrchestrator;
    private readonly IPipelineOrchestrator _pipelineOrchestrator;

    public BatchPipelineOrchestrator(
        IPipelineOrchestrator? pipelineOrchestrator = null,
        IScriptCompilationOrchestrator? scriptCompilationOrchestrator = null)
    {
        _pipelineOrchestrator = pipelineOrchestrator ?? new PipelineOrchestrator();
        _scriptCompilationOrchestrator = scriptCompilationOrchestrator ?? new ScriptCompilationOrchestrator();
    }

    public CliBatchRunResult Run(CliBatchRunRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.ManifestPath))
        {
            throw new ArgumentException("Manifest path is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.SummaryOutputPath))
        {
            throw new ArgumentException("Summary output path is required.", nameof(request));
        }

        var manifestPath = Path.GetFullPath(request.ManifestPath);
        var manifestDirectory = Path.GetDirectoryName(manifestPath) ?? Environment.CurrentDirectory;
        var summaryOutputPath = ResolvePath(manifestDirectory, request.SummaryOutputPath);
        var summaryDirectory = Path.GetDirectoryName(summaryOutputPath) ?? Environment.CurrentDirectory;

        CliBatchRunResult result;

        try
        {
            var manifest = LoadManifest(manifestPath);
            var orderedJobs = manifest.Jobs
                .Select((job, index) => NormalizeJob(job, index))
                .ToList();

            EnsureUniqueJobIds(orderedJobs);

            var jobResults = orderedJobs
                .Select(orderedJob => ExecuteJob(manifestDirectory, summaryDirectory, orderedJob))
                .ToList();

            result = BuildResult(jobResults, summaryOutputPath);
        }
        catch (Exception ex) when (ex is InvalidDataException or FileNotFoundException)
        {
            result = CreateValidationFailureResult(request, ex.Message, summaryOutputPath);
        }

        WriteSummary(summaryOutputPath, result);
        return result;
    }

    private static CliBatchManifest LoadManifest(string manifestPath)
    {
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException("Batch manifest was not found.", manifestPath);
        }

        var json = File.ReadAllText(manifestPath);
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidDataException("Batch manifest is empty.");
        }

        var manifest = JsonSerializer.Deserialize<CliBatchManifest>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (manifest is null || manifest.Jobs.Count == 0)
        {
            throw new InvalidDataException("Batch manifest must contain at least one job.");
        }

        return manifest;
    }

    private static IndexedJob NormalizeJob(CliBatchJob job, int index)
    {
        ArgumentNullException.ThrowIfNull(job);

        var normalizedJobId = job.JobId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedJobId))
        {
            throw new InvalidDataException("Batch job 'jobId' is required.");
        }

        if (normalizedJobId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || normalizedJobId.Contains(Path.DirectorySeparatorChar)
            || normalizedJobId.Contains(Path.AltDirectorySeparatorChar))
        {
            throw new InvalidDataException($"Batch job '{normalizedJobId}' contains invalid characters for deterministic workspace naming.");
        }

        var normalizedScriptPath = job.ScriptPath?.Trim() ?? string.Empty;

        var normalizedSpecPath = job.SpecPath?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedScriptPath) && string.IsNullOrWhiteSpace(normalizedSpecPath))
        {
            throw new InvalidDataException($"Batch job '{normalizedJobId}' is missing 'scriptPath' or compatibility 'specPath'.");
        }

        var normalizedOutputPath = job.OutputPath?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedOutputPath))
        {
            throw new InvalidDataException($"Batch job '{normalizedJobId}' is missing 'outputPath'.");
        }

        return new IndexedJob(job with
        {
            JobId = normalizedJobId,
            ScriptPath = normalizedScriptPath,
            SpecPath = normalizedSpecPath,
            OutputPath = normalizedOutputPath,
            FrameIndex = job.FrameIndex.HasValue
                ? Math.Max(0, job.FrameIndex.Value)
                : null
        }, index);
    }

    private static void EnsureUniqueJobIds(IReadOnlyList<IndexedJob> jobs)
    {
        var duplicate = jobs
            .GroupBy(job => job.Job.JobId, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidDataException($"Batch manifest contains duplicate jobId '{duplicate.Key}'.");
        }
    }

    private CliBatchJobResult ExecuteJob(string manifestDirectory, string summaryDirectory, IndexedJob indexedJob)
    {
        var job = indexedJob.Job;
        var workspace = BuildWorkspace(summaryDirectory, indexedJob.Index, job.JobId);

        try
        {
            var resolvedSpecPath = ResolvePath(manifestDirectory, job.SpecPath);
            var logicalSpecPath = ToLogicalPath(manifestDirectory, resolvedSpecPath);
            var resolvedOutputPath = ResolvePath(manifestDirectory, job.OutputPath);

            if (!string.IsNullOrWhiteSpace(job.ScriptPath))
            {
                var compileResult = _scriptCompilationOrchestrator.Compile(new CliScriptCompileCommandRequest
                {
                    InputPath = ResolvePath(manifestDirectory, job.ScriptPath),
                    SpecOutputPath = workspace.CompiledSpecPath,
                    ReportOutputPath = workspace.CompileReportPath
                });

                if (!compileResult.Success)
                {
                    return BuildCompileFailureResult(job, workspace, compileResult);
                }

                resolvedSpecPath = compileResult.SpecOutputPath;
                logicalSpecPath = workspace.LogicalSpecPath ?? logicalSpecPath;
            }

            var result = _pipelineOrchestrator.Run(new CliRunRequest
            {
                SpecPath = resolvedSpecPath,
                OutputPath = resolvedOutputPath,
                FrameIndex = job.FrameIndex
            });

            return BuildJobResult(manifestDirectory, logicalSpecPath, job, result);
        }
        catch (Exception ex)
        {
            return new CliBatchJobResult
            {
                JobId = job.JobId,
                SpecPath = string.IsNullOrWhiteSpace(job.ScriptPath)
                    ? job.SpecPath
                    : workspace.LogicalSpecPath ?? string.Empty,
                OutputPath = job.OutputPath,
                FrameIndex = job.FrameIndex,
                Success = false,
                Message = ex.Message,
                ExportStatus = "failed",
                DeterministicKey = $"job-failed:{job.JobId}:{ex.Message}",
                ExportDeterministicKey = string.Empty
            };
        }
    }

    private static CliBatchJobResult BuildJobResult(
        string manifestDirectory,
        string logicalSpecPath,
        CliBatchJob job,
        CliRunResult result)
    {
        return new CliBatchJobResult
        {
            JobId = job.JobId,
            SpecPath = logicalSpecPath,
            OutputPath = job.OutputPath,
            FrameIndex = job.FrameIndex,
            FirstFrameIndex = result.FirstFrameIndex,
            LastFrameIndex = result.LastFrameIndex,
            PlannedFrameCount = result.PlannedFrameCount,
            RenderedFrameCount = result.RenderedFrameCount,
            ProjectDurationSeconds = result.ProjectDurationSeconds,
            Success = result.Success,
            Message = result.Message,
            ExportStatus = result.ExportStatus,
            ExportPackageRootPath = ToLogicalPath(manifestDirectory, result.ExportPackageRootPath),
            ExportManifestPath = ToLogicalPath(manifestDirectory, result.ExportManifestPath),
            DeterministicKey = result.DeterministicKey,
            ExportDeterministicKey = result.ExportDeterministicKey,
            PlayableMediaPath = ToLogicalPath(manifestDirectory, result.PlayableMediaPath),
            PlayableMediaStatus = result.PlayableMediaStatus,
            PlayableMediaDeterministicKey = result.PlayableMediaDeterministicKey,
            PlayableMediaByteCount = result.PlayableMediaByteCount,
            PlayableMediaAudioStatus = result.PlayableMediaAudioStatus,
            PlayableMediaAudioCueCount = result.PlayableMediaAudioCueCount
        };
    }

    private static CliBatchJobResult BuildCompileFailureResult(
        CliBatchJob job,
        JobWorkspace workspace,
        CliScriptCompileCommandResult compileResult)
    {
        return new CliBatchJobResult
        {
            JobId = job.JobId,
            SpecPath = workspace.LogicalSpecPath ?? string.Empty,
            OutputPath = job.OutputPath,
            FrameIndex = job.FrameIndex,
            Success = false,
            Message = BuildCompileFailureMessage(job.JobId, compileResult.Diagnostics),
            ExportStatus = "not-run",
            DeterministicKey = compileResult.DeterministicKey,
            ExportDeterministicKey = string.Empty
        };
    }

    private static CliBatchRunResult BuildResult(IReadOnlyList<CliBatchJobResult> jobResults, string summaryOutputPath)
    {
        var successCount = jobResults.Count(job => job.Success);
        var failureCount = jobResults.Count - successCount;

        return new CliBatchRunResult
        {
            JobCount = jobResults.Count,
            SuccessCount = successCount,
            FailureCount = failureCount,
            Success = failureCount == 0,
            DeterministicKey = BuildDeterministicKey(jobResults),
            Jobs = jobResults,
            SummaryOutputPath = summaryOutputPath
        };
    }

    private static CliBatchRunResult CreateValidationFailureResult(
        CliBatchRunRequest request,
        string message,
        string summaryOutputPath)
    {
        var jobResult = new CliBatchJobResult
        {
            JobId = "validation-error",
            SpecPath = request.ManifestPath,
            OutputPath = string.Empty,
            Success = false,
            Message = message,
            DeterministicKey = $"batch-validation:{message}",
            ExportDeterministicKey = string.Empty
        };

        return new CliBatchRunResult
        {
            JobCount = 1,
            SuccessCount = 0,
            FailureCount = 1,
            Success = false,
            DeterministicKey = BuildDeterministicKey([jobResult]),
            Jobs = [jobResult],
            SummaryOutputPath = summaryOutputPath
        };
    }

    private static string BuildDeterministicKey(IReadOnlyList<CliBatchJobResult> jobResults)
    {
        return string.Join(
            "|",
            jobResults.Select(job =>
                $"{job.JobId}:{job.SpecPath}:{job.OutputPath}:{FormatFrameIndex(job.FrameIndex)}:{job.FirstFrameIndex}:{job.LastFrameIndex}:{job.PlannedFrameCount}:{job.RenderedFrameCount}:{FormatDouble(job.ProjectDurationSeconds)}:{job.Success}:{job.ExportStatus}:{job.ExportPackageRootPath}:{job.ExportManifestPath}:{job.PlayableMediaPath}:{job.PlayableMediaStatus}:{job.PlayableMediaDeterministicKey}:{job.PlayableMediaByteCount}:{job.PlayableMediaAudioStatus}:{job.PlayableMediaAudioCueCount}:{job.DeterministicKey}:{job.ExportDeterministicKey}"));
    }

    private static void WriteSummary(string summaryOutputPath, CliBatchRunResult result)
    {
        var directoryPath = Path.GetDirectoryName(summaryOutputPath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var json = JsonSerializer.Serialize(result, SummaryJsonOptions);
        File.WriteAllText(summaryOutputPath, json);
    }

    private static string ResolvePath(string baseDirectory, string path)
    {
        if (Path.IsPathRooted(path))
        {
            return Path.GetFullPath(path);
        }

        return Path.GetFullPath(Path.Combine(baseDirectory, path));
    }

    private static JobWorkspace BuildWorkspace(string summaryDirectory, int index, string jobId)
    {
        var workspaceName = $"{index:000}-{jobId}";
        var workspaceDirectory = Path.Combine(summaryDirectory, "jobs", workspaceName);
        return new JobWorkspace(
            workspaceDirectory,
            Path.Combine(workspaceDirectory, "compiled-spec.json"),
            Path.Combine(workspaceDirectory, "compile-report.json"),
            Path.Combine("jobs", workspaceName, "compiled-spec.json").Replace('\\', '/'));
    }

    private static string BuildCompileFailureMessage(
        string jobId,
        IReadOnlyList<Whiteboard.Core.Compilation.ScriptCompileDiagnostic> diagnostics)
    {
        if (diagnostics.Count == 0)
        {
            return $"Batch job '{jobId}' compile failed before render/export.";
        }

        return string.Join(
            " | ",
            diagnostics.Select(diagnostic =>
                $"{diagnostic.Code} ({diagnostic.Path}): {diagnostic.Message}"));
    }

    private static string ToLogicalPath(string baseDirectory, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var fullPath = Path.GetFullPath(path);
        var fullBaseDirectory = Path.GetFullPath(baseDirectory);
        return fullPath.StartsWith(fullBaseDirectory, StringComparison.OrdinalIgnoreCase)
            ? Path.GetRelativePath(fullBaseDirectory, fullPath).Replace('\\', '/')
            : fullPath.Replace('\\', '/');
    }

    private static string FormatFrameIndex(int? frameIndex)
    {
        return frameIndex.HasValue
            ? frameIndex.Value.ToString(CultureInfo.InvariantCulture)
            : "full";
    }

    private static string FormatDouble(double value)
    {
        return value.ToString("0.######", CultureInfo.InvariantCulture);
    }

    private sealed record IndexedJob(CliBatchJob Job, int Index);
    private sealed record JobWorkspace(string DirectoryPath, string CompiledSpecPath, string CompileReportPath, string? LogicalSpecPath);
}
