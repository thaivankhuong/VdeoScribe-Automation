using Whiteboard.Core.Assets;
using Whiteboard.Core.Enums;
using Whiteboard.Core.Models;
using Whiteboard.Core.Scene;
using Whiteboard.Core.Timeline;
using Whiteboard.Core.ValueObjects;
using Whiteboard.Engine.Context;
using Whiteboard.Engine.Models;
using Whiteboard.Engine.Resolvers;
using Whiteboard.Engine.Services;
using Xunit;

namespace Whiteboard.Engine.Tests;

public sealed class FrameStateResolverContractTests
{
    [Fact]
    public void FrameContext_CanBeCreated()
    {
        var frameContext = FrameContext.FromFrameIndex(frameIndex: 30, frameRate: 30);

        Assert.Equal(30, frameContext.FrameIndex);
        Assert.Equal(30, frameContext.FrameRate);
        Assert.Equal(1, frameContext.CurrentTimeSeconds);
    }

    [Fact]
    public void FrameContext_NegativeFrameIndex_IsClampedToZero()
    {
        var frameContext = FrameContext.FromFrameIndex(frameIndex: -5, frameRate: 30);

        Assert.Equal(0, frameContext.FrameIndex);
        Assert.Equal(0, frameContext.CurrentTimeSeconds);
    }

    [Fact]
    public void Resolver_CanAcceptProjectAndFrameContext_AndProduceResolvedFrameState()
    {
        var project = CreateProject();
        var frameContext = FrameContext.FromFrameIndex(frameIndex: 0, frameRate: 30);
        var resolver = new FrameStateResolver();

        var resolved = resolver.Resolve(project, frameContext);

        Assert.NotNull(resolved);
        Assert.Equal(frameContext, resolved.FrameContext);
        Assert.Single(resolved.Scenes);
        Assert.Single(resolved.TimelineEvents);

        var firstObject = resolved.Scenes[0].Objects[0];
        Assert.Equal(SceneObjectType.Svg, firstObject.Type);
        Assert.Equal("svg-1", firstObject.AssetRefId);
        Assert.Equal(2, firstObject.Layer);
        Assert.Equal(1, firstObject.RevealProgress);
    }

    [Fact]
    public void Resolver_RevealProgress_IsZero_WhenRevealEventIsNotActive()
    {
        var project = CreateProject();
        var frameContext = FrameContext.FromFrameIndex(frameIndex: 120, frameRate: 30);
        var resolver = new FrameStateResolver();

        var resolved = resolver.Resolve(project, frameContext);

        Assert.Equal(0, resolved.Scenes[0].Objects[0].RevealProgress);
    }

    [Fact]
    public void Resolver_UsesNearestCameraKeyframe_AtOrBeforeCurrentTime()
    {
        var project = CreateProject();
        var frameContext = FrameContext.FromFrameIndex(frameIndex: 45, frameRate: 30);
        var resolver = new FrameStateResolver();

        var resolved = resolver.Resolve(project, frameContext);

        Assert.Equal(new Position2D(10, 10), resolved.Camera.Position);
        Assert.Equal(1.2, resolved.Camera.Zoom);
    }

    [Fact]
    public void Resolver_WithSameInput_ProducesDeterministicStructure()
    {
        var project = CreateProject();
        var frameContext = FrameContext.FromFrameIndex(frameIndex: 0, frameRate: 30);
        var resolver = new FrameStateResolver();

        var first = resolver.Resolve(project, frameContext);
        var second = resolver.Resolve(project, frameContext);

        Assert.Equal(first.FrameContext, second.FrameContext);
        Assert.Equal(first.Scenes.Count, second.Scenes.Count);
        Assert.Equal(first.TimelineEvents.Count, second.TimelineEvents.Count);
        Assert.Equal(first.Scenes[0].SceneId, second.Scenes[0].SceneId);
        Assert.Equal(first.TimelineEvents[0].EventId, second.TimelineEvents[0].EventId);
        Assert.Equal(first.Camera.Position, second.Camera.Position);
        Assert.Equal(first.Camera.Zoom, second.Camera.Zoom);
        Assert.Equal(first.Scenes[0].Objects[0].SceneObjectId, second.Scenes[0].Objects[0].SceneObjectId);
        Assert.Equal(first.Scenes[0].Objects[0].Transform.Position, second.Scenes[0].Objects[0].Transform.Position);
        Assert.Equal(first.Scenes[0].Objects[0].Transform.Size, second.Scenes[0].Objects[0].Transform.Size);
        Assert.Equal(first.Scenes[0].Objects[0].IsVisible, second.Scenes[0].Objects[0].IsVisible);
        Assert.Equal(first.TimelineEvents[0].ActionType, second.TimelineEvents[0].ActionType);
        Assert.Equal(first.TimelineEvents[0].IsActive, second.TimelineEvents[0].IsActive);
    }

    [Fact]
    public void Resolver_WithEquivalentRequests_ProducesEquivalentResolvedObjectState()
    {
        var firstProject = CreateProject();
        var secondProject = CreateProject();
        var firstContext = FrameContext.FromFrameIndex(frameIndex: 30, frameRate: 30);
        var secondContext = FrameContext.FromFrameIndex(frameIndex: 30, frameRate: 30);
        var resolver = new FrameStateResolver();

        var first = resolver.Resolve(firstProject, firstContext);
        var second = resolver.Resolve(secondProject, secondContext);
        var firstObject = first.Scenes[0].Objects[0];
        var secondObject = second.Scenes[0].Objects[0];

        Assert.Equal(first.FrameContext, second.FrameContext);
        Assert.Equal(firstObject.SceneObjectId, secondObject.SceneObjectId);
        Assert.Equal(firstObject.Type, secondObject.Type);
        Assert.Equal(firstObject.AssetRefId, secondObject.AssetRefId);
        Assert.Equal(firstObject.Layer, secondObject.Layer);
        Assert.Equal(firstObject.IsVisible, secondObject.IsVisible);
        Assert.Equal(firstObject.RevealProgress, secondObject.RevealProgress);
        Assert.Equal(firstObject.Transform.Position, secondObject.Transform.Position);
        Assert.Equal(firstObject.Transform.Size, secondObject.Transform.Size);
    }

    [Fact]
    public void Resolver_PreservesTimelineResolverOrderingContract()
    {
        var project = CreateProject();
        var frameContext = FrameContext.FromFrameIndex(frameIndex: 0, frameRate: 30);
        var orderedEvents = new[]
        {
            CreateResolvedTimelineEvent("event-2", TimelineActionType.Reveal),
            CreateResolvedTimelineEvent("event-1", TimelineActionType.Draw)
        };

        var resolver = new FrameStateResolver(
            timelineResolver: new StubTimelineResolver(orderedEvents));

        var resolved = resolver.Resolve(project, frameContext);

        Assert.Equal(
            orderedEvents.Select(evt => evt.EventId).ToArray(),
            resolved.TimelineEvents.Select(evt => evt.EventId).ToArray());
    }

    private static VideoProject CreateProject()
    {
        return new VideoProject
        {
            Meta = new ProjectMeta
            {
                ProjectId = "project-001",
                Name = "Engine Contract Test"
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
                            Name = "Shape Object",
                            Type = SceneObjectType.Svg,
                            AssetRefId = "svg-1",
                            IsVisible = true,
                            Layer = 2,
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
                Events =
                [
                    new TimelineEvent
                    {
                        Id = "event-1",
                        SceneId = "scene-1",
                        SceneObjectId = "object-1",
                        ActionType = TimelineActionType.Draw,
                        StartSeconds = 0,
                        DurationSeconds = 2
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
                        },
                        new CameraKeyframe
                        {
                            TimeSeconds = 1,
                            Position = new Position2D(10, 10),
                            Zoom = 1.2
                        },
                        new CameraKeyframe
                        {
                            TimeSeconds = 2,
                            Position = new Position2D(20, 20),
                            Zoom = 1.4
                        }
                    ]
                }
            }
        };
    }

    private static ResolvedTimelineEvent CreateResolvedTimelineEvent(string eventId, TimelineActionType actionType)
    {
        return new ResolvedTimelineEvent
        {
            EventId = eventId,
            SceneId = "scene-1",
            SceneObjectId = "object-1",
            ActionType = actionType,
            StartFrameIndex = 0,
            EndFrameIndexExclusive = 60,
            IsActive = true
        };
    }

    private sealed class StubTimelineResolver(IReadOnlyList<ResolvedTimelineEvent> events) : ITimelineResolver
    {
        public IReadOnlyList<ResolvedTimelineEvent> Resolve(VideoProject project, FrameContext frameContext)
        {
            return events;
        }
    }
}
