using System.Collections.Generic;

namespace Whiteboard.Core.Timeline;

public record CameraTrack
{
    public List<CameraKeyframe> Keyframes { get; init; } = [];
}
