using Whiteboard.Renderer.Models;

namespace Whiteboard.Renderer.Contracts;

public interface IFrameRenderer
{
    RenderFrameResult Render(RenderFrameRequest request);
}
