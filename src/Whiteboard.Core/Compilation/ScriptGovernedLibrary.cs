using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Whiteboard.Core.Compilation;

public sealed record ScriptGovernedLibrary
{
    [JsonPropertyName("snapshotId")]
    public string SnapshotId { get; init; } = string.Empty;

    [JsonPropertyName("assets")]
    public List<ScriptGovernedAssetDefinition> Assets { get; init; } = [];

    [JsonPropertyName("effectProfiles")]
    public List<ScriptGovernedEffectProfileDefinition> EffectProfiles { get; init; } = [];
}

public sealed record ScriptGovernedAssetDefinition
{
    [JsonPropertyName("assetId")]
    public string AssetId { get; init; } = string.Empty;

    [JsonPropertyName("assetType")]
    public string AssetType { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;
}

public sealed record ScriptGovernedEffectProfileDefinition
{
    [JsonPropertyName("effectProfileId")]
    public string EffectProfileId { get; init; } = string.Empty;

    [JsonPropertyName("actionType")]
    public string ActionType { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;
}
