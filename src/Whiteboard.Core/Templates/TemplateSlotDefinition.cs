using System.Text.Json.Serialization;

namespace Whiteboard.Core.Templates;

public record TemplateSlotDefinition
{
    [JsonPropertyName("slotId")]
    public string SlotId { get; init; } = string.Empty;

    [JsonPropertyName("valueType")]
    public string ValueType { get; init; } = string.Empty;

    [JsonPropertyName("required")]
    public bool Required { get; init; }

    [JsonPropertyName("defaultValue")]
    public string? DefaultValue { get; init; }

    [JsonPropertyName("constraints")]
    public TemplateSlotConstraint Constraints { get; init; } = new();
}
