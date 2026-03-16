using System.Collections.Generic;

namespace Whiteboard.Renderer.Models;

public record RenderFrameResult
{
    public int FrameIndex { get; init; }
    public bool Success { get; init; }
    public int SceneCount { get; init; }
    public int ObjectCount { get; init; }
    public IReadOnlyList<string> Operations { get; init; } = [];
}
