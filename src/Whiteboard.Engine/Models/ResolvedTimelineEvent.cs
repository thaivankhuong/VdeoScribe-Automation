using Whiteboard.Core.Enums;

namespace Whiteboard.Engine.Models;

public record ResolvedTimelineEvent
{
    public string EventId { get; init; } = string.Empty;
    public string SceneId { get; init; } = string.Empty;
    public string SceneObjectId { get; init; } = string.Empty;
    public TimelineActionType ActionType { get; init; } = TimelineActionType.Draw;
    public bool IsActive { get; init; }
}
