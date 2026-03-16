using System.Collections.Generic;
using System.Linq;
using Whiteboard.Engine.Models;
using Whiteboard.Renderer.Contracts;

namespace Whiteboard.Renderer.Services;

public sealed class SceneRenderer : ISceneRenderer
{
    private readonly IReadOnlyList<IObjectRenderer> _objectRenderers;

    public SceneRenderer(IEnumerable<IObjectRenderer>? objectRenderers = null)
    {
        _objectRenderers = objectRenderers?.ToList() ?? [new PlaceholderObjectRenderer()];
    }

    public void RenderScene(ResolvedSceneState sceneState, IRenderSurface surface)
    {
        foreach (var objectState in sceneState.Objects.OrderBy(o => o.Layer).ThenBy(o => o.SceneObjectId))
        {
            var renderer = _objectRenderers.FirstOrDefault(r => r.CanRender(objectState));
            if (renderer is null)
            {
                continue;
            }

            renderer.RenderObject(objectState, surface);
        }
    }
}
