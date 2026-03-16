using Whiteboard.Core.Models;
using Whiteboard.Engine.Context;
using Whiteboard.Engine.Models;

namespace Whiteboard.Engine.Resolvers;

public interface IFrameStateResolver
{
    ResolvedFrameState Resolve(VideoProject project, FrameContext frameContext);
}
