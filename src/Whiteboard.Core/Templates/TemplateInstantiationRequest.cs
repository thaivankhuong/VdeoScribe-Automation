using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Whiteboard.Core.Templates;

public sealed record TemplateInstantiationRequest
{
    [JsonPropertyName("template")]
    public SceneTemplateDefinition Template { get; init; } = new();

    [JsonPropertyName("slotValues")]
    public IReadOnlyDictionary<string, string> SlotValues { get; init; } = new Dictionary<string, string>();

    [JsonPropertyName("instanceId")]
    public string InstanceId { get; init; } = string.Empty;

    [JsonPropertyName("timeOffsetSeconds")]
    public double TimeOffsetSeconds { get; init; }

    [JsonPropertyName("layerOffset")]
    public int LayerOffset { get; init; }
}
