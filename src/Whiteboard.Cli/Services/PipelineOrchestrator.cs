using System;
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
        var renderResult = _frameRenderer.Render(new RenderFrameRequest
        {
            FrameState = frameState,
            SurfaceSize = new RenderSurfaceSize(project.Output.Width, project.Output.Height)
        });

        var exportResult = _exportPipeline.Export(new ExportRequest
        {
            ProjectId = project.Meta.ProjectId,
            Frames = [renderResult],
            Target = new ExportTarget
            {
                OutputPath = request.OutputPath ?? string.Empty
            }
        });

        return new CliRunResult
        {
            Success = renderResult.Success && exportResult.Success,
            Message = "Integrated placeholder pipeline executed through export stage.",
            SpecPath = request.SpecPath,
            FrameIndex = renderResult.FrameIndex,
            SceneCount = renderResult.SceneCount,
            ObjectCount = renderResult.ObjectCount,
            OperationCount = renderResult.Operations.Count,
            ExportedFrameCount = exportResult.ExportedFrameCount,
            OutputPath = exportResult.OutputPath,
            Operations = renderResult.Operations,
            ExportStatus = exportResult.Message,
            DeterministicKey = $"{request.SpecPath}|{request.FrameIndex}|{renderResult.SceneCount}|{renderResult.ObjectCount}|{exportResult.DeterministicKey}"
        };
    }
}
