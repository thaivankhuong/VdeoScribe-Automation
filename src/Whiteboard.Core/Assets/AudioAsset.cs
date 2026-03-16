using Whiteboard.Core.Enums;

namespace Whiteboard.Core.Assets;

public record AudioAsset
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string SourcePath { get; init; } = string.Empty;
    public AssetType Type { get; init; } = AssetType.Audio;
    public double DefaultVolume { get; init; } = 1;
}
