using Whiteboard.Core.Enums;
using Whiteboard.Core.ValueObjects;

namespace Whiteboard.Engine.Models;

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
    public TransformSpec Transform { get; init; } = new();
}
