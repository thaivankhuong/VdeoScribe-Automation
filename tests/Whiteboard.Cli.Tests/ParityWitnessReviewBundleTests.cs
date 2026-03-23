using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Whiteboard.Cli.Models;
using Whiteboard.Cli.Services;
using Xunit;

namespace Whiteboard.Cli.Tests;

public sealed class ParityWitnessReviewBundleTests
{
    [Fact]
    public void ParityWitnessReviewBundleTests_RenderAnchorFramesFromReviewBundle()
    {
        var bundlePath = ResolveRepoRelativePath("artifacts", "source-parity-demo", "check", "phase15-review-bundle.json");
        var bundle = LoadBundle(bundlePath);
        var specPath = ResolveRepoRelativePath("artifacts", "source-parity-demo", "project-engine.json");
        var outputDirectory = Path.Combine(Path.GetTempPath(), "whiteboard-cli-phase15-review-bundle-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "phase15-review-witness.mp4");
        var previousEnabled = Environment.GetEnvironmentVariable("WHITEBOARD_ENABLE_PLAYABLE_MEDIA");
        var previousFfmpegPath = Environment.GetEnvironmentVariable("WHITEBOARD_FFMPEG_PATH");
        Environment.SetEnvironmentVariable("WHITEBOARD_ENABLE_PLAYABLE_MEDIA", null);
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
            Assert.Equal(bundle.FrameCount, result.ExportedFrameCount);
            Assert.Equal(bundle.PlayableMedia.Status, result.PlayableMediaStatus);
            Assert.True(string.IsNullOrWhiteSpace(result.PlayableMediaPath));

            foreach (var anchorFrame in bundle.AnchorFrames)
            {
                var frame = Assert.Single(result.ExportFrames, entry => entry.FrameIndex == anchorFrame.FrameIndex);
                Assert.Equal(anchorFrame.RelativeArtifactPath, frame.ArtifactRelativePath);

                var artifactPath = Path.Combine(result.ExportPackageRootPath, frame.ArtifactRelativePath.Replace('/', Path.DirectorySeparatorChar));
                Assert.True(File.Exists(artifactPath));
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable("WHITEBOARD_ENABLE_PLAYABLE_MEDIA", previousEnabled);
            Environment.SetEnvironmentVariable("WHITEBOARD_FFMPEG_PATH", previousFfmpegPath);

            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void ParityWitnessReviewBundleTests_BundleTracksAuthoredWitnessLineage()
    {
        var bundlePath = ResolveRepoRelativePath("artifacts", "source-parity-demo", "check", "phase15-review-bundle.json");
        var bundle = LoadBundle(bundlePath);

        Assert.Equal("project-engine.json", bundle.SourceSpec);
        Assert.Equal("out/phase14-fidelity-witness", bundle.BaselineWitness);
        Assert.Equal("out/phase15-review-witness", bundle.ReviewWitness);
        Assert.Equal("out/phase15-review-witness/frame-manifest.json", bundle.FrameManifest);
        Assert.Equal(264, bundle.FrameCount);
        Assert.Equal(new[] { 27, 72, 93, 130, 185, 214 }, bundle.AnchorFrames.Select(frame => frame.FrameIndex).ToArray());
        Assert.Equal(new[]
        {
            "frames/frame-000027.svg",
            "frames/frame-000072.svg",
            "frames/frame-000093.svg",
            "frames/frame-000130.svg",
            "frames/frame-000185.svg",
            "frames/frame-000214.svg"
        }, bundle.AnchorFrames.Select(frame => frame.RelativeArtifactPath).ToArray());
        Assert.Equal("not-configured", bundle.PlayableMedia.Status);
        Assert.Equal("out/phase15-review-witness.mp4", bundle.PlayableMedia.ExpectedWhenEnabled);
    }

    private static ReviewBundle LoadBundle(string path)
    {
        var bundle = JsonSerializer.Deserialize<ReviewBundle>(
            File.ReadAllText(path),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        return bundle ?? throw new InvalidOperationException("Review bundle could not be deserialized.");
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

    private sealed record ReviewBundle
    {
        public string SourceSpec { get; init; } = string.Empty;

        public string BaselineWitness { get; init; } = string.Empty;

        public string ReviewWitness { get; init; } = string.Empty;

        public string FrameManifest { get; init; } = string.Empty;

        public int FrameCount { get; init; }

        public IReadOnlyList<AnchorFrame> AnchorFrames { get; init; } = [];

        public PlayableMediaBundle PlayableMedia { get; init; } = new();
    }

    private sealed record AnchorFrame
    {
        public int FrameIndex { get; init; }

        public string ObjectId { get; init; } = string.Empty;

        public string AssetId { get; init; } = string.Empty;

        public string RelativeArtifactPath { get; init; } = string.Empty;
    }

    private sealed record PlayableMediaBundle
    {
        public string Status { get; init; } = string.Empty;

        public string? RelativePath { get; init; }

        public string ExpectedWhenEnabled { get; init; } = string.Empty;
    }
}
