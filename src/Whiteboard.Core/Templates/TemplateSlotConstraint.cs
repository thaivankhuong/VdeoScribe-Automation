using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Whiteboard.Core.Templates;

public record TemplateSlotConstraint
{
    [JsonPropertyName("allowAssetId")]
    public bool AllowAssetId { get; init; }

    [JsonPropertyName("allowEffectProfileId")]
    public bool AllowEffectProfileId { get; init; }

    [JsonPropertyName("minValue")]
    public double? MinValue { get; init; }

    [JsonPropertyName("maxValue")]
    public double? MaxValue { get; init; }

    [JsonPropertyName("allowedValues")]
    public List<string> AllowedValues { get; init; } = [];
}
