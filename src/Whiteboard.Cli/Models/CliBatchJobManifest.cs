using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Whiteboard.Cli.Models;

public sealed record CliBatchJobManifest
{
    public string JobId { get; init; } = string.Empty;
    public int SequenceNumber { get; init; }
    public int RetryLimit { get; init; }
    public int AttemptCount { get; init; }
    public bool Success { get; init; }
    public CliBatchJobStatus FinalStatus { get; init; }
    public CliBatchFailureStage FailureStage { get; init; }
    public string Message { get; init; } = string.Empty;
    public string FailureSummary { get; init; } = string.Empty;
    public string ScriptPath { get; init; } = string.Empty;
    public string RegressionBaselinePath { get; init; } = string.Empty;
    public string OutputPath { get; init; } = string.Empty;
    public string CompiledSpecPath { get; init; } = string.Empty;
    public string ReportOutputPath { get; init; } = string.Empty;
    public CliBatchStageStatus GateStatus { get; init; }
    public string GateReportPath { get; init; } = string.Empty;
    public string GateDeterministicKey { get; init; } = string.Empty;
    public string ExportManifestPath { get; init; } = string.Empty;
    public string ExportDeterministicKey { get; init; } = string.Empty;
    public string PlayableMediaPath { get; init; } = string.Empty;
    public string PlayableMediaDeterministicKey { get; init; } = string.Empty;
    public string DeterministicKey { get; init; } = string.Empty;
    public IReadOnlyList<CliBatchJobAttemptRecord> Attempts { get; init; } = [];
}

public sealed record CliBatchJobAttemptRecord
{
    public int AttemptNumber { get; init; }
    public int RetryLimit { get; init; }
    public bool FinalAttempt { get; init; }
    public bool Success { get; init; }
    public CliBatchStageStatus CompileStatus { get; init; }
    public CliBatchStageStatus RunStatus { get; init; }
    public CliBatchJobStatus FinalStatus { get; init; }
    public CliBatchFailureStage FailureStage { get; init; }
    public string Message { get; init; } = string.Empty;
    public string FailureSummary { get; init; } = string.Empty;
    public string ScriptPath { get; init; } = string.Empty;
    public string RegressionBaselinePath { get; init; } = string.Empty;
    public string OutputPath { get; init; } = string.Empty;
    public string CompiledSpecPath { get; init; } = string.Empty;
    public string ReportOutputPath { get; init; } = string.Empty;
    public string CompileDeterministicKey { get; init; } = string.Empty;
    public CliBatchStageStatus GateStatus { get; init; }
    public string GateReportPath { get; init; } = string.Empty;
    public string GateDeterministicKey { get; init; } = string.Empty;
    public string ExportManifestPath { get; init; } = string.Empty;
    public string ExportDeterministicKey { get; init; } = string.Empty;
    public string PlayableMediaPath { get; init; } = string.Empty;
    public string PlayableMediaDeterministicKey { get; init; } = string.Empty;
    public string DeterministicKey { get; init; } = string.Empty;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CliBatchStageStatus
{
    NotRun = 0,
    Succeeded = 1,
    Failed = 2
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CliBatchJobStatus
{
    Succeeded = 0,
    Failed = 1,
    Invalid = 2
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CliBatchFailureStage
{
    None = 0,
    Manifest = 1,
    Compile = 2,
    Run = 3,
    Gate = 4
}
