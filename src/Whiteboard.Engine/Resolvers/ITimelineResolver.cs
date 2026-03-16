using System.Collections.Generic;
using Whiteboard.Core.Models;
using Whiteboard.Engine.Context;
using Whiteboard.Engine.Models;

namespace Whiteboard.Engine.Resolvers;

public interface ITimelineResolver
{
    IReadOnlyList<ResolvedTimelineEvent> Resolve(VideoProject project, FrameContext frameContext);
}
