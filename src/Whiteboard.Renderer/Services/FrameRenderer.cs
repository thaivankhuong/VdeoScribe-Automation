using System;
using System.Linq;
using Whiteboard.Renderer.Contracts;
using Whiteboard.Renderer.Models;

namespace Whiteboard.Renderer.Services;

public sealed class FrameRenderer : IFrameRenderer
{
    private readonly ISceneRenderer _sceneRenderer;

    public FrameRenderer(ISceneRenderer? sceneRenderer = null)
    {
        _sceneRenderer = sceneRenderer ?? new SceneRenderer();
    }

    public RenderFrameResult Render(RenderFrameRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.FrameState);

        var surface = new InMemoryRenderSurface();

        foreach (var scene in request.FrameState.Scenes)
        {
            _sceneRenderer.RenderScene(scene, surface);
        }

        var objectCount = request.FrameState.Scenes.Sum(s => s.Objects.Count);

        return new RenderFrameResult
        {
            FrameIndex = request.FrameState.FrameContext.FrameIndex,
            Success = true,
            SceneCount = request.FrameState.Scenes.Count,
            ObjectCount = objectCount,
            Operations = surface.Operations
        };
    }
}
