using Whiteboard.Engine.Models;

namespace Whiteboard.Renderer.Contracts;

public interface ISceneRenderer
{
    void RenderScene(ResolvedSceneState sceneState, IRenderSurface surface);
}
