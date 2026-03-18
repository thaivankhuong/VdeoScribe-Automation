using System.Collections.Generic;
using Whiteboard.Core.Assets;
using Whiteboard.Core.Enums;
using Whiteboard.Core.Models;
using Whiteboard.Core.Scene;
using Whiteboard.Core.Timeline;
using Whiteboard.Core.ValueObjects;
using Whiteboard.Engine.Context;
using Whiteboard.Engine.Models;
using Whiteboard.Engine.Services;
using Xunit;

namespace Whiteboard.Engine.Tests;

public sealed class CameraInterpolationResolutionTests
{
    private readonly CameraStateResolver _resolver = new();

    [Fact]
    public void Camera_UsesFirstKeyframeBeforeTimelineStart()
    {
        var resolved = ResolveFrame(CreateProject(), frameIndex: 0);

        Assert.Equal(0, resolved.FrameTimeSeconds);
        Assert.Equal(new Position2D(0, 0), resolved.Position);
        Assert.Equal(1, resolved.Zoom);
        Assert.Equal(EasingType.Linear, resolved.Interpolation);
    }

    [Fact]
    public void Camera_UsesLastMatchingKeyframeOnExactDuplicateTimestamp()
    {
        var project = CreateProject(
            new CameraKeyframe
            {
                TimeSeconds = 1,
                Position = new Position2D(5, 5),
                Zoom = 1.1,
                Interpolation = EasingType.Linear
            },
            new CameraKeyframe
            {
                TimeSeconds = 1,
                Position = new Position2D(8, 8),
                Zoom = 1.25,
                Interpolation = EasingType.Step
            },
            new CameraKeyframe
            {
                TimeSeconds = 2,
                Position = new Position2D(20, 10),
                Zoom = 1.5,
                Interpolation = EasingType.Linear
            });

        var resolved = ResolveFrame(project, frameIndex: 30);

        Assert.Equal(new Position2D(8, 8), resolved.Position);
        Assert.Equal(1.25, resolved.Zoom);
        Assert.Equal(EasingType.Step, resolved.Interpolation);
    }

    [Fact]
    public void Camera_InterpolatesLinearlyBetweenDistinctKeyframes()
    {
        var resolved = ResolveFrame(CreateProject(), frameIndex: 45);

        Assert.Equal(1.5, resolved.FrameTimeSeconds);
        Assert.Equal(new Position2D(10, 5), resolved.Position);
        Assert.Equal(1.25, resolved.Zoom);
        Assert.Equal(EasingType.Linear, resolved.Interpolation);
    }

    [Fact]
    public void Camera_StepInterpolationHoldsLeadingKeyframeUntilNextKeyframe()
    {
        var project = CreateProject(
            new CameraKeyframe
            {
                TimeSeconds = 0,
                Position = new Position2D(0, 0),
                Zoom = 1,
                Interpolation = EasingType.Step
            },
            new CameraKeyframe
            {
                TimeSeconds = 2,
                Position = new Position2D(20, 10),
                Zoom = 1.5,
                Interpolation = EasingType.Linear
            });

        var resolved = ResolveFrame(project, frameIndex: 30);

        Assert.Equal(1, resolved.FrameTimeSeconds);
        Assert.Equal(new Position2D(0, 0), resolved.Position);
        Assert.Equal(1, resolved.Zoom);
        Assert.Equal(EasingType.Step, resolved.Interpolation);
    }

    [Fact]
    public void Camera_UsesLastKeyframeAfterTimelineEnd()
    {
        var resolved = ResolveFrame(CreateProject(), frameIndex: 90);

        Assert.Equal(3, resolved.FrameTimeSeconds);
        Assert.Equal(new Position2D(20, 10), resolved.Position);
        Assert.Equal(1.5, resolved.Zoom);
        Assert.Equal(EasingType.Linear, resolved.Interpolation);
    }

    [Fact]
    public void Camera_RepeatedRunsProduceIdenticalInterpolatedState()
    {
        var project = CreateProject();

        var first = ResolveFrame(project, frameIndex: 45);
        var second = ResolveFrame(project, frameIndex: 45);

        Assert.Equal(first, second);
    }

    private ResolvedCameraState ResolveFrame(VideoProject project, int frameIndex)
    {
        return _resolver.Resolve(project, FrameContext.FromFrameIndex(frameIndex, 30), new List<ResolvedTimelineEvent>());
    }

    private static VideoProject CreateProject(params CameraKeyframe[] keyframes)
    {
        return new VideoProject
        {
            Meta = new ProjectMeta
            {
                ProjectId = "camera-project",
                Name = "Camera Interpolation"
            },
            Output = new OutputSpec
            {
                Width = 1280,
                Height = 720,
                FrameRate = 30
            },
            Assets = new AssetCollection
            {
                SvgAssets =
                [
                    new SvgAsset
                    {
                        Id = "svg-1",
                        Name = "Shape",
                        SourcePath = "assets/shape.svg"
                    }
                ]
            },
            Scenes =
            [
                new SceneDefinition
                {
                    Id = "scene-1",
                    Name = "Scene 1",
                    DurationSeconds = 5,
                    Objects =
                    [
                        new SceneObject
                        {
                            Id = "object-1",
                            Name = "Shape",
                            Type = SceneObjectType.Svg,
                            AssetRefId = "svg-1",
                            IsVisible = true,
                            Layer = 1,
                            Transform = new TransformSpec
                            {
                                Position = new Position2D(100, 100),
                                Size = new Size2D(200, 200)
                            }
                        }
                    ]
                }
            ],
            Timeline = new TimelineDefinition
            {
                CameraTrack = new CameraTrack
                {
                    Keyframes =
                    [
                        .. (keyframes.Length == 0
                            ? new[]
                            {
                                new CameraKeyframe
                                {
                                    TimeSeconds = 1,
                                    Position = new Position2D(0, 0),
                                    Zoom = 1,
                                    Interpolation = EasingType.Linear
                                },
                                new CameraKeyframe
                                {
                                    TimeSeconds = 2,
                                    Position = new Position2D(20, 10),
                                    Zoom = 1.5,
                                    Interpolation = EasingType.Linear
                                }
                            }
                            : keyframes)
                    ]
                }
            }
        };
    }
}
