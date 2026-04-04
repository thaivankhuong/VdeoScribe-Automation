namespace Whiteboard.Cli.Models;

public sealed record CliBatchJob
{
    public string JobId { get; init; } = string.Empty;
    public string ScriptPath { get; init; } = string.Empty;
    public string SpecPath { get; init; } = string.Empty;
    public string OutputPath { get; init; } = string.Empty;
    public string RegressionBaselinePath { get; init; } = string.Empty;
    // retryLimit = 0 means a job gets one attempt only.
    public int? RetryLimit { get; init; }
    public int? FrameIndex { get; init; }
}
