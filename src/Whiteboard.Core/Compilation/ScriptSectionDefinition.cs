using System.Text.Json.Serialization;

namespace Whiteboard.Core.Compilation;

public sealed record ScriptSectionDefinition
{
    [JsonPropertyName("sectionId")]
    public string SectionId { get; init; } = string.Empty;

    [JsonPropertyName("order")]
    public int Order { get; init; }

    [JsonPropertyName("templateId")]
    public string TemplateId { get; init; } = string.Empty;

    [JsonPropertyName("headline")]
    public string? Headline { get; init; }

    [JsonPropertyName("supportingText")]
    public string? SupportingText { get; init; }

    [JsonPropertyName("illustrationAssetId")]
    public string? IllustrationAssetId { get; init; }

    [JsonPropertyName("drawEffectProfileId")]
    public string? DrawEffectProfileId { get; init; }
}
