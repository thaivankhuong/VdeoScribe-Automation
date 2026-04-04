using System.Text.Json;
using Whiteboard.Core.Templates;
using Whiteboard.Core.Validation;
using Xunit;

namespace Whiteboard.Core.Tests;

public sealed class TemplateContractPipelineTests
{
    private readonly TemplateContractPipeline _pipeline = new();

    [Fact]
    public void GateOrder_ExecutesThroughSemanticForValidTemplate()
    {
        var result = _pipeline.Process(CreateValidTemplateVariantOne(), "templates/title-card-basic/template.json");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Template);
        Assert.Equal(
            new[] { ValidationGate.Contract, ValidationGate.Schema, ValidationGate.Normalization, ValidationGate.Semantic },
            result.Gates.Select(gate => gate.Gate).ToArray());
    }

    [Fact]
    public void Normalization_EquivalentTemplatesProduceIdenticalCanonicalOutput()
    {
        var first = _pipeline.Process(CreateValidTemplateVariantOne(), "templates/title-card-basic/template.json");
        var second = _pipeline.Process(CreateValidTemplateVariantTwo(), "templates/title-card-basic/template.json");

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.NotNull(first.Template);
        Assert.NotNull(second.Template);
        Assert.Equal(first.Template.CanonicalJson, second.Template.CanonicalJson);

        using var document = JsonDocument.Parse(first.Template.CanonicalJson);
        var root = document.RootElement;
        var slotIds = root.GetProperty("slots").EnumerateArray().Select(slot => slot.GetProperty("slotId").GetString()).ToArray();
        var sceneIds = root.GetProperty("sceneFragments").EnumerateArray().Select(scene => scene.GetProperty("localId").GetString()).ToArray();

        Assert.Equal(new[] { "heroAssetId", "titleText" }, slotIds);
        Assert.Equal(new[] { "title-scene", "title-scene-secondary" }, sceneIds);
    }

    [Fact]
    public void SemanticValidation_RejectsMissingSlotDefinitionMetadata()
    {
        var result = _pipeline.Process(CreateMissingSlotDefinitionJson(), "templates/missing-slot/template.json");

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Issues, issue => issue.Code == "template.slot.definition.required");
    }

    [Fact]
    public void SemanticValidation_RejectsDuplicateSlotIds()
    {
        var result = _pipeline.Process(CreateDuplicateSlotIdsJson(), "templates/duplicate-slot/template.json");

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Issues, issue => issue.Code == "template.slot.id.duplicate");
    }

    [Fact]
    public void SemanticValidation_RejectsGovernedDefaultsAndSourcePathFallbacks()
    {
        var result = _pipeline.Process(CreateGovernedDefaultAndSourcePathJson(), "templates/governed-default/template.json");

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Issues, issue => issue.Code == "template.slot.default_governed_disallowed");
        Assert.Contains(result.Issues, issue => issue.Code == "template.reference.path_fallback.disallowed");
    }

    [Fact]
    public void SemanticValidation_RejectsUndeclaredPlaceholders()
    {
        var result = _pipeline.Process(CreateUndeclaredPlaceholderJson(), "templates/undeclared-placeholder/template.json");

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Issues, issue => issue.Code == "template.fragment.placeholder.undeclared");
    }

    [Fact]
    public void SemanticValidation_RejectsInvalidStatus()
    {
        var result = _pipeline.Process(CreateInvalidStatusJson(), "templates/invalid-status/template.json");

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Issues, issue => issue.Code == "template.status.invalid");
    }

    private static string CreateValidTemplateVariantOne()
    {
        return """
            {
              "templateId": " title-card-basic ",
              "version": " 1.0.0 ",
              "status": " Active ",
              "name": " Basic Title Card ",
              "description": " Example template ",
              "slots": [
                {
                  "slotId": "titleText",
                  "valueType": "text",
                  "required": true,
                  "constraints": {
                    "allowedValues": [ "headline", "headline" ]
                  }
                },
                {
                  "slotId": "heroAssetId",
                  "valueType": "assetId",
                  "required": true,
                  "constraints": {
                    "allowAssetId": true,
                    "allowedValues": [ "asset-c", "asset-a" ]
                  }
                }
              ],
              "sceneFragments": [
                {
                  "localId": "title-scene-secondary",
                  "name": "Secondary",
                  "durationSeconds": 3,
                  "objects": [
                    {
                      "id": "secondary-text",
                      "name": "Secondary Text",
                      "type": "text",
                      "textContent": "{{slot:titleText}}",
                      "layer": 2
                    }
                  ]
                },
                {
                  "localId": "title-scene",
                  "name": "Primary",
                  "durationSeconds": 5,
                  "objects": [
                    {
                      "id": "hero-svg",
                      "name": "Hero Svg",
                      "type": "svg",
                      "assetRefId": "{{slot:heroAssetId}}",
                      "layer": 3
                    },
                    {
                      "id": "hero-title",
                      "name": "Hero Title",
                      "type": "text",
                      "textContent": "{{slot:titleText}}",
                      "layer": 1
                    }
                  ]
                }
              ],
              "timelineEventFragments": [
                {
                  "localId": "reveal-title",
                  "sceneLocalId": "title-scene",
                  "sceneObjectLocalId": "hero-title",
                  "actionType": "reveal",
                  "startSeconds": 0.5,
                  "durationSeconds": 1.0,
                  "easing": "linear",
                  "parameters": {
                    "z": "last",
                    "a": "first"
                  }
                }
              ]
            }
            """;
    }

    private static string CreateValidTemplateVariantTwo()
    {
        return """
            {
              "timelineEventFragments": [
                {
                  "parameters": {
                    "a": "first",
                    "z": "last"
                  },
                  "easing": "linear",
                  "durationSeconds": 1.0,
                  "startSeconds": 0.5,
                  "actionType": "reveal",
                  "sceneObjectLocalId": "hero-title",
                  "sceneLocalId": "title-scene",
                  "localId": "reveal-title"
                }
              ],
              "sceneFragments": [
                {
                  "objects": [
                    {
                      "textContent": "{{slot:titleText}}",
                      "layer": 1,
                      "type": "text",
                      "name": "Hero Title",
                      "id": "hero-title"
                    },
                    {
                      "assetRefId": "{{slot:heroAssetId}}",
                      "layer": 3,
                      "type": "svg",
                      "name": "Hero Svg",
                      "id": "hero-svg"
                    }
                  ],
                  "durationSeconds": 5,
                  "name": "Primary",
                  "localId": "title-scene"
                },
                {
                  "objects": [
                    {
                      "textContent": "{{slot:titleText}}",
                      "layer": 2,
                      "type": "text",
                      "name": "Secondary Text",
                      "id": "secondary-text"
                    }
                  ],
                  "durationSeconds": 3,
                  "name": "Secondary",
                  "localId": "title-scene-secondary"
                }
              ],
              "slots": [
                {
                  "slotId": "heroAssetId",
                  "valueType": "assetId",
                  "required": true,
                  "constraints": {
                    "allowAssetId": true,
                    "allowedValues": [ "asset-a", "asset-c" ]
                  }
                },
                {
                  "slotId": "titleText",
                  "valueType": "text",
                  "required": true,
                  "constraints": {
                    "allowedValues": [ "headline", "headline" ]
                  }
                }
              ],
              "description": "Example template",
              "name": "Basic Title Card",
              "status": "active",
              "version": "1.0.0",
              "templateId": "title-card-basic"
            }
            """;
    }

    private static string CreateMissingSlotDefinitionJson()
    {
        return """
            {
              "templateId": "missing-slot-definition",
              "version": "1.0.0",
              "status": "active",
              "name": "Missing Slot Definition",
              "slots": [
                {
                  "slotId": "",
                  "valueType": "",
                  "required": true
                }
              ],
              "sceneFragments": [],
              "timelineEventFragments": []
            }
            """;
    }

    private static string CreateDuplicateSlotIdsJson()
    {
        return """
            {
              "templateId": "duplicate-slot-definition",
              "version": "1.0.0",
              "status": "active",
              "name": "Duplicate Slot Definition",
              "slots": [
                {
                  "slotId": "titleText",
                  "valueType": "text",
                  "required": true
                },
                {
                  "slotId": "titleText",
                  "valueType": "text",
                  "required": false
                }
              ],
              "sceneFragments": [],
              "timelineEventFragments": []
            }
            """;
    }

    private static string CreateGovernedDefaultAndSourcePathJson()
    {
        return """
            {
              "templateId": "governed-default-and-source-path",
              "version": "1.0.0",
              "status": "active",
              "name": "Governed Default And Source Path",
              "slots": [
                {
                  "slotId": "heroAssetId",
                  "valueType": "assetId",
                  "required": false,
                  "defaultValue": "asset-default",
                  "constraints": {
                    "allowAssetId": true
                  }
                }
              ],
              "sceneFragments": [
                {
                  "localId": "title-scene",
                  "name": "Primary",
                  "durationSeconds": 5,
                  "objects": [
                    {
                      "id": "hero-svg",
                      "name": "Hero Svg",
                      "type": "svg",
                      "assetRefId": "{{slot:heroAssetId}}",
                      "sourcePath": "assets/hero.svg",
                      "layer": 1
                    }
                  ]
                }
              ],
              "timelineEventFragments": []
            }
            """;
    }

    private static string CreateUndeclaredPlaceholderJson()
    {
        return """
            {
              "templateId": "undeclared-placeholder",
              "version": "1.0.0",
              "status": "active",
              "name": "Undeclared Placeholder",
              "slots": [
                {
                  "slotId": "titleText",
                  "valueType": "text",
                  "required": true
                }
              ],
              "sceneFragments": [
                {
                  "localId": "title-scene",
                  "name": "Primary",
                  "durationSeconds": 5,
                  "objects": [
                    {
                      "id": "hero-title",
                      "name": "Hero Title",
                      "type": "text",
                      "textContent": "{{slot:subtitleText}}",
                      "layer": 1
                    }
                  ]
                }
              ],
              "timelineEventFragments": []
            }
            """;
    }

    private static string CreateInvalidStatusJson()
    {
        return """
            {
              "templateId": "invalid-status",
              "version": "1.0.0",
              "status": "draft",
              "name": "Invalid Status",
              "slots": [
                {
                  "slotId": "titleText",
                  "valueType": "text",
                  "required": true
                }
              ],
              "sceneFragments": [],
              "timelineEventFragments": []
            }
            """;
    }
}
