using System.Collections.Generic;
using Whiteboard.Core.Timeline;
using Whiteboard.Renderer.Models;

namespace Whiteboard.Export.Models;

public record ExportRequest
{
    public string ProjectId { get; init; } = string.Empty;
    public IReadOnlyList<RenderFrameResult> Frames { get; init; } = [];
    public IReadOnlyList<ExportFrameTiming> FrameTimings { get; init; } = [];
    public IReadOnlyList<AudioCue> AudioCues { get; init; } = [];
    public IReadOnlyList<ExportAudioAssetInput> AudioAssets { get; init; } = [];
    public ExportTarget Target { get; init; } = new();
}

public record ExportFrameTiming
{
    public int FrameIndex { get; init; }
    public double StartSeconds { get; init; }
    public double DurationSeconds { get; init; }
}

public record ExportAudioAssetInput
{
    public string AssetId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string DeclaredSourcePath { get; init; } = string.Empty;
    public string ResolvedSourcePath { get; init; } = string.Empty;
    public double DefaultVolume { get; init; } = 1;
}
