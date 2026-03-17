using Whiteboard.Core.Normalization;

namespace Whiteboard.Core.Validation;

public interface ISpecProcessingPipeline
{
    SpecProcessingResult Process(string json, string sourcePath);
}

public sealed record SpecProcessingResult(
    IReadOnlyList<ValidationGateResult> Gates,
    IReadOnlyList<ValidationIssue> Issues,
    NormalizedVideoProject? Project)
{
    public bool IsSuccess => Project is not null && Issues.All(issue => issue.Severity != ValidationSeverity.Error);
}
