using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Whiteboard.Cli.Models;
using Whiteboard.Cli.Services;
using Whiteboard.Export.Contracts;
using Whiteboard.Export.Models;
using Whiteboard.Export.Services;
using Xunit;

namespace Whiteboard.Cli.Tests;

public sealed class ParityWitnessRegressionTests
{
    [Fact]
    public void ParityWitnessRegressionTests_RepeatedRunsProduceByteEquivalentPackages()
    {
        var specPath = ResolveRepoRelativePath("artifacts", "source-parity-demo", "project-engine.json");
        var firstOutputDirectory = CreateTempDirectory();
        var secondOutputDirectory = CreateTempDirectory();
        var firstOutputPath = Path.Combine(firstOutputDirectory, "phase15-first.mp4");
        var secondOutputPath = Path.Combine(secondOutputDirectory, "phase15-second.mp4");
        var previousEnabled = Environment.GetEnvironmentVariable("WHITEBOARD_ENABLE_PLAYABLE_MEDIA");
        var previousFfmpegPath = Environment.GetEnvironmentVariable("WHITEBOARD_FFMPEG_PATH");
        Environment.SetEnvironmentVariable("WHITEBOARD_ENABLE_PLAYABLE_MEDIA", "0");
        Environment.SetEnvironmentVariable("WHITEBOARD_FFMPEG_PATH", null);

        try
        {
            var orchestrator = new PipelineOrchestrator();
            var first = orchestrator.Run(new CliRunRequest
            {
                SpecPath = specPath,
                OutputPath = firstOutputPath
            });
            var second = orchestrator.Run(new CliRunRequest
            {
                SpecPath = specPath,
                OutputPath = secondOutputPath
            });

            Assert.True(first.Success);
            Assert.True(second.Success);
            AssertArtifactPackagesEquivalent(first, second);
        }
        finally
        {
            Environment.SetEnvironmentVariable("WHITEBOARD_ENABLE_PLAYABLE_MEDIA", previousEnabled);
            Environment.SetEnvironmentVariable("WHITEBOARD_FFMPEG_PATH", previousFfmpegPath);
            DeleteDirectory(firstOutputDirectory);
            DeleteDirectory(secondOutputDirectory);
        }
    }

    [Fact]
    public void ParityWitnessRegressionTests_RenderMatchesCommittedBaselineManifestAndAnchorKeys()
    {
        var specPath = ResolveRepoRelativePath("artifacts", "source-parity-demo", "project-engine.json");
        var baselinePath = ResolveRepoRelativePath("artifacts", "source-parity-demo", "check", "phase15-regression-baseline.json");
        var baseline = LoadBaseline(baselinePath);
        var outputDirectory = CreateTempDirectory();
        var outputPath = Path.Combine(outputDirectory, "phase15-baseline.mp4");
        var previousEnabled = Environment.GetEnvironmentVariable("WHITEBOARD_ENABLE_PLAYABLE_MEDIA");
        var previousFfmpegPath = Environment.GetEnvironmentVariable("WHITEBOARD_FFMPEG_PATH");
        Environment.SetEnvironmentVariable("WHITEBOARD_ENABLE_PLAYABLE_MEDIA", "0");
        Environment.SetEnvironmentVariable("WHITEBOARD_FFMPEG_PATH", null);

        try
        {
            var orchestrator = new PipelineOrchestrator();
            var result = orchestrator.Run(new CliRunRequest
            {
                SpecPath = specPath,
                OutputPath = outputPath
            });

            Assert.True(result.Success);
            Assert.Equal(baseline.ExpectedProjectId, result.ExportSummary.ProjectId);
            Assert.Equal(baseline.ExpectedFrameCount, result.ExportedFrameCount);
            Assert.Equal(baseline.ExpectedAudioCueCount, result.ExportedAudioCueCount);
            Assert.Equal(baseline.ExpectedTotalDurationSeconds, result.ExportSummary.TotalDurationSeconds, 6);

            foreach (var expectedAnchor in baseline.AnchorArtifactDeterministicKeys)
            {
                var frame = Assert.Single(result.ExportFrames, entry => entry.FrameIndex == expectedAnchor.FrameIndex);
                Assert.Equal(expectedAnchor.ArtifactDeterministicKey, frame.ArtifactDeterministicKey);
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable("WHITEBOARD_ENABLE_PLAYABLE_MEDIA", previousEnabled);
            Environment.SetEnvironmentVariable("WHITEBOARD_FFMPEG_PATH", previousFfmpegPath);
            DeleteDirectory(outputDirectory);
        }
    }

    [Fact]
    public void ParityWitnessRegressionTests_FakeRunnerPlayableMediaWitnessStaysDeterministicAcrossRepeatedRuns()
    {
        var specPath = ResolveRepoRelativePath("artifacts", "source-parity-demo", "project-engine.json");
        var firstOutputDirectory = CreateTempDirectory();
        var secondOutputDirectory = CreateTempDirectory();
        var firstOutputPath = Path.Combine(firstOutputDirectory, "phase15-first.mp4");
        var secondOutputPath = Path.Combine(secondOutputDirectory, "phase15-second.mp4");
        var firstExecutablePath = Path.Combine(firstOutputDirectory, "ffmpeg.exe");
        var secondExecutablePath = Path.Combine(secondOutputDirectory, "ffmpeg.exe");
        File.WriteAllText(firstExecutablePath, "fake-ffmpeg");
        File.WriteAllText(secondExecutablePath, "fake-ffmpeg");
        var previousEnabled = Environment.GetEnvironmentVariable("WHITEBOARD_ENABLE_PLAYABLE_MEDIA");
        var previousFfmpegPath = Environment.GetEnvironmentVariable("WHITEBOARD_FFMPEG_PATH");

        try
        {
            var orchestrator = new PipelineOrchestrator(exportPipeline: new ExportPipeline(new PlayableMediaEncoder(new DeterministicWitnessProcessRunner())));

            Environment.SetEnvironmentVariable("WHITEBOARD_ENABLE_PLAYABLE_MEDIA", "1");
            Environment.SetEnvironmentVariable("WHITEBOARD_FFMPEG_PATH", firstExecutablePath);
            var first = orchestrator.Run(new CliRunRequest
            {
                SpecPath = specPath,
                OutputPath = firstOutputPath
            });

            Environment.SetEnvironmentVariable("WHITEBOARD_ENABLE_PLAYABLE_MEDIA", "1");
            Environment.SetEnvironmentVariable("WHITEBOARD_FFMPEG_PATH", secondExecutablePath);
            var second = orchestrator.Run(new CliRunRequest
            {
                SpecPath = specPath,
                OutputPath = secondOutputPath
            });

            Assert.True(first.Success);
            Assert.True(second.Success);
            Assert.Equal("encoded", first.PlayableMediaStatus);
            Assert.Equal("video-only", first.PlayableMediaAudioStatus);
            AssertArtifactPackagesEquivalent(first, second);
            AssertPlayableMediaOutputsEquivalent(first, second);
        }
        finally
        {
            Environment.SetEnvironmentVariable("WHITEBOARD_ENABLE_PLAYABLE_MEDIA", previousEnabled);
            Environment.SetEnvironmentVariable("WHITEBOARD_FFMPEG_PATH", previousFfmpegPath);
            DeleteDirectory(firstOutputDirectory);
            DeleteDirectory(secondOutputDirectory);
        }
    }

    private static RegressionBaseline LoadBaseline(string path)
    {
        var baseline = JsonSerializer.Deserialize<RegressionBaseline>(
            File.ReadAllText(path),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        return baseline ?? throw new InvalidOperationException("Regression baseline could not be deserialized.");
    }

    private static void AssertArtifactPackagesEquivalent(CliRunResult expected, CliRunResult actual)
    {
        Assert.Equal(expected.Success, actual.Success);
        Assert.Equal(expected.ExportedFrameCount, actual.ExportedFrameCount);
        Assert.Equal(expected.ExportedAudioCueCount, actual.ExportedAudioCueCount);
        Assert.Equal(expected.ExportSummary.ProjectId, actual.ExportSummary.ProjectId);
        Assert.Equal(expected.ExportSummary.FrameCount, actual.ExportSummary.FrameCount);
        Assert.Equal(expected.ExportSummary.AudioCueCount, actual.ExportSummary.AudioCueCount);
        Assert.Equal(expected.ExportSummary.TotalDurationSeconds, actual.ExportSummary.TotalDurationSeconds, 6);
        Assert.Equal(expected.ExportStatus, actual.ExportStatus);
        Assert.Equal(expected.ExportDeterministicKey, actual.ExportDeterministicKey);
        Assert.Equal(expected.DeterministicKey, actual.DeterministicKey);
        Assert.Equal(expected.PlayableMediaStatus, actual.PlayableMediaStatus);
        Assert.Equal(expected.ExportFrames.Select(frame => frame.FrameIndex).ToArray(), actual.ExportFrames.Select(frame => frame.FrameIndex).ToArray());
        Assert.Equal(expected.ExportFrames.Select(frame => frame.ArtifactRelativePath).ToArray(), actual.ExportFrames.Select(frame => frame.ArtifactRelativePath).ToArray());
        Assert.Equal(expected.ExportFrames.Select(frame => frame.ArtifactDeterministicKey).ToArray(), actual.ExportFrames.Select(frame => frame.ArtifactDeterministicKey).ToArray());
        Assert.Equal(File.ReadAllText(expected.ExportManifestPath), File.ReadAllText(actual.ExportManifestPath));

        for (var index = 0; index < expected.ExportFrames.Count; index++)
        {
            var expectedFrame = expected.ExportFrames[index];
            var actualFrame = actual.ExportFrames[index];
            var expectedArtifactPath = Path.Combine(expected.ExportPackageRootPath, expectedFrame.ArtifactRelativePath.Replace('/', Path.DirectorySeparatorChar));
            var actualArtifactPath = Path.Combine(actual.ExportPackageRootPath, actualFrame.ArtifactRelativePath.Replace('/', Path.DirectorySeparatorChar));

            Assert.True(File.Exists(expectedArtifactPath));
            Assert.True(File.Exists(actualArtifactPath));
            Assert.Equal(File.ReadAllBytes(expectedArtifactPath), File.ReadAllBytes(actualArtifactPath));
        }
    }

    private static void AssertPlayableMediaOutputsEquivalent(CliRunResult expected, CliRunResult actual)
    {
        Assert.Equal(expected.PlayableMediaStatus, actual.PlayableMediaStatus);
        Assert.Equal(expected.PlayableMediaAudioStatus, actual.PlayableMediaAudioStatus);
        Assert.Equal(expected.PlayableMediaAudioCueCount, actual.PlayableMediaAudioCueCount);
        Assert.Equal(expected.PlayableMediaByteCount, actual.PlayableMediaByteCount);
        Assert.Equal(expected.PlayableMediaDeterministicKey, actual.PlayableMediaDeterministicKey);
        Assert.True(File.Exists(expected.PlayableMediaPath));
        Assert.True(File.Exists(actual.PlayableMediaPath));
        Assert.Equal(File.ReadAllBytes(expected.PlayableMediaPath), File.ReadAllBytes(actual.PlayableMediaPath));
    }

    private static string ResolveRepoRelativePath(params string[] segments)
    {
        var baseDirectory = new DirectoryInfo(AppContext.BaseDirectory);

        for (var current = baseDirectory; current is not null; current = current.Parent)
        {
            var candidate = Path.Combine(new[] { current.FullName }.Concat(segments).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException($"Repo file '{Path.Combine(segments)}' was not found.");
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "whiteboard-phase15-regression-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    private sealed record RegressionBaseline
    {
        public string ExpectedProjectId { get; init; } = string.Empty;

        public int ExpectedFrameCount { get; init; }

        public int ExpectedAudioCueCount { get; init; }

        public double ExpectedTotalDurationSeconds { get; init; }

        public IReadOnlyList<AnchorArtifactDeterministicKey> AnchorArtifactDeterministicKeys { get; init; } = [];
    }

    private sealed record AnchorArtifactDeterministicKey
    {
        public int FrameIndex { get; init; }

        public string ArtifactDeterministicKey { get; init; } = string.Empty;
    }

    private sealed class DeterministicWitnessProcessRunner : IProcessRunner
    {
        public ProcessRunResult Run(ProcessRunRequest request)
        {
            var outputPath = request.Arguments.Last();
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? Environment.CurrentDirectory);
            var payload = string.Join("\n", request.Arguments.Select(argument => NormalizeArgument(request, argument)));
            File.WriteAllText(outputPath, payload);
            return new ProcessRunResult
            {
                Success = true,
                ExitCode = 0,
                StandardOutput = "ok",
                StandardError = string.Empty
            };
        }

        private static string NormalizeArgument(ProcessRunRequest request, string argument)
        {
            if (!Path.IsPathRooted(argument))
            {
                return argument.Replace('\\', '/');
            }

            var fullPath = Path.GetFullPath(argument);
            var outputPath = Path.GetFullPath(request.Arguments.Last());
            if (string.Equals(fullPath, outputPath, StringComparison.OrdinalIgnoreCase))
            {
                return $"output{Path.GetExtension(fullPath).ToLowerInvariant()}";
            }

            var workingDirectory = string.IsNullOrWhiteSpace(request.WorkingDirectory)
                ? Environment.CurrentDirectory
                : Path.GetFullPath(request.WorkingDirectory);

            return fullPath.StartsWith(workingDirectory, StringComparison.OrdinalIgnoreCase)
                ? Path.GetRelativePath(workingDirectory, fullPath).Replace('\\', '/')
                : Path.GetFileName(fullPath);
        }
    }
}

