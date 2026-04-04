namespace Whiteboard.Core.Compilation;

public sealed record ScriptCompileDiagnostic
{
    public string Severity { get; init; } = "error";
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public string Gate { get; init; } = string.Empty;
    public string SectionId { get; init; } = string.Empty;
    public string TemplateId { get; init; } = string.Empty;

    public static IReadOnlyList<ScriptCompileDiagnostic> Sort(IEnumerable<ScriptCompileDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        return diagnostics
            .OrderBy(diagnostic => diagnostic, Comparer.Instance)
            .ToArray();
    }

    private sealed class Comparer : IComparer<ScriptCompileDiagnostic>
    {
        public static Comparer Instance { get; } = new();

        public int Compare(ScriptCompileDiagnostic? x, ScriptCompileDiagnostic? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x is null)
            {
                return -1;
            }

            if (y is null)
            {
                return 1;
            }

            var severityComparison = GetSeverityRank(x.Severity).CompareTo(GetSeverityRank(y.Severity));
            if (severityComparison != 0)
            {
                return severityComparison;
            }

            var codeComparison = StringComparer.Ordinal.Compare(x.Code, y.Code);
            if (codeComparison != 0)
            {
                return codeComparison;
            }

            var sectionComparison = StringComparer.Ordinal.Compare(x.SectionId, y.SectionId);
            if (sectionComparison != 0)
            {
                return sectionComparison;
            }

            var templateComparison = StringComparer.Ordinal.Compare(x.TemplateId, y.TemplateId);
            if (templateComparison != 0)
            {
                return templateComparison;
            }

            var pathComparison = StringComparer.Ordinal.Compare(x.Path, y.Path);
            if (pathComparison != 0)
            {
                return pathComparison;
            }

            var gateComparison = StringComparer.Ordinal.Compare(x.Gate, y.Gate);
            if (gateComparison != 0)
            {
                return gateComparison;
            }

            return StringComparer.Ordinal.Compare(x.Message, y.Message);
        }

        private static int GetSeverityRank(string severity)
        {
            return severity.ToLowerInvariant() switch
            {
                "error" => 0,
                "warning" => 1,
                "info" => 2,
                _ => 3
            };
        }
    }
}
