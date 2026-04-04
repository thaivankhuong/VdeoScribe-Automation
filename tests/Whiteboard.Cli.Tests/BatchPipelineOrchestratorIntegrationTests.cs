using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Whiteboard.Cli.Models;
using Whiteboard.Cli.Services;
using Xunit;

namespace Whiteboard.Cli.Tests;

public sealed class BatchPipelineOrchestratorIntegrationTests
{
    [Fact]
    public void Run_ScriptFixture_WritesDeterministicJobManifestArtifacts()
    {
        var manifestPath = CreateTemporaryManifestFromFixture("primary-manifest.json");
        var summaryPath = Path.Combine(Path.GetDirectoryName(manifestPath)!, "summary.json");

        try
        {
            var result = new BatchPipelineOrchestrator().Run(new CliBatchRunRequest
            {
                ManifestPath = manifestPath,
                SummaryOutputPath = summaryPath
            });

            if (!result.Success)
            {
                throw new Xunit.Sdk.XunitException(
                    string.Join(
                        Environment.NewLine,
                        result.Jobs.Select(job => $"{job.JobId}: {job.Message} | failureSummary={job.FailureSummary} | export={job.ExportStatus}")));
            }

            Assert.Equal(new[] { "job-b", "job-a" }, result.Jobs.Select(job => job.JobId).ToArray());
            Assert.True(File.Exists(summaryPath));

            foreach (var job in result.Jobs)
            {
                Assert.Equal(1, job.AttemptCount);
                Assert.False(string.IsNullOrWhiteSpace(job.ManifestPath));
                Assert.False(string.IsNullOrWhiteSpace(job.CompiledSpecPath));
                Assert.False(string.IsNullOrWhiteSpace(job.ReportOutputPath));
                Assert.False(string.IsNullOrWhiteSpace(job.ExportManifestPath));
                Assert.False(string.IsNullOrWhiteSpace(job.ExportDeterministicKey));

                var manifest = ReadJobManifest(summaryPath, job.ManifestPath);
                Assert.Equal(job.JobId, manifest.JobId);
                Assert.Equal(job.AttemptCount, manifest.AttemptCount);
                Assert.Equal(job.CompiledSpecPath, manifest.CompiledSpecPath);
                Assert.Equal(job.ReportOutputPath, manifest.ReportOutputPath);
                Assert.Equal(job.ExportManifestPath, manifest.ExportManifestPath);
                Assert.Equal(job.ExportDeterministicKey, manifest.ExportDeterministicKey);
                Assert.Equal(job.PlayableMediaPath, manifest.PlayableMediaPath);
                Assert.Equal(job.PlayableMediaDeterministicKey, manifest.PlayableMediaDeterministicKey);
                Assert.Single(manifest.Attempts);
                Assert.True(manifest.Attempts[0].FinalAttempt);
                Assert.Equal(CliBatchStageStatus.Succeeded, manifest.Attempts[0].CompileStatus);
                Assert.Equal(CliBatchStageStatus.Succeeded, manifest.Attempts[0].RunStatus);
                Assert.True(File.Exists(Path.Combine(Path.GetDirectoryName(summaryPath)!, job.ManifestPath.Replace('/', Path.DirectorySeparatorChar))));
                Assert.True(File.Exists(Path.Combine(Path.GetDirectoryName(summaryPath)!, job.CompiledSpecPath.Replace('/', Path.DirectorySeparatorChar))));
                Assert.True(File.Exists(Path.Combine(Path.GetDirectoryName(summaryPath)!, job.ReportOutputPath.Replace('/', Path.DirectorySeparatorChar))));
                Assert.True(File.Exists(Path.Combine(Path.GetDirectoryName(summaryPath)!, job.ExportManifestPath.Replace('/', Path.DirectorySeparatorChar))));
            }
        }
        finally
        {
            DeleteDirectory(Path.GetDirectoryName(manifestPath)!);
        }
    }

    [Fact]
    public void Run_ScriptFixture_RepeatedRunsProduceByteEquivalentSummaryAndJobManifestArtifacts()
    {
        var firstManifestPath = CreateTemporaryManifestFromFixture("primary-manifest.json");
        var secondManifestPath = CreateTemporaryManifestFromFixture("primary-manifest.json");
        var firstSummaryPath = Path.Combine(Path.GetDirectoryName(firstManifestPath)!, "summary.json");
        var secondSummaryPath = Path.Combine(Path.GetDirectoryName(secondManifestPath)!, "summary.json");

        try
        {
            var orchestrator = new BatchPipelineOrchestrator();
            var first = orchestrator.Run(new CliBatchRunRequest
            {
                ManifestPath = firstManifestPath,
                SummaryOutputPath = firstSummaryPath
            });
            var second = orchestrator.Run(new CliBatchRunRequest
            {
                ManifestPath = secondManifestPath,
                SummaryOutputPath = secondSummaryPath
            });

            Assert.True(first.Success);
            Assert.True(second.Success);
            Assert.Equal(first.DeterministicKey, second.DeterministicKey);
            Assert.Equal(File.ReadAllText(firstSummaryPath), File.ReadAllText(secondSummaryPath));

            for (var index = 0; index < first.Jobs.Count; index++)
            {
                var firstJob = first.Jobs[index];
                var secondJob = second.Jobs[index];
                Assert.Equal(firstJob.JobId, secondJob.JobId);
                Assert.Equal(firstJob.ManifestPath, secondJob.ManifestPath);
                Assert.Equal(firstJob.CompiledSpecPath, secondJob.CompiledSpecPath);
                Assert.Equal(firstJob.ReportOutputPath, secondJob.ReportOutputPath);
                Assert.Equal(firstJob.ExportManifestPath, secondJob.ExportManifestPath);
                Assert.Equal(firstJob.ExportDeterministicKey, secondJob.ExportDeterministicKey);
                Assert.Equal(firstJob.PlayableMediaPath, secondJob.PlayableMediaPath);
                Assert.Equal(firstJob.PlayableMediaDeterministicKey, secondJob.PlayableMediaDeterministicKey);

                var firstJobManifestPath = Path.Combine(Path.GetDirectoryName(firstSummaryPath)!, firstJob.ManifestPath.Replace('/', Path.DirectorySeparatorChar));
                var secondJobManifestPath = Path.Combine(Path.GetDirectoryName(secondSummaryPath)!, secondJob.ManifestPath.Replace('/', Path.DirectorySeparatorChar));
                Assert.Equal(File.ReadAllText(firstJobManifestPath), File.ReadAllText(secondJobManifestPath));
            }
        }
        finally
        {
            DeleteDirectory(Path.GetDirectoryName(firstManifestPath)!);
            DeleteDirectory(Path.GetDirectoryName(secondManifestPath)!);
        }
    }

    [Fact]
    public void Run_ReorderedFixture_PreservesDeclaredManifestOrder()
    {
        var manifestPath = CreateTemporaryManifestFromFixture("equivalent-reordered-manifest.json");
        var summaryPath = Path.Combine(Path.GetDirectoryName(manifestPath)!, "summary.json");

        try
        {
            var result = new BatchPipelineOrchestrator().Run(new CliBatchRunRequest
            {
                ManifestPath = manifestPath,
                SummaryOutputPath = summaryPath
            });

            if (!result.Success)
            {
                throw new Xunit.Sdk.XunitException(
                    string.Join(
                        Environment.NewLine,
                        result.Jobs.Select(job => $"{job.JobId}: {job.Message} | failureSummary={job.FailureSummary} | export={job.ExportStatus}")));
            }

            Assert.Equal(new[] { "job-a", "job-b" }, result.Jobs.Select(job => job.JobId).ToArray());
            Assert.Equal("jobs/000-job-a/job-manifest.json", result.Jobs[0].ManifestPath);
            Assert.Equal("jobs/001-job-b/job-manifest.json", result.Jobs[1].ManifestPath);
        }
        finally
        {
            DeleteDirectory(Path.GetDirectoryName(manifestPath)!);
        }
    }

    [Fact]
    public void Run_DeterministicQaGate_WithGeneratedBaselines_PassesAndWritesGateReports()
    {
        var manifestPath = CreateTemporaryManifestFromFixture("primary-manifest.json");
        var directoryPath = Path.GetDirectoryName(manifestPath)!;
        var firstSummaryPath = Path.Combine(directoryPath, "summary-initial.json");
        var gatedSummaryPath = Path.Combine(directoryPath, "summary-gated.json");

        try
        {
            var orchestrator = new BatchPipelineOrchestrator();
            var initial = orchestrator.Run(new CliBatchRunRequest
            {
                ManifestPath = manifestPath,
                SummaryOutputPath = firstSummaryPath
            });
            Assert.True(initial.Success);

            var baselineFilesByJob = WriteRegressionBaselinesFromResult(directoryPath, initial);
            var gatedManifestPath = WriteGatedManifest(manifestPath, baselineFilesByJob);

            var gated = orchestrator.Run(new CliBatchRunRequest
            {
                ManifestPath = gatedManifestPath,
                SummaryOutputPath = gatedSummaryPath
            });

            Assert.True(gated.Success);
            Assert.Equal(new[] { "job-b", "job-a" }, gated.Jobs.Select(job => job.JobId).ToArray());
            Assert.All(gated.Jobs, job =>
            {
                Assert.Equal(CliBatchStageStatus.Succeeded, job.GateStatus);
                Assert.False(string.IsNullOrWhiteSpace(job.GateReportPath));
                var gateReportPath = Path.Combine(Path.GetDirectoryName(gatedSummaryPath)!, job.GateReportPath.Replace('/', Path.DirectorySeparatorChar));
                Assert.True(File.Exists(gateReportPath));
                Assert.Contains("\"success\": true", File.ReadAllText(gateReportPath), StringComparison.Ordinal);
            });
        }
        finally
        {
            DeleteDirectory(directoryPath);
        }
    }

    [Fact]
    public void Run_DeterministicQaGate_DriftBlocksBatchSuccess()
    {
        var manifestPath = CreateTemporaryManifestFromFixture("primary-manifest.json");
        var directoryPath = Path.GetDirectoryName(manifestPath)!;
        var firstSummaryPath = Path.Combine(directoryPath, "summary-initial.json");
        var gatedSummaryPath = Path.Combine(directoryPath, "summary-gated-drift.json");

        try
        {
            var orchestrator = new BatchPipelineOrchestrator();
            var initial = orchestrator.Run(new CliBatchRunRequest
            {
                ManifestPath = manifestPath,
                SummaryOutputPath = firstSummaryPath
            });
            Assert.True(initial.Success);

            var baselineFilesByJob = WriteRegressionBaselinesFromResult(directoryPath, initial);
            var driftBaselineFile = baselineFilesByJob["job-b"];
            var driftBaselinePath = Path.Combine(directoryPath, driftBaselineFile);
            var driftBaseline = JsonSerializer.Deserialize<RegressionBaseline>(
                File.ReadAllText(driftBaselinePath),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            driftBaseline = driftBaseline with { ExpectedFrameCount = driftBaseline.ExpectedFrameCount + 1 };
            File.WriteAllText(
                driftBaselinePath,
                JsonSerializer.Serialize(driftBaseline, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                }));

            var gatedManifestPath = WriteGatedManifest(manifestPath, baselineFilesByJob);
            var gated = orchestrator.Run(new CliBatchRunRequest
            {
                ManifestPath = gatedManifestPath,
                SummaryOutputPath = gatedSummaryPath
            });

            Assert.False(gated.Success);
            var driftJob = Assert.Single(gated.Jobs, job => job.JobId == "job-b");
            Assert.Equal(CliBatchFailureStage.Gate, driftJob.FailureStage);
            Assert.Equal(CliBatchStageStatus.Failed, driftJob.GateStatus);
            Assert.Contains("qa.frame-count", driftJob.FailureSummary, StringComparison.Ordinal);
            var unchangedJob = Assert.Single(gated.Jobs, job => job.JobId == "job-a");
            Assert.Equal(CliBatchStageStatus.Succeeded, unchangedJob.GateStatus);
        }
        finally
        {
            DeleteDirectory(directoryPath);
        }
    }

    [Fact]
    public void Run_DeterministicQaGate_RepeatedRunsProduceByteEquivalentSummaryManifestAndGateReports()
    {
        var firstManifestPath = CreateTemporaryManifestFromFixture("primary-manifest.json");
        var secondManifestPath = CreateTemporaryManifestFromFixture("primary-manifest.json");
        var firstDirectoryPath = Path.GetDirectoryName(firstManifestPath)!;
        var secondDirectoryPath = Path.GetDirectoryName(secondManifestPath)!;
        var firstSummaryPath = Path.Combine(firstDirectoryPath, "summary-gated.json");
        var secondSummaryPath = Path.Combine(secondDirectoryPath, "summary-gated.json");

        try
        {
            var first = RunGatedWithGeneratedBaselines(firstManifestPath, firstSummaryPath);
            var second = RunGatedWithGeneratedBaselines(secondManifestPath, secondSummaryPath);

            Assert.True(first.Success);
            Assert.True(second.Success);
            Assert.Equal(first.DeterministicKey, second.DeterministicKey);
            Assert.Equal(File.ReadAllText(firstSummaryPath), File.ReadAllText(secondSummaryPath));

            for (var index = 0; index < first.Jobs.Count; index++)
            {
                var firstJob = first.Jobs[index];
                var secondJob = second.Jobs[index];

                Assert.Equal(firstJob.JobId, secondJob.JobId);
                Assert.Equal(firstJob.ManifestPath, secondJob.ManifestPath);
                Assert.Equal(firstJob.GateReportPath, secondJob.GateReportPath);
                Assert.Equal(firstJob.GateDeterministicKey, secondJob.GateDeterministicKey);

                var firstJobManifestPath = Path.Combine(firstDirectoryPath, firstJob.ManifestPath.Replace('/', Path.DirectorySeparatorChar));
                var secondJobManifestPath = Path.Combine(secondDirectoryPath, secondJob.ManifestPath.Replace('/', Path.DirectorySeparatorChar));
                Assert.Equal(File.ReadAllText(firstJobManifestPath), File.ReadAllText(secondJobManifestPath));

                var firstGateReportPath = Path.Combine(firstDirectoryPath, firstJob.GateReportPath.Replace('/', Path.DirectorySeparatorChar));
                var secondGateReportPath = Path.Combine(secondDirectoryPath, secondJob.GateReportPath.Replace('/', Path.DirectorySeparatorChar));
                Assert.Equal(File.ReadAllText(firstGateReportPath), File.ReadAllText(secondGateReportPath));
            }
        }
        finally
        {
            DeleteDirectory(firstDirectoryPath);
            DeleteDirectory(secondDirectoryPath);
        }
    }

    private static CliBatchJobManifest ReadJobManifest(string summaryPath, string manifestPath)
    {
        var fullPath = Path.Combine(Path.GetDirectoryName(summaryPath)!, manifestPath.Replace('/', Path.DirectorySeparatorChar));
        return JsonSerializer.Deserialize<CliBatchJobManifest>(File.ReadAllText(fullPath), new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Job manifest could not be deserialized.");
    }

    private static string CreateTemporaryManifestFromFixture(string manifestFileName)
    {
        var fixtureDirectory = ResolveFixtureDirectory();
        var manifest = JsonSerializer.Deserialize<CliBatchManifest>(
            File.ReadAllText(Path.Combine(fixtureDirectory, manifestFileName)),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidOperationException("Fixture manifest could not be deserialized.");

        var outputDirectory = Path.Combine(Path.GetTempPath(), "whiteboard-phase19-batch-integration", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDirectory);

        var remappedJobs = manifest.Jobs
            .Select(job => job with
            {
                ScriptPath = Path.Combine(fixtureDirectory, job.ScriptPath),
                OutputPath = Path.Combine("out", job.JobId)
            })
            .ToArray();

        var manifestPath = Path.Combine(outputDirectory, manifestFileName);
        File.WriteAllText(
            manifestPath,
            JsonSerializer.Serialize(new CliBatchManifest
            {
                RetryLimit = manifest.RetryLimit,
                Jobs = remappedJobs
            }, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            }));

        return manifestPath;
    }

    private static string ResolveFixtureDirectory()
    {
        var baseDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        var candidateRoots = new List<DirectoryInfo>();

        for (var current = baseDirectory; current is not null; current = current.Parent)
        {
            candidateRoots.Add(current);
        }

        foreach (var candidate in candidateRoots)
        {
            var fixtureDirectory = Path.Combine(
                candidate.FullName,
                "tests",
                "Whiteboard.Cli.Tests",
                "Fixtures",
                "phase19-batch-automation");

            if (Directory.Exists(fixtureDirectory))
            {
                return fixtureDirectory;
            }
        }

        throw new DirectoryNotFoundException("Phase 19 batch fixture directory was not found.");
    }

    private static Dictionary<string, string> WriteRegressionBaselinesFromResult(string outputDirectory, CliBatchRunResult result)
    {
        var baselineFilesByJob = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var job in result.Jobs)
        {
            var baselineFileName = $"{job.JobId}-regression-baseline.json";
            baselineFilesByJob[job.JobId] = baselineFileName;
            var baselinePath = Path.Combine(outputDirectory, baselineFileName);
            var baseline = new RegressionBaseline
            {
                ExpectedProjectId = job.ProjectId,
                ExpectedFrameCount = job.ExportedFrameCount,
                ExpectedAudioCueCount = job.ExportedAudioCueCount,
                ExpectedTotalDurationSeconds = job.ProjectDurationSeconds,
                AnchorArtifactDeterministicKeys = []
            };
            File.WriteAllText(
                baselinePath,
                JsonSerializer.Serialize(baseline, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                }));
        }

        return baselineFilesByJob;
    }

    private static string WriteGatedManifest(string originalManifestPath, IReadOnlyDictionary<string, string> baselineFilesByJob)
    {
        var original = JsonSerializer.Deserialize<CliBatchManifest>(
            File.ReadAllText(originalManifestPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        var gatedManifest = new CliBatchManifest
        {
            RetryLimit = original.RetryLimit,
            EnforceDeterministicQaGates = true,
            DefaultRegressionBaselinePath = string.Empty,
            Jobs = original.Jobs
                .Select(job => job with
                {
                    RegressionBaselinePath = baselineFilesByJob[job.JobId]
                })
                .ToArray()
        };

        var gatedManifestPath = Path.Combine(Path.GetDirectoryName(originalManifestPath)!, "manifest-gated.json");
        File.WriteAllText(
            gatedManifestPath,
            JsonSerializer.Serialize(gatedManifest, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            }));
        return gatedManifestPath;
    }

    private static CliBatchRunResult RunGatedWithGeneratedBaselines(string manifestPath, string gatedSummaryPath)
    {
        var directoryPath = Path.GetDirectoryName(manifestPath)!;
        var initialSummaryPath = Path.Combine(directoryPath, "summary-initial.json");
        var orchestrator = new BatchPipelineOrchestrator();
        var initial = orchestrator.Run(new CliBatchRunRequest
        {
            ManifestPath = manifestPath,
            SummaryOutputPath = initialSummaryPath
        });
        Assert.True(initial.Success);

        var baselineFilesByJob = WriteRegressionBaselinesFromResult(directoryPath, initial);
        var gatedManifestPath = WriteGatedManifest(manifestPath, baselineFilesByJob);
        return orchestrator.Run(new CliBatchRunRequest
        {
            ManifestPath = gatedManifestPath,
            SummaryOutputPath = gatedSummaryPath
        });
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

    private static void DeleteDirectory(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }
}
