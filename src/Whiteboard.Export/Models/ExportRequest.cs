using System.Collections.Generic;
using Whiteboard.Renderer.Models;

namespace Whiteboard.Export.Models;

public record ExportRequest
{
    public string ProjectId { get; init; } = string.Empty;
    public IReadOnlyList<RenderFrameResult> Frames { get; init; } = [];
    public ExportTarget Target { get; init; } = new();
}
