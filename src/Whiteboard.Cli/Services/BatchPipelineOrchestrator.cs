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
                .Select((job, index) => NormalizeJob(
                    job,
                    index,
                    manifest.RetryLimit,
                    manifest.DefaultRegressionBaselinePath,
                    manifest.EnforceDeterministicQaGates))
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

    private static IndexedJob NormalizeJob(
        CliBatchJob job,
        int index,
        int defaultRetryLimit,
        string defaultRegressionBaselinePath,
        bool enforceDeterministicQaGates)
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

        var normalizedDefaultBaselinePath = defaultRegressionBaselinePath?.Trim() ?? string.Empty;
        var normalizedBaselinePath = job.RegressionBaselinePath?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedBaselinePath))
        {
            normalizedBaselinePath = normalizedDefaultBaselinePath;
        }

        if (enforceDeterministicQaGates && string.IsNullOrWhiteSpace(normalizedBaselinePath))
        {
            throw new InvalidDataException(
                $"Batch job '{normalizedJobId}' is missing 'regressionBaselinePath' while deterministic QA gates are required.");
        }

        return new IndexedJob(
            job with
            {
                JobId = normalizedJobId,
                ScriptPath = normalizedScriptPath,
                SpecPath = normalizedSpecPath,
                OutputPath = normalizedOutputPath,
                RegressionBaselinePath = normalizedBaselinePath,
                RetryLimit = effectiveRetryLimit,
                FrameIndex = job.FrameIndex.HasValue
                    ? Math.Max(0, job.FrameIndex.Value)
                    : null
            },
            index,
            effectiveRetryLimit,
            normalizedBaselinePath,
            enforceDeterministicQaGates);
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
        var logicalBaselinePath = ToLogicalPath(artifactBaseDirectory, ResolvePath(manifestDirectory, indexedJob.RegressionBaselinePath));
        var logicalOutputPath = job.OutputPath.Replace('\\', '/');
        var compileStatus = string.IsNullOrWhiteSpace(job.ScriptPath)
            ? CliBatchStageStatus.NotRun
            : CliBatchStageStatus.Failed;
        var runStatus = CliBatchStageStatus.NotRun;
        var gateStatus = CliBatchStageStatus.NotRun;
        var failureStage = string.IsNullOrWhiteSpace(job.ScriptPath)
            ? CliBatchFailureStage.Run
            : CliBatchFailureStage.Compile;
        var compiledSpecPath = string.Empty;
        var reportOutputPath = string.Empty;
        var compileDeterministicKey = string.Empty;
        var gateReportPath = string.Empty;
        var gateDeterministicKey = string.Empty;

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
                            RegressionBaselinePath = logicalBaselinePath,
                            OutputPath = logicalOutputPath,
                            CompiledSpecPath = compiledSpecPath,
                            ReportOutputPath = reportOutputPath,
                            CompileDeterministicKey = compileDeterministicKey,
                            GateStatus = gateStatus,
                            GateReportPath = gateReportPath,
                            GateDeterministicKey = gateDeterministicKey,
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
            var attemptSuccess = runResult.Success;
            var attemptFailureStage = runResult.Success ? CliBatchFailureStage.None : CliBatchFailureStage.Run;
            var attemptMessage = runResult.Message;
            var failureSummary = runResult.Success ? string.Empty : runResult.Message;

            if (runResult.Success && !string.IsNullOrWhiteSpace(indexedJob.RegressionBaselinePath))
            {
                failureStage = CliBatchFailureStage.Gate;
                gateReportPath = ToLogicalPath(artifactBaseDirectory, workspace.GateReportPath);
                var gateEvaluation = EvaluateDeterministicQaGate(
                    manifestDirectory,
                    artifactBaseDirectory,
                    indexedJob,
                    runResult,
                    workspace.GateReportPath);
                gateStatus = gateEvaluation.Success
                    ? CliBatchStageStatus.Succeeded
                    : CliBatchStageStatus.Failed;
                gateDeterministicKey = gateEvaluation.DeterministicKey;

                if (!gateEvaluation.Success)
                {
                    attemptSuccess = false;
                    attemptFailureStage = CliBatchFailureStage.Gate;
                    attemptMessage = gateEvaluation.Message;
                    failureSummary = gateEvaluation.FailureSummary;
                }
            }

            var deterministicTail = string.Join(
                "|",
                new[] { runResult.DeterministicKey, gateDeterministicKey }
                    .Where(value => !string.IsNullOrWhiteSpace(value)));

            return new AttemptExecutionOutcome(
                new CliBatchJobAttemptRecord
                {
                    AttemptNumber = attemptNumber,
                    RetryLimit = indexedJob.RetryLimit,
                    Success = attemptSuccess,
                    CompileStatus = compileStatus,
                    RunStatus = runStatus,
                    FinalStatus = attemptSuccess ? CliBatchJobStatus.Succeeded : CliBatchJobStatus.Failed,
                    FailureStage = attemptFailureStage,
                    Message = attemptMessage,
                    FailureSummary = failureSummary,
                    ScriptPath = logicalScriptPath,
                    RegressionBaselinePath = logicalBaselinePath,
                    OutputPath = logicalOutputPath,
                    CompiledSpecPath = compiledSpecPath,
                    ReportOutputPath = reportOutputPath,
                    CompileDeterministicKey = compileDeterministicKey,
                    GateStatus = gateStatus,
                    GateReportPath = gateReportPath,
                    GateDeterministicKey = gateDeterministicKey,
                    ExportManifestPath = ToLogicalPath(artifactBaseDirectory, runResult.ExportManifestPath),
                    ExportDeterministicKey = runResult.ExportDeterministicKey,
                    PlayableMediaPath = ToLogicalPath(artifactBaseDirectory, runResult.PlayableMediaPath),
                    PlayableMediaDeterministicKey = runResult.PlayableMediaDeterministicKey,
                    DeterministicKey = BuildAttemptDeterministicKey(compileDeterministicKey, deterministicTail)
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
                    RegressionBaselinePath = logicalBaselinePath,
                    OutputPath = logicalOutputPath,
                    CompiledSpecPath = compiledSpecPath,
                    ReportOutputPath = reportOutputPath,
                    CompileDeterministicKey = compileDeterministicKey,
                    GateStatus = gateStatus,
                    GateReportPath = gateReportPath,
                    GateDeterministicKey = gateDeterministicKey,
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
            RegressionBaselinePath = finalAttempt.RegressionBaselinePath,
            OutputPath = finalAttempt.OutputPath,
            CompiledSpecPath = finalAttempt.CompiledSpecPath,
            ReportOutputPath = finalAttempt.ReportOutputPath,
            GateStatus = finalAttempt.GateStatus,
            GateReportPath = finalAttempt.GateReportPath,
            GateDeterministicKey = finalAttempt.GateDeterministicKey,
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
            RegressionBaselinePath = manifest.RegressionBaselinePath,
            CompiledSpecPath = manifest.CompiledSpecPath,
            ReportOutputPath = manifest.ReportOutputPath,
            GateStatus = manifest.GateStatus,
            GateReportPath = manifest.GateReportPath,
            GateDeterministicKey = manifest.GateDeterministicKey,
            SpecPath = manifest.CompiledSpecPath,
            OutputPath = indexedJob.Job.OutputPath.Replace('\\', '/'),
            FrameIndex = indexedJob.Job.FrameIndex,
            FirstFrameIndex = runResult?.FirstFrameIndex ?? 0,
            LastFrameIndex = runResult?.LastFrameIndex ?? 0,
            PlannedFrameCount = runResult?.PlannedFrameCount ?? 0,
            RenderedFrameCount = runResult?.RenderedFrameCount ?? 0,
            ProjectDurationSeconds = runResult?.ProjectDurationSeconds ?? 0,
            ProjectId = runResult?.ExportSummary.ProjectId ?? string.Empty,
            ExportedFrameCount = runResult?.ExportedFrameCount ?? 0,
            ExportedAudioCueCount = runResult?.ExportedAudioCueCount ?? 0,
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
                $"{job.SequenceNumber}:{job.JobId}:{job.RetryLimit}:{job.AttemptCount}:{job.ManifestPath}:{job.RegressionBaselinePath}:{job.CompiledSpecPath}:{job.ReportOutputPath}:{job.GateStatus}:{job.GateReportPath}:{job.GateDeterministicKey}:{job.OutputPath}:{FormatFrameIndex(job.FrameIndex)}:{job.FirstFrameIndex}:{job.LastFrameIndex}:{job.PlannedFrameCount}:{job.RenderedFrameCount}:{FormatDouble(job.ProjectDurationSeconds)}:{job.ProjectId}:{job.ExportedFrameCount}:{job.ExportedAudioCueCount}:{job.Success}:{job.FinalStatus}:{job.FailureStage}:{job.Message}:{job.FailureSummary}:{job.ExportStatus}:{job.ExportPackageRootPath}:{job.ExportManifestPath}:{job.PlayableMediaPath}:{job.PlayableMediaStatus}:{job.PlayableMediaDeterministicKey}:{job.PlayableMediaByteCount}:{job.PlayableMediaAudioStatus}:{job.PlayableMediaAudioCueCount}:{job.DeterministicKey}:{job.ExportDeterministicKey}"));
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
                        $"{attempt.AttemptNumber}:{attempt.RetryLimit}:{attempt.FinalAttempt}:{attempt.Success}:{attempt.CompileStatus}:{attempt.RunStatus}:{attempt.GateStatus}:{attempt.FinalStatus}:{attempt.FailureStage}:{attempt.Message}:{attempt.FailureSummary}:{attempt.ScriptPath}:{attempt.RegressionBaselinePath}:{attempt.OutputPath}:{attempt.CompiledSpecPath}:{attempt.ReportOutputPath}:{attempt.GateReportPath}:{attempt.CompileDeterministicKey}:{attempt.GateDeterministicKey}:{attempt.ExportManifestPath}:{attempt.ExportDeterministicKey}:{attempt.PlayableMediaPath}:{attempt.PlayableMediaDeterministicKey}:{attempt.DeterministicKey}"))
            });
    }

    private static QaGateEvaluation EvaluateDeterministicQaGate(
        string manifestDirectory,
        string artifactBaseDirectory,
        IndexedJob indexedJob,
        CliRunResult runResult,
        string gateReportOutputPath)
    {
        var resolvedBaselinePath = ResolvePath(manifestDirectory, indexedJob.RegressionBaselinePath);
        var logicalBaselinePath = ToLogicalPath(artifactBaseDirectory, resolvedBaselinePath);

        var checks = new List<QaGateCheck>();

        if (!File.Exists(resolvedBaselinePath))
        {
            checks.Add(new QaGateCheck(
                "qa.baseline.missing",
                false,
                logicalBaselinePath,
                "<missing>",
                "Regression baseline file was not found."));
        }
        else
        {
            try
            {
                var baseline = JsonSerializer.Deserialize<RegressionBaseline>(
                    File.ReadAllText(resolvedBaselinePath),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (baseline is null)
                {
                    checks.Add(new QaGateCheck(
                        "qa.baseline.invalid",
                        false,
                        logicalBaselinePath,
                        "<null>",
                        "Regression baseline file could not be deserialized."));
                }
                else
                {
                    checks.Add(new QaGateCheck(
                        "qa.project-id",
                        string.Equals(runResult.ExportSummary.ProjectId, baseline.ExpectedProjectId, StringComparison.Ordinal),
                        baseline.ExpectedProjectId,
                        runResult.ExportSummary.ProjectId,
                        "ExportSummary.ProjectId must match baseline."));
                    checks.Add(new QaGateCheck(
                        "qa.frame-count",
                        runResult.ExportedFrameCount == baseline.ExpectedFrameCount,
                        baseline.ExpectedFrameCount.ToString(CultureInfo.InvariantCulture),
                        runResult.ExportedFrameCount.ToString(CultureInfo.InvariantCulture),
                        "ExportedFrameCount must match baseline."));
                    checks.Add(new QaGateCheck(
                        "qa.audio-cue-count",
                        runResult.ExportedAudioCueCount == baseline.ExpectedAudioCueCount,
                        baseline.ExpectedAudioCueCount.ToString(CultureInfo.InvariantCulture),
                        runResult.ExportedAudioCueCount.ToString(CultureInfo.InvariantCulture),
                        "ExportedAudioCueCount must match baseline."));

                    var durationDelta = Math.Abs(runResult.ExportSummary.TotalDurationSeconds - baseline.ExpectedTotalDurationSeconds);
                    checks.Add(new QaGateCheck(
                        "qa.total-duration-seconds",
                        durationDelta <= 1e-6,
                        FormatDouble(baseline.ExpectedTotalDurationSeconds),
                        FormatDouble(runResult.ExportSummary.TotalDurationSeconds),
                        "ExportSummary.TotalDurationSeconds must match baseline."));

                    foreach (var anchor in baseline.AnchorArtifactDeterministicKeys)
                    {
                        var exportedFrame = runResult.ExportFrames.FirstOrDefault(frame => frame.FrameIndex == anchor.FrameIndex);
                        if (exportedFrame is null)
                        {
                            checks.Add(new QaGateCheck(
                                "qa.anchor.frame.missing",
                                false,
                                $"{anchor.FrameIndex}:{anchor.ArtifactDeterministicKey}",
                                "<missing-frame>",
                                "Anchor frame was not exported."));
                            continue;
                        }

                        checks.Add(new QaGateCheck(
                            "qa.anchor.key",
                            string.Equals(exportedFrame.ArtifactDeterministicKey, anchor.ArtifactDeterministicKey, StringComparison.Ordinal),
                            $"{anchor.FrameIndex}:{anchor.ArtifactDeterministicKey}",
                            $"{anchor.FrameIndex}:{exportedFrame.ArtifactDeterministicKey}",
                            "Anchor deterministic key must match baseline."));
                    }
                }
            }
            catch (Exception ex)
            {
                checks.Add(new QaGateCheck(
                    "qa.baseline.invalid",
                    false,
                    logicalBaselinePath,
                    "<error>",
                    ex.Message));
            }
        }

        var success = checks.All(check => check.Success);
        var failedChecks = checks.Where(check => !check.Success).ToList();
        var failureSummary = failedChecks.Count == 0
            ? string.Empty
            : string.Join(" | ", failedChecks.Select(check => $"{check.Code}:{check.Message}"));
        var deterministicKey = string.Join(
            "|",
            new[] { logicalBaselinePath }.Concat(
                checks.Select(check => $"{check.Code}:{check.Success}:{check.Expected}:{check.Actual}:{check.Message}")));
        var report = new QaGateReport
        {
            JobId = indexedJob.Job.JobId,
            SequenceNumber = indexedJob.Index,
            BaselinePath = logicalBaselinePath,
            Success = success,
            FailureSummary = failureSummary,
            DeterministicKey = deterministicKey,
            Checks = checks
        };
        WriteJson(gateReportOutputPath, report);

        var message = success
            ? $"Deterministic QA gate passed for batch job '{indexedJob.Job.JobId}'."
            : $"Deterministic QA gate failed for batch job '{indexedJob.Job.JobId}'.";

        return new QaGateEvaluation(success, failureSummary, message, deterministicKey);
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
            Path.Combine(workspaceDirectory, "qa-gate-report.json"),
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

    private sealed record QaGateEvaluation(
        bool Success,
        string FailureSummary,
        string Message,
        string DeterministicKey);

    private sealed record QaGateCheck(
        string Code,
        bool Success,
        string Expected,
        string Actual,
        string Message);

    private sealed record QaGateReport
    {
        public string JobId { get; init; } = string.Empty;
        public int SequenceNumber { get; init; }
        public string BaselinePath { get; init; } = string.Empty;
        public bool Success { get; init; }
        public string FailureSummary { get; init; } = string.Empty;
        public string DeterministicKey { get; init; } = string.Empty;
        public IReadOnlyList<QaGateCheck> Checks { get; init; } = [];
    }

    private sealed record RegressionBaseline
    {
        public string ExpectedProjectId { get; init; } = string.Empty;
        public int ExpectedFrameCount { get; init; }
        public int ExpectedAudioCueCount { get; init; }
        public double ExpectedTotalDurationSeconds { get; init; }
        public IReadOnlyList<RegressionAnchor> AnchorArtifactDeterministicKeys { get; init; } = [];
    }

    private sealed record RegressionAnchor
    {
        public int FrameIndex { get; init; }
        public string ArtifactDeterministicKey { get; init; } = string.Empty;
    }

    private sealed record IndexedJob(
        CliBatchJob Job,
        int Index,
        int RetryLimit,
        string RegressionBaselinePath,
        bool EnforceDeterministicQaGates);

    private sealed record JobWorkspace(
        string DirectoryPath,
        string CompiledSpecPath,
        string CompileReportPath,
        string GateReportPath,
        string JobManifestPath);

    private sealed record AttemptExecutionOutcome(CliBatchJobAttemptRecord Attempt, CliRunResult? RunResult);
}
