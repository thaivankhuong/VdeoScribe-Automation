using System.Text.Json;
using Whiteboard.Cli.Models;
using Whiteboard.Cli.Services;
using Xunit;

namespace Whiteboard.Cli.Tests;

public sealed class ScriptCompilationOrchestratorTests
{
    [Fact]
    public void Compile_WritesSpecAndReportOutputForValidFixture()
    {
        var orchestrator = new ScriptCompilationOrchestrator();
        var outputDirectory = CreateTemporaryDirectory();
        var specOutputPath = Path.Combine(outputDirectory, "compiled-spec.json");
        var reportOutputPath = Path.Combine(outputDirectory, "compile-report.json");

        try
        {
            var result = orchestrator.Compile(new CliScriptCompileCommandRequest
            {
                InputPath = ResolveRepoRelativePath("tests", "Whiteboard.Cli.Tests", "Fixtures", "phase18-script-compiler", "script-valid.json"),
                SpecOutputPath = specOutputPath,
                ReportOutputPath = reportOutputPath
            });

            Assert.True(result.Success);
            Assert.Equal("phase18-script-demo", result.ScriptId);
            Assert.Equal(1, result.TemplateCount);
            Assert.Equal(2, result.SectionCount);
            Assert.Equal(Path.GetFullPath(specOutputPath), result.SpecOutputPath);
            Assert.Equal(Path.GetFullPath(reportOutputPath), result.ReportOutputPath);
            Assert.True(File.Exists(result.SpecOutputPath));
            Assert.True(File.Exists(result.ReportOutputPath));
            Assert.Empty(result.Diagnostics);

            using var specDocument = JsonDocument.Parse(File.ReadAllText(result.SpecOutputPath));
            Assert.Equal("phase18-script-demo", GetPropertyIgnoreCase(GetPropertyIgnoreCase(specDocument.RootElement, "meta"), "projectId").GetString());

            using var reportDocument = JsonDocument.Parse(File.ReadAllText(result.ReportOutputPath));
            Assert.Equal("phase18-script-demo", GetPropertyIgnoreCase(GetPropertyIgnoreCase(reportDocument.RootElement, "script"), "scriptId").GetString());
            Assert.Equal(2, GetPropertyIgnoreCase(reportDocument.RootElement, "sections").GetArrayLength());
        }
        finally
        {
            DeleteDirectory(outputDirectory);
        }
    }

    [Fact]
    public void Compile_FailureWritesReportAndLeavesSpecOutputAbsent()
    {
        var orchestrator = new ScriptCompilationOrchestrator();
        var outputDirectory = CreateTemporaryDirectory();
        var specOutputPath = Path.Combine(outputDirectory, "compiled-spec.json");
        var reportOutputPath = Path.Combine(outputDirectory, "compile-report.json");

        try
        {
            var result = orchestrator.Compile(new CliScriptCompileCommandRequest
            {
                InputPath = ResolveRepoRelativePath("tests", "Whiteboard.Cli.Tests", "Fixtures", "phase18-script-compiler", "script-missing-required-field.json"),
                SpecOutputPath = specOutputPath,
                ReportOutputPath = reportOutputPath
            });

            Assert.False(result.Success);
            Assert.False(File.Exists(specOutputPath));
            Assert.True(File.Exists(reportOutputPath));
            Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "script.mapping.field.required");

            using var reportDocument = JsonDocument.Parse(File.ReadAllText(reportOutputPath));
            var diagnostics = GetPropertyIgnoreCase(reportDocument.RootElement, "diagnostics").EnumerateArray().ToArray();
            Assert.Contains(diagnostics, diagnostic => GetPropertyIgnoreCase(diagnostic, "code").GetString() == "script.mapping.field.required");
        }
        finally
        {
            DeleteDirectory(outputDirectory);
        }
    }

    [Fact]
    public void Compile_FailureDiagnosticsStayOrderedAndReportsStayDeterministicAcrossRuns()
    {
        var orchestrator = new ScriptCompilationOrchestrator();
        var outputDirectory = CreateTemporaryDirectory();
        var specOutputPathA = Path.Combine(outputDirectory, "compiled-spec-a.json");
        var specOutputPathB = Path.Combine(outputDirectory, "compiled-spec-b.json");
        var reportOutputPathA = Path.Combine(outputDirectory, "compile-report-a.json");
        var reportOutputPathB = Path.Combine(outputDirectory, "compile-report-b.json");

        try
        {
            var request = new CliScriptCompileCommandRequest
            {
                InputPath = ResolveRepoRelativePath("tests", "Whiteboard.Cli.Tests", "Fixtures", "phase18-script-compiler", "script-unknown-governed-id.json"),
                SpecOutputPath = specOutputPathA,
                ReportOutputPath = reportOutputPathA
            };

            var first = orchestrator.Compile(request);
            var second = orchestrator.Compile(request with
            {
                SpecOutputPath = specOutputPathB,
                ReportOutputPath = reportOutputPathB
            });

            Assert.False(first.Success);
            Assert.False(second.Success);
            Assert.Equal(first.DeterministicKey, second.DeterministicKey);
            Assert.Equal(first.Diagnostics.Select(diagnostic => diagnostic.Code).ToArray(), second.Diagnostics.Select(diagnostic => diagnostic.Code).ToArray());
            Assert.Equal(File.ReadAllText(first.ReportOutputPath), File.ReadAllText(second.ReportOutputPath));

            using var reportDocument = JsonDocument.Parse(File.ReadAllText(first.ReportOutputPath));
            var diagnostics = GetPropertyIgnoreCase(reportDocument.RootElement, "diagnostics").EnumerateArray().ToArray();
            Assert.Equal(
                new[] { "script.governed.asset.missing", "script.governed.effect.missing" },
                diagnostics.Select(diagnostic => GetPropertyIgnoreCase(diagnostic, "code").GetString()).ToArray());
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
