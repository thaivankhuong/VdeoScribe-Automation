using System.Collections.Generic;
using System.Linq;
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

public sealed class DrawProgressionResolutionTests
{
    [Fact]
    public void Progression_IsMonotonicAcrossSequentialPaths()
    {
        var project = CreateProject(
            CreateDrawEvent("draw-b", startFrame: 2, durationFrames: 2, pathOrder: 1),
            CreateDrawEvent("draw-a", startFrame: 0, durationFrames: 2, pathOrder: 0));
        var resolver = new ObjectStateResolver();

        var frame0 = ResolveObject(project, resolver, frameIndex: 0);
        var frame1 = ResolveObject(project, resolver, frameIndex: 1);
        var frame2 = ResolveObject(project, resolver, frameIndex: 2);
        var frame3 = ResolveObject(project, resolver, frameIndex: 3);

        Assert.Equal(0.25, frame0.DrawProgress, 3);
        Assert.Equal(0.5, frame1.DrawProgress, 3);
        Assert.Equal(0.75, frame2.DrawProgress, 3);
        Assert.Equal(1, frame3.DrawProgress, 3);
        Assert.True(frame0.DrawProgress < frame1.DrawProgress);
        Assert.True(frame1.DrawProgress < frame2.DrawProgress);
        Assert.True(frame2.DrawProgress < frame3.DrawProgress);
    }

    [Fact]
    public void Ordering_PrefersExplicitPathOrderMetadata()
    {
        var project = CreateProject(
            CreateDrawEvent("draw-z", startFrame: 2, durationFrames: 2, pathOrder: 2),
            CreateDrawEvent("draw-x", startFrame: 0, durationFrames: 2, pathOrder: 1),
            CreateDrawEvent("draw-y", startFrame: 4, durationFrames: 2, pathOrder: 0));
        var resolver = new ObjectStateResolver();

        var resolved = ResolveObject(project, resolver, frameIndex: 4);

        Assert.Equal(new[] { 0, 1, 2 }, resolved.DrawPaths.Select(path => path.PathIndex).ToArray());
        Assert.Equal(
            new[]
            {
                "scene-1:1:object-1:path:0",
                "scene-1:1:object-1:path:1",
                "scene-1:1:object-1:path:2"
            },
            resolved.DrawPaths.Select(path => path.OrderingKey).ToArray());
    }

    [Fact]
    public void Ordering_FallsBackToDeterministicEventOrderingWhenMetadataMissing()
    {
        var project = CreateProject(
            CreateDrawEvent("draw-b", startFrame: 0, durationFrames: 2),
            CreateDrawEvent("draw-a", startFrame: 0, durationFrames: 2),
            CreateDrawEvent("draw-c", startFrame: 1, durationFrames: 2));
        var resolver = new ObjectStateResolver();

        var resolved = ResolveObject(project, resolver, frameIndex: 0);

        Assert.Equal(
            new[]
            {
                "scene-1:1:object-1:path:0",
                "scene-1:1:object-1:path:1",
                "scene-1:1:object-1:path:2"
            },
            resolved.DrawPaths.Select(path => path.OrderingKey).ToArray());
    }

    [Fact]
    public void RedrawAfterHide_ResetsPriorProgressAndStartsFreshCycle()
    {
        var project = CreateProject(
            CreateDrawEvent("draw-1", startFrame: 0, durationFrames: 2, pathOrder: 0),
            CreateHideEvent("hide-1", startFrame: 2),
            CreateDrawEvent("draw-2", startFrame: 3, durationFrames: 2, pathOrder: 0));
        var resolver = new ObjectStateResolver();

        var hidden = ResolveObject(project, resolver, frameIndex: 2);
        var redraw = ResolveObject(project, resolver, frameIndex: 3);

        Assert.False(hidden.IsVisible);
        Assert.Equal(0, hidden.DrawProgress);
        Assert.Empty(hidden.DrawPaths);

        Assert.True(redraw.IsVisible);
        Assert.Equal(ObjectLifecycleState.Enter, redraw.LifecycleState);
        Assert.Equal(0.5, redraw.DrawProgress, 3);
        Assert.Single(redraw.DrawPaths);
        Assert.Equal(0, redraw.DrawPaths[0].PathIndex);
    }

    [Fact]
    public void OverlappingHideWindow_DoesNotInterruptActiveDrawCycle()
    {
        var project = CreateProject(
            CreateDrawEvent("draw-1", startFrame: 0, durationFrames: 4, pathOrder: 0),
            CreateHideEvent("hide-1", startFrame: 1));
        var resolver = new ObjectStateResolver();

        var frame1 = ResolveObject(project, resolver, frameIndex: 1);
        var frame2 = ResolveObject(project, resolver, frameIndex: 2);
        var frame3 = ResolveObject(project, resolver, frameIndex: 3);

        Assert.True(frame1.IsVisible);
        Assert.Equal(ObjectLifecycleState.Draw, frame1.LifecycleState);
        Assert.Equal(0.5, frame1.DrawProgress, 3);
        Assert.Equal(0.75, frame2.DrawProgress, 3);
        Assert.Equal(1, frame3.DrawProgress, 3);
    }

    [Fact]
    public void RepeatedRuns_ProduceStableDrawOrderingAndProgression()
    {
        var project = CreateProject(
            CreateDrawEvent("draw-b", startFrame: 2, durationFrames: 2, pathOrder: 2),
            CreateDrawEvent("draw-a", startFrame: 0, durationFrames: 2, pathOrder: 0),
            CreateDrawEvent("draw-c", startFrame: 4, durationFrames: 2, pathOrder: 1));
        var resolver = new ObjectStateResolver();

        var first = ResolveObject(project, resolver, frameIndex: 4);
        var second = ResolveObject(project, resolver, frameIndex: 4);

        Assert.Equal(first.DrawProgress, second.DrawProgress);
        Assert.Equal(first.DrawPathCount, second.DrawPathCount);
        Assert.Equal(first.ActiveDrawPathIndex, second.ActiveDrawPathIndex);
        Assert.Equal(first.DrawOrderingKey, second.DrawOrderingKey);
        Assert.Equal(first.DrawPaths, second.DrawPaths);
    }

    private static Whiteboard.Engine.Models.ResolvedObjectState ResolveObject(VideoProject project, ObjectStateResolver resolver, int frameIndex)
    {
        var frameContext = FrameContext.FromFrameIndex(frameIndex, frameRate: 30);
        var timelineEvents = new TimelineResolver().Resolve(project, frameContext);
        return resolver.Resolve(project, frameContext, timelineEvents).Single().Objects.Single();
    }

    private static VideoProject CreateProject(params TimelineEvent[] events)
    {
        return new VideoProject
        {
            Meta = new ProjectMeta
            {
                ProjectId = "project-001",
                Name = "Draw Progression Test"
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
                            Name = "Draw Object",
                            Type = SceneObjectType.Svg,
                            AssetRefId = "svg-1",
                            IsVisible = false,
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
                Events = new List<TimelineEvent>(events),
                CameraTrack = new CameraTrack()
            }
        };
    }

    private static TimelineEvent CreateDrawEvent(string id, int startFrame, int durationFrames, int? pathOrder = null)
    {
        var parameters = new Dictionary<string, string>();
        if (pathOrder is not null)
        {
            parameters["pathOrder"] = pathOrder.Value.ToString();
        }

        return new TimelineEvent
        {
            Id = id,
            SceneId = "scene-1",
            SceneObjectId = "object-1",
            ActionType = TimelineActionType.Draw,
            StartSeconds = startFrame / 30d,
            DurationSeconds = durationFrames / 30d,
            Parameters = parameters
        };
    }

    private static TimelineEvent CreateHideEvent(string id, int startFrame)
    {
        return new TimelineEvent
        {
            Id = id,
            SceneId = "scene-1",
            SceneObjectId = "object-1",
            ActionType = TimelineActionType.Hide,
            StartSeconds = startFrame / 30d,
            DurationSeconds = 1d / 30d
        };
    }
}
