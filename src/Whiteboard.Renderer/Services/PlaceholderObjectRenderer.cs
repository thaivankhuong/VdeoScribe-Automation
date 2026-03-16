using Whiteboard.Engine.Models;
using Whiteboard.Renderer.Contracts;

namespace Whiteboard.Renderer.Services;

public sealed class PlaceholderObjectRenderer : IObjectRenderer
{
    public bool CanRender(ResolvedObjectState objectState)
    {
        return true;
    }

    public void RenderObject(ResolvedObjectState objectState, IRenderSurface surface)
    {
        surface.AddOperation($"object:{objectState.SceneObjectId}:type:{objectState.Type}:reveal:{objectState.RevealProgress:0.###}");
    }
}
