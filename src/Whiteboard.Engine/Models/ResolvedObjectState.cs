using System.Collections.Generic;
using Whiteboard.Core.Enums;
using Whiteboard.Core.ValueObjects;

namespace Whiteboard.Engine.Models;

public record ResolvedDrawPathState
{
    public int PathIndex { get; init; }
    public double Progress { get; init; }
    public bool IsActive { get; init; }
    public string OrderingKey { get; init; } = string.Empty;
}

public enum ObjectLifecycleState
{
    Exit = 0,
    Enter = 1,
    Draw = 2,
    Hold = 3
}

public record ResolvedObjectState
{
    public string SceneObjectId { get; init; } = string.Empty;
    public SceneObjectType Type { get; init; } = SceneObjectType.Svg;
    public string? AssetRefId { get; init; }
    public string? TextContent { get; init; }
    public int Layer { get; init; }
    public bool IsVisible { get; init; }
    public ObjectLifecycleState LifecycleState { get; init; } = ObjectLifecycleState.Exit;
    public double RevealProgress { get; init; }
    public double DrawProgress { get; init; }
    public int DrawPathCount { get; init; }
    public int ActiveDrawPathIndex { get; init; } = -1;
    public string DrawOrderingKey { get; init; } = string.Empty;
    public IReadOnlyList<ResolvedDrawPathState> DrawPaths { get; init; } = [];
    public TransformSpec Transform { get; init; } = new();
}
