using System;
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

    private readonly IPipelineOrchestrator _pipelineOrchestrator;

    public BatchPipelineOrchestrator(IPipelineOrchestrator? pipelineOrchestrator = null)
    {
        _pipelineOrchestrator = pipelineOrchestrator ?? new PipelineOrchestrator();
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

        CliBatchRunResult result;

        try
        {
            var manifest = LoadManifest(manifestPath);
            var orderedJobs = manifest.Jobs
                .Select((job, index) => NormalizeJob(job, index))
                .OrderBy(job => job.Job.JobId, StringComparer.Ordinal)
                .ToList();

            EnsureUniqueJobIds(orderedJobs);

            var jobResults = orderedJobs
                .Select(orderedJob => ExecuteJob(manifestDirectory, orderedJob.Job))
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

        var normalizedSpecPath = job.SpecPath?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedSpecPath))
        {
            throw new InvalidDataException($"Batch job '{normalizedJobId}' is missing 'specPath'.");
        }

        var normalizedOutputPath = job.OutputPath?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedOutputPath))
        {
            throw new InvalidDataException($"Batch job '{normalizedJobId}' is missing 'outputPath'.");
        }

        return new IndexedJob(job with
        {
            JobId = normalizedJobId,
            SpecPath = normalizedSpecPath,
            OutputPath = normalizedOutputPath,
            FrameIndex = job.FrameIndex < 0 ? 0 : job.FrameIndex
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

    private CliBatchJobResult ExecuteJob(string manifestDirectory, CliBatchJob job)
    {
        try
        {
            var resolvedSpecPath = ResolvePath(manifestDirectory, job.SpecPath);
            var resolvedOutputPath = ResolvePath(manifestDirectory, job.OutputPath);
            var result = _pipelineOrchestrator.Run(new CliRunRequest
            {
                SpecPath = resolvedSpecPath,
                OutputPath = resolvedOutputPath,
                FrameIndex = job.FrameIndex
            });

            return new CliBatchJobResult
            {
                JobId = job.JobId,
                SpecPath = job.SpecPath,
                OutputPath = job.OutputPath,
                FrameIndex = job.FrameIndex,
                Success = result.Success,
                Message = result.Message,
                DeterministicKey = result.DeterministicKey,
                ExportDeterministicKey = result.ExportDeterministicKey
            };
        }
        catch (Exception ex)
        {
            return new CliBatchJobResult
            {
                JobId = job.JobId,
                SpecPath = job.SpecPath,
                OutputPath = job.OutputPath,
                FrameIndex = job.FrameIndex,
                Success = false,
                Message = ex.Message,
                DeterministicKey = $"job-failed:{job.JobId}:{ex.Message}",
                ExportDeterministicKey = string.Empty
            };
        }
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
            FrameIndex = 0,
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
                $"{job.JobId}:{job.SpecPath}:{job.OutputPath}:{job.FrameIndex}:{job.Success}:{job.DeterministicKey}:{job.ExportDeterministicKey}"));
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

    private sealed record IndexedJob(CliBatchJob Job, int Index);
}

