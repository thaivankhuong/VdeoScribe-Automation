using System.Collections.Generic;
using System.Text.Json.Serialization;
using Whiteboard.Core.Enums;

namespace Whiteboard.Core.Templates;

public record TemplateTimelineEventFragment
{
    [JsonPropertyName("localId")]
    public string LocalId { get; init; } = string.Empty;

    [JsonPropertyName("sceneLocalId")]
    public string SceneLocalId { get; init; } = string.Empty;

    [JsonPropertyName("sceneObjectLocalId")]
    public string SceneObjectLocalId { get; init; } = string.Empty;

    [JsonPropertyName("actionType")]
    public TimelineActionType ActionType { get; init; } = TimelineActionType.Draw;

    [JsonPropertyName("startSeconds")]
    public double StartSeconds { get; init; }

    [JsonPropertyName("durationSeconds")]
    public double DurationSeconds { get; init; }

    [JsonPropertyName("easing")]
    public EasingType Easing { get; init; } = EasingType.Linear;

    [JsonPropertyName("parameters")]
    public Dictionary<string, string> Parameters { get; init; } = [];
}
