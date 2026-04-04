using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Whiteboard.Core.Compilation;

public sealed record ScriptSectionMappingRule
{
    [JsonPropertyName("sourceField")]
    public string SourceField { get; init; } = string.Empty;

    [JsonPropertyName("slotId")]
    public string SlotId { get; init; } = string.Empty;

    [JsonPropertyName("required")]
    public bool Required { get; init; }

    [JsonPropertyName("governedReferenceType")]
    public string? GovernedReferenceType { get; init; }
}

public sealed record ScriptTemplateMappingDefinition
{
    [JsonPropertyName("templateId")]
    public string TemplateId { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("fieldMappings")]
    public List<ScriptSectionMappingRule> FieldMappings { get; init; } = [];
}
