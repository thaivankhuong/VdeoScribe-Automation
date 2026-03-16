using System.Collections.Generic;

namespace Whiteboard.Renderer.Contracts;

public interface IRenderSurface
{
    IReadOnlyList<string> Operations { get; }
    void AddOperation(string operation);
}
