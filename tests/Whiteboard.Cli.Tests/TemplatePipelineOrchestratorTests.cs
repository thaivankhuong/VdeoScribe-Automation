using System.Text.Json;
using Whiteboard.Cli.Models;
using Whiteboard.Cli.Services;
using Xunit;

namespace Whiteboard.Cli.Tests;

public sealed class TemplatePipelineOrchestratorTests
{
    [Fact]
    public void Validate_TitleCardBasicTemplate_SucceedsThroughCatalogBackedLookup()
    {
        var orchestrator = new TemplatePipelineOrchestrator();

        var result = orchestrator.Validate(new CliTemplateValidateRequest
        {
            TemplateId = "title-card-basic",
            CatalogPath = ResolveRepoRelativePath(".planning", "templates", "index.json"),
            SlotValuesPath = ResolveRepoRelativePath("tests", "Whiteboard.Cli.Tests", "Fixtures", "phase17-templates", "slot-values-valid.json")
        });

        Assert.True(result.Success);
        Assert.Equal("title-card-basic", result.TemplateId);
        Assert.Equal("1.0.0", result.Version);
        Assert.Equal("active", result.Status);
        Assert.Equal("passed", result.SlotValidationStatus);
    }

    [Fact]
    public void Instantiate_RepeatedRunsWithReorderedSlotJsonProduceSameDeterministicKey()
    {
        var orchestrator = new TemplatePipelineOrchestrator();
        var outputDirectory = CreateTemporaryDirectory();
        var firstOutputPath = Path.Combine(outputDirectory, "first.json");
        var secondOutputPath = Path.Combine(outputDirectory, "second.json");
        var reorderedSlotValuesPath = Path.Combine(outputDirectory, "slot-values-reordered.json");

        try
        {
            File.WriteAllText(
                reorderedSlotValuesPath,
                """
                {
                  "drawEffectProfileId": "effect-draw-default",
                  "illustrationAssetId": "svg-hero-governed",
                  "titleText": "Template CLI flow"
                }
                """);

            var first = orchestrator.Instantiate(new CliTemplateInstantiateRequest
            {
                TemplateId = "title-card-basic",
                CatalogPath = ResolveRepoRelativePath(".planning", "templates", "index.json"),
                SlotValuesPath = ResolveRepoRelativePath("tests", "Whiteboard.Cli.Tests", "Fixtures", "phase17-templates", "slot-values-valid.json"),
                OutputPath = firstOutputPath,
                InstanceId = "title-card-cli",
                TimeOffsetSeconds = 0,
                LayerOffset = 0
            });
            var second = orchestrator.Instantiate(new CliTemplateInstantiateRequest
            {
                TemplateId = "title-card-basic",
                CatalogPath = ResolveRepoRelativePath(".planning", "templates", "index.json"),
                SlotValuesPath = reorderedSlotValuesPath,
                OutputPath = secondOutputPath,
                InstanceId = "title-card-cli",
                TimeOffsetSeconds = 0,
                LayerOffset = 0
            });

            Assert.True(first.Success);
            Assert.True(second.Success);
            Assert.Equal(first.DeterministicKey, second.DeterministicKey);

            using var document = JsonDocument.Parse(File.ReadAllText(first.OutputPath));
            var root = document.RootElement;
            Assert.Equal(first.DeterministicKey, root.GetProperty("deterministicKey").GetString());
            Assert.Equal("title-card-basic", root.GetProperty("templateId").GetString());
            Assert.True(root.GetProperty("scenes").GetArrayLength() > 0);
            Assert.True(root.GetProperty("timelineEvents").GetArrayLength() > 0);
        }
        finally
        {
            DeleteDirectory(outputDirectory);
        }
    }

    [Fact]
    public void Instantiate_RejectsMissingRequiredSlotFixture()
    {
        var orchestrator = new TemplatePipelineOrchestrator();

        var result = orchestrator.Instantiate(new CliTemplateInstantiateRequest
        {
            TemplateId = "title-card-basic",
            CatalogPath = ResolveRepoRelativePath(".planning", "templates", "index.json"),
            SlotValuesPath = ResolveRepoRelativePath("tests", "Whiteboard.Cli.Tests", "Fixtures", "phase17-templates", "slot-values-missing-required.json"),
            OutputPath = Path.Combine(CreateTemporaryDirectory(), "missing-required.json"),
            InstanceId = "missing-required"
        });

        Assert.False(result.Success);
        Assert.Contains(result.Issues, issue => issue.Code == "template.slot.required");
    }

    [Fact]
    public void Instantiate_RejectsUnknownSlotFixture()
    {
        var orchestrator = new TemplatePipelineOrchestrator();

        var result = orchestrator.Instantiate(new CliTemplateInstantiateRequest
        {
            TemplateId = "title-card-basic",
            CatalogPath = ResolveRepoRelativePath(".planning", "templates", "index.json"),
            SlotValuesPath = ResolveRepoRelativePath("tests", "Whiteboard.Cli.Tests", "Fixtures", "phase17-templates", "slot-values-unknown-slot.json"),
            OutputPath = Path.Combine(CreateTemporaryDirectory(), "unknown-slot.json"),
            InstanceId = "unknown-slot"
        });

        Assert.False(result.Success);
        Assert.Contains(result.Issues, issue => issue.Code == "template.slot.unknown");
    }

    [Fact]
    public void Validate_RejectsUnresolvedTemplateId()
    {
        var orchestrator = new TemplatePipelineOrchestrator();

        var result = orchestrator.Validate(new CliTemplateValidateRequest
        {
            TemplateId = "missing-template",
            CatalogPath = ResolveRepoRelativePath(".planning", "templates", "index.json")
        });

        Assert.False(result.Success);
        Assert.Contains(result.Issues, issue => issue.Code == "template.catalog.template_missing");
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
        var directoryPath = Path.Combine(Path.GetTempPath(), "whiteboard-template-cli-tests", Guid.NewGuid().ToString("N"));
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
}
