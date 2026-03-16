using System.Collections.Generic;
using Whiteboard.Engine.Context;

namespace Whiteboard.Engine.Models;

public record ResolvedFrameState
{
    public FrameContext FrameContext { get; init; }
    public List<ResolvedSceneState> Scenes { get; init; } = [];
    public ResolvedCameraState Camera { get; init; } = new();
    public List<ResolvedTimelineEvent> TimelineEvents { get; init; } = [];
}
