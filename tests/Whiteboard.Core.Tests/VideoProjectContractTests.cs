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
        var project = CreateProject();

        Assert.Single(project.Assets.SvgAssets);
        Assert.Single(project.Scenes);
        Assert.Single(project.Timeline.Events);
    }

    [Fact]
    public void VideoProject_DefaultContractShape_IsDeterministic()
    {
        var first = new VideoProject();
        var second = new VideoProject();

        Assert.Equal(first.Meta.ProjectId, second.Meta.ProjectId);
        Assert.Equal(first.Meta.Name, second.Meta.Name);
        Assert.Equal(first.Output.Width, second.Output.Width);
        Assert.Equal(first.Output.Height, second.Output.Height);
        Assert.Equal(first.Output.FrameRate, second.Output.FrameRate);
        Assert.Equal(first.Output.BackgroundColorHex, second.Output.BackgroundColorHex);
        Assert.Equal(first.Scenes.Count, second.Scenes.Count);
        Assert.Equal(first.Timeline.Events.Count, second.Timeline.Events.Count);
        Assert.Equal(first.Assets.SvgAssets.Count, second.Assets.SvgAssets.Count);
        Assert.Equal(first.Assets.AudioAssets.Count, second.Assets.AudioAssets.Count);
    }

    [Fact]
    public void VideoProject_ComposedContracts_AreStableForEquivalentInput()
    {
        var first = CreateProject();
        var second = CreateProject();

        Assert.Equal(first.Meta.ProjectId, second.Meta.ProjectId);
        Assert.Equal(first.Meta.Name, second.Meta.Name);
        Assert.Equal(first.Output.Width, second.Output.Width);
        Assert.Equal(first.Output.Height, second.Output.Height);
        Assert.Equal(first.Output.FrameRate, second.Output.FrameRate);
        Assert.Equal(first.Scenes[0].Id, second.Scenes[0].Id);
        Assert.Equal(first.Scenes[0].Objects[0].Id, second.Scenes[0].Objects[0].Id);
        Assert.Equal(first.Scenes[0].Objects[0].Transform.Position, second.Scenes[0].Objects[0].Transform.Position);
        Assert.Equal(first.Timeline.Events[0].Id, second.Timeline.Events[0].Id);
        Assert.Equal(first.Timeline.Events[0].ActionType, second.Timeline.Events[0].ActionType);
        Assert.Equal(first.Timeline.CameraTrack.Keyframes[0].Position, second.Timeline.CameraTrack.Keyframes[0].Position);
        Assert.Equal(first.Timeline.AudioCues[0].AudioAssetId, second.Timeline.AudioCues[0].AudioAssetId);
    }

    private static VideoProject CreateProject()
    {
        return new VideoProject
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
    }
}
