using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Whiteboard.Core.Compilation;

public sealed record ScriptGovernedLibrary
{
    [JsonPropertyName("registryId")]
    public string RegistryId { get; init; } = string.Empty;

    [JsonPropertyName("snapshotId")]
    public string SnapshotId { get; init; } = string.Empty;

    [JsonPropertyName("snapshotVersion")]
    public string SnapshotVersion { get; init; } = string.Empty;

    [JsonPropertyName("assets")]
    public List<ScriptGovernedAssetDefinition> Assets { get; init; } = [];

    [JsonPropertyName("effectProfiles")]
    public List<ScriptGovernedEffectProfileDefinition> EffectProfiles { get; init; } = [];
}

public sealed record ScriptGovernedAssetDefinition
{
    [JsonPropertyName("assetId")]
    public string AssetId { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("sourcePath")]
    public string SourcePath { get; init; } = string.Empty;

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

    [JsonPropertyName("minDurationSeconds")]
    public double MinDurationSeconds { get; init; }

    [JsonPropertyName("maxDurationSeconds")]
    public double MaxDurationSeconds { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("parameterBounds")]
    public Dictionary<string, ScriptGovernedEffectParameterBound> ParameterBounds { get; init; } = new(StringComparer.Ordinal);
}

public sealed record ScriptGovernedEffectParameterBound
{
    [JsonPropertyName("key")]
    public string Key { get; init; } = string.Empty;

    [JsonPropertyName("minValue")]
    public double MinValue { get; init; }

    [JsonPropertyName("maxValue")]
    public double MaxValue { get; init; }
}
