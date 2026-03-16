using System.Collections.Generic;
using System.Linq;
using Whiteboard.Core.Models;
using Whiteboard.Engine.Context;
using Whiteboard.Engine.Models;
using Whiteboard.Engine.Resolvers;

namespace Whiteboard.Engine.Services;

public sealed class TimelineResolver : ITimelineResolver
{
    public IReadOnlyList<ResolvedTimelineEvent> Resolve(VideoProject project, FrameContext frameContext)
    {
        return project.Timeline.Events
            .Select(evt => new ResolvedTimelineEvent
            {
                EventId = evt.Id,
                SceneId = evt.SceneId,
                SceneObjectId = evt.SceneObjectId,
                ActionType = evt.ActionType,
                IsActive = frameContext.CurrentTimeSeconds >= evt.StartSeconds
                    && frameContext.CurrentTimeSeconds < evt.StartSeconds + evt.DurationSeconds
            })
            .ToList();
    }
}
