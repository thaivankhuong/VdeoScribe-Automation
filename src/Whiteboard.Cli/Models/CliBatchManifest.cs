using System.Collections.Generic;

namespace Whiteboard.Cli.Models;

public sealed record CliBatchManifest
{
    // retryLimit = 0 means a job gets one attempt only.
    public int RetryLimit { get; init; }
    // When true, each job must pass deterministic regression baseline checks before batch success.
    public bool EnforceDeterministicQaGates { get; init; }
    // Optional default baseline path used when a job does not override RegressionBaselinePath.
    public string DefaultRegressionBaselinePath { get; init; } = string.Empty;
    public IReadOnlyList<CliBatchJob> Jobs { get; init; } = [];
}
