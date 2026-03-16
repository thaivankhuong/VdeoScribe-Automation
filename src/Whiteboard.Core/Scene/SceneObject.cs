using Whiteboard.Core.Enums;
using Whiteboard.Core.ValueObjects;

namespace Whiteboard.Core.Scene;

public record SceneObject
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public SceneObjectType Type { get; init; } = SceneObjectType.Svg;
    public string? AssetRefId { get; init; }
    public string? TextContent { get; init; }
    public int Layer { get; init; }
    public bool IsVisible { get; init; } = true;
    public TransformSpec Transform { get; init; } = new();
}
