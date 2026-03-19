namespace Whiteboard.Cli.Models;

public sealed record CliBatchJob
{
    public string JobId { get; init; } = string.Empty;
    public string SpecPath { get; init; } = string.Empty;
    public string OutputPath { get; init; } = string.Empty;
    public int FrameIndex { get; init; }
}
