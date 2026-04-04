using System.Text.Json;
using Whiteboard.Cli.Models;
using Whiteboard.Cli.Services;
using Xunit;

namespace Whiteboard.Cli.Tests;

public sealed class ScriptCompilationOrchestratorTests
{
    [Fact]
    public void Compile_WritesSpecOutputForValidFixture()
    {
        var orchestrator = new ScriptCompilationOrchestrator();
        var outputDirectory = CreateTemporaryDirectory();
        var specOutputPath = Path.Combine(outputDirectory, "compiled-spec.json");

        try
        {
            var result = orchestrator.Compile(new CliScriptCompileCommandRequest
            {
                InputPath = ResolveRepoRelativePath("tests", "Whiteboard.Cli.Tests", "Fixtures", "phase18-script-compiler", "script-valid.json"),
                SpecOutputPath = specOutputPath
            });

            Assert.True(result.Success);
            Assert.Equal("phase18-script-demo", result.ScriptId);
            Assert.Equal(1, result.TemplateCount);
            Assert.Equal(2, result.SectionCount);
            Assert.Equal(Path.GetFullPath(specOutputPath), result.SpecOutputPath);
            Assert.True(File.Exists(result.SpecOutputPath));

            using var document = JsonDocument.Parse(File.ReadAllText(result.SpecOutputPath));
            var root = document.RootElement;
            Assert.Equal("phase18-script-demo", GetPropertyIgnoreCase(GetPropertyIgnoreCase(root, "meta"), "projectId").GetString());
            Assert.Equal("reg-main-2026-04", GetPropertyIgnoreCase(GetPropertyIgnoreCase(GetPropertyIgnoreCase(root, "assets"), "registrySnapshot"), "snapshotId").GetString());
            Assert.Equal(2, GetPropertyIgnoreCase(root, "scenes").GetArrayLength());
        }
        finally
        {
            DeleteDirectory(outputDirectory);
        }
    }

    [Fact]
    public void Compile_RepeatedRunsProduceEquivalentSpecOutputAndDeterministicKey()
    {
        var orchestrator = new ScriptCompilationOrchestrator();
        var outputDirectory = CreateTemporaryDirectory();
        var firstOutputPath = Path.Combine(outputDirectory, "compiled-spec-a.json");
        var secondOutputPath = Path.Combine(outputDirectory, "compiled-spec-b.json");

        try
        {
            var request = new CliScriptCompileCommandRequest
            {
                InputPath = ResolveRepoRelativePath("tests", "Whiteboard.Cli.Tests", "Fixtures", "phase18-script-compiler", "script-valid.json"),
                SpecOutputPath = firstOutputPath
            };
            var first = orchestrator.Compile(request);
            var second = orchestrator.Compile(request with { SpecOutputPath = secondOutputPath });

            Assert.True(first.Success);
            Assert.True(second.Success);
            Assert.Equal(first.DeterministicKey, second.DeterministicKey);
            Assert.Equal(File.ReadAllText(first.SpecOutputPath), File.ReadAllText(second.SpecOutputPath));
        }
        finally
        {
            DeleteDirectory(outputDirectory);
        }
    }

    private static string ResolveRepoRelativePath(params string[] segments)
    {
        var baseDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        for (var current = baseDirectory; current is not null; current = current.Parent)
        {
            var candidate = Path.Combine(new[] { current.FullName }.Concat(segments).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException($"Could not resolve repo file: {Path.Combine(segments)}");
    }

    private static string CreateTemporaryDirectory()
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), "whiteboard-script-cli-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directoryPath);
        return directoryPath;
    }

    private static void DeleteDirectory(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    private static JsonElement GetPropertyIgnoreCase(JsonElement element, string propertyName)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value;
            }
        }

        throw new KeyNotFoundException(propertyName);
    }
}
