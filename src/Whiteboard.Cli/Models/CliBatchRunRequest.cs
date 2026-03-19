namespace Whiteboard.Cli.Models;

public sealed record CliBatchRunRequest
{
    public string ManifestPath { get; init; } = string.Empty;
    public string SummaryOutputPath { get; init; } = string.Empty;
}
