using System;
using System.Linq;
using Whiteboard.Core.Models;
using Whiteboard.Engine.Context;
using Whiteboard.Engine.Models;
using Whiteboard.Engine.Resolvers;

namespace Whiteboard.Engine.Services;

public sealed class FrameStateResolver : IFrameStateResolver
{
    private readonly ITimelineResolver _timelineResolver;
    private readonly IObjectStateResolver _objectStateResolver;
    private readonly ICameraStateResolver _cameraStateResolver;

    public FrameStateResolver(
        ITimelineResolver? timelineResolver = null,
        IObjectStateResolver? objectStateResolver = null,
        ICameraStateResolver? cameraStateResolver = null)
    {
        _timelineResolver = timelineResolver ?? new TimelineResolver();
        _objectStateResolver = objectStateResolver ?? new ObjectStateResolver();
        _cameraStateResolver = cameraStateResolver ?? new CameraStateResolver();
    }

    public ResolvedFrameState Resolve(VideoProject project, FrameContext frameContext)
    {
        ArgumentNullException.ThrowIfNull(project);

        var resolvedTimelineEvents = _timelineResolver.Resolve(project, frameContext);
        var resolvedScenes = _objectStateResolver.Resolve(project, frameContext, resolvedTimelineEvents).ToList();
        var resolvedCamera = _cameraStateResolver.Resolve(project, frameContext, resolvedTimelineEvents);

        return new ResolvedFrameState
        {
            FrameContext = frameContext,
            TimelineEvents = resolvedTimelineEvents.ToList(),
            Scenes = resolvedScenes,
            Camera = resolvedCamera
        };
    }
}
