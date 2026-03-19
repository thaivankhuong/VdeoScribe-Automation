using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Whiteboard.Cli.Models;

public sealed record CliBatchRunResult
{
    public int JobCount { get; init; }
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
    public bool Success { get; init; }
    public string DeterministicKey { get; init; } = string.Empty;
    public IReadOnlyList<CliBatchJobResult> Jobs { get; init; } = [];

    [JsonIgnore]
    public string SummaryOutputPath { get; init; } = string.Empty;
}

public sealed record CliBatchJobResult
{
    public string JobId { get; init; } = string.Empty;
    public string SpecPath { get; init; } = string.Empty;
    public string OutputPath { get; init; } = string.Empty;
    public int FrameIndex { get; init; }
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string DeterministicKey { get; init; } = string.Empty;
    public string ExportDeterministicKey { get; init; } = string.Empty;
}
