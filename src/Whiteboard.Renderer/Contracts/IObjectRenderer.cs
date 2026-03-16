using Whiteboard.Engine.Models;

namespace Whiteboard.Renderer.Contracts;

public interface IObjectRenderer
{
    bool CanRender(ResolvedObjectState objectState);
    void RenderObject(ResolvedObjectState objectState, IRenderSurface surface);
}
