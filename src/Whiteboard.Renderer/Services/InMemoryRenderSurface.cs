using System.Collections.Generic;
using Whiteboard.Renderer.Contracts;

namespace Whiteboard.Renderer.Services;

public sealed class InMemoryRenderSurface : IRenderSurface
{
    private readonly List<string> _operations = [];

    public IReadOnlyList<string> Operations => _operations;

    public void AddOperation(string operation)
    {
        _operations.Add(operation);
    }
}
