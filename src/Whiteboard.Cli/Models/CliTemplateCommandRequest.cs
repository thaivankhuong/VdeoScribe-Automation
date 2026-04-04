using System.Collections.Generic;
using Whiteboard.Core.Validation;

namespace Whiteboard.Cli.Models;

public sealed record CliTemplateValidateRequest
{
    public string TemplateId { get; init; } = string.Empty;
    public string? CatalogPath { get; init; }
    public string? SlotValuesPath { get; init; }
}

public sealed record CliTemplateInstantiateRequest
{
    public string TemplateId { get; init; } = string.Empty;
    public string? CatalogPath { get; init; }
    public string SlotValuesPath { get; init; } = string.Empty;
    public string OutputPath { get; init; } = string.Empty;
    public string InstanceId { get; init; } = string.Empty;
    public double TimeOffsetSeconds { get; init; }
    public int LayerOffset { get; init; }
}

public sealed record CliTemplateValidateResult
{
    public bool Success { get; init; }
    public string TemplateId { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string SlotValidationStatus { get; init; } = "skipped";
    public IReadOnlyList<ValidationIssue> Issues { get; init; } = [];
}

public sealed record CliTemplateInstantiateResult
{
    public bool Success { get; init; }
    public string TemplateId { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string InstanceId { get; init; } = string.Empty;
    public string OutputPath { get; init; } = string.Empty;
    public string SlotValidationStatus { get; init; } = "failed";
    public string DeterministicKey { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string> SlotBindings { get; init; } = new Dictionary<string, string>();
    public IReadOnlyList<ValidationIssue> Issues { get; init; } = [];
}
