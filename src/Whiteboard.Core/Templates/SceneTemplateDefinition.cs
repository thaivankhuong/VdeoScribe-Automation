using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Whiteboard.Core.Templates;

public record SceneTemplateDefinition
{
    [JsonPropertyName("templateId")]
    public string TemplateId { get; init; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("slots")]
    public List<TemplateSlotDefinition> Slots { get; init; } = [];

    [JsonPropertyName("sceneFragments")]
    public List<TemplateSceneFragment> SceneFragments { get; init; } = [];

    [JsonPropertyName("timelineEventFragments")]
    public List<TemplateTimelineEventFragment> TimelineEventFragments { get; init; } = [];
}
