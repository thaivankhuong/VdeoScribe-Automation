using Whiteboard.Core.Models;
using Whiteboard.Core.Validation;

namespace Whiteboard.Core.Compilation;

public sealed record ScriptCompileResult
{
    public bool Success { get; init; }
    public string ScriptId { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public int TemplateCount { get; init; }
    public int SectionCount { get; init; }
    public VideoProject? Project { get; init; }
    public string CanonicalJson { get; init; } = string.Empty;
    public string DeterministicKey { get; init; } = string.Empty;
    public ScriptCompilationPlan? CompilationPlan { get; init; }
    public IReadOnlyList<ValidationIssue> Issues { get; init; } = [];
}
