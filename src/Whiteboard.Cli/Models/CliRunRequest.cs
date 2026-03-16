namespace Whiteboard.Cli.Models;

public record CliRunRequest
{
    public string SpecPath { get; init; } = string.Empty;
    public string? OutputPath { get; init; }
    public int FrameIndex { get; init; }
}
