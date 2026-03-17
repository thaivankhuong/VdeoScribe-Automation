using Whiteboard.Core.Validation;
using Xunit;

namespace Whiteboard.Core.Tests;

public sealed class SpecProcessingPipelineTests
{
    [Fact]
    public void Ordering_SortsIssuesByGatePathSeverityCodeAndOccurrence()
    {
        var issues = new[]
        {
            new ValidationIssue(ValidationGate.Readiness, "$.timeline.events[0]", ValidationSeverity.Error, "timeline.event.out_of_range", "Out of range", 0),
            new ValidationIssue(ValidationGate.Contract, "$.meta.name", ValidationSeverity.Error, "contract.required", "Name required", 0),
            new ValidationIssue(ValidationGate.Schema, "$.scenes[0].durationSeconds", ValidationSeverity.Warning, "schema.range", "Duration warning", 0),
            new ValidationIssue(ValidationGate.Schema, "$.scenes[0].durationSeconds", ValidationSeverity.Error, "schema.range", "Duration error", 0),
            new ValidationIssue(ValidationGate.Schema, "$.scenes[0].durationSeconds", ValidationSeverity.Error, "schema.range", "Duration error second", 1),
            new ValidationIssue(ValidationGate.Schema, "$.scenes[0].id", ValidationSeverity.Error, "schema.required", "Scene id required", 0)
        };

        var ordered = ValidationIssueOrdering.Sort(issues);

        Assert.Collection(
            ordered,
            issue => Assert.Equal((ValidationGate.Contract, "$.meta.name", ValidationSeverity.Error, "contract.required", 0), (issue.Gate, issue.Path, issue.Severity, issue.Code, issue.OccurrenceIndex)),
            issue => Assert.Equal((ValidationGate.Schema, "$.scenes[0].durationSeconds", ValidationSeverity.Error, "schema.range", 0), (issue.Gate, issue.Path, issue.Severity, issue.Code, issue.OccurrenceIndex)),
            issue => Assert.Equal((ValidationGate.Schema, "$.scenes[0].durationSeconds", ValidationSeverity.Error, "schema.range", 1), (issue.Gate, issue.Path, issue.Severity, issue.Code, issue.OccurrenceIndex)),
            issue => Assert.Equal((ValidationGate.Schema, "$.scenes[0].durationSeconds", ValidationSeverity.Warning, "schema.range", 0), (issue.Gate, issue.Path, issue.Severity, issue.Code, issue.OccurrenceIndex)),
            issue => Assert.Equal((ValidationGate.Schema, "$.scenes[0].id", ValidationSeverity.Error, "schema.required", 0), (issue.Gate, issue.Path, issue.Severity, issue.Code, issue.OccurrenceIndex)),
            issue => Assert.Equal((ValidationGate.Readiness, "$.timeline.events[0]", ValidationSeverity.Error, "timeline.event.out_of_range", 0), (issue.Gate, issue.Path, issue.Severity, issue.Code, issue.OccurrenceIndex)));
    }

    [Fact]
    public void Ordering_IsStableAcrossRepeatedSorts()
    {
        var issues = new[]
        {
            new ValidationIssue(ValidationGate.Semantic, "$.scenes[0].objects[1].assetRefId", ValidationSeverity.Error, "semantic.asset.missing", "Missing asset", 1),
            new ValidationIssue(ValidationGate.Semantic, "$.scenes[0].objects[1].assetRefId", ValidationSeverity.Error, "semantic.asset.missing", "Missing asset", 0),
            new ValidationIssue(ValidationGate.Schema, "$.output.frameRate", ValidationSeverity.Error, "schema.range", "Frame rate invalid", 0)
        };

        var first = ValidationIssueOrdering.Sort(issues);
        var second = ValidationIssueOrdering.Sort(issues);

        Assert.Equal(first, second);
    }
}
