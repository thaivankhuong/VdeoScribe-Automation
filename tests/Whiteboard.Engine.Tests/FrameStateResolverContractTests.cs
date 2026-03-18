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
        Assert.Equal(ObjectLifecycleState.Enter, firstObject.LifecycleState);
        Assert.Equal(0.5, firstObject.RevealProgress, 3);
        Assert.Equal(0, firstObject.DrawProgress);
        Assert.Equal(0, firstObject.DrawPathCount);
        Assert.Equal(-1, firstObject.ActiveDrawPathIndex);
        Assert.Empty(firstObject.DrawPaths);
    }

    [Fact]
    public void Resolver_ObjectLifecycle_IsExit_WhenObjectStartsHiddenAndHasNoRevealHistory()
    {
        var project = CreateProjectWithNoTimelineEvents(initiallyVisible: false);
        var frameContext = FrameContext.FromFrameIndex(frameIndex: 120, frameRate: 30);
        var resolver = new FrameStateResolver();

        var resolved = resolver.Resolve(project, frameContext);

        Assert.Equal(ObjectLifecycleState.Exit, resolved.Scenes[0].Objects[0].LifecycleState);
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
        Assert.Equal(first.Scenes[0].Objects[0].LifecycleState, second.Scenes[0].Objects[0].LifecycleState);
        Assert.Equal(first.TimelineEvents[0].ActionType, second.TimelineEvents[0].ActionType);
        Assert.Equal(first.TimelineEvents[0].IsActive, second.TimelineEvents[0].IsActive);
        Assert.Equal(first.DeterministicKey, second.DeterministicKey);
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
        Assert.Equal(firstObject.LifecycleState, secondObject.LifecycleState);
        Assert.Equal(firstObject.RevealProgress, secondObject.RevealProgress);
        Assert.Equal(firstObject.Transform.Position, secondObject.Transform.Position);
        Assert.Equal(firstObject.Transform.Size, secondObject.Transform.Size);
        Assert.Equal(first.DeterministicKey, second.DeterministicKey);
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

    [Fact]
    public void Resolver_OrdersScenesAndObjectsDeterministically()
    {
        var project = CreateProjectWithOutOfOrderSceneObjects();
        var frameContext = FrameContext.FromFrameIndex(frameIndex: 0, frameRate: 30);
        var resolver = new FrameStateResolver();

        var resolved = resolver.Resolve(project, frameContext);

        Assert.Equal(new[] { "scene-a", "scene-b" }, resolved.Scenes.Select(scene => scene.SceneId).ToArray());
        Assert.Equal(new[] { "object-a", "object-b" }, resolved.Scenes[0].Objects.Select(obj => obj.SceneObjectId).ToArray());
    }

    [Fact]
    public void Contracts_ExposeRendererReadyDrawProgressionFieldsFromResolvedObjects()
    {
        var project = CreateProject();
        var frameContext = FrameContext.FromFrameIndex(frameIndex: 15, frameRate: 30);
        var objectState = new ResolvedObjectState
        {
            SceneObjectId = "object-1",
            Type = SceneObjectType.Svg,
            AssetRefId = "svg-1",
            Layer = 2,
            IsVisible = true,
            LifecycleState = ObjectLifecycleState.Draw,
            RevealProgress = 0.5,
            DrawProgress = 0.75,
            DrawPathCount = 2,
            ActiveDrawPathIndex = 1,
            DrawOrderingKey = "scene-1:2:object-1",
            DrawPaths =
            [
                new ResolvedDrawPathState
                {
                    PathIndex = 0,
                    Progress = 1,
                    OrderingKey = "scene-1:2:object-1:path-0"
                },
                new ResolvedDrawPathState
                {
                    PathIndex = 1,
                    Progress = 0.5,
                    IsActive = true,
                    OrderingKey = "scene-1:2:object-1:path-1"
                }
            ],
            Transform = new TransformSpec
            {
                Position = new Position2D(100, 100),
                Size = new Size2D(200, 200)
            }
        };

        var resolver = new FrameStateResolver(
            objectStateResolver: new StubObjectStateResolver(
                [new ResolvedSceneState { SceneId = "scene-1", Objects = [objectState] }]));

        var resolved = resolver.Resolve(project, frameContext);
        var resolvedObject = resolved.Scenes.Single().Objects.Single();

        Assert.Equal(0.75, resolvedObject.DrawProgress, 3);
        Assert.Equal(2, resolvedObject.DrawPathCount);
        Assert.Equal(1, resolvedObject.ActiveDrawPathIndex);
        Assert.Equal("scene-1:2:object-1", resolvedObject.DrawOrderingKey);
        Assert.Collection(
            resolvedObject.DrawPaths,
            path =>
            {
                Assert.Equal(0, path.PathIndex);
                Assert.Equal(1, path.Progress, 3);
                Assert.False(path.IsActive);
            },
            path =>
            {
                Assert.Equal(1, path.PathIndex);
                Assert.Equal(0.5, path.Progress, 3);
                Assert.True(path.IsActive);
            });
    }

    private static VideoProject CreateProject(bool initiallyVisible = true)
    {
        return CreateProjectCore(
            initiallyVisible,
            new List<TimelineEvent>
            {
                new()
                {
                    Id = "event-1",
                    SceneId = "scene-1",
                    SceneObjectId = "object-1",
                    ActionType = TimelineActionType.Draw,
                    StartSeconds = 0,
                    DurationSeconds = 2d / 30d
                }
            });
    }

    private static VideoProject CreateProjectWithNoTimelineEvents(bool initiallyVisible)
    {
        return CreateProjectCore(initiallyVisible, new List<TimelineEvent>());
    }

    private static VideoProject CreateProjectCore(bool initiallyVisible, List<TimelineEvent> events)
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
                            IsVisible = initiallyVisible,
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
                Events = events,
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

    private static VideoProject CreateProjectWithOutOfOrderSceneObjects()
    {
        return new VideoProject
        {
            Meta = new ProjectMeta
            {
                ProjectId = "project-002",
                Name = "Ordering Test"
            },
            Output = new OutputSpec
            {
                Width = 1280,
                Height = 720,
                FrameRate = 30
            },
            Assets = new AssetCollection(),
            Scenes =
            [
                new SceneDefinition
                {
                    Id = "scene-b",
                    Name = "Scene B",
                    Objects =
                    [
                        new SceneObject { Id = "object-z", Layer = 5, Type = SceneObjectType.Text, IsVisible = true },
                        new SceneObject { Id = "object-y", Layer = 4, Type = SceneObjectType.Text, IsVisible = true }
                    ]
                },
                new SceneDefinition
                {
                    Id = "scene-a",
                    Name = "Scene A",
                    Objects =
                    [
                        new SceneObject { Id = "object-b", Layer = 2, Type = SceneObjectType.Text, IsVisible = true },
                        new SceneObject { Id = "object-a", Layer = 1, Type = SceneObjectType.Text, IsVisible = true }
                    ]
                }
            ],
            Timeline = new TimelineDefinition
            {
                Events = new List<TimelineEvent>(),
                CameraTrack = new CameraTrack()
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

    private sealed class StubObjectStateResolver(IReadOnlyList<ResolvedSceneState> scenes) : IObjectStateResolver
    {
        public IReadOnlyList<ResolvedSceneState> Resolve(
            VideoProject project,
            FrameContext frameContext,
            IReadOnlyList<ResolvedTimelineEvent> timelineEvents)
        {
            return scenes;
        }
    }
}
