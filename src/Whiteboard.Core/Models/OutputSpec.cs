namespace Whiteboard.Core.Models;

public record OutputSpec
{
    public int Width { get; init; } = 1920;
    public int Height { get; init; } = 1080;
    public double FrameRate { get; init; } = 30;
    public string BackgroundColorHex { get; init; } = "#FFFFFF";
}
