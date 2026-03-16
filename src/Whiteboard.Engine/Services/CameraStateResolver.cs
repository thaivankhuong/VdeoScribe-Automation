using System.Linq;
using Whiteboard.Core.Models;
using Whiteboard.Engine.Context;
using Whiteboard.Engine.Models;
using Whiteboard.Engine.Resolvers;

namespace Whiteboard.Engine.Services;

public sealed class CameraStateResolver : ICameraStateResolver
{
    public ResolvedCameraState Resolve(
        VideoProject project,
        FrameContext frameContext,
        System.Collections.Generic.IReadOnlyList<ResolvedTimelineEvent> timelineEvents)
    {
        var keyframes = project.Timeline.CameraTrack.Keyframes;
        if (keyframes.Count == 0)
        {
            return new ResolvedCameraState();
        }

        var applicable = keyframes
            .Where(k => k.TimeSeconds <= frameContext.CurrentTimeSeconds)
            .OrderBy(k => k.TimeSeconds)
            .LastOrDefault();

        var selected = applicable ?? keyframes.OrderBy(k => k.TimeSeconds).First();

        return new ResolvedCameraState
        {
            Position = selected.Position,
            Zoom = selected.Zoom
        };
    }
}
