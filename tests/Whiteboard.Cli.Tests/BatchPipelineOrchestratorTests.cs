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
using Xunit;

namespace Whiteboard.Cli.Tests;

public sealed class BatchPipelineOrchestratorTests
{
    [Fact]
    public void Run_ScriptedJobs_ExecuteInDeclaredOrderThroughCompileThenRenderExport()
    {
        var manifestPath = CreateManifestFile(
            new CliBatchJob { JobId = "job-b", ScriptPath = "job-b-script.json", OutputPath = "out/job-b.mp4" },
            new CliBatchJob { JobId = "job-a", ScriptPath = "job-a-script.json", OutputPath = "out/job-a.mp4" });
        var summaryPath = Path.Combine(Path.GetDirectoryName(manifestPath)!, "summary.json");
        var scriptCompiler = new RecordingScriptCompilationOrchestrator();
        var pipeline = new RecordingPipelineOrchestrator();

        try
        {
            var orchestrator = new BatchPipelineOrchestrator(pipeline, scriptCompiler);
            var result = orchestrator.Run(new CliBatchRunRequest
            {
                ManifestPath = manifestPath,
                SummaryOutputPath = summaryPath
            });

            Assert.True(result.Success);
            Assert.Equal(new[] { "job-b", "job-a" }, result.Jobs.Select(job => job.JobId).ToArray());
            Assert.Equal(
                new[]
                {
                    "job-b-script.json",
                    "job-a-script.json"
                },
                scriptCompiler.Requests.Select(request => Path.GetFileName(request.InputPath)).ToArray());
            Assert.Equal(
                new[]
                {
                    "jobs/000-job-b/compiled-spec.json",
                    "jobs/001-job-a/compiled-spec.json"
                },
                result.Jobs.Select(job => job.SpecPath).ToArray());
            Assert.Equal(
                new[]
                {
                    Path.GetFullPath(Path.Combine(Path.GetDirectoryName(summaryPath)!, "jobs", "000-job-b", "compiled-spec.json")),
                    Path.GetFullPath(Path.Combine(Path.GetDirectoryName(summaryPath)!, "jobs", "001-job-a", "compiled-spec.json"))
                },
                pipeline.Requests.Select(request => request.SpecPath).ToArray());
            Assert.All(pipeline.Requests, request => Assert.Null(request.FrameIndex));
            Assert.True(File.Exists(summaryPath));
        }
        finally
        {
            DeleteManifestFile(manifestPath);
        }
    }

    [Fact]
    public void Run_CompileFailure_SkipsRenderAndExportForThatJobButContinuesDeclaredOrder()
    {
        var manifestPath = CreateManifestFile(
            new CliBatchJob { JobId = "job-b", ScriptPath = "job-b-script.json", OutputPath = "out/job-b.mp4" },
            new CliBatchJob { JobId = "job-a", ScriptPath = "job-a-script.json", OutputPath = "out/job-a.mp4" });
        var summaryPath = Path.Combine(Path.GetDirectoryName(manifestPath)!, "summary.json");
        var scriptCompiler = new RecordingScriptCompilationOrchestrator(failingScriptFileName: "job-b-script.json");
        var pipeline = new RecordingPipelineOrchestrator();

        try
        {
            var orchestrator = new BatchPipelineOrchestrator(pipeline, scriptCompiler);
            var result = orchestrator.Run(new CliBatchRunRequest
            {
                ManifestPath = manifestPath,
                SummaryOutputPath = summaryPath
            });

            Assert.False(result.Success);
            Assert.Equal(2, result.JobCount);
            Assert.Equal(1, result.SuccessCount);
            Assert.Equal(1, result.FailureCount);
            Assert.Equal(new[] { "job-b", "job-a" }, result.Jobs.Select(job => job.JobId).ToArray());
            Assert.False(result.Jobs[0].Success);
            Assert.Contains("script.compile.failed", result.Jobs[0].Message, StringComparison.Ordinal);
            Assert.Equal("not-run", result.Jobs[0].ExportStatus);
            Assert.True(File.Exists(Path.Combine(Path.GetDirectoryName(summaryPath)!, "jobs", "000-job-b", "compile-report.json")));
            Assert.True(result.Jobs[1].Success);
            Assert.Single(pipeline.Requests);
            Assert.Equal(
                Path.GetFullPath(Path.Combine(Path.GetDirectoryName(summaryPath)!, "jobs", "001-job-a", "compiled-spec.json")),
                pipeline.Requests[0].SpecPath);
            Assert.Equal("out/job-a.mp4", result.Jobs[1].OutputPath);
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
            new CliBatchJob { JobId = "job-a", ScriptPath = "job-a-script.json", OutputPath = "out/a.mp4" },
            new CliBatchJob { JobId = "job-a", ScriptPath = "job-b-script.json", OutputPath = "out/b.mp4" });
        var summaryPath = Path.Combine(Path.GetDirectoryName(manifestPath)!, "summary.json");

        try
        {
            var orchestrator = new BatchPipelineOrchestrator(new RecordingPipelineOrchestrator(), new RecordingScriptCompilationOrchestrator());
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

    private sealed class RecordingScriptCompilationOrchestrator : IScriptCompilationOrchestrator
    {
        private readonly string? _failingScriptFileName;

        public RecordingScriptCompilationOrchestrator(string? failingScriptFileName = null)
        {
            _failingScriptFileName = failingScriptFileName;
        }

        public List<CliScriptCompileCommandRequest> Requests { get; } = [];

        public CliScriptCompileCommandResult Compile(CliScriptCompileCommandRequest request)
        {
            Requests.Add(request);
            Directory.CreateDirectory(Path.GetDirectoryName(request.ReportOutputPath)!);

            if (string.Equals(Path.GetFileName(request.InputPath), _failingScriptFileName, StringComparison.Ordinal))
            {
                File.WriteAllText(request.ReportOutputPath, "compile failed");
                return new CliScriptCompileCommandResult
                {
                    Success = false,
                    ScriptId = Path.GetFileNameWithoutExtension(request.InputPath),
                    SpecOutputPath = request.SpecOutputPath,
                    ReportOutputPath = request.ReportOutputPath,
                    DeterministicKey = "compile-failed",
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

            Directory.CreateDirectory(Path.GetDirectoryName(request.SpecOutputPath)!);
            File.WriteAllText(request.SpecOutputPath, "{}");
            File.WriteAllText(request.ReportOutputPath, "compile succeeded");

            return new CliScriptCompileCommandResult
            {
                Success = true,
                ScriptId = Path.GetFileNameWithoutExtension(request.InputPath),
                SpecOutputPath = request.SpecOutputPath,
                ReportOutputPath = request.ReportOutputPath,
                DeterministicKey = $"compile:{Path.GetFileNameWithoutExtension(request.InputPath)}"
            };
        }
    }

    private sealed class RecordingPipelineOrchestrator : IPipelineOrchestrator
    {
        public List<CliRunRequest> Requests { get; } = [];

        public CliRunResult Run(CliRunRequest request)
        {
            Requests.Add(request);

            var specName = Path.GetFileNameWithoutExtension(request.SpecPath);
            var outputPath = Path.GetFullPath(request.OutputPath ?? Path.Combine(Path.GetTempPath(), $"{specName}.mp4"));
            var packageRootPath = Path.Combine(Path.GetDirectoryName(outputPath)!, Path.GetFileNameWithoutExtension(outputPath));
            var manifestPath = Path.Combine(packageRootPath, "frame-manifest.json");
            Directory.CreateDirectory(packageRootPath);
            File.WriteAllText(manifestPath, $"manifest:{specName}");

            return new CliRunResult
            {
                Success = true,
                Message = $"Rendered and exported {specName}.",
                SpecPath = request.SpecPath,
                FrameIndex = request.FrameIndex,
                FirstFrameIndex = 0,
                LastFrameIndex = 149,
                PlannedFrameCount = 150,
                RenderedFrameCount = 150,
                ProjectDurationSeconds = 5,
                OutputPath = outputPath,
                ExportPackageRootPath = packageRootPath,
                ExportManifestPath = manifestPath,
                ExportStatus = "ok",
                ExportDeterministicKey = $"export:{specName}",
                DeterministicKey = $"run:{specName}"
            };
        }
    }
}
