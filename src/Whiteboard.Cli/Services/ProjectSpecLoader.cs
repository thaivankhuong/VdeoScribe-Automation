using System;
using System.IO;
using Whiteboard.Cli.Contracts;
using Whiteboard.Core.Models;
using Whiteboard.Core.Validation;

namespace Whiteboard.Cli.Services;

public sealed class ProjectSpecLoader : IProjectSpecLoader
{
    private readonly ISpecProcessingPipeline _specProcessingPipeline;

    public ProjectSpecLoader(ISpecProcessingPipeline? specProcessingPipeline = null)
    {
        _specProcessingPipeline = specProcessingPipeline ?? new SpecProcessingPipeline();
    }

    public VideoProject Load(string specPath)
    {
        if (string.IsNullOrWhiteSpace(specPath))
        {
            throw new ArgumentException("Spec path is required.", nameof(specPath));
        }

        var normalizedSpecPath = Path.GetFullPath(specPath);

        if (!File.Exists(normalizedSpecPath))
        {
            throw new FileNotFoundException("Spec file was not found.", normalizedSpecPath);
        }

        var json = File.ReadAllText(normalizedSpecPath);

        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidDataException("Spec file is empty.");
        }

        var result = _specProcessingPipeline.Process(json, normalizedSpecPath);
        if (!result.IsSuccess || result.Project is null)
        {
            throw new InvalidDataException(BuildFailureMessage(normalizedSpecPath, result.Issues));
        }

        return result.Project.Project;
    }

    private static string BuildFailureMessage(string specPath, IReadOnlyList<ValidationIssue> issues)
    {
        var lines = new List<string>
        {
            $"Spec processing failed for '{specPath}'."
        };

        foreach (var issue in issues)
        {
            lines.Add($"[{issue.Gate}] {issue.Code} at {issue.Path}: {issue.Message}");
        }

        return string.Join(Environment.NewLine, lines);
    }
}
