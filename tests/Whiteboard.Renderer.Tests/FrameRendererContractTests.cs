using System.Collections.Generic;
using Whiteboard.Core.Enums;
using Whiteboard.Core.ValueObjects;
using Whiteboard.Engine.Context;
using Whiteboard.Engine.Models;
using Whiteboard.Renderer.Models;
using Whiteboard.Renderer.Services;
using Xunit;

namespace Whiteboard.Renderer.Tests;

public sealed class FrameRendererContractTests
{
    [Fact]
    public void RenderFrameRequest_CanBeCreatedFromResolvedFrameState()
    {
        var frameState = CreateResolvedFrameState();

        var request = new RenderFrameRequest
        {
            FrameState = frameState,
            SurfaceSize = new RenderSurfaceSize(1280, 720)
        };

        Assert.Equal(0, request.FrameState.FrameContext.FrameIndex);
        Assert.Equal(1280, request.SurfaceSize.Width);
        Assert.Equal(720, request.SurfaceSize.Height);
    }

    [Fact]
    public void FrameRenderer_CanAcceptRequest_AndProduceResult()
    {
        var renderer = new FrameRenderer();
        var request = new RenderFrameRequest
        {
            FrameState = CreateResolvedFrameState(),
            SurfaceSize = new RenderSurfaceSize(1280, 720)
        };

        var result = renderer.Render(request);

        Assert.True(result.Success);
        Assert.Equal(0, result.FrameIndex);
        Assert.Equal(1, result.SceneCount);
        Assert.Equal(1, result.ObjectCount);
        Assert.Single(result.Operations);
    }

    [Fact]
    public void FrameRenderer_ProducesDeterministicStructure_ForSameInput()
    {
        var renderer = new FrameRenderer();
        var request = new RenderFrameRequest
        {
            FrameState = CreateResolvedFrameState(),
            SurfaceSize = new RenderSurfaceSize(1280, 720)
        };

        var first = renderer.Render(request);
        var second = renderer.Render(request);

        Assert.Equal(first.Success, second.Success);
        Assert.Equal(first.FrameIndex, second.FrameIndex);
        Assert.Equal(first.SceneCount, second.SceneCount);
        Assert.Equal(first.ObjectCount, second.ObjectCount);
        Assert.Equal(first.Operations.Count, second.Operations.Count);
        Assert.Equal(first.Operations[0], second.Operations[0]);
    }

    [Fact]
    public void FrameRenderer_WithEquivalentRequests_ProducesEquivalentRenderSummary()
    {
        var renderer = new FrameRenderer();
        var firstRequest = new RenderFrameRequest
        {
            FrameState = CreateResolvedFrameState(),
            SurfaceSize = new RenderSurfaceSize(1280, 720)
        };
        var secondRequest = new RenderFrameRequest
        {
            FrameState = CreateResolvedFrameState(),
            SurfaceSize = new RenderSurfaceSize(1280, 720)
        };

        var first = renderer.Render(firstRequest);
        var second = renderer.Render(secondRequest);

        Assert.Equal(first.Success, second.Success);
        Assert.Equal(first.FrameIndex, second.FrameIndex);
        Assert.Equal(first.SceneCount, second.SceneCount);
        Assert.Equal(first.ObjectCount, second.ObjectCount);
        Assert.Equal(first.Operations, second.Operations);
    }

    private static ResolvedFrameState CreateResolvedFrameState()
    {
        return new ResolvedFrameState
        {
            FrameContext = FrameContext.FromFrameIndex(0, 30),
            Camera = new ResolvedCameraState
            {
                Position = new Position2D(0, 0),
                Zoom = 1
            },
            TimelineEvents =
            [
                new ResolvedTimelineEvent
                {
                    EventId = "evt-1",
                    SceneId = "scene-1",
                    SceneObjectId = "obj-1",
                    ActionType = TimelineActionType.Draw,
                    IsActive = true
                }
            ],
            Scenes =
            [
                new ResolvedSceneState
                {
                    SceneId = "scene-1",
                    Objects =
                    [
                        new ResolvedObjectState
                        {
                            SceneObjectId = "obj-1",
                            Type = SceneObjectType.Svg,
                            AssetRefId = "svg-1",
                            Layer = 0,
                            IsVisible = true,
                            RevealProgress = 1,
                            Transform = new TransformSpec
                            {
                                Position = new Position2D(100, 100),
                                Size = new Size2D(200, 200)
                            }
                        }
                    ]
                }
            ]
        };
    }
}
