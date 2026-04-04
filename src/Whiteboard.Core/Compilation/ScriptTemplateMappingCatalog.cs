using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Whiteboard.Core.Compilation;

public sealed record ScriptTemplateMappingCatalog
{
    [JsonPropertyName("catalogVersion")]
    public string CatalogVersion { get; init; } = "1.0.0";

    [JsonPropertyName("mappings")]
    public List<ScriptTemplateMappingDefinition> Mappings { get; init; } = [];
}
