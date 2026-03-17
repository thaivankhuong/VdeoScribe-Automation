using System.Collections.Generic;

namespace Whiteboard.Core.Validation;

public enum ValidationGate
{
    Contract = 0,
    Schema = 1,
    Normalization = 2,
    Semantic = 3,
    Readiness = 4
}

public enum ValidationSeverity
{
    Error = 0,
    Warning = 1,
    Info = 2
}

public sealed record ValidationIssue(
    ValidationGate Gate,
    string Path,
    ValidationSeverity Severity,
    string Code,
    string Message,
    int OccurrenceIndex = 0);

public sealed record ValidationGateResult(
    ValidationGate Gate,
    IReadOnlyList<ValidationIssue> Issues)
{
    public bool HasErrors => Issues.Any(issue => issue.Severity == ValidationSeverity.Error);
}
