using Whiteboard.Core.Enums;
using Whiteboard.Core.ValueObjects;

namespace Whiteboard.Engine.Models;

public record ResolvedCameraState
{
    public double FrameTimeSeconds { get; init; }
    public Position2D Position { get; init; } = new(0, 0);
    public double Zoom { get; init; } = 1;
    public EasingType Interpolation { get; init; } = EasingType.Linear;
}
