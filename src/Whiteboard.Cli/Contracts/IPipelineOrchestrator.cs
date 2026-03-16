using Whiteboard.Cli.Models;

namespace Whiteboard.Cli.Contracts;

public interface IPipelineOrchestrator
{
    CliRunResult Run(CliRunRequest request);
}
