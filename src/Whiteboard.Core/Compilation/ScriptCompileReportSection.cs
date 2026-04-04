namespace Whiteboard.Core.Compilation;

public sealed record ScriptCompileReportSection
{
    public string SectionId { get; init; } = string.Empty;
    public int Order { get; init; }
    public string TemplateId { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string> SlotBindings { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);
    public IReadOnlyList<string> AssetIds { get; init; } = [];
    public IReadOnlyList<string> EffectProfileIds { get; init; } = [];
    public IReadOnlyList<string> SceneIds { get; init; } = [];
    public IReadOnlyList<string> TimelineEventIds { get; init; } = [];
}
