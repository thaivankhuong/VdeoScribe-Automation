using Whiteboard.Engine.Models;

namespace Whiteboard.Renderer.Models;

public record RenderFrameRequest
{
    public ResolvedFrameState FrameState { get; init; }
    public RenderSurfaceSize SurfaceSize { get; init; }
}
