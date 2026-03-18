using System;

namespace Whiteboard.Engine.Context;

public readonly record struct FrameContext(int FrameIndex, double FrameRate, double CurrentTimeSeconds)
{
    private const double FrameBoundaryTolerance = 1e-9;

    public static FrameContext FromFrameIndex(int frameIndex, double frameRate)
    {
        var safeFrameIndex = frameIndex < 0 ? 0 : frameIndex;
        var currentTimeSeconds = FrameIndexToTimeSeconds(safeFrameIndex, frameRate);

        return new FrameContext(safeFrameIndex, frameRate, currentTimeSeconds);
    }

    public static int TimeToFrameIndex(double timeSeconds, double frameRate)
    {
        if (frameRate <= 0 || timeSeconds <= 0)
        {
            return 0;
        }

        return (int)Math.Ceiling((timeSeconds * frameRate) - FrameBoundaryTolerance);
    }

    public static double FrameIndexToTimeSeconds(int frameIndex, double frameRate)
    {
        if (frameRate <= 0)
        {
            return 0;
        }

        var safeFrameIndex = frameIndex < 0 ? 0 : frameIndex;
        return safeFrameIndex / frameRate;
    }
}
