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

public sealed class ObjectLifecycleResolutionTests
{
    [Fact]
    public void Contracts_ExposeExplicitLifecycleStatesForObjectFrames()
    {
        var project = CreateLifecycleProject(initiallyVisible: false);
        var resolver = new ObjectStateResolver();

        var enter = ResolveObject(project, resolver, frameIndex: 0);
        var draw = ResolveObject(project, resolver, frameIndex: 1);
        var hold = ResolveObject(project, resolver, frameIndex: 2);
        var exit = ResolveObject(project, resolver, frameIndex: 4);

        Assert.Equal(ObjectLifecycleState.Enter, enter.LifecycleState);
        Assert.True(enter.IsVisible);
        Assert.Equal(0.5, enter.RevealProgress, 3);

        Assert.Equal(ObjectLifecycleState.Draw, draw.LifecycleState);
        Assert.True(draw.IsVisible);
        Assert.Equal(1, draw.RevealProgress, 3);

        Assert.Equal(ObjectLifecycleState.Hold, hold.LifecycleState);
        Assert.True(hold.IsVisible);
        Assert.Equal(1, hold.RevealProgress, 3);

        Assert.Equal(ObjectLifecycleState.Exit, exit.LifecycleState);
        Assert.False(exit.IsVisible);
        Assert.Equal(0, exit.RevealProgress);
    }

    [Fact]
    public void Contracts_DefaultToHoldForInitiallyVisibleObjectsWithoutEvents()
    {
        var project = CreateStaticVisibleProject();
        var resolver = new ObjectStateResolver();

        var resolved = ResolveObject(project, resolver, frameIndex: 0);

        Assert.Equal(ObjectLifecycleState.Hold, resolved.LifecycleState);
        Assert.True(resolved.IsVisible);
        Assert.Equal(1, resolved.RevealProgress);
    }

    [Fact]
    public void Transitions_UsePriorVisibilityAfterRevealCompletes()
    {
        var project = CreateLifecycleProject(initiallyVisible: false);
        var resolver = new ObjectStateResolver();

        var afterRevealCompletes = ResolveObject(project, resolver, frameIndex: 3);

        Assert.Equal(ObjectLifecycleState.Hold, afterRevealCompletes.LifecycleState);
        Assert.True(afterRevealCompletes.IsVisible);
        Assert.Equal(1, afterRevealCompletes.RevealProgress);
    }

    [Fact]
    public void Conflicts_PreferFirstOrderedActiveRevealEventOverLaterHide()
    {
        var project = CreateConflictProject();
        var frameContext = FrameContext.FromFrameIndex(frameIndex: 0, frameRate: 30);
        var timelineEvents = new TimelineResolver().Resolve(project, frameContext);
        var resolver = new ObjectStateResolver();

        var resolved = resolver.Resolve(project, frameContext, timelineEvents);
        var resolvedObject = resolved.Single().Objects.Single();

        Assert.Equal(ObjectLifecycleState.Enter, resolvedObject.LifecycleState);
        Assert.True(resolvedObject.IsVisible);
    }

    [Fact]
    public void Conflicts_ProduceStableLifecycleAcrossRepeatedRuns()
    {
        var project = CreateConflictProject();
        var frameContext = FrameContext.FromFrameIndex(frameIndex: 0, frameRate: 30);
        var timelineResolver = new TimelineResolver();
        var objectResolver = new ObjectStateResolver();

        var first = objectResolver.Resolve(project, frameContext, timelineResolver.Resolve(project, frameContext));
        var second = objectResolver.Resolve(project, frameContext, timelineResolver.Resolve(project, frameContext));

        Assert.Equal(first.Single().Objects.Single().LifecycleState, second.Single().Objects.Single().LifecycleState);
        Assert.Equal(first.Single().Objects.Single().RevealProgress, second.Single().Objects.Single().RevealProgress);
        Assert.Equal(first.Single().Objects.Single().IsVisible, second.Single().Objects.Single().IsVisible);
    }

    private static ResolvedObjectState ResolveObject(VideoProject project, ObjectStateResolver resolver, int frameIndex)
    {
        var frameContext = FrameContext.FromFrameIndex(frameIndex, frameRate: 30);
        var timelineEvents = new TimelineResolver().Resolve(project, frameContext);
        return resolver.Resolve(project, frameContext, timelineEvents).Single().Objects.Single();
    }

    private static VideoProject CreateLifecycleProject(bool initiallyVisible)
    {
        return CreateProject(
            initiallyVisible,
            new TimelineEvent
            {
                Id = "draw-1",
                SceneId = "scene-1",
                SceneObjectId = "object-1",
                ActionType = TimelineActionType.Draw,
                StartSeconds = 0,
                DurationSeconds = 2d / 30d
            },
            new TimelineEvent
            {
                Id = "hide-1",
                SceneId = "scene-1",
                SceneObjectId = "object-1",
                ActionType = TimelineActionType.Hide,
                StartSeconds = 4d / 30d,
                DurationSeconds = 1d / 30d
            });
    }

    private static VideoProject CreateConflictProject()
    {
        return CreateProject(
            initiallyVisible: false,
            new TimelineEvent
            {
                Id = "draw-1",
                SceneId = "scene-1",
                SceneObjectId = "object-1",
                ActionType = TimelineActionType.Draw,
                StartSeconds = 0,
                DurationSeconds = 2d / 30d
            },
            new TimelineEvent
            {
                Id = "hide-1",
                SceneId = "scene-1",
                SceneObjectId = "object-1",
                ActionType = TimelineActionType.Hide,
                StartSeconds = 0,
                DurationSeconds = 2d / 30d
            });
    }

    private static VideoProject CreateStaticVisibleProject()
    {
        return CreateProject(initiallyVisible: true);
    }

    private static VideoProject CreateProject(bool initiallyVisible, params TimelineEvent[] events)
    {
        return new VideoProject
        {
            Meta = new ProjectMeta
            {
                ProjectId = "project-001",
                Name = "Object Lifecycle Contract Test"
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
                            Name = "Lifecycle Object",
                            Type = SceneObjectType.Svg,
                            AssetRefId = "svg-1",
                            IsVisible = initiallyVisible,
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
}
