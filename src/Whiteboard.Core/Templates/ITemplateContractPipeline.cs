using Whiteboard.Core.Validation;

namespace Whiteboard.Core.Templates;

public interface ITemplateContractPipeline
{
    TemplateContractProcessingResult Process(string json, string sourcePath);
}

public sealed record TemplateContractProcessingResult(
    IReadOnlyList<ValidationGateResult> Gates,
    IReadOnlyList<ValidationIssue> Issues,
    NormalizedSceneTemplateDefinition? Template)
{
    public bool IsSuccess => Template is not null && Issues.All(issue => issue.Severity != ValidationSeverity.Error);
}

public sealed record NormalizedSceneTemplateDefinition(
    SceneTemplateDefinition Template,
    string CanonicalJson);
