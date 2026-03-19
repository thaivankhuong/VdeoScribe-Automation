using System.Collections.Generic;

namespace Whiteboard.Export.Models;

public record ExportResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string OutputPath { get; init; } = string.Empty;
    public int ExportedFrameCount { get; init; }
    public int ExportedAudioCueCount { get; init; }
    public int TotalOperations { get; init; }
    public IReadOnlyList<ExportFramePackage> Frames { get; init; } = [];
    public IReadOnlyList<ExportAudioCuePackage> AudioCues { get; init; } = [];
    public ExportPackageSummary Summary { get; init; } = new();
    public string DeterministicKey { get; init; } = string.Empty;
}

public record ExportFramePackage
{
    public int FrameIndex { get; init; }
    public double StartSeconds { get; init; }
    public double DurationSeconds { get; init; }
    public int SceneCount { get; init; }
    public int ObjectCount { get; init; }
    public IReadOnlyList<string> Operations { get; init; } = [];
}

public record ExportAudioCuePackage
{
    public string CueId { get; init; } = string.Empty;
    public string AudioAssetId { get; init; } = string.Empty;
    public string AudioAssetName { get; init; } = string.Empty;
    public string SourcePath { get; init; } = string.Empty;
    public double StartSeconds { get; init; }
    public double? DurationSeconds { get; init; }
    public double Volume { get; init; } = 1;
    public double DefaultVolume { get; init; } = 1;
}

public record ExportPackageSummary
{
    public string ProjectId { get; init; } = string.Empty;
    public string Format { get; init; } = string.Empty;
    public int Width { get; init; }
    public int Height { get; init; }
    public double FrameRate { get; init; } = 30;
    public int FrameCount { get; init; }
    public int AudioCueCount { get; init; }
    public int TotalOperations { get; init; }
    public double TotalDurationSeconds { get; init; }
}
