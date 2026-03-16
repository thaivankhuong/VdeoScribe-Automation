using Whiteboard.Core.Enums;

namespace Whiteboard.Core.Assets;

public record FontAsset
{
    public string Id { get; init; } = string.Empty;
    public string FamilyName { get; init; } = string.Empty;
    public string SourcePath { get; init; } = string.Empty;
    public AssetType Type { get; init; } = AssetType.Font;
}
