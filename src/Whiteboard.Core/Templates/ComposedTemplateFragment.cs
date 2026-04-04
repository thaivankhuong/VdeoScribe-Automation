using System.Collections.Generic;
using System.Text.Json.Serialization;
using Whiteboard.Core.Scene;
using Whiteboard.Core.Timeline;

namespace Whiteboard.Core.Templates;

public sealed record ComposedTemplateFragment
{
    [JsonPropertyName("scenes")]
    public IReadOnlyList<SceneDefinition> Scenes { get; init; } = [];

    [JsonPropertyName("timelineEvents")]
    public IReadOnlyList<TimelineEvent> TimelineEvents { get; init; } = [];
}
