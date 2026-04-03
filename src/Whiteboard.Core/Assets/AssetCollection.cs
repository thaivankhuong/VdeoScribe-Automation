using System.Collections.Generic;

namespace Whiteboard.Core.Assets;

public record AssetCollection
{
    public AssetRegistrySnapshot RegistrySnapshot { get; init; } = new();
    public List<SvgAsset> SvgAssets { get; init; } = [];
    public List<AudioAsset> AudioAssets { get; init; } = [];
    public List<FontAsset> FontAssets { get; init; } = [];
    public List<HandAsset> HandAssets { get; init; } = [];
    public List<ImageAsset> ImageAssets { get; init; } = [];
}
