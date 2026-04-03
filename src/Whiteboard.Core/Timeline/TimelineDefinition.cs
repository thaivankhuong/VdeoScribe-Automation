using System.Collections.Generic;

namespace Whiteboard.Core.Timeline;

public record TimelineDefinition
{
    public List<TimelineEvent> Events { get; init; } = [];
    public List<EffectProfile> EffectProfiles { get; init; } = [];
    public CameraTrack CameraTrack { get; init; } = new();
    public List<AudioCue> AudioCues { get; init; } = [];
}
