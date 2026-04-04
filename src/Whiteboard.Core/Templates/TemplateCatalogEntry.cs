using System.Text.Json.Serialization;

namespace Whiteboard.Core.Templates;

public record TemplateCatalogEntry
{
    [JsonPropertyName("templateId")]
    public string TemplateId { get; init; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("entryPath")]
    public string EntryPath { get; init; } = string.Empty;
}
