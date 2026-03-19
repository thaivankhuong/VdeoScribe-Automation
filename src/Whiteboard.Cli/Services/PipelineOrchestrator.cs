using System;
using System.IO;
using System.Linq;
using Whiteboard.Cli.Contracts;
using Whiteboard.Cli.Models;
using Whiteboard.Engine.Context;
using Whiteboard.Engine.Resolvers;
using Whiteboard.Engine.Services;
using Whiteboard.Export.Contracts;
using Whiteboard.Export.Models;
using Whiteboard.Export.Services;
using Whiteboard.Renderer.Contracts;
using Whiteboard.Renderer.Models;
using Whiteboard.Renderer.Services;

namespace Whiteboard.Cli.Services;

public sealed class PipelineOrchestrator : IPipelineOrchestrator
{
    private readonly IProjectSpecLoader _projectSpecLoader;
    private readonly IFrameStateResolver _frameStateResolver;
    private readonly IFrameRenderer _frameRenderer;
    private readonly IExportPipeline _exportPipeline;

    public PipelineOrchestrator(
        IProjectSpecLoader? projectSpecLoader = null,
        IFrameStateResolver? frameStateResolver = null,
        IFrameRenderer? frameRenderer = null,
        IExportPipeline? exportPipeline = null)
    {
        _projectSpecLoader = projectSpecLoader ?? new ProjectSpecLoader();
        _frameStateResolver = frameStateResolver ?? new FrameStateResolver();
        _frameRenderer = frameRenderer ?? new FrameRenderer();
        _exportPipeline = exportPipeline ?? new ExportPipeline();
    }

    public CliRunResult Run(CliRunRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.SpecPath))
        {
            throw new ArgumentException("Spec path is required.", nameof(request));
        }

        var project = _projectSpecLoader.Load(request.SpecPath);
        var frameRate = project.Output.FrameRate <= 0 ? 30 : project.Output.FrameRate;
        var frameContext = FrameContext.FromFrameIndex(request.FrameIndex, frameRate);
        var frameState = _frameStateResolver.Resolve(project, frameContext);
        var specDirectory = Path.GetDirectoryName(Path.GetFullPath(request.SpecPath)) ?? Environment.CurrentDirectory;
        var exportTarget = new ExportTarget
        {
            OutputPath = request.OutputPath ?? string.Empty,
            Format = ResolveTargetFormat(request.OutputPath),
            Width = project.Output.Width,
            Height = project.Output.Height,
            FrameRate = frameRate
        };

        var renderResult = _frameRenderer.Render(new RenderFrameRequest
        {
            FrameState = frameState,
            SurfaceSize = new RenderSurfaceSize(project.Output.Width, project.Output.Height),
            SvgAssets = project.Assets.SvgAssets.ToDictionary(
                asset => asset.Id,
                asset => new SvgRenderAsset
                {
                    Id = asset.Id,
                    Name = asset.Name,
                    SourcePath = ResolveAssetPath(specDirectory, asset.SourcePath)
                },
                StringComparer.Ordinal)
        });

        if (!renderResult.Success)
        {
            return new CliRunResult
            {
                Success = false,
                Message = "Renderer failed before export.",
                SpecPath = request.SpecPath,
                FrameIndex = renderResult.FrameIndex,
                SceneCount = renderResult.SceneCount,
                ObjectCount = renderResult.ObjectCount,
                OperationCount = renderResult.Operations.Count,
                ExportedFrameCount = 0,
                ExportedAudioCueCount = 0,
                OutputPath = request.OutputPath ?? string.Empty,
                Operations = renderResult.Operations,
                ExportSummary = new ExportPackageSummary
                {
                    ProjectId = project.Meta.ProjectId,
                    Format = exportTarget.Format,
                    Width = exportTarget.Width,
                    Height = exportTarget.Height,
                    FrameRate = exportTarget.FrameRate
                },
                ExportStatus = renderResult.Message,
                ExportDeterministicKey = "render-failed",
                DeterministicKey = $"{frameState.DeterministicKey}|{string.Join(';', renderResult.Operations)}|render-failed:{renderResult.Message}"
            };
        }

        var exportResult = _exportPipeline.Export(new ExportRequest
        {
            ProjectId = project.Meta.ProjectId,
            Frames = [renderResult],
            FrameTimings =
            [
                new ExportFrameTiming
                {
                    FrameIndex = renderResult.FrameIndex,
                    StartSeconds = frameState.FrameContext.CurrentTimeSeconds,
                    DurationSeconds = 1d / frameRate
                }
            ],
            AudioCues = project.Timeline.AudioCues.ToArray(),
            AudioAssets = project.Assets.AudioAssets
                .Select(asset => new ExportAudioAssetInput
                {
                    AssetId = asset.Id,
                    Name = asset.Name,
                    DeclaredSourcePath = asset.SourcePath,
                    ResolvedSourcePath = ResolveAssetPath(specDirectory, asset.SourcePath),
                    DefaultVolume = asset.DefaultVolume
                })
                .ToArray(),
            Target = exportTarget
        });

        return new CliRunResult
        {
            Success = renderResult.Success && exportResult.Success,
            Message = exportResult.Success
                ? "Integrated SVG pipeline executed through export stage."
                : "Export packaging failed after renderer completed.",
            SpecPath = request.SpecPath,
            FrameIndex = renderResult.FrameIndex,
            SceneCount = renderResult.SceneCount,
            ObjectCount = renderResult.ObjectCount,
            OperationCount = renderResult.Operations.Count,
            ExportedFrameCount = exportResult.ExportedFrameCount,
            ExportedAudioCueCount = exportResult.ExportedAudioCueCount,
            OutputPath = exportResult.OutputPath,
            Operations = renderResult.Operations,
            ExportFrames = exportResult.Frames,
            ExportAudioCues = exportResult.AudioCues,
            ExportSummary = exportResult.Summary,
            ExportStatus = exportResult.Message,
            ExportDeterministicKey = exportResult.DeterministicKey,
            DeterministicKey = $"{frameState.DeterministicKey}|{string.Join(';', renderResult.Operations)}|{exportResult.DeterministicKey}"
        };
    }

    private static string ResolveAssetPath(string specDirectory, string sourcePath)
    {
        if (Path.IsPathRooted(sourcePath))
        {
            return Path.GetFullPath(sourcePath);
        }

        return Path.GetFullPath(Path.Combine(specDirectory, sourcePath));
    }

    private static string ResolveTargetFormat(string? outputPath)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return "export-package";
        }

        var extension = Path.GetExtension(outputPath);
        return string.IsNullOrWhiteSpace(extension)
            ? "export-package"
            : extension.TrimStart('.').ToLowerInvariant();
    }
}
