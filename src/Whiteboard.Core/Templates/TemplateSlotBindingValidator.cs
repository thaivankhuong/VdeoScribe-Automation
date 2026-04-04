using Whiteboard.Core.Validation;

namespace Whiteboard.Core.Templates;

public sealed class TemplateSlotBindingValidator : ITemplateSlotBindingValidator
{
    public TemplateSlotBindingValidationResult Validate(
        SceneTemplateDefinition template,
        IReadOnlyDictionary<string, string>? slotValues)
    {
        var issues = new List<ValidationIssue>();
        var normalizedBindings = new Dictionary<string, string>(StringComparer.Ordinal);
        var providedSlots = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var pair in slotValues ?? new Dictionary<string, string>())
        {
            var slotId = pair.Key.Trim();
            if (string.IsNullOrWhiteSpace(slotId))
            {
                continue;
            }

            providedSlots[slotId] = pair.Value;
        }

        var declaredSlots = template.Slots
            .OrderBy(slot => slot.SlotId, StringComparer.Ordinal)
            .ToDictionary(slot => slot.SlotId, slot => slot, StringComparer.Ordinal);

        foreach (var providedSlot in providedSlots)
        {
            if (!declaredSlots.ContainsKey(providedSlot.Key))
            {
                issues.Add(new ValidationIssue(
                    ValidationGate.Semantic,
                    $"$.slotValues.{providedSlot.Key}",
                    ValidationSeverity.Error,
                    "template.slot.unknown",
                    $"Slot '{providedSlot.Key}' is not declared by template '{template.TemplateId}'."));
            }
        }

        foreach (var slot in template.Slots.OrderBy(slot => slot.SlotId, StringComparer.Ordinal))
        {
            var hasProvidedValue = providedSlots.TryGetValue(slot.SlotId, out var providedValue);
            var hasMeaningfulValue = hasProvidedValue && !string.IsNullOrWhiteSpace(providedValue);

            if (hasMeaningfulValue)
            {
                normalizedBindings[slot.SlotId] = providedValue!;
                continue;
            }

            if (!string.IsNullOrWhiteSpace(slot.DefaultValue))
            {
                normalizedBindings[slot.SlotId] = slot.DefaultValue!;
                continue;
            }

            if (slot.Required)
            {
                issues.Add(new ValidationIssue(
                    ValidationGate.Semantic,
                    $"$.slotValues.{slot.SlotId}",
                    ValidationSeverity.Error,
                    "template.slot.required",
                    $"Required slot '{slot.SlotId}' is missing."));
            }
        }

        return new TemplateSlotBindingValidationResult
        {
            SlotBindings = normalizedBindings
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
            Issues = ValidationIssueOrdering.Sort(issues)
        };
    }
}
