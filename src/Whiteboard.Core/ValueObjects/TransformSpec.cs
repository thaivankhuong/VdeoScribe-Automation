namespace Whiteboard.Core.ValueObjects;

public record TransformSpec
{
    public Position2D Position { get; init; } = new(0, 0);
    public Size2D Size { get; init; } = new(1, 1);
    public double RotationDegrees { get; init; }
    public double ScaleX { get; init; } = 1;
    public double ScaleY { get; init; } = 1;
    public double Opacity { get; init; } = 1;
}
