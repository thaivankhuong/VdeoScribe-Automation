using System.Collections.Generic;
using Whiteboard.Core.Enums;

namespace Whiteboard.Core.Timeline;

public record EffectProfile
{
    public string Id { get; init; } = string.Empty;
    public TimelineActionType ActionType { get; init; } = TimelineActionType.Draw;
    public double MinDurationSeconds { get; init; }
    public double MaxDurationSeconds { get; init; }
    public Dictionary<string, EffectParameterBound> ParameterBounds { get; init; } = [];
}
