namespace Whiteboard.Core.Compilation;

public sealed record ScriptCompileReport
{
    public ScriptCompileReportScript Script { get; init; } = new();
    public IReadOnlyList<ScriptCompileReportTemplate> Templates { get; init; } = [];
    public IReadOnlyList<ScriptCompileReportSection> Sections { get; init; } = [];
    public ScriptCompileReportGovernedResources GovernedResources { get; init; } = new();
    public ScriptCompileReportSpec Spec { get; init; } = new();
    public IReadOnlyList<ScriptCompileDiagnostic> Diagnostics { get; init; } = [];
}

public sealed record ScriptCompileReportScript
{
    public string ScriptId { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public string AssetRegistrySnapshotId { get; init; } = string.Empty;
    public string SourcePath { get; init; } = string.Empty;
}

public sealed record ScriptCompileReportTemplate
{
    public string TemplateId { get; init; } = string.Empty;
    public int SectionCount { get; init; }
    public IReadOnlyList<string> SectionIds { get; init; } = [];
}

public sealed record ScriptCompileReportGovernedResources
{
    public string RequestedSnapshotId { get; init; } = string.Empty;
    public string RegistryId { get; init; } = string.Empty;
    public string SnapshotId { get; init; } = string.Empty;
    public string SnapshotVersion { get; init; } = string.Empty;
    public IReadOnlyList<string> AssetIds { get; init; } = [];
    public IReadOnlyList<string> EffectProfileIds { get; init; } = [];
}

public sealed record ScriptCompileReportSpec
{
    public bool Success { get; init; }
    public bool SpecOutputGenerated { get; init; }
    public int SceneCount { get; init; }
    public int TimelineEventCount { get; init; }
    public int SvgAssetCount { get; init; }
    public int EffectProfileCount { get; init; }
    public string SpecOutputDeterministicKey { get; init; } = string.Empty;
}
