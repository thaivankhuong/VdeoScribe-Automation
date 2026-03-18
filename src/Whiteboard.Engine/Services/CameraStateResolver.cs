using System;
using System.Collections.Generic;
using System.Linq;
using Whiteboard.Core.Enums;
using Whiteboard.Core.Models;
using Whiteboard.Core.Timeline;
using Whiteboard.Core.ValueObjects;
using Whiteboard.Engine.Context;
using Whiteboard.Engine.Models;
using Whiteboard.Engine.Resolvers;

namespace Whiteboard.Engine.Services;

public sealed class CameraStateResolver : ICameraStateResolver
{
    private const int DeterministicPrecision = 6;

    public ResolvedCameraState Resolve(
        VideoProject project,
        FrameContext frameContext,
        IReadOnlyList<ResolvedTimelineEvent> timelineEvents)
    {
        var keyframes = BuildEffectiveKeyframes(project.Timeline.CameraTrack.Keyframes);
        if (keyframes.Count == 0)
        {
            return new ResolvedCameraState
            {
                FrameTimeSeconds = Round(frameContext.CurrentTimeSeconds)
            };
        }

        var currentTime = frameContext.CurrentTimeSeconds;
        var exactKeyframe = keyframes.LastOrDefault(keyframe => AreSameTime(keyframe.TimeSeconds, currentTime));
        if (exactKeyframe is not null)
        {
            return CreateResolvedState(exactKeyframe, currentTime);
        }

        var firstKeyframe = keyframes[0];
        if (currentTime < firstKeyframe.TimeSeconds)
        {
            return CreateResolvedState(firstKeyframe, currentTime);
        }

        var lastKeyframe = keyframes[^1];
        if (currentTime > lastKeyframe.TimeSeconds)
        {
            return CreateResolvedState(lastKeyframe, currentTime);
        }

        var leading = keyframes.Last(keyframe => keyframe.TimeSeconds < currentTime);
        var trailing = keyframes.First(keyframe => keyframe.TimeSeconds > currentTime);
        if (leading.Interpolation == EasingType.Step)
        {
            return CreateResolvedState(leading, currentTime);
        }

        var progress = (currentTime - leading.TimeSeconds) / (trailing.TimeSeconds - leading.TimeSeconds);
        return new ResolvedCameraState
        {
            FrameTimeSeconds = Round(currentTime),
            Position = new Position2D(
                Round(Lerp(leading.Position.X, trailing.Position.X, progress)),
                Round(Lerp(leading.Position.Y, trailing.Position.Y, progress))),
            Zoom = Round(Lerp(leading.Zoom, trailing.Zoom, progress)),
            Interpolation = EasingType.Linear
        };
    }

    private static List<EffectiveCameraKeyframe> BuildEffectiveKeyframes(IReadOnlyList<CameraKeyframe> keyframes)
    {
        return keyframes
            .OrderBy(keyframe => keyframe.TimeSeconds)
            .ThenBy(keyframe => keyframe.Position.X)
            .ThenBy(keyframe => keyframe.Position.Y)
            .ThenBy(keyframe => keyframe.Zoom)
            .ThenBy(keyframe => keyframe.Interpolation)
            .ThenBy(keyframe => keyframe.Easing)
            .GroupBy(keyframe => keyframe.TimeSeconds)
            .Select(group => group.Last())
            .Select(keyframe => new EffectiveCameraKeyframe(
                keyframe.TimeSeconds,
                keyframe.Position,
                keyframe.Zoom,
                keyframe.Interpolation))
            .ToList();
    }

    private static ResolvedCameraState CreateResolvedState(EffectiveCameraKeyframe keyframe, double frameTimeSeconds)
    {
        return new ResolvedCameraState
        {
            FrameTimeSeconds = Round(frameTimeSeconds),
            Position = new Position2D(
                Round(keyframe.Position.X),
                Round(keyframe.Position.Y)),
            Zoom = Round(keyframe.Zoom),
            Interpolation = keyframe.Interpolation
        };
    }

    private static bool AreSameTime(double left, double right)
    {
        return Math.Abs(left - right) < 1e-9;
    }

    private static double Lerp(double start, double end, double progress)
    {
        return start + ((end - start) * progress);
    }

    private static double Round(double value)
    {
        return Math.Round(value, DeterministicPrecision, MidpointRounding.AwayFromZero);
    }

    private sealed record EffectiveCameraKeyframe(
        double TimeSeconds,
        Position2D Position,
        double Zoom,
        EasingType Interpolation);
}
