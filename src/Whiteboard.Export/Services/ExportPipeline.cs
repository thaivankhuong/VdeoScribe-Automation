using System;
using System.Linq;
using Whiteboard.Export.Contracts;
using Whiteboard.Export.Models;

namespace Whiteboard.Export.Services;

public sealed class ExportPipeline : IExportPipeline
{
    public ExportResult Export(ExportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Target);

        var frames = request.Frames ?? [];
        var totalOperations = frames.Sum(frame => frame.Operations.Count);
        var normalizedOutputPath = request.Target.OutputPath ?? string.Empty;
        var normalizedFormat = string.IsNullOrWhiteSpace(request.Target.Format)
            ? "placeholder-video"
            : request.Target.Format;

        return new ExportResult
        {
            Success = true,
            Message = string.IsNullOrWhiteSpace(normalizedOutputPath)
                ? "Export placeholder completed without output path."
                : $"Export placeholder completed for '{normalizedOutputPath}'.",
            OutputPath = normalizedOutputPath,
            ExportedFrameCount = frames.Count,
            TotalOperations = totalOperations,
            DeterministicKey = $"{request.ProjectId}|{frames.Count}|{totalOperations}|{normalizedFormat}|{normalizedOutputPath}"
        };
    }
}
