using System.Text.Json;
using Whiteboard.Core.Compilation;
using Xunit;

namespace Whiteboard.Core.Tests;

public sealed class ScriptMappingPipelineTests
{
    private readonly ScriptMappingPipeline _pipeline = new();

    [Fact]
    public void Process_ReorderedEquivalentInputsProduceTheSameOrderedPlan()
    {
        var first = _pipeline.Process(
            CreateValidJson(
                new SectionInput("section-b", 2, "Second section"),
                new SectionInput("section-a", 1, "First section")),
            GetScriptPath("script-valid-a.json"),
            GetRepoPath(".planning/templates/index.json"),
            GetRepoPath(".planning/script-compiler/template-mappings.json"),
            GetRepoPath(".planning/script-compiler/governed-library.json"));
        var second = _pipeline.Process(
            CreateValidJson(
                new SectionInput("section-a", 1, "First section"),
                new SectionInput("section-b", 2, "Second section")),
            GetScriptPath("script-valid-b.json"),
            GetRepoPath(".planning/templates/index.json"),
            GetRepoPath(".planning/script-compiler/template-mappings.json"),
            GetRepoPath(".planning/script-compiler/governed-library.json"));

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.Equal(
            JsonSerializer.Serialize(first.Sections.Select(ToComparableShape)),
            JsonSerializer.Serialize(second.Sections.Select(ToComparableShape)));
        Assert.Equal(
            new[] { "section-a", "section-b" },
            first.Sections.Select(section => section.Section.SectionId).ToArray());
        Assert.All(first.Sections, section => Assert.StartsWith("script-demo.", section.InstantiationRequest.InstanceId, StringComparison.Ordinal));
    }

    [Fact]
    public void Process_FailsWhenSectionIdsAreDuplicated()
    {
        var result = Process(CreateValidJson(
            new SectionInput("section-a", 1, "First section"),
            new SectionInput("section-a", 2, "Duplicate section")));

        Assert.False(result.Success);
        Assert.Contains(result.Issues, issue => issue.Code == "script.section.id.duplicate");
    }

    [Fact]
    public void Process_FailsWhenRequiredMappedFieldIsMissing()
    {
        var result = Process(CreateValidJson(
            new SectionInput("section-a", 1, null)));

        Assert.False(result.Success);
        Assert.Contains(result.Issues, issue => issue.Code == "script.mapping.field.required");
    }

    [Fact]
    public void Process_FailsWhenTemplateIdCannotBeResolved()
    {
        var result = Process(CreateValidJson(
            new SectionInput("section-a", 1, "Headline", TemplateId: "missing-template")));

        Assert.False(result.Success);
        Assert.Contains(result.Issues, issue => issue.Code == "script.template.unresolved");
    }

    [Fact]
    public void Process_FailsWhenGovernedAssetIdIsUnknown()
    {
        var result = Process(CreateValidJson(
            new SectionInput("section-a", 1, "Headline", IllustrationAssetId: "svg-missing")));

        Assert.False(result.Success);
        Assert.Contains(result.Issues, issue => issue.Code == "script.governed.asset.missing");
    }

    [Fact]
    public void Process_FailsWhenGovernedEffectProfileIdIsUnknown()
    {
        var result = Process(CreateValidJson(
            new SectionInput("section-a", 1, "Headline", DrawEffectProfileId: "effect-missing")));

        Assert.False(result.Success);
        Assert.Contains(result.Issues, issue => issue.Code == "script.governed.effect.missing");
    }

    private ScriptCompilationPlan Process(string json)
    {
        return _pipeline.Process(
            json,
            GetScriptPath("script-input.json"),
            GetRepoPath(".planning/templates/index.json"),
            GetRepoPath(".planning/script-compiler/template-mappings.json"),
            GetRepoPath(".planning/script-compiler/governed-library.json"));
    }

    private static string CreateValidJson(params SectionInput[] sections)
    {
        var payload = new
        {
            scriptId = "script-demo",
            version = "1.0.0",
            projectName = "Phase 18 Demo",
            assetRegistrySnapshotId = "reg-main-2026-04",
            output = new
            {
                width = 1920,
                height = 1080,
                frameRate = 30,
                backgroundColorHex = "#FFFFFF"
            },
            sections = sections.Select(section => new
            {
                sectionId = section.SectionId,
                order = section.Order,
                templateId = section.TemplateId,
                headline = section.Headline,
                supportingText = "Deterministic subtitle",
                illustrationAssetId = section.IllustrationAssetId,
                drawEffectProfileId = section.DrawEffectProfileId
            })
        };

        return JsonSerializer.Serialize(payload);
    }

    private static object ToComparableShape(ScriptSectionCompilationPlan section)
    {
        return new
        {
            sectionId = section.Section.SectionId,
            order = section.Section.Order,
            templateId = section.TemplateId,
            assetId = section.GovernedAssetId,
            effectId = section.GovernedEffectProfileId,
            slotBindings = section.SlotBindings.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToArray()
        };
    }

    private static string GetRepoPath(string relativePath)
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../", relativePath));
    }

    private static string GetScriptPath(string fileName)
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../tests/Whiteboard.Core.Tests", fileName));
    }

    private sealed record SectionInput(
        string SectionId,
        int Order,
        string? Headline,
        string TemplateId = "title-card-basic",
        string IllustrationAssetId = "svg-hero-governed",
        string DrawEffectProfileId = "effect-draw-default");
}
