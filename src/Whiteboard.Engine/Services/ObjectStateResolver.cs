using System;
using System.Collections.Generic;
using System.Globalization;
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

        var lifecycle = ResolveLifecycleSnapshot(sceneId, sceneObject, frameIndex, objectEvents);

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
            DrawProgress = lifecycle.DrawProgress,
            DrawPathCount = lifecycle.DrawPaths.Count,
            ActiveDrawPathIndex = lifecycle.ActiveDrawPathIndex,
            DrawOrderingKey = lifecycle.DrawOrderingKey,
            DrawPaths = lifecycle.DrawPaths,
            Transform = sceneObject.Transform
        };
    }

    private static LifecycleSnapshot ResolveLifecycleSnapshot(
        string sceneId,
        SceneObject sceneObject,
        int frameIndex,
        IReadOnlyList<ResolvedTimelineEvent> objectEvents)
    {
        var orderedEvents = OrderEvents(objectEvents);
        var drawWindows = BuildDrawWindows(sceneId, sceneObject.Layer, sceneObject.Id, orderedEvents);
        var activeDraw = drawWindows.FirstOrDefault(window => IsActiveAtFrame(window.Event, frameIndex));
        var resetFrame = ResolveResetFrame(frameIndex, orderedEvents, activeDraw?.Event.StartFrameIndex);
        var activeHide = orderedEvents.FirstOrDefault(evt => evt.ActionType == TimelineActionType.Hide
            && evt.StartFrameIndex >= resetFrame
            && IsActiveAtFrame(evt, frameIndex));

        if (activeDraw is null && activeHide is not null)
        {
            return LifecycleSnapshot.Hidden(BuildObjectOrderingKey(sceneId, sceneObject.Layer, sceneObject.Id));
        }

        var cycleDrawWindows = drawWindows
            .Where(window => window.Event.StartFrameIndex >= resetFrame)
            .ToList();
        var resolvedDrawPaths = cycleDrawWindows
            .Select(window => ResolveDrawPath(window, frameIndex))
            .ToList();

        var drawProgress = ResolveAggregateProgress(resolvedDrawPaths, sceneObject.IsVisible, activeHide is null);
        var activePathIndex = resolvedDrawPaths.FindIndex(path => path.IsActive);
        var priorVisibility = ResolvePriorVisibility(sceneObject.IsVisible, frameIndex, orderedEvents);

        if (activeDraw is not null)
        {
            var entering = frameIndex == activeDraw.Event.StartFrameIndex && !priorVisibility;
            return new LifecycleSnapshot(
                entering ? ObjectLifecycleState.Enter : ObjectLifecycleState.Draw,
                IsVisible: true,
                RevealProgress: drawProgress,
                DrawProgress: drawProgress,
                ActiveDrawPathIndex: activePathIndex,
                DrawOrderingKey: BuildObjectOrderingKey(sceneId, sceneObject.Layer, sceneObject.Id),
                DrawPaths: resolvedDrawPaths);
        }

        if (priorVisibility)
        {
            return new LifecycleSnapshot(
                ObjectLifecycleState.Hold,
                IsVisible: true,
                RevealProgress: drawProgress,
                DrawProgress: drawProgress,
                ActiveDrawPathIndex: activePathIndex,
                DrawOrderingKey: BuildObjectOrderingKey(sceneId, sceneObject.Layer, sceneObject.Id),
                DrawPaths: resolvedDrawPaths);
        }

        return LifecycleSnapshot.Hidden(
            BuildObjectOrderingKey(sceneId, sceneObject.Layer, sceneObject.Id),
            resolvedDrawPaths);
    }

    private static List<DrawWindow> BuildDrawWindows(
        string sceneId,
        int layer,
        string objectId,
        IReadOnlyList<ResolvedTimelineEvent> orderedEvents)
    {
        var drawEvents = orderedEvents
            .Where(evt => evt.ActionType is TimelineActionType.Draw or TimelineActionType.Reveal)
            .Select((evt, fallbackIndex) => new DrawWindow(
                evt,
                fallbackIndex,
                TryGetExplicitPathOrder(evt),
                BuildPathOrderingKey(sceneId, layer, objectId, TryGetExplicitPathOrder(evt), fallbackIndex)))
            .OrderBy(window => window.ExplicitPathOrder ?? int.MaxValue)
            .ThenBy(window => window.Event.StartFrameIndex)
            .ThenBy(window => window.Event.EndFrameIndexExclusive)
            .ThenBy(window => window.Event.EventId, StringComparer.Ordinal)
            .ToList();

        return drawEvents
            .Select((window, orderedIndex) => window with
            {
                OrderedPathIndex = orderedIndex,
                OrderingKey = BuildPathOrderingKey(sceneId, layer, objectId, window.ExplicitPathOrder, orderedIndex)
            })
            .ToList();
    }

    private static ResolvedDrawPathState ResolveDrawPath(DrawWindow window, int frameIndex)
    {
        var progress = ResolveWindowProgress(window.Event, frameIndex);
        return new ResolvedDrawPathState
        {
            PathIndex = window.OrderedPathIndex,
            Progress = progress,
            IsActive = IsActiveAtFrame(window.Event, frameIndex),
            OrderingKey = window.OrderingKey
        };
    }

    private static double ResolveWindowProgress(ResolvedTimelineEvent timelineEvent, int frameIndex)
    {
        if (frameIndex < timelineEvent.StartFrameIndex)
        {
            return 0;
        }

        if (frameIndex >= timelineEvent.EndFrameIndexExclusive)
        {
            return 1;
        }

        var durationFrames = Math.Max(1, timelineEvent.EndFrameIndexExclusive - timelineEvent.StartFrameIndex);
        var elapsedFrames = Math.Clamp(frameIndex - timelineEvent.StartFrameIndex + 1, 0, durationFrames);
        return Math.Clamp((double)elapsedFrames / durationFrames, 0, 1);
    }

    private static double ResolveAggregateProgress(
        IReadOnlyList<ResolvedDrawPathState> drawPaths,
        bool baseVisibility,
        bool canRemainVisible)
    {
        if (drawPaths.Count == 0)
        {
            return baseVisibility && canRemainVisible ? 1 : 0;
        }

        return Math.Clamp(drawPaths.Average(path => path.Progress), 0, 1);
    }

    private static int ResolveResetFrame(
        int frameIndex,
        IReadOnlyList<ResolvedTimelineEvent> orderedEvents,
        int? activeDrawStartFrame)
    {
        var resetHides = orderedEvents
            .Where(evt => evt.ActionType == TimelineActionType.Hide)
            .Where(evt => evt.StartFrameIndex <= frameIndex)
            .Where(evt => activeDrawStartFrame is null || evt.StartFrameIndex < activeDrawStartFrame.Value)
            .Select(evt => evt.StartFrameIndex);

        return resetHides.DefaultIfEmpty(int.MinValue).Max();
    }

    private static bool ResolvePriorVisibility(
        bool baseVisibility,
        int frameIndex,
        IReadOnlyList<ResolvedTimelineEvent> orderedEvents)
    {
        var visible = baseVisibility;

        foreach (var timelineEvent in orderedEvents.Where(evt => evt.StartFrameIndex < frameIndex))
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

    private static List<ResolvedTimelineEvent> OrderEvents(IReadOnlyList<ResolvedTimelineEvent> objectEvents)
    {
        return objectEvents
            .OrderBy(evt => evt.StartFrameIndex)
            .ThenBy(evt => evt.EndFrameIndexExclusive)
            .ThenBy(evt => GetActionPrecedence(evt.ActionType))
            .ThenBy(evt => evt.EventId, StringComparer.Ordinal)
            .ToList();
    }

    private static bool IsActiveAtFrame(ResolvedTimelineEvent timelineEvent, int frameIndex)
    {
        return frameIndex >= timelineEvent.StartFrameIndex
            && frameIndex < timelineEvent.EndFrameIndexExclusive;
    }

    private static int? TryGetExplicitPathOrder(ResolvedTimelineEvent timelineEvent)
    {
        foreach (var key in new[] { "pathOrder", "pathIndex", "path" })
        {
            if (timelineEvent.Parameters.TryGetValue(key, out var value)
                && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                && parsed >= 0)
            {
                return parsed;
            }
        }

        return null;
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

    private static string BuildObjectOrderingKey(string sceneId, int layer, string objectId)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{sceneId}:{layer}:{objectId}");
    }

    private static string BuildPathOrderingKey(string sceneId, int layer, string objectId, int? explicitPathOrder, int fallbackIndex)
    {
        var pathToken = explicitPathOrder?.ToString(CultureInfo.InvariantCulture)
            ?? fallbackIndex.ToString(CultureInfo.InvariantCulture);
        return string.Create(CultureInfo.InvariantCulture, $"{sceneId}:{layer}:{objectId}:path:{pathToken}");
    }

    private sealed record DrawWindow(
        ResolvedTimelineEvent Event,
        int FallbackIndex,
        int? ExplicitPathOrder,
        string OrderingKey)
    {
        public int OrderedPathIndex { get; init; } = FallbackIndex;
    }

    private readonly record struct LifecycleSnapshot(
        ObjectLifecycleState State,
        bool IsVisible,
        double RevealProgress,
        double DrawProgress,
        int ActiveDrawPathIndex,
        string DrawOrderingKey,
        IReadOnlyList<ResolvedDrawPathState> DrawPaths)
    {
        public static LifecycleSnapshot Hidden(
            string drawOrderingKey,
            IReadOnlyList<ResolvedDrawPathState>? drawPaths = null)
        {
            return new LifecycleSnapshot(
                ObjectLifecycleState.Exit,
                IsVisible: false,
                RevealProgress: 0,
                DrawProgress: 0,
                ActiveDrawPathIndex: -1,
                DrawOrderingKey: drawOrderingKey,
                DrawPaths: drawPaths ?? []);
        }
    }
}
