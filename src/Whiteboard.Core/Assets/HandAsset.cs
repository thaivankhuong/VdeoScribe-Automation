using Whiteboard.Core.Enums;
using Whiteboard.Core.ValueObjects;

namespace Whiteboard.Core.Assets;

public record HandAsset
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string SourcePath { get; init; } = string.Empty;
    public AssetType Type { get; init; } = AssetType.Hand;
    public Position2D TipOffset { get; init; } = new(0, 0);
}
