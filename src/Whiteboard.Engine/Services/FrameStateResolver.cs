using System;
using System.Globalization;
using System.Linq;
using System.Text;
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
        var resolvedScenes = _objectStateResolver.Resolve(project, frameContext, resolvedTimelineEvents)
            .OrderBy(scene => scene.SceneId, StringComparer.Ordinal)
            .ToList();
        var resolvedCamera = _cameraStateResolver.Resolve(project, frameContext, resolvedTimelineEvents);

        var resolvedFrameState = new ResolvedFrameState
        {
            FrameContext = frameContext,
            TimelineEvents = resolvedTimelineEvents.ToList(),
            Scenes = resolvedScenes,
            Camera = resolvedCamera
        };

        return resolvedFrameState with
        {
            DeterministicKey = BuildDeterministicKey(resolvedFrameState)
        };
    }

    private static string BuildDeterministicKey(ResolvedFrameState frameState)
    {
        var builder = new StringBuilder();
        builder.Append(frameState.FrameContext.FrameIndex)
            .Append('|')
            .Append(FormatDeterministicDouble(frameState.FrameContext.FrameRate))
            .Append('|')
            .Append(FormatDeterministicDouble(frameState.Camera.FrameTimeSeconds))
            .Append('|')
            .Append(frameState.Camera.Interpolation)
            .Append('|')
            .Append(FormatDeterministicDouble(frameState.Camera.Position.X))
            .Append(',')
            .Append(FormatDeterministicDouble(frameState.Camera.Position.Y))
            .Append('|')
            .Append(FormatDeterministicDouble(frameState.Camera.Zoom));

        foreach (var scene in frameState.Scenes)
        {
            builder.Append("|scene:")
                .Append(scene.SceneId);

            foreach (var obj in scene.Objects.OrderBy(o => o.Layer).ThenBy(o => o.SceneObjectId, StringComparer.Ordinal))
            {
                builder.Append("|object:")
                    .Append(obj.SceneObjectId)
                    .Append(':')
                    .Append(obj.Layer)
                    .Append(':')
                    .Append(obj.LifecycleState)
                    .Append(':')
                    .Append(obj.IsVisible)
                    .Append(':')
                    .Append(obj.RevealProgress.ToString("0.###", CultureInfo.InvariantCulture))
                    .Append(':')
                    .Append(obj.DrawProgress.ToString("0.###", CultureInfo.InvariantCulture))
                    .Append(':')
                    .Append(obj.DrawPathCount)
                    .Append(':')
                    .Append(obj.ActiveDrawPathIndex)
                    .Append(':')
                    .Append(obj.DrawOrderingKey)
                    .Append(':')
                    .Append(obj.Transform.Position.X)
                    .Append(',')
                    .Append(obj.Transform.Position.Y)
                    .Append(':')
                    .Append(obj.Transform.Size.Width)
                    .Append(',')
                    .Append(obj.Transform.Size.Height);

                foreach (var drawPath in obj.DrawPaths
                             .OrderBy(path => path.PathIndex)
                             .ThenBy(path => path.OrderingKey, StringComparer.Ordinal))
                {
                    builder.Append(":path:")
                        .Append(drawPath.PathIndex)
                        .Append(':')
                        .Append(drawPath.Progress.ToString("0.###", CultureInfo.InvariantCulture))
                        .Append(':')
                        .Append(drawPath.IsActive)
                        .Append(':')
                        .Append(drawPath.OrderingKey);
                }
            }
        }

        foreach (var timelineEvent in frameState.TimelineEvents)
        {
            builder.Append("|event:")
                .Append(timelineEvent.EventId)
                .Append(':')
                .Append(timelineEvent.ActionType)
                .Append(':')
                .Append(timelineEvent.SceneObjectId)
                .Append(':')
                .Append(timelineEvent.StartFrameIndex)
                .Append(':')
                .Append(timelineEvent.EndFrameIndexExclusive)
                .Append(':')
                .Append(timelineEvent.IsActive);
        }

        return builder.ToString();
    }

    private static string FormatDeterministicDouble(double value)
    {
        return value.ToString("0.######", CultureInfo.InvariantCulture);
    }
}
