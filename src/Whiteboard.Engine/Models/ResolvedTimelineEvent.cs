using System.Collections.Generic;
using Whiteboard.Core.Enums;

namespace Whiteboard.Engine.Models;

public record ResolvedTimelineEvent
{
    public string EventId { get; init; } = string.Empty;
    public string SceneId { get; init; } = string.Empty;
    public string SceneObjectId { get; init; } = string.Empty;
    public TimelineActionType ActionType { get; init; } = TimelineActionType.Draw;
    public EasingType Easing { get; init; } = EasingType.Linear;
    public int StartFrameIndex { get; init; }
    public int EndFrameIndexExclusive { get; init; }
    public bool IsActive { get; init; }
    public IReadOnlyDictionary<string, string> Parameters { get; init; } = new Dictionary<string, string>();
}
