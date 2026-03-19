using System.Collections.Generic;
using Whiteboard.Export.Models;

namespace Whiteboard.Cli.Models;

public record CliRunResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string SpecPath { get; init; } = string.Empty;
    public int FrameIndex { get; init; }
    public int SceneCount { get; init; }
    public int ObjectCount { get; init; }
    public int OperationCount { get; init; }
    public int ExportedFrameCount { get; init; }
    public int ExportedAudioCueCount { get; init; }
    public string OutputPath { get; init; } = string.Empty;
    public IReadOnlyList<string> Operations { get; init; } = [];
    public IReadOnlyList<ExportFramePackage> ExportFrames { get; init; } = [];
    public IReadOnlyList<ExportAudioCuePackage> ExportAudioCues { get; init; } = [];
    public ExportPackageSummary ExportSummary { get; init; } = new();
    public string ExportStatus { get; init; } = string.Empty;
    public string ExportDeterministicKey { get; init; } = string.Empty;
    public string DeterministicKey { get; init; } = string.Empty;
}
