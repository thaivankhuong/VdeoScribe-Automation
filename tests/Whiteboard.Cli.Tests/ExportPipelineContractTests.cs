using Whiteboard.Core.Timeline;
using Whiteboard.Export.Models;
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
}
