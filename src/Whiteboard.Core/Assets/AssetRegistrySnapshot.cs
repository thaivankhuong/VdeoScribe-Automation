using System;

namespace Whiteboard.Core.Assets;

public record AssetRegistrySnapshot
{
    public string RegistryId { get; init; } = string.Empty;
    public string SnapshotId { get; init; } = string.Empty;
    public string SnapshotVersion { get; init; } = string.Empty;
    public DateTimeOffset? GeneratedUtc { get; init; }
    public string? SourceManifestPath { get; init; }
}
