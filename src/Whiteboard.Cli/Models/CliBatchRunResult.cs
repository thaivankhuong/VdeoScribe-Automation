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
    public int SequenceNumber { get; init; }
    public int RetryLimit { get; init; }
    public int AttemptCount { get; init; }
    public string ManifestPath { get; init; } = string.Empty;
    public string CompiledSpecPath { get; init; } = string.Empty;
    public string ReportOutputPath { get; init; } = string.Empty;
    public string SpecPath { get; init; } = string.Empty;
    public string OutputPath { get; init; } = string.Empty;
    public int? FrameIndex { get; init; }
    public int FirstFrameIndex { get; init; }
    public int LastFrameIndex { get; init; }
    public int PlannedFrameCount { get; init; }
    public int RenderedFrameCount { get; init; }
    public double ProjectDurationSeconds { get; init; }
    public bool Success { get; init; }
    public CliBatchJobStatus FinalStatus { get; init; }
    public CliBatchFailureStage FailureStage { get; init; }
    public string Message { get; init; } = string.Empty;
    public string FailureSummary { get; init; } = string.Empty;
    public string ExportStatus { get; init; } = string.Empty;
    public string ExportPackageRootPath { get; init; } = string.Empty;
    public string ExportManifestPath { get; init; } = string.Empty;
    public string DeterministicKey { get; init; } = string.Empty;
    public string ExportDeterministicKey { get; init; } = string.Empty;
    public string PlayableMediaPath { get; init; } = string.Empty;
    public string PlayableMediaStatus { get; init; } = string.Empty;
    public string PlayableMediaDeterministicKey { get; init; } = string.Empty;
    public long PlayableMediaByteCount { get; init; }
    public string PlayableMediaAudioStatus { get; init; } = string.Empty;
    public int PlayableMediaAudioCueCount { get; init; }
}
