using Whiteboard.Core.Assets;
using Whiteboard.Core.Enums;
using Whiteboard.Core.Models;
using Whiteboard.Core.Scene;
using Whiteboard.Core.Timeline;
using Whiteboard.Core.ValueObjects;
using Xunit;

namespace Whiteboard.Core.Tests;

public sealed class VideoProjectContractTests
{
    [Fact]
    public void VideoProject_CanBeInstantiated()
    {
        var project = new VideoProject();

        Assert.NotNull(project);
        Assert.NotNull(project.Meta);
        Assert.NotNull(project.Output);
        Assert.NotNull(project.Assets);
        Assert.NotNull(project.Scenes);
        Assert.NotNull(project.Timeline);
    }

    [Fact]
    public void VideoProject_CanComposeCoreContracts()
    {
        var project = new VideoProject
        {
            Meta = new ProjectMeta
            {
                ProjectId = "project-001",
                Name = "Sample Whiteboard"
            },
            Output = new OutputSpec
            {
                Width = 1280,
                Height = 720,
                FrameRate = 25
            },
            Assets = new AssetCollection
            {
                SvgAssets =
                [
                    new SvgAsset
                    {
                        Id = "svg-1",
                        Name = "Idea Bulb",
                        SourcePath = "assets/idea.svg",
                        DefaultSize = new Size2D(320, 320)
                    }
                ],
                AudioAssets =
                [
                    new AudioAsset
                    {
                        Id = "audio-1",
                        Name = "Background",
                        SourcePath = "assets/bg.mp3"
                    }
                ]
            },
            Scenes =
            [
                new SceneDefinition
                {
                    Id = "scene-1",
                    Name = "Intro",
                    DurationSeconds = 12,
                    Objects =
                    [
                        new SceneObject
                        {
                            Id = "object-1",
                            Name = "Bulb",
                            Type = SceneObjectType.Svg,
                            AssetRefId = "svg-1",
                            Transform = new TransformSpec
                            {
                                Position = new Position2D(100, 200),
                                Size = new Size2D(300, 300)
                            }
                        }
                    ]
                }
            ],
            Timeline = new TimelineDefinition
            {
                Events =
                [
                    new TimelineEvent
                    {
                        Id = "event-1",
                        SceneId = "scene-1",
                        SceneObjectId = "object-1",
                        ActionType = TimelineActionType.Draw,
                        StartSeconds = 0,
                        DurationSeconds = 3,
                        Parameters =
                        {
                            ["strokeOrder"] = "sequential"
                        }
                    }
                ],
                CameraTrack = new CameraTrack
                {
                    Keyframes =
                    [
                        new CameraKeyframe
                        {
                            TimeSeconds = 0,
                            Position = new Position2D(0, 0),
                            Zoom = 1
                        }
                    ]
                },
                AudioCues =
                [
                    new AudioCue
                    {
                        Id = "cue-1",
                        AudioAssetId = "audio-1",
                        StartSeconds = 0,
                        DurationSeconds = 12,
                        Volume = 0.8
                    }
                ]
            }
        };

        Assert.Single(project.Assets.SvgAssets);
        Assert.Single(project.Scenes);
        Assert.Single(project.Timeline.Events);
    }
}
