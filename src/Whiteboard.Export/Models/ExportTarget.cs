namespace Whiteboard.Export.Models;

public record ExportTarget
{
    public string OutputPath { get; init; } = string.Empty;
    public string Format { get; init; } = "placeholder-video";
}
