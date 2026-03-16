using System.Collections.Generic;

namespace Whiteboard.Engine.Models;

public record ResolvedSceneState
{
    public string SceneId { get; init; } = string.Empty;
    public List<ResolvedObjectState> Objects { get; init; } = [];
}
