using System.Collections.Generic;
using System.Text.Json.Serialization;
using Whiteboard.Core.Scene;

namespace Whiteboard.Core.Templates;

public record TemplateSceneFragment
{
    [JsonPropertyName("localId")]
    public string LocalId { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("durationSeconds")]
    public double DurationSeconds { get; init; }

    [JsonPropertyName("objects")]
    public List<SceneObject> Objects { get; init; } = [];
}
