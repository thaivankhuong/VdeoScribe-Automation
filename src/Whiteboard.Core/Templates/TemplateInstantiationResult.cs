using System.Collections.Generic;
using System.Text.Json.Serialization;
using Whiteboard.Core.Validation;

namespace Whiteboard.Core.Templates;

public sealed record TemplateInstantiationResult
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("templateId")]
    public string TemplateId { get; init; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; init; } = string.Empty;

    [JsonPropertyName("instanceId")]
    public string InstanceId { get; init; } = string.Empty;

    [JsonPropertyName("slotBindings")]
    public IReadOnlyDictionary<string, string> SlotBindings { get; init; } = new Dictionary<string, string>();

    [JsonPropertyName("fragment")]
    public ComposedTemplateFragment Fragment { get; init; } = new();

    [JsonPropertyName("canonicalJson")]
    public string CanonicalJson { get; init; } = string.Empty;

    [JsonPropertyName("deterministicKey")]
    public string DeterministicKey { get; init; } = string.Empty;

    [JsonPropertyName("issues")]
    public IReadOnlyList<ValidationIssue> Issues { get; init; } = [];
}
