using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Whiteboard.Core.Timeline;
using Whiteboard.Export.Models;
using Whiteboard.Export.Services;
using Whiteboard.Renderer.Models;
using Xunit;

namespace Whiteboard.Cli.Tests;

public sealed class ExportPipelineContractTests
{
    [Fact]
    public void ExportContracts_CarryExplicitFrameAndAudioPackagingMetadata()
    {
        var frame = new RenderFrameResult
        {
            FrameIndex = 12,
            Success = true,
            SceneCount = 1,
            ObjectCount = 2,
            Operations = ["camera:0,0:1", "svg-path:idea:mode:partial"]
        };

        var request = new ExportRequest
        {
            ProjectId = "demo-project",
            Frames = [frame],
            FrameTimings =
            [
                new ExportFrameTiming
                {
                    FrameIndex = 12,
                    StartSeconds = 0.4,
                    DurationSeconds = 0.033333
                }
            ],
            AudioCues =
            [
                new AudioCue
                {
                    Id = "cue-1",
                    AudioAssetId = "audio-1",
                    StartSeconds = 0.5,
                    DurationSeconds = 4,
                    Volume = 0.8
                }
            ],
            AudioAssets =
            [
                new ExportAudioAssetInput
                {
                    AssetId = "audio-1",
                    Name = "Narration",
                    DeclaredSourcePath = "assets/narration.mp3",
                    ResolvedSourcePath = "C:/exports/assets/narration.mp3",
                    DefaultVolume = 0.6
                }
            ],
            Target = new ExportTarget
            {
                OutputPath = "out/video.mp4",
                Format = "mp4",
                Width = 1280,
                Height = 720,
                FrameRate = 30
            }
        };

        Assert.Equal("demo-project", request.ProjectId);
        Assert.Single(request.Frames);
        Assert.Equal(12, request.Frames[0].FrameIndex);
        Assert.Single(request.FrameTimings);
        Assert.Equal(0.4, request.FrameTimings[0].StartSeconds, 6);
        Assert.Single(request.AudioCues);
        Assert.Equal("audio-1", request.AudioCues[0].AudioAssetId);
        Assert.Single(request.AudioAssets);
        Assert.Equal("assets/narration.mp3", request.AudioAssets[0].DeclaredSourcePath);
        Assert.Equal("mp4", request.Target.Format);
        Assert.Equal(1280, request.Target.Width);
        Assert.Equal(720, request.Target.Height);
        Assert.Equal(30, request.Target.FrameRate, 6);
    }

    [Fact]
    public void ExportResult_CarriesDeterministicPackageSummary()
    {
        var result = new ExportResult
        {
            Success = true,
            Message = "Packaged export metadata.",
            OutputPath = "out/video.mp4",
            ExportedFrameCount = 2,
            ExportedAudioCueCount = 1,
            TotalOperations = 3,
            Frames =
            [
                new ExportFramePackage
                {
                    FrameIndex = 0,
                    StartSeconds = 0,
                    DurationSeconds = 0.033333,
                    SceneCount = 1,
                    ObjectCount = 2,
                    Operations = ["camera:0,0:1", "svg-path:idea:mode:complete"]
                },
                new ExportFramePackage
                {
                    FrameIndex = 1,
                    StartSeconds = 0.033333,
                    DurationSeconds = 0.033333,
                    SceneCount = 1,
                    ObjectCount = 2,
                    Operations = ["camera:0,0:1"]
                }
            ],
            AudioCues =
            [
                new ExportAudioCuePackage
                {
                    CueId = "cue-1",
                    AudioAssetId = "audio-1",
                    AudioAssetName = "Narration",
                    SourcePath = "assets/narration.mp3",
                    StartSeconds = 0.5,
                    DurationSeconds = 4,
                    Volume = 0.8,
                    DefaultVolume = 0.6
                }
            ],
            Summary = new ExportPackageSummary
            {
                ProjectId = "demo-project",
                Format = "mp4",
                Width = 1280,
                Height = 720,
                FrameRate = 30,
                FrameCount = 2,
                AudioCueCount = 1,
                TotalOperations = 3,
                TotalDurationSeconds = 4.5
            },
            DeterministicKey = "demo-project|mp4|2|1|3|4.5"
        };

        Assert.True(result.Success);
        Assert.Equal(2, result.ExportedFrameCount);
        Assert.Equal(1, result.ExportedAudioCueCount);
        Assert.Equal(2, result.Frames.Count);
        Assert.Equal("camera:0,0:1", result.Frames[0].Operations[0]);
        Assert.Single(result.AudioCues);
        Assert.Equal("Narration", result.AudioCues[0].AudioAssetName);
        Assert.Equal("demo-project", result.Summary.ProjectId);
        Assert.Equal(4.5, result.Summary.TotalDurationSeconds, 6);
        Assert.Equal("demo-project|mp4|2|1|3|4.5", result.DeterministicKey);
    }

    [Fact]
    public void ExportPipeline_Repeatability_OrdersFramesAndPreservesTimingMetadata()
    {
        var pipeline = new ExportPipeline();
        var request = new ExportRequest
        {
            ProjectId = "demo-project",
            Frames =
            [
                CreateFrame(8, "camera:0,0:1", "svg-path:idea:mode:complete"),
                CreateFrame(3, "camera:0,0:1", "svg-path:idea:mode:partial")
            ],
            FrameTimings =
            [
                new ExportFrameTiming { FrameIndex = 8, StartSeconds = 0.266667, DurationSeconds = 0.033333 },
                new ExportFrameTiming { FrameIndex = 3, StartSeconds = 0.1, DurationSeconds = 0.033333 }
            ],
            Target = new ExportTarget
            {
                OutputPath = "out/video.mp4",
                Format = "mp4",
                Width = 1280,
                Height = 720,
                FrameRate = 30
            }
        };

        var reorderedRequest = request with
        {
            Frames = request.Frames.Reverse().ToArray(),
            FrameTimings = request.FrameTimings.Reverse().ToArray()
        };

        var first = pipeline.Export(request);
        var second = pipeline.Export(reorderedRequest);

        Assert.True(first.Success);
        Assert.Equal([3, 8], first.Frames.Select(frame => frame.FrameIndex).ToArray());
        Assert.Equal(0.1, first.Frames[0].StartSeconds, 6);
        Assert.Equal(0.033333, first.Frames[0].DurationSeconds, 6);
        Assert.Equal(["camera:0,0:1", "svg-path:idea:mode:partial"], first.Frames[0].Operations);
        Assert.Equal(2, first.ExportedFrameCount);
        Assert.Equal(4, first.TotalOperations);
        Assert.Equal(first.DeterministicKey, second.DeterministicKey);
        Assert.Equal(first.Frames.Select(frame => frame.FrameIndex).ToArray(), second.Frames.Select(frame => frame.FrameIndex).ToArray());
        Assert.Equal(first.Frames.SelectMany(frame => frame.Operations).ToArray(), second.Frames.SelectMany(frame => frame.Operations).ToArray());
    }

    [Fact]
    public void ExportPipeline_Repeatability_OrdersCueMetadataAndUsesLogicalSourcePaths()
    {
        var firstAudioPath = CreateTempAudioAsset();
        var secondAudioPath = CreateTempAudioAsset();

        try
        {
            var pipeline = new ExportPipeline();
            var result = pipeline.Export(new ExportRequest
            {
                ProjectId = "demo-project",
                Frames = [CreateFrame(0, "camera:0,0:1")],
                FrameTimings = [new ExportFrameTiming { FrameIndex = 0, StartSeconds = 0, DurationSeconds = 0.033333 }],
                AudioCues =
                [
                    new AudioCue { Id = "cue-late", AudioAssetId = "audio-2", StartSeconds = 2.25, DurationSeconds = 1.5, Volume = 0.5 },
                    new AudioCue { Id = "cue-early", AudioAssetId = "audio-1", StartSeconds = 0.25, DurationSeconds = 2, Volume = 0.8 }
                ],
                AudioAssets =
                [
                    new ExportAudioAssetInput
                    {
                        AssetId = "audio-2",
                        Name = "Music",
                        DeclaredSourcePath = "assets/music.mp3",
                        ResolvedSourcePath = secondAudioPath,
                        DefaultVolume = 0.4
                    },
                    new ExportAudioAssetInput
                    {
                        AssetId = "audio-1",
                        Name = "Narration",
                        DeclaredSourcePath = "assets/narration.mp3",
                        ResolvedSourcePath = firstAudioPath,
                        DefaultVolume = 0.6
                    }
                ],
                Target = new ExportTarget
                {
                    OutputPath = "out/video.mp4",
                    Format = "mp4",
                    Width = 1280,
                    Height = 720,
                    FrameRate = 30
                }
            });

            Assert.True(result.Success);
            Assert.Equal(["cue-early", "cue-late"], result.AudioCues.Select(cue => cue.CueId).ToArray());
            Assert.Equal("assets/narration.mp3", result.AudioCues[0].SourcePath);
            Assert.Equal(2, result.ExportedAudioCueCount);
            Assert.Equal(2.25, result.AudioCues[1].StartSeconds, 6);
            Assert.Equal(3.75, result.Summary.TotalDurationSeconds, 6);
        }
        finally
        {
            DeleteTempAsset(firstAudioPath);
            DeleteTempAsset(secondAudioPath);
        }
    }

    [Fact]
    public void ExportPipeline_Repeatability_DerivesFrameTimingFromTargetFrameRateWhenTimingMetadataIsMissing()
    {
        var pipeline = new ExportPipeline();
        var result = pipeline.Export(new ExportRequest
        {
            ProjectId = "demo-project",
            Frames = [CreateFrame(12, "camera:0,0:1", "svg-path:idea:mode:partial")],
            Target = new ExportTarget
            {
                OutputPath = "out/video.mp4",
                Format = "mp4",
                Width = 1280,
                Height = 720,
                FrameRate = 24
            }
        });

        Assert.True(result.Success);
        Assert.Single(result.Frames);
        Assert.Equal(12d / 24d, result.Frames[0].StartSeconds, 6);
        Assert.Equal(1d / 24d, result.Frames[0].DurationSeconds, 6);
        Assert.Equal(result.Frames[0].StartSeconds + result.Frames[0].DurationSeconds, result.Summary.TotalDurationSeconds, 6);
    }

    [Fact]
    public void ExportPipeline_Repeatability_ProducesIdenticalDeterministicPackagesForEquivalentInputs()
    {
        var narrationPath = CreateTempAudioAsset();
        var musicPath = CreateTempAudioAsset();

        try
        {
            var pipeline = new ExportPipeline();
            var primaryRequest = new ExportRequest
            {
                ProjectId = "demo-project",
                Frames =
                [
                    CreateFrame(15, "camera:2,1:1.1", "svg-path:idea:mode:partial"),
                    CreateFrame(9, "camera:1,0.5:1.05", "svg-path:idea:mode:complete")
                ],
                FrameTimings =
                [
                    new ExportFrameTiming { FrameIndex = 15, StartSeconds = 0.5, DurationSeconds = 1d / 30d },
                    new ExportFrameTiming { FrameIndex = 9, StartSeconds = 0.3, DurationSeconds = 1d / 30d }
                ],
                AudioCues =
                [
                    new AudioCue { Id = "cue-2", AudioAssetId = "audio-2", StartSeconds = 1.1, DurationSeconds = 2.2, Volume = 0.5 },
                    new AudioCue { Id = "cue-1", AudioAssetId = "audio-1", StartSeconds = 0.45, DurationSeconds = 1.5, Volume = 0.85 }
                ],
                AudioAssets =
                [
                    new ExportAudioAssetInput
                    {
                        AssetId = "audio-2",
                        Name = "Music",
                        DeclaredSourcePath = "assets/music.mp3",
                        ResolvedSourcePath = musicPath,
                        DefaultVolume = 0.4
                    },
                    new ExportAudioAssetInput
                    {
                        AssetId = "audio-1",
                        Name = "Narration",
                        DeclaredSourcePath = "assets/narration.mp3",
                        ResolvedSourcePath = narrationPath,
                        DefaultVolume = 0.6
                    }
                ],
                Target = new ExportTarget
                {
                    OutputPath = "out/video.mp4",
                    Format = "mp4",
                    Width = 1280,
                    Height = 720,
                    FrameRate = 30
                }
            };

            var equivalentRequest = primaryRequest with
            {
                Frames = primaryRequest.Frames.Reverse().ToArray(),
                FrameTimings = primaryRequest.FrameTimings.Reverse().ToArray(),
                AudioCues = primaryRequest.AudioCues.Reverse().ToArray(),
                AudioAssets = primaryRequest.AudioAssets.Reverse().ToArray()
            };

            var first = pipeline.Export(primaryRequest);
            var second = pipeline.Export(primaryRequest);
            var equivalent = pipeline.Export(equivalentRequest);

            AssertExportResultsEquivalent(first, second);
            AssertExportResultsEquivalent(first, equivalent);
            Assert.Equal(3.3, first.Summary.TotalDurationSeconds, 6);
        }
        finally
        {
            DeleteTempAsset(narrationPath);
            DeleteTempAsset(musicPath);
        }
    }

    [Fact]
    public void ExportPipeline_MissingAudio_FailsFastWhenReferencedAssetIsMissing()
    {
        var pipeline = new ExportPipeline();
        var missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.mp3");

        var result = pipeline.Export(new ExportRequest
        {
            ProjectId = "demo-project",
            Frames = [CreateFrame(0, "camera:0,0:1")],
            FrameTimings = [new ExportFrameTiming { FrameIndex = 0, StartSeconds = 0, DurationSeconds = 0.033333 }],
            AudioCues =
            [
                new AudioCue
                {
                    Id = "cue-missing",
                    AudioAssetId = "audio-missing",
                    StartSeconds = 0.5,
                    DurationSeconds = 1,
                    Volume = 0.7
                }
            ],
            AudioAssets =
            [
                new ExportAudioAssetInput
                {
                    AssetId = "audio-missing",
                    Name = "Missing",
                    DeclaredSourcePath = "assets/missing.mp3",
                    ResolvedSourcePath = missingPath,
                    DefaultVolume = 0.5
                }
            ],
            Target = new ExportTarget
            {
                OutputPath = "out/video.mp4",
                Format = "mp4",
                Width = 1280,
                Height = 720,
                FrameRate = 30
            }
        });

        Assert.False(result.Success);
        Assert.Equal(0, result.ExportedFrameCount);
        Assert.Equal(0, result.ExportedAudioCueCount);
        Assert.Empty(result.Frames);
        Assert.Empty(result.AudioCues);
        Assert.Contains("cue-missing", result.Message, StringComparison.Ordinal);
        Assert.Contains("audio-missing", result.DeterministicKey, StringComparison.Ordinal);
    }

    private static void AssertExportResultsEquivalent(ExportResult expected, ExportResult actual)
    {
        Assert.Equal(expected.Success, actual.Success);
        Assert.Equal(expected.Message, actual.Message);
        Assert.Equal(expected.OutputPath, actual.OutputPath);
        Assert.Equal(expected.ExportedFrameCount, actual.ExportedFrameCount);
        Assert.Equal(expected.ExportedAudioCueCount, actual.ExportedAudioCueCount);
        Assert.Equal(expected.TotalOperations, actual.TotalOperations);
        Assert.Equal(expected.DeterministicKey, actual.DeterministicKey);

        Assert.Equal(expected.Summary.ProjectId, actual.Summary.ProjectId);
        Assert.Equal(expected.Summary.Format, actual.Summary.Format);
        Assert.Equal(expected.Summary.Width, actual.Summary.Width);
        Assert.Equal(expected.Summary.Height, actual.Summary.Height);
        Assert.Equal(expected.Summary.FrameRate, actual.Summary.FrameRate, 6);
        Assert.Equal(expected.Summary.FrameCount, actual.Summary.FrameCount);
        Assert.Equal(expected.Summary.AudioCueCount, actual.Summary.AudioCueCount);
        Assert.Equal(expected.Summary.TotalOperations, actual.Summary.TotalOperations);
        Assert.Equal(expected.Summary.TotalDurationSeconds, actual.Summary.TotalDurationSeconds, 6);

        Assert.Equal(expected.Frames.Count, actual.Frames.Count);
        Assert.Equal(expected.AudioCues.Count, actual.AudioCues.Count);

        for (var index = 0; index < expected.Frames.Count; index++)
        {
            var expectedFrame = expected.Frames[index];
            var actualFrame = actual.Frames[index];

            Assert.Equal(expectedFrame.FrameIndex, actualFrame.FrameIndex);
            Assert.Equal(expectedFrame.StartSeconds, actualFrame.StartSeconds, 6);
            Assert.Equal(expectedFrame.DurationSeconds, actualFrame.DurationSeconds, 6);
            Assert.Equal(expectedFrame.SceneCount, actualFrame.SceneCount);
            Assert.Equal(expectedFrame.ObjectCount, actualFrame.ObjectCount);
            Assert.Equal(expectedFrame.Operations, actualFrame.Operations);
        }

        for (var index = 0; index < expected.AudioCues.Count; index++)
        {
            var expectedCue = expected.AudioCues[index];
            var actualCue = actual.AudioCues[index];

            Assert.Equal(expectedCue.CueId, actualCue.CueId);
            Assert.Equal(expectedCue.AudioAssetId, actualCue.AudioAssetId);
            Assert.Equal(expectedCue.AudioAssetName, actualCue.AudioAssetName);
            Assert.Equal(expectedCue.SourcePath, actualCue.SourcePath);
            Assert.Equal(expectedCue.StartSeconds, actualCue.StartSeconds, 6);
            Assert.Equal(expectedCue.DurationSeconds, actualCue.DurationSeconds);
            Assert.Equal(expectedCue.Volume, actualCue.Volume, 6);
            Assert.Equal(expectedCue.DefaultVolume, actualCue.DefaultVolume, 6);
        }
    }

    private static RenderFrameResult CreateFrame(int frameIndex, params string[] operations)
    {
        return new RenderFrameResult
        {
            FrameIndex = frameIndex,
            Success = true,
            SceneCount = 1,
            ObjectCount = 2,
            Operations = operations
        };
    }

    private static string CreateTempAudioAsset()
    {
        var directory = Path.Combine(Path.GetTempPath(), "whiteboard-export-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, "audio.mp3");
        File.WriteAllText(filePath, "placeholder-audio");
        return filePath;
    }

    private static void DeleteTempAsset(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        var directoryPath = Path.GetDirectoryName(filePath);
        File.Delete(filePath);

        if (!string.IsNullOrWhiteSpace(directoryPath) && Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }
}

