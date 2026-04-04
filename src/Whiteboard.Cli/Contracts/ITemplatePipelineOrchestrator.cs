using Whiteboard.Cli.Models;

namespace Whiteboard.Cli.Contracts;

public interface ITemplatePipelineOrchestrator
{
    CliTemplateValidateResult Validate(CliTemplateValidateRequest request);

    CliTemplateInstantiateResult Instantiate(CliTemplateInstantiateRequest request);
}
