using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Whiteboard.Export.Contracts;
using Whiteboard.Export.Models;
using Whiteboard.Renderer.Models;

namespace Whiteboard.Export.Services;

public sealed class ExportPipeline : IExportPipeline
{
    public ExportResult Export(ExportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Target);

        var target = NormalizeTarget(request.Target);
        var orderedFrames = (request.Frames ?? [])
            .Select((frame, index) => new IndexedFrame(frame, index))
            .OrderBy(item => item.Frame.FrameIndex)
            .ThenBy(item => item.Index)
            .ToList();

        foreach (var indexedFrame in orderedFrames)
        {
            if (!indexedFrame.Frame.Success)
            {
                return CreateFailureResult(
                    request.ProjectId,
                    target,
                    $"Frame {indexedFrame.Frame.FrameIndex} failed before export packaging: {indexedFrame.Frame.Message}",
                    $"render-failed:{indexedFrame.Frame.FrameIndex}:{indexedFrame.Frame.Message}");
            }
        }

        var frameTimingByFrameIndex = (request.FrameTimings ?? [])
            .Select((timing, index) => new IndexedTiming(timing, index))
            .GroupBy(item => item.Timing.FrameIndex)
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(item => item.Index).First().Timing);

        var packagedFrames = orderedFrames
            .Select(indexedFrame => PackageFrame(indexedFrame.Frame, ResolveFrameTiming(indexedFrame.Frame, frameTimingByFrameIndex, target)))
            .ToList();

        var audioAssetById = (request.AudioAssets ?? [])
            .Select((asset, index) => new IndexedAudioAsset(asset, index))
            .GroupBy(item => item.Asset.AssetId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(item => item.Index).First().Asset,
                StringComparer.Ordinal);

        var packagedAudioCues = new List<ExportAudioCuePackage>();
        foreach (var indexedCue in (request.AudioCues ?? [])
                     .Select((cue, index) => new IndexedCue(cue, index))
                     .OrderBy(item => item.Cue.StartSeconds)
                     .ThenBy(item => item.Cue.Id, StringComparer.Ordinal)
                     .ThenBy(item => item.Cue.AudioAssetId, StringComparer.Ordinal)
                     .ThenBy(item => item.Index))
        {
            if (!audioAssetById.TryGetValue(indexedCue.Cue.AudioAssetId, out var asset))
            {
                return CreateFailureResult(
                    request.ProjectId,
                    target,
                    $"Audio cue '{indexedCue.Cue.Id}' references missing asset '{indexedCue.Cue.AudioAssetId}'.",
                    $"audio-asset-missing:{indexedCue.Cue.Id}:{indexedCue.Cue.AudioAssetId}");
            }

            var resolvedSourcePath = asset.ResolvedSourcePath ?? string.Empty;
            var sourcePath = string.IsNullOrWhiteSpace(asset.DeclaredSourcePath)
                ? resolvedSourcePath
                : asset.DeclaredSourcePath;

            if (string.IsNullOrWhiteSpace(resolvedSourcePath) || !File.Exists(resolvedSourcePath))
            {
                return CreateFailureResult(
                    request.ProjectId,
                    target,
                    $"Audio cue '{indexedCue.Cue.Id}' references missing asset '{indexedCue.Cue.AudioAssetId}' at '{sourcePath}'.",
                    $"audio-path-missing:{indexedCue.Cue.Id}:{indexedCue.Cue.AudioAssetId}:{sourcePath}");
            }

            packagedAudioCues.Add(new ExportAudioCuePackage
            {
                CueId = indexedCue.Cue.Id,
                AudioAssetId = indexedCue.Cue.AudioAssetId,
                AudioAssetName = asset.Name,
                SourcePath = sourcePath,
                StartSeconds = indexedCue.Cue.StartSeconds,
                DurationSeconds = indexedCue.Cue.DurationSeconds,
                Volume = indexedCue.Cue.Volume,
                DefaultVolume = asset.DefaultVolume
            });
        }

        var totalOperations = packagedFrames.Sum(frame => frame.Operations.Count);
        var summary = BuildSummary(request.ProjectId, target, packagedFrames, packagedAudioCues, totalOperations);

        return new ExportResult
        {
            Success = true,
            Message = $"Packaged export metadata for {packagedFrames.Count} frame(s) and {packagedAudioCues.Count} audio cue(s).",
            OutputPath = target.OutputPath,
            ExportedFrameCount = packagedFrames.Count,
            ExportedAudioCueCount = packagedAudioCues.Count,
            TotalOperations = totalOperations,
            Frames = packagedFrames,
            AudioCues = packagedAudioCues,
            Summary = summary,
            DeterministicKey = BuildDeterministicKey(summary, packagedFrames, packagedAudioCues)
        };
    }

    private static ExportFramePackage PackageFrame(RenderFrameResult frame, ExportFrameTiming timing)
    {
        return new ExportFramePackage
        {
            FrameIndex = frame.FrameIndex,
            StartSeconds = timing.StartSeconds,
            DurationSeconds = timing.DurationSeconds,
            SceneCount = frame.SceneCount,
            ObjectCount = frame.ObjectCount,
            Operations = frame.Operations.ToArray()
        };
    }

    private static ExportFrameTiming ResolveFrameTiming(
        RenderFrameResult frame,
        IReadOnlyDictionary<int, ExportFrameTiming> frameTimingByFrameIndex,
        ExportTarget target)
    {
        if (frameTimingByFrameIndex.TryGetValue(frame.FrameIndex, out var timing))
        {
            return timing;
        }

        var normalizedFrameRate = target.FrameRate <= 0 ? 30 : target.FrameRate;
        return new ExportFrameTiming
        {
            FrameIndex = frame.FrameIndex,
            StartSeconds = frame.FrameIndex / normalizedFrameRate,
            DurationSeconds = 1d / normalizedFrameRate
        };
    }

    private static ExportTarget NormalizeTarget(ExportTarget target)
    {
        return target with
        {
            Format = string.IsNullOrWhiteSpace(target.Format) ? "export-package" : target.Format,
            OutputPath = target.OutputPath ?? string.Empty,
            FrameRate = target.FrameRate <= 0 ? 30 : target.FrameRate
        };
    }

    private static ExportPackageSummary BuildSummary(
        string projectId,
        ExportTarget target,
        IReadOnlyList<ExportFramePackage> frames,
        IReadOnlyList<ExportAudioCuePackage> audioCues,
        int totalOperations)
    {
        var frameEnd = frames.Count == 0
            ? 0
            : frames.Max(frame => frame.StartSeconds + frame.DurationSeconds);
        var audioEnd = audioCues.Count == 0
            ? 0
            : audioCues.Max(cue => cue.StartSeconds + (cue.DurationSeconds ?? 0));

        return new ExportPackageSummary
        {
            ProjectId = projectId ?? string.Empty,
            Format = target.Format,
            Width = target.Width,
            Height = target.Height,
            FrameRate = target.FrameRate,
            FrameCount = frames.Count,
            AudioCueCount = audioCues.Count,
            TotalOperations = totalOperations,
            TotalDurationSeconds = Math.Max(frameEnd, audioEnd)
        };
    }

    private static ExportResult CreateFailureResult(
        string projectId,
        ExportTarget target,
        string message,
        string failureKey)
    {
        var summary = new ExportPackageSummary
        {
            ProjectId = projectId ?? string.Empty,
            Format = target.Format,
            Width = target.Width,
            Height = target.Height,
            FrameRate = target.FrameRate,
            FrameCount = 0,
            AudioCueCount = 0,
            TotalOperations = 0,
            TotalDurationSeconds = 0
        };

        return new ExportResult
        {
            Success = false,
            Message = message,
            OutputPath = target.OutputPath,
            ExportedFrameCount = 0,
            ExportedAudioCueCount = 0,
            TotalOperations = 0,
            Summary = summary,
            DeterministicKey = BuildFailureDeterministicKey(summary, failureKey)
        };
    }

    private static string BuildDeterministicKey(
        ExportPackageSummary summary,
        IReadOnlyList<ExportFramePackage> frames,
        IReadOnlyList<ExportAudioCuePackage> audioCues)
    {
        var builder = new StringBuilder();
        AppendSummaryKey(builder, summary);

        foreach (var frame in frames)
        {
            builder.Append("|frame:")
                .Append(frame.FrameIndex)
                .Append(':')
                .Append(FormatDeterministicDouble(frame.StartSeconds))
                .Append(':')
                .Append(FormatDeterministicDouble(frame.DurationSeconds))
                .Append(':')
                .Append(frame.SceneCount)
                .Append(':')
                .Append(frame.ObjectCount)
                .Append(':')
                .Append(frame.Operations.Count);

            foreach (var operation in frame.Operations)
            {
                builder.Append(':');
                AppendSegment(builder, operation);
            }
        }

        foreach (var cue in audioCues)
        {
            builder.Append("|audio:");
            AppendSegment(builder, cue.CueId);
            builder.Append(':');
            AppendSegment(builder, cue.AudioAssetId);
            builder.Append(':');
            AppendSegment(builder, cue.AudioAssetName);
            builder.Append(':');
            AppendSegment(builder, cue.SourcePath);
            builder.Append(':')
                .Append(FormatDeterministicDouble(cue.StartSeconds))
                .Append(':')
                .Append(cue.DurationSeconds.HasValue ? FormatDeterministicDouble(cue.DurationSeconds.Value) : "null")
                .Append(':')
                .Append(FormatDeterministicDouble(cue.Volume))
                .Append(':')
                .Append(FormatDeterministicDouble(cue.DefaultVolume));
        }

        return builder.ToString();
    }

    private static string BuildFailureDeterministicKey(ExportPackageSummary summary, string failureKey)
    {
        var builder = new StringBuilder();
        AppendSummaryKey(builder, summary);
        builder.Append("|failure:");
        AppendSegment(builder, failureKey);
        return builder.ToString();
    }

    private static void AppendSummaryKey(StringBuilder builder, ExportPackageSummary summary)
    {
        builder.Append("project:");
        AppendSegment(builder, summary.ProjectId);
        builder.Append("|target:");
        AppendSegment(builder, summary.Format);
        builder.Append(':')
            .Append(summary.Width)
            .Append('x')
            .Append(summary.Height)
            .Append(':')
            .Append(FormatDeterministicDouble(summary.FrameRate))
            .Append('|')
            .Append(summary.FrameCount)
            .Append('|')
            .Append(summary.AudioCueCount)
            .Append('|')
            .Append(summary.TotalOperations)
            .Append('|')
            .Append(FormatDeterministicDouble(summary.TotalDurationSeconds));
    }

    private static void AppendSegment(StringBuilder builder, string value)
    {
        var normalizedValue = value ?? string.Empty;
        builder.Append(normalizedValue.Length)
            .Append(':')
            .Append(normalizedValue);
    }

    private static string FormatDeterministicDouble(double value)
    {
        return value.ToString("0.######", CultureInfo.InvariantCulture);
    }

    private sealed record IndexedFrame(RenderFrameResult Frame, int Index);
    private sealed record IndexedTiming(ExportFrameTiming Timing, int Index);
    private sealed record IndexedCue(Whiteboard.Core.Timeline.AudioCue Cue, int Index);
    private sealed record IndexedAudioAsset(ExportAudioAssetInput Asset, int Index);
}

