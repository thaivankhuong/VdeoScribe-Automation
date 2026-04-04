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
    public void Run_ScriptFixture_ReachesCompiledSpecAndExportArtifactsWithoutManualSpecAssembly()
    {
        var manifestPath = CreateTemporaryManifestFromFixture("primary-manifest.json");
        var summaryPath = Path.Combine(Path.GetDirectoryName(manifestPath)!, "summary.json");

        try
        {
            var orchestrator = new BatchPipelineOrchestrator();
            var result = orchestrator.Run(new CliBatchRunRequest
            {
                ManifestPath = manifestPath,
                SummaryOutputPath = summaryPath
            });

            if (!result.Success)
            {
                var assetChecks = string.Join(
                    Environment.NewLine,
                    result.Jobs.Select(job =>
                        $"{job.JobId}: asset-exists={File.Exists(Path.Combine(Path.GetDirectoryName(summaryPath)!, Path.GetDirectoryName(job.SpecPath.Replace('/', Path.DirectorySeparatorChar))!, "assets", "governed", "svg-hero-governed.svg"))}"));
                throw new Xunit.Sdk.XunitException(
                    string.Join(
                        Environment.NewLine,
                        result.Jobs.Select(job => $"{job.JobId}: {job.Message} | spec={job.SpecPath} | export={job.ExportStatus}"))
                    + Environment.NewLine
                    + assetChecks);
            }
            Assert.Equal(new[] { "job-b", "job-a" }, result.Jobs.Select(job => job.JobId).ToArray());
            Assert.All(result.Jobs, job =>
            {
                Assert.True(job.Success);
                Assert.StartsWith("jobs/", job.SpecPath, StringComparison.Ordinal);
                Assert.Contains("compiled-spec.json", job.SpecPath, StringComparison.Ordinal);
                Assert.False(string.IsNullOrWhiteSpace(job.ExportManifestPath));
                Assert.True(File.Exists(Path.Combine(Path.GetDirectoryName(summaryPath)!, job.SpecPath.Replace('/', Path.DirectorySeparatorChar))));
                Assert.True(File.Exists(Path.Combine(Path.GetDirectoryName(summaryPath)!, job.ExportManifestPath.Replace('/', Path.DirectorySeparatorChar))));
            });
            Assert.True(File.Exists(Path.Combine(Path.GetDirectoryName(summaryPath)!, "jobs", "000-job-b", "compile-report.json")));
            Assert.True(File.Exists(summaryPath));
        }
        finally
        {
            DeleteDirectory(Path.GetDirectoryName(manifestPath)!);
        }
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
            JsonSerializer.Serialize(new CliBatchManifest { Jobs = remappedJobs }, new JsonSerializerOptions
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

    private static void DeleteDirectory(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }
}
