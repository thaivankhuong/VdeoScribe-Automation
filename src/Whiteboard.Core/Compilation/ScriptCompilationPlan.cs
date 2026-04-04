using System.Collections.Generic;
using Whiteboard.Core.Templates;
using Whiteboard.Core.Validation;

namespace Whiteboard.Core.Compilation;

public sealed record ScriptCompilationPlan
{
    public IReadOnlyList<ValidationGateResult> Gates { get; init; } = [];
    public IReadOnlyList<ValidationIssue> Issues { get; init; } = [];
    public ScriptCompilationDocument? Document { get; init; }
    public IReadOnlyList<ScriptSectionCompilationPlan> Sections { get; init; } = [];

    public bool Success => Issues.All(issue => issue.Severity != ValidationSeverity.Error);
}

public sealed record ScriptSectionCompilationPlan
{
    public ScriptSectionDefinition Section { get; init; } = new();
    public string TemplateId { get; init; } = string.Empty;
    public string GovernedAssetId { get; init; } = string.Empty;
    public string GovernedEffectProfileId { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string> SlotBindings { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);
    public TemplateInstantiationRequest InstantiationRequest { get; init; } = new();
}
