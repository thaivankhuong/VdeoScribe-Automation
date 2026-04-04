namespace Whiteboard.Core.Templates;

public interface ITemplateComposer
{
    TemplateInstantiationResult Compose(TemplateInstantiationRequest request);
}
