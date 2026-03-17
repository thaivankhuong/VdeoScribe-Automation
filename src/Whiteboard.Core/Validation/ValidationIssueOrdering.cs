using System.Collections.Generic;

namespace Whiteboard.Core.Validation;

public static class ValidationIssueOrdering
{
    public static IComparer<ValidationIssue> Comparer { get; } = new ValidationIssueComparer();

    public static IReadOnlyList<ValidationIssue> Sort(IEnumerable<ValidationIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);

        return issues.OrderBy(issue => issue, Comparer).ToArray();
    }

    private sealed class ValidationIssueComparer : IComparer<ValidationIssue>
    {
        public int Compare(ValidationIssue? x, ValidationIssue? y)
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

            var gateComparison = x.Gate.CompareTo(y.Gate);
            if (gateComparison != 0)
            {
                return gateComparison;
            }

            var pathComparison = StringComparer.Ordinal.Compare(x.Path, y.Path);
            if (pathComparison != 0)
            {
                return pathComparison;
            }

            var severityComparison = x.Severity.CompareTo(y.Severity);
            if (severityComparison != 0)
            {
                return severityComparison;
            }

            var codeComparison = StringComparer.Ordinal.Compare(x.Code, y.Code);
            if (codeComparison != 0)
            {
                return codeComparison;
            }

            return x.OccurrenceIndex.CompareTo(y.OccurrenceIndex);
        }
    }
}
