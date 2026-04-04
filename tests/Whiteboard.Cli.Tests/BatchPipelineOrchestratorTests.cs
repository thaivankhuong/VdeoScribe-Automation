using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Whiteboard.Cli.Contracts;
using Whiteboard.Cli.Models;
using Whiteboard.Cli.Services;
using Whiteboard.Core.Compilation;
using Whiteboard.Core.Validation;
using Whiteboard.Export.Models;
using Xunit;

namespace Whiteboard.Cli.Tests;

public sealed class BatchPipelineOrchestratorTests
{
    [Fact]
    public void Run_NoRetryCompileFailure_WritesSingleAttemptManifestAndSkipsRenderExport()
    {
        var manifestPath = CreateManifestFile(
            retryLimit: 0,
            new CliBatchJob { JobId = "job-a", ScriptPath = "job-a-script.json", OutputPath = "out/job-a.mp4", RetryLimit = 0 });
        var summaryPath = Path.Combine(Path.GetDirectoryName(manifestPath)!, "summary.json");
        var scriptCompiler = new RecordingScriptCompilationOrchestrator(new Dictionary<string, Queue<CompileBehavior>>
        {
            ["job-a-script.json"] = new Queue<CompileBehavior>([CompileBehavior.Fail("compile failed once")])
        });
        var pipeline = new RecordingPipelineOrchestrator();

        try
        {
            var result = new BatchPipelineOrchestrator(pipeline, scriptCompiler).Run(new CliBatchRunRequest
            {
                ManifestPath = manifestPath,
                SummaryOutputPath = summaryPath
            });

            Assert.False(result.Success);
            var job = Assert.Single(result.Jobs);
            Assert.Equal(1, job.AttemptCount);
            Assert.Equal(CliBatchJobStatus.Failed, job.FinalStatus);
            Assert.Equal(CliBatchFailureStage.Compile, job.FailureStage);
            Assert.Equal("jobs/000-job-a/job-manifest.json", job.ManifestPath);
            Assert.Empty(pipeline.Requests);
            Assert.Single(scriptCompiler.Requests);

            var manifest = ReadJobManifest(summaryPath, job.ManifestPath);
            Assert.Equal(1, manifest.AttemptCount);
            Assert.Single(manifest.Attempts);
            Assert.True(manifest.Attempts[0].FinalAttempt);
            Assert.Equal(CliBatchStageStatus.Failed, manifest.Attempts[0].CompileStatus);
            Assert.Equal(CliBatchStageStatus.NotRun, manifest.Attempts[0].RunStatus);
            Assert.Equal("jobs/000-job-a/compile-report.json", manifest.ReportOutputPath);
            Assert.Equal(string.Empty, manifest.ExportManifestPath);
            Assert.Equal(string.Empty, manifest.PlayableMediaPath);
        }
        finally
        {
            DeleteManifestFile(manifestPath);
        }
    }

    [Fact]
    public void Run_RetryEnabledCompileFailure_AppendsSecondAttemptAndSucceeds()
    {
        var manifestPath = CreateManifestFile(
            retryLimit: 0,
            new CliBatchJob { JobId = "job-a", ScriptPath = "job-a-script.json", OutputPath = "out/job-a.mp4", RetryLimit = 1 });
        var summaryPath = Path.Combine(Path.GetDirectoryName(manifestPath)!, "summary.json");
        var scriptCompiler = new RecordingScriptCompilationOrchestrator(new Dictionary<string, Queue<CompileBehavior>>
        {
            ["job-a-script.json"] = new Queue<CompileBehavior>([CompileBehavior.Fail("compile failed once"), CompileBehavior.Succeed()])
        });
        var pipeline = new RecordingPipelineOrchestrator(new Dictionary<string, Queue<RunBehavior>>
        {
            ["job-a"] = new Queue<RunBehavior>([RunBehavior.Succeed()])
        });

        try
        {
            var result = new BatchPipelineOrchestrator(pipeline, scriptCompiler).Run(new CliBatchRunRequest
            {
                ManifestPath = manifestPath,
                SummaryOutputPath = summaryPath
            });

            Assert.True(result.Success);
            var job = Assert.Single(result.Jobs);
            Assert.Equal(2, job.AttemptCount);
            Assert.Equal(CliBatchJobStatus.Succeeded, job.FinalStatus);
            Assert.Equal(CliBatchFailureStage.None, job.FailureStage);
            Assert.Equal(2, scriptCompiler.Requests.Count);
            Assert.Single(pipeline.Requests);

            var manifest = ReadJobManifest(summaryPath, job.ManifestPath);
            Assert.Equal(2, manifest.Attempts.Count);
            Assert.False(manifest.Attempts[0].FinalAttempt);
            Assert.True(manifest.Attempts[1].FinalAttempt);
            Assert.Equal(CliBatchStageStatus.Failed, manifest.Attempts[0].CompileStatus);
            Assert.Equal(CliBatchStageStatus.Succeeded, manifest.Attempts[1].CompileStatus);
            Assert.Equal(CliBatchStageStatus.Succeeded, manifest.Attempts[1].RunStatus);
            Assert.Equal("out/job-a/frame-manifest.json", manifest.ExportManifestPath);
            Assert.Equal("export:job-a:1", manifest.ExportDeterministicKey);
        }
        finally
        {
            DeleteManifestFile(manifestPath);
        }
    }

    [Fact]
    public void Run_RetryEnabledRunFailure_AppendsSecondAttemptAndPreservesWitnessFields()
    {
        var manifestPath = CreateManifestFile(
            retryLimit: 0,
            new CliBatchJob { JobId = "job-a", ScriptPath = "job-a-script.json", OutputPath = "out/job-a.mp4", RetryLimit = 1 });
        var summaryPath = Path.Combine(Path.GetDirectoryName(manifestPath)!, "summary.json");
        var scriptCompiler = new RecordingScriptCompilationOrchestrator(new Dictionary<string, Queue<CompileBehavior>>
        {
            ["job-a-script.json"] = new Queue<CompileBehavior>([CompileBehavior.Succeed(), CompileBehavior.Succeed()])
        });
        var pipeline = new RecordingPipelineOrchestrator(new Dictionary<string, Queue<RunBehavior>>
        {
            ["job-a"] = new Queue<RunBehavior>([RunBehavior.Fail("run failed once"), RunBehavior.Succeed()])
        });

        try
        {
            var result = new BatchPipelineOrchestrator(pipeline, scriptCompiler).Run(new CliBatchRunRequest
            {
                ManifestPath = manifestPath,
                SummaryOutputPath = summaryPath
            });

            Assert.True(result.Success);
            var job = Assert.Single(result.Jobs);
            Assert.Equal(2, job.AttemptCount);
            Assert.Equal(CliBatchJobStatus.Succeeded, job.FinalStatus);
            Assert.Equal(CliBatchFailureStage.None, job.FailureStage);
            Assert.Equal("out/job-a/frame-manifest.json", job.ExportManifestPath);
            Assert.Equal("export:job-a:2", job.ExportDeterministicKey);
            Assert.Equal("out/job-a/playable.mp4", job.PlayableMediaPath);
            Assert.Equal("media:job-a:2", job.PlayableMediaDeterministicKey);

            var manifest = ReadJobManifest(summaryPath, job.ManifestPath);
            Assert.Equal(2, manifest.Attempts.Count);
            Assert.Equal(CliBatchStageStatus.Failed, manifest.Attempts[0].RunStatus);
            Assert.Equal(CliBatchFailureStage.Run, manifest.Attempts[0].FailureStage);
            Assert.Equal(CliBatchStageStatus.Succeeded, manifest.Attempts[1].RunStatus);
            Assert.Equal("out/job-a/frame-manifest.json", manifest.ExportManifestPath);
            Assert.Equal("out/job-a/playable.mp4", manifest.PlayableMediaPath);
        }
        finally
        {
            DeleteManifestFile(manifestPath);
        }
    }

    [Fact]
    public void Run_AggregateSummary_PreservesOrderedFailureSummariesAndWitnessFields()
    {
        var manifestPath = CreateManifestFile(
            retryLimit: 0,
            new CliBatchJob { JobId = "job-b", ScriptPath = "job-b-script.json", OutputPath = "out/job-b.mp4", RetryLimit = 0 },
            new CliBatchJob { JobId = "job-a", ScriptPath = "job-a-script.json", OutputPath = "out/job-a.mp4", RetryLimit = 0 });
        var summaryPath = Path.Combine(Path.GetDirectoryName(manifestPath)!, "summary.json");
        var scriptCompiler = new RecordingScriptCompilationOrchestrator(new Dictionary<string, Queue<CompileBehavior>>
        {
            ["job-b-script.json"] = new Queue<CompileBehavior>([CompileBehavior.Fail("compile failed once")]),
            ["job-a-script.json"] = new Queue<CompileBehavior>([CompileBehavior.Succeed()])
        });
        var pipeline = new RecordingPipelineOrchestrator(new Dictionary<string, Queue<RunBehavior>>
        {
            ["job-a"] = new Queue<RunBehavior>([RunBehavior.Succeed()])
        });

        try
        {
            var result = new BatchPipelineOrchestrator(pipeline, scriptCompiler).Run(new CliBatchRunRequest
            {
                ManifestPath = manifestPath,
                SummaryOutputPath = summaryPath
            });

            Assert.False(result.Success);
            Assert.Equal(new[] { "job-b", "job-a" }, result.Jobs.Select(job => job.JobId).ToArray());
            Assert.Contains("script.compile.failed", result.Jobs[0].FailureSummary, StringComparison.Ordinal);
            Assert.Equal("jobs/001-job-a/job-manifest.json", result.Jobs[1].ManifestPath);
            Assert.Equal("jobs/001-job-a/compile-report.json", result.Jobs[1].ReportOutputPath);
            Assert.Equal("out/job-a/frame-manifest.json", result.Jobs[1].ExportManifestPath);
            Assert.Equal("export:job-a:1", result.Jobs[1].ExportDeterministicKey);
            Assert.Equal("out/job-a/playable.mp4", result.Jobs[1].PlayableMediaPath);
            Assert.Equal("media:job-a:1", result.Jobs[1].PlayableMediaDeterministicKey);

            var summaryJson = File.ReadAllText(summaryPath);
            Assert.Contains("\"failureSummary\": \"script.compile.failed:The scripted batch compile failed.\"", summaryJson, StringComparison.Ordinal);
            Assert.Contains("\"exportManifestPath\": \"out/job-a/frame-manifest.json\"", summaryJson, StringComparison.Ordinal);
            Assert.Contains("\"playableMediaDeterministicKey\": \"media:job-a:1\"", summaryJson, StringComparison.Ordinal);
        }
        finally
        {
            DeleteManifestFile(manifestPath);
        }
    }

    [Fact]
    public void Run_NoRetryFixture_CompileFailuresStopAfterSingleAttemptPerJob()
    {
        var manifestPath = CreateManifestFileFromFixture("no-retry-manifest.json");
        var summaryPath = Path.Combine(Path.GetDirectoryName(manifestPath)!, "summary.json");
        var scriptCompiler = new RecordingScriptCompilationOrchestrator(new Dictionary<string, Queue<CompileBehavior>>
        {
            ["job-b-script.json"] = new Queue<CompileBehavior>([CompileBehavior.Fail("compile failed")]),
            ["job-a-script.json"] = new Queue<CompileBehavior>([CompileBehavior.Fail("compile failed")])
        });
        var pipeline = new RecordingPipelineOrchestrator();

        try
        {
            var result = new BatchPipelineOrchestrator(pipeline, scriptCompiler).Run(new CliBatchRunRequest
            {
                ManifestPath = manifestPath,
                SummaryOutputPath = summaryPath
            });

            Assert.False(result.Success);
            Assert.Equal(new[] { "job-b", "job-a" }, result.Jobs.Select(job => job.JobId).ToArray());
            Assert.All(result.Jobs, job => Assert.Equal(1, job.AttemptCount));
            Assert.Equal(2, scriptCompiler.Requests.Count);
            Assert.Empty(pipeline.Requests);
        }
        finally
        {
            DeleteManifestFile(manifestPath);
        }
    }

    [Fact]
    public void Run_RetryFixture_CompileFailuresRespectFixtureRetryLimits()
    {
        var manifestPath = CreateManifestFileFromFixture("retry-manifest.json");
        var summaryPath = Path.Combine(Path.GetDirectoryName(manifestPath)!, "summary.json");
        var scriptCompiler = new RecordingScriptCompilationOrchestrator(new Dictionary<string, Queue<CompileBehavior>>
        {
            ["job-b-script.json"] = new Queue<CompileBehavior>([CompileBehavior.Fail("compile failed"), CompileBehavior.Succeed()]),
            ["job-a-script.json"] = new Queue<CompileBehavior>([CompileBehavior.Fail("compile failed"), CompileBehavior.Fail("compile failed"), CompileBehavior.Succeed()])
        });
        var pipeline = new RecordingPipelineOrchestrator(new Dictionary<string, Queue<RunBehavior>>
        {
            ["job-b"] = new Queue<RunBehavior>([RunBehavior.Succeed()]),
            ["job-a"] = new Queue<RunBehavior>([RunBehavior.Succeed()])
        });

        try
        {
            var result = new BatchPipelineOrchestrator(pipeline, scriptCompiler).Run(new CliBatchRunRequest
            {
                ManifestPath = manifestPath,
                SummaryOutputPath = summaryPath
            });

            Assert.True(result.Success);
            Assert.Equal(new[] { "job-b", "job-a" }, result.Jobs.Select(job => job.JobId).ToArray());
            Assert.Equal(2, result.Jobs[0].AttemptCount);
            Assert.Equal(3, result.Jobs[1].AttemptCount);
            Assert.Equal(5, scriptCompiler.Requests.Count);
            Assert.Equal(2, pipeline.Requests.Count);
        }
        finally
        {
            DeleteManifestFile(manifestPath);
        }
    }

    [Fact]
    public void Run_DeterministicQaGatePass_SucceedsAndWritesGateReport()
    {
        var frameKey = "anchor:job-a:pass";
        var manifestPath = CreateManifestFile(new CliBatchManifest
        {
            RetryLimit = 1,
            EnforceDeterministicQaGates = true,
            DefaultRegressionBaselinePath = "baseline-pass.json",
            Jobs =
            [
                new CliBatchJob
                {
                    JobId = "job-a",
                    ScriptPath = "job-a-script.json",
                    OutputPath = "out/job-a.mp4",
                    RetryLimit = 1
                }
            ]
        });
        var summaryPath = Path.Combine(Path.GetDirectoryName(manifestPath)!, "summary.json");
        WriteRegressionBaseline(
            manifestPath,
            "baseline-pass.json",
            expectedProjectId: "phase19-script-batch",
            expectedFrameCount: 150,
            expectedAudioCueCount: 0,
            expectedTotalDurationSeconds: 5,
            anchors: [(0, frameKey)]);

        var scriptCompiler = new RecordingScriptCompilationOrchestrator(new Dictionary<string, Queue<CompileBehavior>>
        {
            ["job-a-script.json"] = new Queue<CompileBehavior>([CompileBehavior.Succeed()])
        });
        var pipeline = new RecordingPipelineOrchestrator(new Dictionary<string, Queue<RunBehavior>>
        {
            ["job-a"] = new Queue<RunBehavior>([
                RunBehavior.Succeed(
                    projectId: "phase19-script-batch",
                    exportedFrameCount: 150,
                    exportedAudioCueCount: 0,
                    totalDurationSeconds: 5,
                    exportFrames:
                    [
                        new ExportFramePackage
                        {
                            FrameIndex = 0,
                            ArtifactRelativePath = "job-a/frame-000000.svg",
                            ArtifactDeterministicKey = frameKey
                        }
                    ])
            ])
        });

        try
        {
            var result = new BatchPipelineOrchestrator(pipeline, scriptCompiler).Run(new CliBatchRunRequest
            {
                ManifestPath = manifestPath,
                SummaryOutputPath = summaryPath
            });

            Assert.True(result.Success);
            var job = Assert.Single(result.Jobs);
            Assert.Equal(CliBatchFailureStage.None, job.FailureStage);
            Assert.Equal(CliBatchStageStatus.Succeeded, job.GateStatus);
            Assert.NotEmpty(job.GateReportPath);
            Assert.NotEmpty(job.GateDeterministicKey);

            var gateReportPath = Path.Combine(Path.GetDirectoryName(summaryPath)!, job.GateReportPath.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(gateReportPath));
            var gateReportJson = File.ReadAllText(gateReportPath);
            Assert.Contains("\"success\": true", gateReportJson, StringComparison.Ordinal);
        }
        finally
        {
            DeleteManifestFile(manifestPath);
        }
    }

    [Fact]
    public void Run_DeterministicQaGateDrift_BlocksSuccessWithoutRetry()
    {
        var manifestPath = CreateManifestFile(new CliBatchManifest
        {
            RetryLimit = 3,
            EnforceDeterministicQaGates = true,
            DefaultRegressionBaselinePath = "baseline-drift.json",
            Jobs =
            [
                new CliBatchJob
                {
                    JobId = "job-a",
                    ScriptPath = "job-a-script.json",
                    OutputPath = "out/job-a.mp4",
                    RetryLimit = 3
                }
            ]
        });
        var summaryPath = Path.Combine(Path.GetDirectoryName(manifestPath)!, "summary.json");
        WriteRegressionBaseline(
            manifestPath,
            "baseline-drift.json",
            expectedProjectId: "phase19-script-batch",
            expectedFrameCount: 151,
            expectedAudioCueCount: 0,
            expectedTotalDurationSeconds: 5,
            anchors: []);

        var scriptCompiler = new RecordingScriptCompilationOrchestrator(new Dictionary<string, Queue<CompileBehavior>>
        {
            ["job-a-script.json"] = new Queue<CompileBehavior>([CompileBehavior.Succeed()])
        });
        var pipeline = new RecordingPipelineOrchestrator(new Dictionary<string, Queue<RunBehavior>>
        {
            ["job-a"] = new Queue<RunBehavior>([
                RunBehavior.Succeed(
                    projectId: "phase19-script-batch",
                    exportedFrameCount: 150,
                    exportedAudioCueCount: 0,
                    totalDurationSeconds: 5)
            ])
        });

        try
        {
            var result = new BatchPipelineOrchestrator(pipeline, scriptCompiler).Run(new CliBatchRunRequest
            {
                ManifestPath = manifestPath,
                SummaryOutputPath = summaryPath
            });

            Assert.False(result.Success);
            var job = Assert.Single(result.Jobs);
            Assert.Equal(1, job.AttemptCount);
            Assert.Equal(CliBatchFailureStage.Gate, job.FailureStage);
            Assert.Equal(CliBatchStageStatus.Failed, job.GateStatus);
            Assert.Contains("qa.frame-count", job.FailureSummary, StringComparison.Ordinal);
            Assert.Single(pipeline.Requests);
            Assert.Single(scriptCompiler.Requests);
        }
        finally
        {
            DeleteManifestFile(manifestPath);
        }
    }

    [Fact]
    public void Run_WithDuplicateJobId_ProducesDeterministicValidationFailureWithoutAttempts()
    {
        var manifestPath = CreateManifestFile(
            retryLimit: 2,
            new CliBatchJob { JobId = "job-a", ScriptPath = "job-a-script.json", OutputPath = "out/a.mp4" },
            new CliBatchJob { JobId = "job-a", ScriptPath = "job-b-script.json", OutputPath = "out/b.mp4" });
        var summaryPath = Path.Combine(Path.GetDirectoryName(manifestPath)!, "summary.json");

        try
        {
            var result = new BatchPipelineOrchestrator(new RecordingPipelineOrchestrator(), new RecordingScriptCompilationOrchestrator()).Run(new CliBatchRunRequest
            {
                ManifestPath = manifestPath,
                SummaryOutputPath = summaryPath
            });

            Assert.False(result.Success);
            var job = Assert.Single(result.Jobs);
            Assert.Equal(0, job.AttemptCount);
            Assert.Equal(CliBatchJobStatus.Invalid, job.FinalStatus);
            Assert.Equal(CliBatchFailureStage.Manifest, job.FailureStage);
            Assert.Contains("duplicate jobId", job.Message, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(summaryPath));
        }
        finally
        {
            DeleteManifestFile(manifestPath);
        }
    }

    private static string CreateManifestFile(int retryLimit, params CliBatchJob[] jobs)
    {
        return CreateManifestFile(new CliBatchManifest
        {
            RetryLimit = retryLimit,
            Jobs = jobs
        });
    }

    private static string CreateManifestFile(CliBatchManifest manifest)
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), "whiteboard-batch-contract-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directoryPath);

        var manifestPath = Path.Combine(directoryPath, "manifest.json");
        File.WriteAllText(
            manifestPath,
            JsonSerializer.Serialize(manifest, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            }));

        return manifestPath;
    }

    private static string CreateManifestFileFromFixture(string fixtureManifestFileName)
    {
        var fixtureDirectory = ResolveFixtureDirectory();
        var directoryPath = Path.Combine(Path.GetTempPath(), "whiteboard-batch-contract-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directoryPath);

        var manifestPath = Path.Combine(directoryPath, fixtureManifestFileName);
        File.WriteAllText(manifestPath, File.ReadAllText(Path.Combine(fixtureDirectory, fixtureManifestFileName)));
        return manifestPath;
    }

    private static CliBatchJobManifest ReadJobManifest(string summaryPath, string manifestPath)
    {
        var fullPath = Path.Combine(Path.GetDirectoryName(summaryPath)!, manifestPath.Replace('/', Path.DirectorySeparatorChar));
        return JsonSerializer.Deserialize<CliBatchJobManifest>(File.ReadAllText(fullPath), new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Job manifest could not be deserialized.");
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

    private static string ResolveFixtureDirectory()
    {
        var baseDirectory = new DirectoryInfo(AppContext.BaseDirectory);

        for (var current = baseDirectory; current is not null; current = current.Parent)
        {
            var fixtureDirectory = Path.Combine(
                current.FullName,
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

    private static void WriteRegressionBaseline(
        string manifestPath,
        string baselineFileName,
        string expectedProjectId,
        int expectedFrameCount,
        int expectedAudioCueCount,
        double expectedTotalDurationSeconds,
        IReadOnlyList<(int FrameIndex, string ArtifactDeterministicKey)> anchors)
    {
        var baselinePath = Path.Combine(Path.GetDirectoryName(manifestPath)!, baselineFileName);
        var payload = new
        {
            expectedProjectId,
            expectedFrameCount,
            expectedAudioCueCount,
            expectedTotalDurationSeconds,
            anchorArtifactDeterministicKeys = anchors.Select(anchor => new
            {
                frameIndex = anchor.FrameIndex,
                artifactDeterministicKey = anchor.ArtifactDeterministicKey
            }).ToArray()
        };
        File.WriteAllText(
            baselinePath,
            JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            }));
    }

    private sealed class RecordingScriptCompilationOrchestrator : IScriptCompilationOrchestrator
    {
        private readonly Dictionary<string, Queue<CompileBehavior>> _behaviors;

        public RecordingScriptCompilationOrchestrator(Dictionary<string, Queue<CompileBehavior>>? behaviors = null)
        {
            _behaviors = behaviors ?? new Dictionary<string, Queue<CompileBehavior>>(StringComparer.Ordinal);
        }

        public List<CliScriptCompileCommandRequest> Requests { get; } = [];

        public CliScriptCompileCommandResult Compile(CliScriptCompileCommandRequest request)
        {
            Requests.Add(request);
            Directory.CreateDirectory(Path.GetDirectoryName(request.ReportOutputPath)!);
            Directory.CreateDirectory(Path.GetDirectoryName(request.SpecOutputPath)!);

            var scriptFileName = Path.GetFileName(request.InputPath);
            var behavior = DequeueBehavior(scriptFileName);
            File.WriteAllText(request.ReportOutputPath, behavior.Success ? "compile succeeded" : "compile failed");

            if (!behavior.Success)
            {
                return new CliScriptCompileCommandResult
                {
                    Success = false,
                    ScriptId = Path.GetFileNameWithoutExtension(request.InputPath),
                    SpecOutputPath = request.SpecOutputPath,
                    ReportOutputPath = request.ReportOutputPath,
                    DeterministicKey = $"compile:{Path.GetFileNameWithoutExtension(request.InputPath)}:{Requests.Count}",
                    Diagnostics =
                    [
                        new ScriptCompileDiagnostic
                        {
                            Severity = "error",
                            Code = "script.compile.failed",
                            Message = "The scripted batch compile failed.",
                            Path = "$.sections[0]",
                            Gate = "semantic"
                        }
                    ]
                };
            }

            File.WriteAllText(request.SpecOutputPath, "{}");
            return new CliScriptCompileCommandResult
            {
                Success = true,
                ScriptId = Path.GetFileNameWithoutExtension(request.InputPath),
                SpecOutputPath = request.SpecOutputPath,
                ReportOutputPath = request.ReportOutputPath,
                DeterministicKey = $"compile:{Path.GetFileNameWithoutExtension(request.InputPath)}:{Requests.Count}"
            };
        }

        private CompileBehavior DequeueBehavior(string scriptFileName)
        {
            if (_behaviors.TryGetValue(scriptFileName, out var queue) && queue.Count > 0)
            {
                return queue.Dequeue();
            }

            return CompileBehavior.Succeed();
        }
    }

    private sealed class RecordingPipelineOrchestrator : IPipelineOrchestrator
    {
        private readonly Dictionary<string, Queue<RunBehavior>> _behaviors;

        public RecordingPipelineOrchestrator(Dictionary<string, Queue<RunBehavior>>? behaviors = null)
        {
            _behaviors = behaviors ?? new Dictionary<string, Queue<RunBehavior>>(StringComparer.Ordinal);
        }

        public List<CliRunRequest> Requests { get; } = [];

        public CliRunResult Run(CliRunRequest request)
        {
            Requests.Add(request);

            var jobId = ExtractJobId(request.SpecPath);
            var behavior = DequeueBehavior(jobId);
            var outputPath = Path.GetFullPath(request.OutputPath ?? Path.Combine(Path.GetTempPath(), $"{jobId}.mp4"));
            var packageRootPath = Path.Combine(Path.GetDirectoryName(outputPath)!, Path.GetFileNameWithoutExtension(outputPath));
            var exportManifestPath = Path.Combine(packageRootPath, "frame-manifest.json");
            var playableMediaPath = Path.Combine(packageRootPath, "playable.mp4");
            Directory.CreateDirectory(packageRootPath);

            var exportFrames = behavior.Success
                ? (behavior.ExportFrames.Count == 0
                    ? new[]
                    {
                        new ExportFramePackage
                        {
                            FrameIndex = 0,
                            ArtifactRelativePath = $"{jobId}/frame-000000.svg",
                            ArtifactDeterministicKey = $"anchor:{jobId}:{Requests.Count}"
                        }
                    }
                    : behavior.ExportFrames.ToArray())
                : Array.Empty<ExportFramePackage>();

            if (behavior.Success)
            {
                File.WriteAllText(exportManifestPath, $"manifest:{jobId}:{Requests.Count}");
                File.WriteAllText(playableMediaPath, $"playable:{jobId}:{Requests.Count}");
            }

            return new CliRunResult
            {
                Success = behavior.Success,
                Message = behavior.Message,
                SpecPath = request.SpecPath,
                FrameIndex = request.FrameIndex,
                FirstFrameIndex = behavior.Success ? 0 : 0,
                LastFrameIndex = behavior.Success ? 149 : 0,
                PlannedFrameCount = behavior.Success ? behavior.ExportedFrameCount : 0,
                RenderedFrameCount = behavior.Success ? behavior.ExportedFrameCount : 0,
                ProjectDurationSeconds = behavior.Success ? behavior.ExpectedTotalDurationSeconds : 0,
                OutputPath = outputPath,
                ExportPackageRootPath = packageRootPath,
                ExportManifestPath = behavior.Success ? exportManifestPath : string.Empty,
                ExportStatus = behavior.Success ? "ok" : "failed",
                ExportDeterministicKey = behavior.Success ? $"export:{jobId}:{Requests.Count}" : string.Empty,
                PlayableMediaPath = behavior.Success ? playableMediaPath : string.Empty,
                PlayableMediaStatus = behavior.Success ? "encoded" : string.Empty,
                PlayableMediaDeterministicKey = behavior.Success ? $"media:{jobId}:{Requests.Count}" : string.Empty,
                PlayableMediaByteCount = behavior.Success ? new FileInfo(playableMediaPath).Length : 0,
                PlayableMediaAudioStatus = behavior.Success ? "not-requested" : string.Empty,
                PlayableMediaAudioCueCount = 0,
                ExportedFrameCount = behavior.Success ? behavior.ExportedFrameCount : 0,
                ExportedAudioCueCount = behavior.Success ? behavior.ExportedAudioCueCount : 0,
                ExportFrames = exportFrames,
                ExportSummary = new ExportPackageSummary
                {
                    ProjectId = behavior.Success ? behavior.ProjectId : string.Empty,
                    FrameCount = behavior.Success ? behavior.ExportedFrameCount : 0,
                    AudioCueCount = behavior.Success ? behavior.ExportedAudioCueCount : 0,
                    TotalDurationSeconds = behavior.Success ? behavior.ExpectedTotalDurationSeconds : 0
                },
                DeterministicKey = $"run:{jobId}:{Requests.Count}"
            };
        }

        private RunBehavior DequeueBehavior(string jobId)
        {
            if (_behaviors.TryGetValue(jobId, out var queue) && queue.Count > 0)
            {
                return queue.Dequeue();
            }

            return RunBehavior.Succeed();
        }

        private static string ExtractJobId(string specPath)
        {
            var workspaceName = new DirectoryInfo(Path.GetDirectoryName(specPath)!).Name;
            var separator = workspaceName.IndexOf('-');
            return separator >= 0 ? workspaceName[(separator + 1)..] : workspaceName;
        }
    }

    private sealed record CompileBehavior(bool Success, string Message)
    {
        public static CompileBehavior Succeed()
        {
            return new CompileBehavior(true, "compile succeeded");
        }

        public static CompileBehavior Fail(string message)
        {
            return new CompileBehavior(false, message);
        }
    }

    private sealed record RunBehavior(
        bool Success,
        string Message,
        string ProjectId,
        int ExportedFrameCount,
        int ExportedAudioCueCount,
        double ExpectedTotalDurationSeconds,
        IReadOnlyList<ExportFramePackage> ExportFrames)
    {
        public static RunBehavior Succeed(
            string projectId = "phase19-script-batch",
            int exportedFrameCount = 150,
            int exportedAudioCueCount = 0,
            double totalDurationSeconds = 5,
            IReadOnlyList<ExportFramePackage>? exportFrames = null)
        {
            return new RunBehavior(
                true,
                "Rendered and exported successfully.",
                projectId,
                exportedFrameCount,
                exportedAudioCueCount,
                totalDurationSeconds,
                exportFrames ?? []);
        }

        public static RunBehavior Fail(string message)
        {
            return new RunBehavior(false, message, string.Empty, 0, 0, 0, []);
        }
    }
}
