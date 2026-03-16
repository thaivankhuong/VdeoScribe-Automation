using Whiteboard.Core.Enums;
using Whiteboard.Core.ValueObjects;

namespace Whiteboard.Core.Timeline;

public record CameraKeyframe
{
    public double TimeSeconds { get; init; }
    public Position2D Position { get; init; } = new(0, 0);
    public double Zoom { get; init; } = 1;
    public EasingType Easing { get; init; } = EasingType.Linear;
}
