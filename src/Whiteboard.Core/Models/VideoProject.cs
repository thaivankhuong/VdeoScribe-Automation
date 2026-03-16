using System.Collections.Generic;
using Whiteboard.Core.Assets;
using Whiteboard.Core.Scene;
using Whiteboard.Core.Timeline;

namespace Whiteboard.Core.Models;

public record VideoProject
{
    public ProjectMeta Meta { get; init; } = new();
    public OutputSpec Output { get; init; } = new();
    public AssetCollection Assets { get; init; } = new();
    public List<SceneDefinition> Scenes { get; init; } = [];
    public TimelineDefinition Timeline { get; init; } = new();
}
