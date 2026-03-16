using System.Collections.Generic;

namespace Whiteboard.Core.Assets;

public record AssetCollection
{
    public List<SvgAsset> SvgAssets { get; init; } = [];
    public List<AudioAsset> AudioAssets { get; init; } = [];
    public List<FontAsset> FontAssets { get; init; } = [];
    public List<HandAsset> HandAssets { get; init; } = [];
}
