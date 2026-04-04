using Whiteboard.Core.Validation;

namespace Whiteboard.Cli.Models;

public sealed record CliScriptCompileCommandRequest
{
    public string InputPath { get; init; } = string.Empty;
    public string SpecOutputPath { get; init; } = string.Empty;
}

public sealed record CliScriptCompileCommandResult
{
    public bool Success { get; init; }
    public string ScriptId { get; init; } = string.Empty;
    public int TemplateCount { get; init; }
    public int SectionCount { get; init; }
    public string SpecOutputPath { get; init; } = string.Empty;
    public string DeterministicKey { get; init; } = string.Empty;
    public IReadOnlyList<ValidationIssue> Issues { get; init; } = [];
}
