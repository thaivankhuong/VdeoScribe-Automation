namespace Whiteboard.Export.Models;

public record ExportTarget
{
    public string OutputPath { get; init; } = string.Empty;
    public string Format { get; init; } = "export-package";
    public int Width { get; init; }
    public int Height { get; init; }
    public double FrameRate { get; init; } = 30;
}
