using System.Text.RegularExpressions;
using Whiteboard.Core.Validation;

namespace Whiteboard.Core.Templates;

public sealed partial class TemplateSlotValueResolver
{
    public string? ResolveString(
        string? input,
        IReadOnlyDictionary<string, string> slotBindings,
        string path,
        ICollection<ValidationIssue> issues)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        return SlotPlaceholderRegex().Replace(input, match =>
        {
            var slotId = match.Groups["slotId"].Value.Trim();
            if (!slotBindings.TryGetValue(slotId, out var slotValue))
            {
                issues.Add(new ValidationIssue(
                    ValidationGate.Semantic,
                    path,
                    ValidationSeverity.Error,
                    "template.compose.slot_value_missing",
                    $"Template slot '{slotId}' is required for composition but no value was supplied."));
                return match.Value;
            }

            return slotValue;
        });
    }

    [GeneratedRegex(@"\{\{slot:(?<slotId>[^}]+)\}\}", RegexOptions.CultureInvariant)]
    private static partial Regex SlotPlaceholderRegex();
}
