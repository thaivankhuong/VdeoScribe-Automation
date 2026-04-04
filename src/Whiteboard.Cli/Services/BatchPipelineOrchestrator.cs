using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Whiteboard.Cli.Contracts;
using Whiteboard.Cli.Models;

namespace Whiteboard.Cli.Services;

public sealed class BatchPipelineOrchestrator : IBatchPipelineOrchestrator
{
    private static readonly JsonSerializerOptions ArtifactJsonOptions = new()
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
        var artifactBaseDirectory = FindArtifactBaseDirectory(manifestDirectory, summaryDirectory);

        CliBatchRunResult result;

        try
        {
            var manifest = LoadManifest(manifestPath);
            var orderedJobs = manifest.Jobs
                .Select((job, index) => NormalizeJob(job, index, manifest.RetryLimit))
                .ToList();

            EnsureUniqueJobIds(orderedJobs);

            var jobResults = orderedJobs
                .Select(orderedJob => ExecuteJob(manifestDirectory, summaryDirectory, artifactBaseDirectory, orderedJob))
                .ToList();

            result = BuildResult(jobResults, summaryOutputPath);
        }
        catch (Exception ex) when (ex is InvalidDataException or FileNotFoundException)
        {
            result = CreateValidationFailureResult(request, ex.Message, summaryOutputPath);
        }

        WriteJson(summaryOutputPath, result);
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

        if (manifest.RetryLimit < 0)
        {
            throw new InvalidDataException("Batch manifest retryLimit must be zero or greater.");
        }

        return manifest;
    }

    private static IndexedJob NormalizeJob(CliBatchJob job, int index, int defaultRetryLimit)
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

        var effectiveRetryLimit = job.RetryLimit ?? defaultRetryLimit;
        if (effectiveRetryLimit < 0)
        {
            throw new InvalidDataException($"Batch job '{normalizedJobId}' retryLimit must be zero or greater.");
        }

        return new IndexedJob(
            job with
            {
                JobId = normalizedJobId,
                ScriptPath = normalizedScriptPath,
                SpecPath = normalizedSpecPath,
                OutputPath = normalizedOutputPath,
                RetryLimit = effectiveRetryLimit,
                FrameIndex = job.FrameIndex.HasValue
                    ? Math.Max(0, job.FrameIndex.Value)
                    : null
            },
            index,
            effectiveRetryLimit);
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

    private CliBatchJobResult ExecuteJob(
        string manifestDirectory,
        string summaryDirectory,
        string artifactBaseDirectory,
        IndexedJob indexedJob)
    {
        var job = indexedJob.Job;
        var workspace = BuildWorkspace(summaryDirectory, indexedJob.Index, job.JobId);
        Directory.CreateDirectory(workspace.DirectoryPath);

        var outcomes = new List<AttemptExecutionOutcome>();
        var maxAttempts = indexedJob.RetryLimit + 1;

        for (var attemptNumber = 1; attemptNumber <= maxAttempts; attemptNumber++)
        {
            var outcome = ExecuteAttempt(manifestDirectory, artifactBaseDirectory, indexedJob, workspace, attemptNumber);
            outcomes.Add(outcome);

            if (outcome.Attempt.Success || !CanRetry(outcome.Attempt.FailureStage, attemptNumber, indexedJob.RetryLimit))
            {
                break;
            }
        }

        var finalizedAttempts = MarkFinalAttempt(outcomes.Select(outcome => outcome.Attempt).ToList());
        var manifest = BuildJobManifest(indexedJob, finalizedAttempts);
        WriteJson(workspace.JobManifestPath, manifest);
        return BuildJobResult(artifactBaseDirectory, indexedJob, workspace, manifest, outcomes[^1]);
    }

    private AttemptExecutionOutcome ExecuteAttempt(
        string manifestDirectory,
        string artifactBaseDirectory,
        IndexedJob indexedJob,
        JobWorkspace workspace,
        int attemptNumber)
    {
        var job = indexedJob.Job;
        var logicalScriptPath = ToLogicalPath(artifactBaseDirectory, ResolvePath(manifestDirectory, job.ScriptPath));
        var logicalOutputPath = job.OutputPath.Replace('\\', '/');
        var compileStatus = string.IsNullOrWhiteSpace(job.ScriptPath)
            ? CliBatchStageStatus.NotRun
            : CliBatchStageStatus.Failed;
        var runStatus = CliBatchStageStatus.NotRun;
        var failureStage = string.IsNullOrWhiteSpace(job.ScriptPath)
            ? CliBatchFailureStage.Run
            : CliBatchFailureStage.Compile;
        var compiledSpecPath = string.Empty;
        var reportOutputPath = string.Empty;
        var compileDeterministicKey = string.Empty;

        try
        {
            var resolvedOutputPath = ResolvePath(manifestDirectory, job.OutputPath);
            var resolvedSpecPath = ResolvePath(manifestDirectory, job.SpecPath);

            if (!string.IsNullOrWhiteSpace(job.ScriptPath))
            {
                failureStage = CliBatchFailureStage.Compile;
                var resolvedScriptPath = ResolvePath(manifestDirectory, job.ScriptPath);
                var compileResult = _scriptCompilationOrchestrator.Compile(new CliScriptCompileCommandRequest
                {
                    InputPath = resolvedScriptPath,
                    SpecOutputPath = workspace.CompiledSpecPath,
                    ReportOutputPath = workspace.CompileReportPath
                });

                compiledSpecPath = ToLogicalPath(artifactBaseDirectory, compileResult.SpecOutputPath);
                reportOutputPath = ToLogicalPath(artifactBaseDirectory, compileResult.ReportOutputPath);
                compileDeterministicKey = compileResult.DeterministicKey;

                if (!compileResult.Success)
                {
                    var message = BuildCompileFailureMessage(job.JobId, compileResult.Diagnostics);
                    var compileFailureSummary = BuildCompileFailureSummary(job.JobId, compileResult.Diagnostics);
                    return new AttemptExecutionOutcome(
                        new CliBatchJobAttemptRecord
                        {
                            AttemptNumber = attemptNumber,
                            RetryLimit = indexedJob.RetryLimit,
                            Success = false,
                            CompileStatus = CliBatchStageStatus.Failed,
                            RunStatus = CliBatchStageStatus.NotRun,
                            FinalStatus = CliBatchJobStatus.Failed,
                            FailureStage = CliBatchFailureStage.Compile,
                            Message = message,
                            FailureSummary = compileFailureSummary,
                            ScriptPath = logicalScriptPath,
                            OutputPath = logicalOutputPath,
                            CompiledSpecPath = compiledSpecPath,
                            ReportOutputPath = reportOutputPath,
                            CompileDeterministicKey = compileDeterministicKey,
                            DeterministicKey = BuildAttemptDeterministicKey(compileDeterministicKey, compileFailureSummary)
                        },
                        null);
                }

                compileStatus = CliBatchStageStatus.Succeeded;
                resolvedSpecPath = compileResult.SpecOutputPath;
                StageCompiledSpecDependencies(resolvedSpecPath, resolvedScriptPath, workspace.DirectoryPath);
            }
            else
            {
                failureStage = CliBatchFailureStage.Run;
                compiledSpecPath = ToLogicalPath(artifactBaseDirectory, resolvedSpecPath);
            }

            failureStage = CliBatchFailureStage.Run;
            var runResult = _pipelineOrchestrator.Run(new CliRunRequest
            {
                SpecPath = resolvedSpecPath,
                OutputPath = resolvedOutputPath,
                FrameIndex = job.FrameIndex
            });

            runStatus = runResult.Success
                ? CliBatchStageStatus.Succeeded
                : CliBatchStageStatus.Failed;

            var failureSummary = runResult.Success ? string.Empty : runResult.Message;
            return new AttemptExecutionOutcome(
                new CliBatchJobAttemptRecord
                {
                    AttemptNumber = attemptNumber,
                    RetryLimit = indexedJob.RetryLimit,
                    Success = runResult.Success,
                    CompileStatus = compileStatus,
                    RunStatus = runStatus,
                    FinalStatus = runResult.Success ? CliBatchJobStatus.Succeeded : CliBatchJobStatus.Failed,
                    FailureStage = runResult.Success ? CliBatchFailureStage.None : CliBatchFailureStage.Run,
                    Message = runResult.Message,
                    FailureSummary = failureSummary,
                    ScriptPath = logicalScriptPath,
                    OutputPath = logicalOutputPath,
                    CompiledSpecPath = compiledSpecPath,
                    ReportOutputPath = reportOutputPath,
                    CompileDeterministicKey = compileDeterministicKey,
                    ExportManifestPath = ToLogicalPath(artifactBaseDirectory, runResult.ExportManifestPath),
                    ExportDeterministicKey = runResult.ExportDeterministicKey,
                    PlayableMediaPath = ToLogicalPath(artifactBaseDirectory, runResult.PlayableMediaPath),
                    PlayableMediaDeterministicKey = runResult.PlayableMediaDeterministicKey,
                    DeterministicKey = BuildAttemptDeterministicKey(
                        compileDeterministicKey,
                        runResult.DeterministicKey)
                },
                runResult);
        }
        catch (Exception ex)
        {
            return new AttemptExecutionOutcome(
                new CliBatchJobAttemptRecord
                {
                    AttemptNumber = attemptNumber,
                    RetryLimit = indexedJob.RetryLimit,
                    Success = false,
                    CompileStatus = failureStage == CliBatchFailureStage.Compile
                        ? CliBatchStageStatus.Failed
                        : compileStatus,
                    RunStatus = failureStage == CliBatchFailureStage.Run
                        ? CliBatchStageStatus.Failed
                        : runStatus,
                    FinalStatus = CliBatchJobStatus.Failed,
                    FailureStage = failureStage,
                    Message = ex.Message,
                    FailureSummary = ex.Message,
                    ScriptPath = logicalScriptPath,
                    OutputPath = logicalOutputPath,
                    CompiledSpecPath = compiledSpecPath,
                    ReportOutputPath = reportOutputPath,
                    CompileDeterministicKey = compileDeterministicKey,
                    DeterministicKey = BuildAttemptDeterministicKey(
                        compileDeterministicKey,
                        $"{failureStage}:{ex.Message}")
                },
                null);
        }
    }

    private static CliBatchJobManifest BuildJobManifest(IndexedJob indexedJob, IReadOnlyList<CliBatchJobAttemptRecord> attempts)
    {
        var finalAttempt = attempts[^1];
        return new CliBatchJobManifest
        {
            JobId = indexedJob.Job.JobId,
            SequenceNumber = indexedJob.Index,
            RetryLimit = indexedJob.RetryLimit,
            AttemptCount = attempts.Count,
            Success = finalAttempt.Success,
            FinalStatus = finalAttempt.FinalStatus,
            FailureStage = finalAttempt.FailureStage,
            Message = finalAttempt.Message,
            FailureSummary = finalAttempt.FailureSummary,
            ScriptPath = finalAttempt.ScriptPath,
            OutputPath = finalAttempt.OutputPath,
            CompiledSpecPath = finalAttempt.CompiledSpecPath,
            ReportOutputPath = finalAttempt.ReportOutputPath,
            ExportManifestPath = finalAttempt.ExportManifestPath,
            ExportDeterministicKey = finalAttempt.ExportDeterministicKey,
            PlayableMediaPath = finalAttempt.PlayableMediaPath,
            PlayableMediaDeterministicKey = finalAttempt.PlayableMediaDeterministicKey,
            DeterministicKey = BuildJobManifestDeterministicKey(indexedJob, attempts),
            Attempts = attempts
        };
    }

    private static CliBatchJobResult BuildJobResult(
        string artifactBaseDirectory,
        IndexedJob indexedJob,
        JobWorkspace workspace,
        CliBatchJobManifest manifest,
        AttemptExecutionOutcome finalOutcome)
    {
        var runResult = finalOutcome.RunResult;
        return new CliBatchJobResult
        {
            JobId = indexedJob.Job.JobId,
            SequenceNumber = indexedJob.Index,
            RetryLimit = indexedJob.RetryLimit,
            AttemptCount = manifest.AttemptCount,
            ManifestPath = ToLogicalPath(artifactBaseDirectory, workspace.JobManifestPath),
            CompiledSpecPath = manifest.CompiledSpecPath,
            ReportOutputPath = manifest.ReportOutputPath,
            SpecPath = manifest.CompiledSpecPath,
            OutputPath = indexedJob.Job.OutputPath.Replace('\\', '/'),
            FrameIndex = indexedJob.Job.FrameIndex,
            FirstFrameIndex = runResult?.FirstFrameIndex ?? 0,
            LastFrameIndex = runResult?.LastFrameIndex ?? 0,
            PlannedFrameCount = runResult?.PlannedFrameCount ?? 0,
            RenderedFrameCount = runResult?.RenderedFrameCount ?? 0,
            ProjectDurationSeconds = runResult?.ProjectDurationSeconds ?? 0,
            Success = manifest.Success,
            FinalStatus = manifest.FinalStatus,
            FailureStage = manifest.FailureStage,
            Message = manifest.Message,
            FailureSummary = manifest.FailureSummary,
            ExportStatus = runResult?.ExportStatus ?? (manifest.FailureStage == CliBatchFailureStage.Compile ? "not-run" : "failed"),
            ExportPackageRootPath = ToLogicalPath(artifactBaseDirectory, runResult?.ExportPackageRootPath ?? string.Empty),
            ExportManifestPath = manifest.ExportManifestPath,
            DeterministicKey = manifest.DeterministicKey,
            ExportDeterministicKey = manifest.ExportDeterministicKey,
            PlayableMediaPath = manifest.PlayableMediaPath,
            PlayableMediaStatus = runResult?.PlayableMediaStatus ?? string.Empty,
            PlayableMediaDeterministicKey = manifest.PlayableMediaDeterministicKey,
            PlayableMediaByteCount = runResult?.PlayableMediaByteCount ?? 0,
            PlayableMediaAudioStatus = runResult?.PlayableMediaAudioStatus ?? string.Empty,
            PlayableMediaAudioCueCount = runResult?.PlayableMediaAudioCueCount ?? 0
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
            SequenceNumber = 0,
            RetryLimit = 0,
            AttemptCount = 0,
            ManifestPath = string.Empty,
            CompiledSpecPath = request.ManifestPath.Replace('\\', '/'),
            ReportOutputPath = string.Empty,
            SpecPath = request.ManifestPath.Replace('\\', '/'),
            OutputPath = string.Empty,
            Success = false,
            FinalStatus = CliBatchJobStatus.Invalid,
            FailureStage = CliBatchFailureStage.Manifest,
            Message = message,
            FailureSummary = message,
            ExportStatus = "not-run",
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
                $"{job.SequenceNumber}:{job.JobId}:{job.RetryLimit}:{job.AttemptCount}:{job.ManifestPath}:{job.CompiledSpecPath}:{job.ReportOutputPath}:{job.OutputPath}:{FormatFrameIndex(job.FrameIndex)}:{job.FirstFrameIndex}:{job.LastFrameIndex}:{job.PlannedFrameCount}:{job.RenderedFrameCount}:{FormatDouble(job.ProjectDurationSeconds)}:{job.Success}:{job.FinalStatus}:{job.FailureStage}:{job.Message}:{job.FailureSummary}:{job.ExportStatus}:{job.ExportPackageRootPath}:{job.ExportManifestPath}:{job.PlayableMediaPath}:{job.PlayableMediaStatus}:{job.PlayableMediaDeterministicKey}:{job.PlayableMediaByteCount}:{job.PlayableMediaAudioStatus}:{job.PlayableMediaAudioCueCount}:{job.DeterministicKey}:{job.ExportDeterministicKey}"));
    }

    private static IReadOnlyList<CliBatchJobAttemptRecord> MarkFinalAttempt(IReadOnlyList<CliBatchJobAttemptRecord> attempts)
    {
        return attempts
            .Select((attempt, index) => attempt with
            {
                FinalAttempt = index == attempts.Count - 1
            })
            .ToList();
    }

    private static bool CanRetry(CliBatchFailureStage failureStage, int attemptNumber, int retryLimit)
    {
        return attemptNumber <= retryLimit
            && (failureStage == CliBatchFailureStage.Compile || failureStage == CliBatchFailureStage.Run);
    }

    private static string BuildAttemptDeterministicKey(string compileDeterministicKey, string tail)
    {
        if (string.IsNullOrWhiteSpace(compileDeterministicKey))
        {
            return tail;
        }

        if (string.IsNullOrWhiteSpace(tail))
        {
            return compileDeterministicKey;
        }

        return $"{compileDeterministicKey}|{tail}";
    }

    private static string BuildJobManifestDeterministicKey(
        IndexedJob indexedJob,
        IReadOnlyList<CliBatchJobAttemptRecord> attempts)
    {
        return string.Join(
            "|",
            new[]
            {
                $"{indexedJob.Index}:{indexedJob.Job.JobId}:{indexedJob.RetryLimit}",
                string.Join(
                    ";",
                    attempts.Select(attempt =>
                        $"{attempt.AttemptNumber}:{attempt.RetryLimit}:{attempt.FinalAttempt}:{attempt.Success}:{attempt.CompileStatus}:{attempt.RunStatus}:{attempt.FinalStatus}:{attempt.FailureStage}:{attempt.Message}:{attempt.FailureSummary}:{attempt.ScriptPath}:{attempt.OutputPath}:{attempt.CompiledSpecPath}:{attempt.ReportOutputPath}:{attempt.CompileDeterministicKey}:{attempt.ExportManifestPath}:{attempt.ExportDeterministicKey}:{attempt.PlayableMediaPath}:{attempt.PlayableMediaDeterministicKey}:{attempt.DeterministicKey}"))
            });
    }

    private static void WriteJson<T>(string path, T value)
    {
        var directoryPath = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var json = JsonSerializer.Serialize(value, ArtifactJsonOptions);
        File.WriteAllText(path, json);
    }

    private static string ResolvePath(string baseDirectory, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

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
            Path.Combine(workspaceDirectory, "job-manifest.json"));
    }

    private static void StageCompiledSpecDependencies(string compiledSpecPath, string scriptInputPath, string workspaceDirectory)
    {
        if (!File.Exists(compiledSpecPath))
        {
            return;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(compiledSpecPath));
        var scriptDirectory = Path.GetDirectoryName(scriptInputPath) ?? Environment.CurrentDirectory;
        var repoRoot = FindRepoRoot(scriptInputPath);

        foreach (var sourcePath in CollectSourcePaths(document.RootElement).Distinct(StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || Path.IsPathRooted(sourcePath))
            {
                continue;
            }

            var normalizedSourcePath = sourcePath.Replace('/', Path.DirectorySeparatorChar);
            var sourceFilePath = ResolveExistingDependencyPath(
                Path.Combine(scriptDirectory, normalizedSourcePath),
                Path.Combine(repoRoot, normalizedSourcePath));

            if (sourceFilePath is null)
            {
                continue;
            }

            var destinationPath = Path.Combine(workspaceDirectory, normalizedSourcePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            File.Copy(sourceFilePath, destinationPath, overwrite: true);
        }
    }

    private static IReadOnlyList<string> CollectSourcePaths(JsonElement element)
    {
        var sourcePaths = new List<string>();
        CollectSourcePaths(element, sourcePaths);
        return sourcePaths;
    }

    private static void CollectSourcePaths(JsonElement element, List<string> sourcePaths)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (string.Equals(property.Name, "sourcePath", StringComparison.OrdinalIgnoreCase)
                        && property.Value.ValueKind == JsonValueKind.String)
                    {
                        var sourcePath = property.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(sourcePath))
                        {
                            sourcePaths.Add(sourcePath);
                        }
                    }
                    else
                    {
                        CollectSourcePaths(property.Value, sourcePaths);
                    }
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    CollectSourcePaths(item, sourcePaths);
                }

                break;
        }
    }

    private static string? ResolveExistingDependencyPath(params string[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
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

    private static string BuildCompileFailureSummary(
        string jobId,
        IReadOnlyList<Whiteboard.Core.Compilation.ScriptCompileDiagnostic> diagnostics)
    {
        if (diagnostics.Count == 0)
        {
            return $"Batch job '{jobId}' compile failed.";
        }

        return string.Join(
            " | ",
            diagnostics.Select(diagnostic => $"{diagnostic.Code}:{diagnostic.Message}"));
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

    private static string FindArtifactBaseDirectory(string firstDirectory, string secondDirectory)
    {
        var first = Path.GetFullPath(firstDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var second = Path.GetFullPath(secondDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (first.StartsWith(second, StringComparison.OrdinalIgnoreCase))
        {
            return second;
        }

        if (second.StartsWith(first, StringComparison.OrdinalIgnoreCase))
        {
            return first;
        }

        var candidate = first;
        while (!string.IsNullOrWhiteSpace(candidate))
        {
            if (second.StartsWith(candidate, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }

            var parent = Directory.GetParent(candidate);
            if (parent is null)
            {
                break;
            }

            candidate = parent.FullName.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        return firstDirectory;
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

    private sealed record IndexedJob(CliBatchJob Job, int Index, int RetryLimit);
    private sealed record JobWorkspace(string DirectoryPath, string CompiledSpecPath, string CompileReportPath, string JobManifestPath);
    private sealed record AttemptExecutionOutcome(CliBatchJobAttemptRecord Attempt, CliRunResult? RunResult);
}
