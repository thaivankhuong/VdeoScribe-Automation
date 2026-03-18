using System.Collections.Generic;
using System.Linq;
using Whiteboard.Core.Assets;
using Whiteboard.Core.Enums;
using Whiteboard.Core.Models;
using Whiteboard.Core.Scene;
using Whiteboard.Core.Timeline;
using Whiteboard.Core.ValueObjects;
using Whiteboard.Engine.Context;
using Whiteboard.Engine.Services;
using Xunit;

namespace Whiteboard.Engine.Tests;

public sealed class TimelineResolverDeterminismTests
{
    [Theory]
    [InlineData(0, 30, 0)]
    [InlineData(1d / 30d, 30, 1)]
    [InlineData(0.1, 30, 3)]
    [InlineData(0.100001, 30, 4)]
    [InlineData(-0.25, 30, 0)]
    public void TimeToFrame_UsesFixedFpsBoundarySafePolicy(double timeSeconds, double frameRate, int expectedFrameIndex)
    {
        var frameIndex = FrameContext.TimeToFrameIndex(timeSeconds, frameRate);

        Assert.Equal(expectedFrameIndex, frameIndex);
    }

    [Fact]
    public void TimeToFrame_AlignsFrameIndexAndCurrentTimeAtExactBoundary()
    {
        var frameContext = FrameContext.FromFrameIndex(frameIndex: 3, frameRate: 30);

        Assert.Equal(3, FrameContext.TimeToFrameIndex(frameContext.CurrentTimeSeconds, frameContext.FrameRate));
    }

    [Fact]
    public void Window_UsesInclusiveStartAndExclusiveEndFrames()
    {
        var project = CreateProject(
            new TimelineEvent
            {
                Id = "event-window",
                SceneId = "scene-1",
                SceneObjectId = "object-1",
                ActionType = TimelineActionType.Draw,
                StartSeconds = 0.1,
                DurationSeconds = 0.1
            });

        var resolver = new TimelineResolver();

        var beforeStart = resolver.Resolve(project, FrameContext.FromFrameIndex(frameIndex: 2, frameRate: 30)).Single();
        var atStart = resolver.Resolve(project, FrameContext.FromFrameIndex(frameIndex: 3, frameRate: 30)).Single();
        var lastActive = resolver.Resolve(project, FrameContext.FromFrameIndex(frameIndex: 5, frameRate: 30)).Single();
        var atEnd = resolver.Resolve(project, FrameContext.FromFrameIndex(frameIndex: 6, frameRate: 30)).Single();

        Assert.False(beforeStart.IsActive);
        Assert.True(atStart.IsActive);
        Assert.True(lastActive.IsActive);
        Assert.False(atEnd.IsActive);
        Assert.Equal(3, atStart.StartFrameIndex);
        Assert.Equal(6, atStart.EndFrameIndexExclusive);
    }

    [Fact]
    public void TieBreak_OrdersActiveOverlapsByActionThenTargetThenEventId()
    {
        var project = CreateProject(
            new TimelineEvent
            {
                Id = "event-c",
                SceneId = "scene-1",
                SceneObjectId = "object-2",
                ActionType = TimelineActionType.Draw,
                StartSeconds = 0,
                DurationSeconds = 1
            },
            new TimelineEvent
            {
                Id = "event-b",
                SceneId = "scene-1",
                SceneObjectId = "object-1",
                ActionType = TimelineActionType.Reveal,
                StartSeconds = 0,
                DurationSeconds = 1
            },
            new TimelineEvent
            {
                Id = "event-a",
                SceneId = "scene-1",
                SceneObjectId = "object-1",
                ActionType = TimelineActionType.Draw,
                StartSeconds = 0,
                DurationSeconds = 1
            },
            new TimelineEvent
            {
                Id = "event-d",
                SceneId = "scene-1",
                SceneObjectId = string.Empty,
                ActionType = TimelineActionType.Draw,
                StartSeconds = 0,
                DurationSeconds = 1
            });

        var resolver = new TimelineResolver();

        var resolved = resolver.Resolve(project, FrameContext.FromFrameIndex(frameIndex: 0, frameRate: 30));

        Assert.Equal(
            new[] { "event-d", "event-a", "event-c", "event-b" },
            resolved.Select(evt => evt.EventId).ToArray());
    }

    [Fact]
    public void TieBreak_ProducesStableOrderAcrossRepeatedRuns()
    {
        var project = CreateProject(
            new TimelineEvent
            {
                Id = "event-2",
                SceneId = "scene-1",
                SceneObjectId = "object-1",
                ActionType = TimelineActionType.Draw,
                StartSeconds = 0,
                DurationSeconds = 1
            },
            new TimelineEvent
            {
                Id = "event-1",
                SceneId = "scene-1",
                SceneObjectId = "object-1",
                ActionType = TimelineActionType.Draw,
                StartSeconds = 0,
                DurationSeconds = 1
            });

        var resolver = new TimelineResolver();

        var first = resolver.Resolve(project, FrameContext.FromFrameIndex(frameIndex: 0, frameRate: 30));
        var second = resolver.Resolve(project, FrameContext.FromFrameIndex(frameIndex: 0, frameRate: 30));

        Assert.Equal(
            first.Select(evt => evt.EventId).ToArray(),
            second.Select(evt => evt.EventId).ToArray());
    }

    private static VideoProject CreateProject(params TimelineEvent[] events)
    {
        return new VideoProject
        {
            Meta = new ProjectMeta
            {
                ProjectId = "project-001",
                Name = "Timeline Resolver Determinism"
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
                        CreateObject("object-1", layer: 1),
                        CreateObject("object-2", layer: 2)
                    ]
                }
            ],
            Timeline = new TimelineDefinition
            {
                Events = new List<TimelineEvent>(events),
                CameraTrack = new CameraTrack()
            }
        };
    }

    private static SceneObject CreateObject(string id, int layer)
    {
        return new SceneObject
        {
            Id = id,
            Name = id,
            Type = SceneObjectType.Svg,
            AssetRefId = "svg-1",
            IsVisible = true,
            Layer = layer,
            Transform = new TransformSpec
            {
                Position = new Position2D(100, 100),
                Size = new Size2D(200, 200)
            }
        };
    }
}
