using System.Collections.Generic;
using Whiteboard.Core.Models;
using Whiteboard.Engine.Context;
using Whiteboard.Engine.Models;

namespace Whiteboard.Engine.Resolvers;

public interface ICameraStateResolver
{
    ResolvedCameraState Resolve(
        VideoProject project,
        FrameContext frameContext,
        IReadOnlyList<ResolvedTimelineEvent> timelineEvents);
}
