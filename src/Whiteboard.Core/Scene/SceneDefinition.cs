using System.Collections.Generic;

namespace Whiteboard.Core.Scene;

public record SceneDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public double DurationSeconds { get; init; }
    public List<SceneObject> Objects { get; init; } = [];
}
