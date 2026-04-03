namespace Whiteboard.Core.Timeline;

public record EffectParameterBound
{
    public string Key { get; init; } = string.Empty;
    public double MinValue { get; init; }
    public double MaxValue { get; init; }
}
