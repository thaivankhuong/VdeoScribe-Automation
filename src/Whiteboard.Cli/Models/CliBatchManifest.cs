using System.Collections.Generic;

namespace Whiteboard.Cli.Models;

public sealed record CliBatchManifest
{
    public IReadOnlyList<CliBatchJob> Jobs { get; init; } = [];
}
