using Whiteboard.Core.Enums;
using Whiteboard.Core.ValueObjects;

namespace Whiteboard.Core.Assets;

public record ImageAsset
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string SourcePath { get; init; } = string.Empty;
    public AssetType Type { get; init; } = AssetType.Image;
    public Size2D? DefaultSize { get; init; }
}
