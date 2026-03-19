using Whiteboard.Cli.Models;

namespace Whiteboard.Cli.Contracts;

public interface IBatchPipelineOrchestrator
{
    CliBatchRunResult Run(CliBatchRunRequest request);
}
