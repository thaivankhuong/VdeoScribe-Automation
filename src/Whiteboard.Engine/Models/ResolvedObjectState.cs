using Whiteboard.Core.Enums;
using Whiteboard.Core.ValueObjects;

namespace Whiteboard.Engine.Models;

public record ResolvedObjectState
{
    public string SceneObjectId { get; init; } = string.Empty;
    public SceneObjectType Type { get; init; } = SceneObjectType.Svg;
    public string? AssetRefId { get; init; }
    public string? TextContent { get; init; }
    public int Layer { get; init; }
    public bool IsVisible { get; init; }
    public double RevealProgress { get; init; }
    public TransformSpec Transform { get; init; } = new();
}
