using Whiteboard.Cli.Models;

namespace Whiteboard.Cli.Contracts;

public interface IScriptCompilationOrchestrator
{
    CliScriptCompileCommandResult Compile(CliScriptCompileCommandRequest request);
}
