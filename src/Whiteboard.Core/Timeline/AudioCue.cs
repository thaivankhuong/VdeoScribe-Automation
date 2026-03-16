namespace Whiteboard.Core.Timeline;

public record AudioCue
{
    public string Id { get; init; } = string.Empty;
    public string AudioAssetId { get; init; } = string.Empty;
    public double StartSeconds { get; init; }
    public double? DurationSeconds { get; init; }
    public double Volume { get; init; } = 1;
}
