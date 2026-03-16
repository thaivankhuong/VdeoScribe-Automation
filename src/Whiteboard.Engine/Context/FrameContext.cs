namespace Whiteboard.Engine.Context;

public readonly record struct FrameContext(int FrameIndex, double FrameRate, double CurrentTimeSeconds)
{
    public static FrameContext FromFrameIndex(int frameIndex, double frameRate)
    {
        var safeFrameIndex = frameIndex < 0 ? 0 : frameIndex;
        var currentTimeSeconds = frameRate <= 0 ? 0 : safeFrameIndex / frameRate;

        return new FrameContext(safeFrameIndex, frameRate, currentTimeSeconds);
    }
}
