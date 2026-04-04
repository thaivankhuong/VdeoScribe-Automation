using System.Collections.Generic;

namespace Whiteboard.Cli.Models;

public sealed record CliBatchManifest
{
    // retryLimit = 0 means a job gets one attempt only.
    public int RetryLimit { get; init; }
    public IReadOnlyList<CliBatchJob> Jobs { get; init; } = [];
}
