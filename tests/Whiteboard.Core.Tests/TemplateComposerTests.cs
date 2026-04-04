using System.Text.Json;
using Whiteboard.Core.Scene;
using Whiteboard.Core.Templates;
using Xunit;

namespace Whiteboard.Core.Tests;

public sealed class TemplateComposerTests
{
    private readonly TemplateContractPipeline _contractPipeline = new();
    private readonly TemplateComposer _composer = new();

    [Fact]
    public void Compose_RepeatedRunsProduceIdenticalCanonicalJsonAndDeterministicKey()
    {
        var template = LoadTemplate();
        var slotValues = CreateValidSlotValues();

        var first = _composer.Compose(new TemplateInstantiationRequest
        {
            Template = template,
            SlotValues = slotValues,
            InstanceId = "title-card-001"
        });
        var second = _composer.Compose(new TemplateInstantiationRequest
        {
            Template = template,
            SlotValues = slotValues,
            InstanceId = "title-card-001"
        });

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.Equal(first.CanonicalJson, second.CanonicalJson);
        Assert.Equal(first.DeterministicKey, second.DeterministicKey);

        using var document = JsonDocument.Parse(first.CanonicalJson);
        var root = document.RootElement;

        Assert.Equal("title-card-basic", root.GetProperty("templateId").GetString());
        Assert.Equal("title-card-001", root.GetProperty("instanceId").GetString());
    }

    [Fact]
    public void Compose_AppliesTimeOffsetSecondsAndLayerOffsetToGeneratedContracts()
    {
        var result = _composer.Compose(new TemplateInstantiationRequest
        {
            Template = LoadTemplate(),
            SlotValues = CreateValidSlotValues(),
            InstanceId = "offset-demo",
            TimeOffsetSeconds = 2.5,
            LayerOffset = 4
        });

        Assert.True(result.Success);
        Assert.Equal(new[] { 2.5, 3.9, 3.3 }, result.Fragment.TimelineEvents.Select(evt => evt.StartSeconds).ToArray());
        Assert.Equal(new[] { 5, 6, 7 }, result.Fragment.Scenes.Single().Objects.Select(obj => obj.Layer).ToArray());
        Assert.All(result.Fragment.TimelineEvents, evt => Assert.StartsWith("offset-demo.", evt.Id, StringComparison.Ordinal));
        Assert.All(result.Fragment.Scenes.Single().Objects, obj => Assert.StartsWith("offset-demo.title-scene.", obj.Id, StringComparison.Ordinal));
    }

    [Fact]
    public void Compose_RejectsUnknownSlotKeysBeforeComposition()
    {
        var slotValues = CreateValidSlotValues();
        slotValues["unknownSlot"] = "unexpected";

        var result = _composer.Compose(new TemplateInstantiationRequest
        {
            Template = LoadTemplate(),
            SlotValues = slotValues,
            InstanceId = "unknown-slot"
        });

        Assert.False(result.Success);
        Assert.Contains(result.Issues, issue => issue.Code == "template.slot.unknown");
    }

    [Fact]
    public void Compose_RejectsMissingRequiredSlotValuesBeforeComposition()
    {
        var slotValues = CreateValidSlotValues();
        slotValues.Remove("illustrationAssetId");

        var result = _composer.Compose(new TemplateInstantiationRequest
        {
            Template = LoadTemplate(),
            SlotValues = slotValues,
            InstanceId = "missing-required"
        });

        Assert.False(result.Success);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "template.slot.required" &&
                     issue.Path.EndsWith("illustrationAssetId", StringComparison.Ordinal));
    }

    [Fact]
    public void Compose_RejectsIdCollisionsAfterNamespacing()
    {
        var template = LoadTemplate();
        var duplicateScene = template.SceneFragments[0] with
        {
            Name = "Title Card Duplicate",
            Objects = new List<SceneObject>()
        };

        var collidingTemplate = template with
        {
            SceneFragments = new List<TemplateSceneFragment>
            {
                template.SceneFragments[0],
                duplicateScene
            }
        };

        var result = _composer.Compose(new TemplateInstantiationRequest
        {
            Template = collidingTemplate,
            SlotValues = CreateValidSlotValues(),
            InstanceId = "collision-instance"
        });

        Assert.False(result.Success);
        Assert.Contains(result.Issues, issue => issue.Code == "template.compose.id_collision");
    }

    private SceneTemplateDefinition LoadTemplate()
    {
        var templatePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../.planning/templates/title-card-basic/template.json"));
        var result = _contractPipeline.Process(File.ReadAllText(templatePath), templatePath);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Template);

        return result.Template.Template;
    }

    private static Dictionary<string, string> CreateValidSlotValues()
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["titleText"] = "Deterministic composition",
            ["illustrationAssetId"] = "svg-hero-governed",
            ["drawEffectProfileId"] = "effect-draw-default"
        };
    }
}
