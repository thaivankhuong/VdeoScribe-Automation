using System.Collections.Generic;

namespace Whiteboard.Core.Templates;

public interface ITemplateSlotBindingValidator
{
    TemplateSlotBindingValidationResult Validate(
        SceneTemplateDefinition template,
        IReadOnlyDictionary<string, string>? slotValues);
}
