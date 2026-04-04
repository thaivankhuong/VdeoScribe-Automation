using System.Collections.Generic;
using Whiteboard.Core.Validation;

namespace Whiteboard.Core.Templates;

public sealed record TemplateSlotBindingValidationResult
{
    public bool Success => Issues.All(issue => issue.Severity != ValidationSeverity.Error);

    public IReadOnlyDictionary<string, string> SlotBindings { get; init; } = new Dictionary<string, string>();

    public IReadOnlyList<ValidationIssue> Issues { get; init; } = [];
}
