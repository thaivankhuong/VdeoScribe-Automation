using System;
using Whiteboard.Cli.Contracts;
using Whiteboard.Cli.Models;
using Whiteboard.Engine.Context;
using Whiteboard.Engine.Resolvers;
using Whiteboard.Engine.Services;
using Whiteboard.Renderer.Contracts;
using Whiteboard.Renderer.Models;
using Whiteboard.Renderer.Services;

namespace Whiteboard.Cli.Services;

public sealed class PipelineOrchestrator : IPipelineOrchestrator
{
    private readonly IProjectSpecLoader _projectSpecLoader;
    private readonly IFrameStateResolver _frameStateResolver;
    private readonly IFrameRenderer _frameRenderer;

    public PipelineOrchestrator(
        IProjectSpecLoader? projectSpecLoader = null,
        IFrameStateResolver? frameStateResolver = null,
        IFrameRenderer? frameRenderer = null)
    {
        _projectSpecLoader = projectSpecLoader ?? new ProjectSpecLoader();
        _frameStateResolver = frameStateResolver ?? new FrameStateResolver();
        _frameRenderer = frameRenderer ?? new FrameRenderer();
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

        return new CliRunResult
        {
            Success = renderResult.Success,
            Message = "Pipeline skeleton executed with placeholder export stage.",
            SpecPath = request.SpecPath,
            FrameIndex = renderResult.FrameIndex,
            SceneCount = renderResult.SceneCount,
            ObjectCount = renderResult.ObjectCount,
            OperationCount = renderResult.Operations.Count,
            Operations = renderResult.Operations,
            ExportStatus = string.IsNullOrWhiteSpace(request.OutputPath)
                ? "Export skipped: no output path provided."
                : $"Export placeholder prepared for '{request.OutputPath}'.",
            DeterministicKey = $"{request.SpecPath}|{request.FrameIndex}|{project.Output.Width}x{project.Output.Height}|{frameRate}"
        };
    }
}
