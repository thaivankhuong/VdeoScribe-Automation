using System;
using System.Collections.Generic;
using System.Linq;
using Whiteboard.Core.Enums;
using Whiteboard.Core.Models;
using Whiteboard.Core.Timeline;
using Whiteboard.Engine.Context;
using Whiteboard.Engine.Models;
using Whiteboard.Engine.Resolvers;

namespace Whiteboard.Engine.Services;

public sealed class TimelineResolver : ITimelineResolver
{
    public IReadOnlyList<ResolvedTimelineEvent> Resolve(VideoProject project, FrameContext frameContext)
    {
        ArgumentNullException.ThrowIfNull(project);

        return project.Timeline.Events
            .Select(evt => ResolveEvent(evt, frameContext.FrameIndex, frameContext.FrameRate))
            .OrderByDescending(evt => evt.IsActive)
            .ThenBy(evt => GetActionPrecedence(evt.ActionType))
            .ThenBy(evt => GetTargetIdentifier(evt), StringComparer.Ordinal)
            .ThenBy(evt => evt.EventId, StringComparer.Ordinal)
            .ToList();
    }

    private static ResolvedTimelineEvent ResolveEvent(TimelineEvent timelineEvent, int frameIndex, double frameRate)
    {
        var startFrameIndex = ResolveFrameIndex(timelineEvent.StartSeconds, frameRate);
        var endFrameIndexExclusive = ResolveExclusiveEndFrameIndex(
            timelineEvent.StartSeconds,
            timelineEvent.DurationSeconds,
            frameRate);

        return new ResolvedTimelineEvent
        {
            EventId = timelineEvent.Id,
            SceneId = timelineEvent.SceneId,
            SceneObjectId = timelineEvent.SceneObjectId,
            ActionType = timelineEvent.ActionType,
            StartFrameIndex = startFrameIndex,
            EndFrameIndexExclusive = endFrameIndexExclusive,
            IsActive = IsFrameActive(frameIndex, startFrameIndex, endFrameIndexExclusive)
        };
    }

    private static int ResolveFrameIndex(double timeSeconds, double frameRate)
    {
        return FrameContext.TimeToFrameIndex(timeSeconds, frameRate);
    }

    private static int ResolveExclusiveEndFrameIndex(double startSeconds, double durationSeconds, double frameRate)
    {
        return ResolveFrameIndex(startSeconds + Math.Max(0, durationSeconds), frameRate);
    }

    private static bool IsFrameActive(int frameIndex, int startFrameIndex, int endFrameIndexExclusive)
    {
        return frameIndex >= startFrameIndex && frameIndex < endFrameIndexExclusive;
    }

    private static int GetActionPrecedence(TimelineActionType actionType)
    {
        return actionType switch
        {
            TimelineActionType.Draw => 0,
            TimelineActionType.Reveal => 1,
            TimelineActionType.Move => 2,
            TimelineActionType.Scale => 3,
            TimelineActionType.Rotate => 4,
            TimelineActionType.Fade => 5,
            TimelineActionType.Hide => 6,
            _ => int.MaxValue
        };
    }

    private static string GetTargetIdentifier(ResolvedTimelineEvent timelineEvent)
    {
        return string.IsNullOrWhiteSpace(timelineEvent.SceneObjectId)
            ? $"0:{timelineEvent.SceneId}"
            : $"1:{timelineEvent.SceneObjectId}";
    }
}

