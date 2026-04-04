using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Whiteboard.Core.Templates;

public record TemplateCatalog
{
    [JsonPropertyName("catalogVersion")]
    public string CatalogVersion { get; init; } = "1.0.0";

    [JsonPropertyName("templates")]
    public List<TemplateCatalogEntry> Templates { get; init; } = [];
}
