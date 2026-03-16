using System;
using System.IO;
using Whiteboard.Cli.Contracts;
using Whiteboard.Core.Models;

namespace Whiteboard.Cli.Services;

public sealed class ProjectSpecLoader : IProjectSpecLoader
{
    public VideoProject Load(string specPath)
    {
        if (string.IsNullOrWhiteSpace(specPath))
        {
            throw new ArgumentException("Spec path is required.", nameof(specPath));
        }

        // Placeholder deterministic loader for contract-first phase.
        return new VideoProject
        {
            Meta = new ProjectMeta
            {
                ProjectId = specPath,
                Name = Path.GetFileNameWithoutExtension(specPath)
            }
        };
    }
}
