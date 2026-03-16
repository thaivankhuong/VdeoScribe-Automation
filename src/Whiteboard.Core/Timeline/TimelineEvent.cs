using System.Collections.Generic;
using Whiteboard.Core.Enums;

namespace Whiteboard.Core.Timeline;

public record TimelineEvent
{
    public string Id { get; init; } = string.Empty;
    public string SceneId { get; init; } = string.Empty;
    public string SceneObjectId { get; init; } = string.Empty;
    public TimelineActionType ActionType { get; init; } = TimelineActionType.Draw;
    public double StartSeconds { get; init; }
    public double DurationSeconds { get; init; }
    public EasingType Easing { get; init; } = EasingType.Linear;
    public Dictionary<string, string> Parameters { get; init; } = [];
}
