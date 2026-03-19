using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Whiteboard.Cli.Contracts;
using Whiteboard.Cli.Models;
using Whiteboard.Cli.Services;
using Xunit;

namespace Whiteboard.Cli.Tests;

public sealed class BatchPipelineOrchestratorTests
{
    [Fact]
    public void Run_OrdersJobsByCanonicalJobIdAndWritesSummaryArtifact()
    {
        var manifestPath = CreateManifestFile(
            new CliBatchJob { JobId = "job-b", SpecPath = "b.json", OutputPath = "out/b.mp4", FrameIndex = 8 },
            new CliBatchJob { JobId = "job-a", SpecPath = "a.json", OutputPath = "out/a.mp4", FrameIndex = 4 });
        var summaryPath = Path.Combine(Path.GetDirectoryName(manifestPath)!, "summary.json");

        try
        {
            var orchestrator = new BatchPipelineOrchestrator(new FakePipelineOrchestrator());
            var result = orchestrator.Run(new CliBatchRunRequest
            {
                ManifestPath = manifestPath,
                SummaryOutputPath = summaryPath
            });

            Assert.True(result.Success);
            Assert.Equal(new[] { "job-a", "job-b" }, result.Jobs.Select(job => job.JobId).ToArray());
            Assert.True(File.Exists(summaryPath));

            var summary = JsonSerializer.Deserialize<CliBatchRunResult>(File.ReadAllText(summaryPath), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(summary);
            Assert.Equal(result.DeterministicKey, summary!.DeterministicKey);
            Assert.Equal(result.Jobs.Select(job => job.JobId).ToArray(), summary.Jobs.Select(job => job.JobId).ToArray());
        }
        finally
        {
            DeleteManifestFile(manifestPath);
        }
    }

    [Fact]
    public void Run_WithDuplicateJobId_ProducesDeterministicFailureArtifact()
    {
        var manifestPath = CreateManifestFile(
            new CliBatchJob { JobId = "job-a", SpecPath = "a.json", OutputPath = "out/a.mp4", FrameIndex = 1 },
            new CliBatchJob { JobId = "job-a", SpecPath = "b.json", OutputPath = "out/b.mp4", FrameIndex = 2 });
        var summaryPath = Path.Combine(Path.GetDirectoryName(manifestPath)!, "summary.json");

        try
        {
            var orchestrator = new BatchPipelineOrchestrator(new FakePipelineOrchestrator());
            var result = orchestrator.Run(new CliBatchRunRequest
            {
                ManifestPath = manifestPath,
                SummaryOutputPath = summaryPath
            });

            Assert.False(result.Success);
            Assert.Equal(1, result.FailureCount);
            Assert.Single(result.Jobs);
            Assert.Contains("duplicate jobId", result.Jobs[0].Message, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(summaryPath));
        }
        finally
        {
            DeleteManifestFile(manifestPath);
        }
    }

    [Fact]
    public void Run_AggregatesMixedJobFailuresWithoutStoppingBatch()
    {
        var manifestPath = CreateManifestFile(
            new CliBatchJob { JobId = "job-b", SpecPath = "missing.json", OutputPath = "out/b.mp4", FrameIndex = 1 },
            new CliBatchJob { JobId = "job-a", SpecPath = "a.json", OutputPath = "out/a.mp4", FrameIndex = 0 });
        var summaryPath = Path.Combine(Path.GetDirectoryName(manifestPath)!, "summary.json");

        try
        {
            var orchestrator = new BatchPipelineOrchestrator(new FakePipelineOrchestrator());
            var result = orchestrator.Run(new CliBatchRunRequest
            {
                ManifestPath = manifestPath,
                SummaryOutputPath = summaryPath
            });

            Assert.False(result.Success);
            Assert.Equal(2, result.JobCount);
            Assert.Equal(1, result.SuccessCount);
            Assert.Equal(1, result.FailureCount);
            Assert.Equal(new[] { "job-a", "job-b" }, result.Jobs.Select(job => job.JobId).ToArray());
            Assert.True(result.Jobs[0].Success);
            Assert.False(result.Jobs[1].Success);
            Assert.True(File.Exists(summaryPath));
        }
        finally
        {
            DeleteManifestFile(manifestPath);
        }
    }

    private static string CreateManifestFile(params CliBatchJob[] jobs)
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), "whiteboard-batch-contract-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directoryPath);

        var manifestPath = Path.Combine(directoryPath, "manifest.json");
        File.WriteAllText(
            manifestPath,
            JsonSerializer.Serialize(new CliBatchManifest { Jobs = jobs }, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            }));

        return manifestPath;
    }

    private static void DeleteManifestFile(string manifestPath)
    {
        if (!File.Exists(manifestPath))
        {
            return;
        }

        var directoryPath = Path.GetDirectoryName(manifestPath);
        File.Delete(manifestPath);

        if (!string.IsNullOrWhiteSpace(directoryPath) && Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    private sealed class FakePipelineOrchestrator : IPipelineOrchestrator
    {
        public CliRunResult Run(CliRunRequest request)
        {
            if (request.SpecPath.Contains("missing", StringComparison.OrdinalIgnoreCase))
            {
                throw new FileNotFoundException("Spec file was not found.", request.SpecPath);
            }

            var specName = Path.GetFileNameWithoutExtension(request.SpecPath);
            return new CliRunResult
            {
                Success = true,
                Message = $"Processed {specName}.",
                SpecPath = request.SpecPath,
                FrameIndex = request.FrameIndex,
                OutputPath = request.OutputPath ?? string.Empty,
                ExportStatus = "ok",
                ExportDeterministicKey = $"export:{specName}",
                DeterministicKey = $"run:{specName}:{request.FrameIndex}"
            };
        }
    }
}
