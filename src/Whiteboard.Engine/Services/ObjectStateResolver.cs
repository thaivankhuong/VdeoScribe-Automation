using System;
using System.Collections.Generic;
using System.Linq;
using Whiteboard.Core.Enums;
using Whiteboard.Core.Models;
using Whiteboard.Core.Scene;
using Whiteboard.Engine.Context;
using Whiteboard.Engine.Models;
using Whiteboard.Engine.Resolvers;

namespace Whiteboard.Engine.Services;

public sealed class ObjectStateResolver : IObjectStateResolver
{
    public IReadOnlyList<ResolvedSceneState> Resolve(
        VideoProject project,
        FrameContext frameContext,
        IReadOnlyList<ResolvedTimelineEvent> timelineEvents)
    {
        return project.Scenes
            .OrderBy(scene => scene.Id, StringComparer.Ordinal)
            .Select(scene => new ResolvedSceneState
            {
                SceneId = scene.Id,
                Objects = scene.Objects
                    .OrderBy(obj => obj.Layer)
                    .ThenBy(obj => obj.Id, StringComparer.Ordinal)
                    .Select(obj => ResolveObjectState(scene.Id, obj, frameContext.FrameIndex, timelineEvents))
                    .ToList()
            })
            .ToList();
    }

    private static ResolvedObjectState ResolveObjectState(
        string sceneId,
        SceneObject sceneObject,
        int frameIndex,
        IReadOnlyList<ResolvedTimelineEvent> timelineEvents)
    {
        var objectEvents = timelineEvents
            .Where(evt => evt.SceneId == sceneId && evt.SceneObjectId == sceneObject.Id)
            .ToList();

        var lifecycle = ResolveLifecycleSnapshot(sceneObject.IsVisible, frameIndex, objectEvents);

        return new ResolvedObjectState
        {
            SceneObjectId = sceneObject.Id,
            Type = sceneObject.Type,
            AssetRefId = sceneObject.AssetRefId,
            TextContent = sceneObject.TextContent,
            Layer = sceneObject.Layer,
            IsVisible = lifecycle.IsVisible,
            LifecycleState = lifecycle.State,
            RevealProgress = lifecycle.RevealProgress,
            Transform = sceneObject.Transform
        };
    }

    private static LifecycleSnapshot ResolveLifecycleSnapshot(
        bool baseVisibility,
        int frameIndex,
        IReadOnlyList<ResolvedTimelineEvent> objectEvents)
    {
        var activeEvents = objectEvents
            .Where(evt => IsActiveAtFrame(evt, frameIndex))
            .ToList();

        var priorVisibility = ResolvePriorVisibility(baseVisibility, frameIndex, objectEvents);
        var revealEvent = activeEvents.FirstOrDefault(evt => evt.ActionType is TimelineActionType.Draw or TimelineActionType.Reveal);

        if (revealEvent is not null)
        {
            var durationFrames = Math.Max(1, revealEvent.EndFrameIndexExclusive - revealEvent.StartFrameIndex);
            var elapsedFrames = Math.Clamp(frameIndex - revealEvent.StartFrameIndex + 1, 0, durationFrames);
            var progress = Math.Clamp((double)elapsedFrames / durationFrames, 0, 1);
            var state = frameIndex == revealEvent.StartFrameIndex
                ? ObjectLifecycleState.Enter
                : ObjectLifecycleState.Draw;

            return new LifecycleSnapshot(state, IsVisible: true, progress);
        }

        if (activeEvents.Any(evt => evt.ActionType == TimelineActionType.Hide))
        {
            return new LifecycleSnapshot(ObjectLifecycleState.Exit, IsVisible: false, RevealProgress: 0);
        }

        if (activeEvents.Count > 0)
        {
            return priorVisibility
                ? new LifecycleSnapshot(ObjectLifecycleState.Hold, IsVisible: true, RevealProgress: 1)
                : new LifecycleSnapshot(ObjectLifecycleState.Exit, IsVisible: false, RevealProgress: 0);
        }

        return priorVisibility
            ? new LifecycleSnapshot(ObjectLifecycleState.Hold, IsVisible: true, RevealProgress: 1)
            : new LifecycleSnapshot(ObjectLifecycleState.Exit, IsVisible: false, RevealProgress: 0);
    }

    private static bool ResolvePriorVisibility(
        bool baseVisibility,
        int frameIndex,
        IReadOnlyList<ResolvedTimelineEvent> objectEvents)
    {
        var visible = baseVisibility;

        foreach (var timelineEvent in objectEvents
                     .Where(evt => evt.StartFrameIndex < frameIndex)
                     .OrderBy(evt => evt.StartFrameIndex)
                     .ThenBy(evt => evt.EndFrameIndexExclusive)
                     .ThenBy(evt => GetActionPrecedence(evt.ActionType))
                     .ThenBy(evt => evt.EventId, StringComparer.Ordinal))
        {
            switch (timelineEvent.ActionType)
            {
                case TimelineActionType.Draw:
                case TimelineActionType.Reveal:
                    visible = true;
                    break;
                case TimelineActionType.Hide:
                    visible = false;
                    break;
            }
        }

        return visible;
    }

    private static bool IsActiveAtFrame(ResolvedTimelineEvent timelineEvent, int frameIndex)
    {
        return frameIndex >= timelineEvent.StartFrameIndex
            && frameIndex < timelineEvent.EndFrameIndexExclusive;
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

    private readonly record struct LifecycleSnapshot(
        ObjectLifecycleState State,
        bool IsVisible,
        double RevealProgress);
}

