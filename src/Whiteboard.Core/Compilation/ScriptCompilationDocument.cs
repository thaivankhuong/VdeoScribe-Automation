using System.Collections.Generic;
using System.Text.Json.Serialization;
using Whiteboard.Core.Models;

namespace Whiteboard.Core.Compilation;

public sealed record ScriptCompilationDocument
{
    [JsonPropertyName("scriptId")]
    public string ScriptId { get; init; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; init; } = string.Empty;

    [JsonPropertyName("projectName")]
    public string ProjectName { get; init; } = string.Empty;

    [JsonPropertyName("assetRegistrySnapshotId")]
    public string AssetRegistrySnapshotId { get; init; } = string.Empty;

    [JsonPropertyName("output")]
    public OutputSpec Output { get; init; } = new();

    [JsonPropertyName("sections")]
    public List<ScriptSectionDefinition> Sections { get; init; } = [];
}
