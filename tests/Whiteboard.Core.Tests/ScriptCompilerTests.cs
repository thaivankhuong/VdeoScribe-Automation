using System.Text.Json;
using Whiteboard.Core.Compilation;
using Xunit;

namespace Whiteboard.Core.Tests;

public sealed class ScriptCompilerTests
{
    private readonly ScriptCompiler _compiler = new();

    [Fact]
    public void Compile_ProducesValidSpecForTwoOrderedSections()
    {
        var result = Compile(CreateValidJson(
            new SectionInput("section-b", 2, "Second section"),
            new SectionInput("section-a", 1, "First section")));

        Assert.True(result.Success);
        Assert.NotNull(result.Project);
        Assert.Equal("script-demo", result.ScriptId);
        Assert.Equal("Phase 18 Demo", result.Project!.Meta.Name);
        Assert.Equal("reg-main-2026-04", result.Project.Meta.AssetRegistrySnapshotId);
        Assert.Equal("reg-main-2026-04", result.Project.Assets.RegistrySnapshot.SnapshotId);
        Assert.Equal("2026.04.0", result.Project.Assets.RegistrySnapshot.SnapshotVersion);
        Assert.Single(result.Project.Assets.SvgAssets);
        Assert.Single(result.Project.Timeline.EffectProfiles);
        Assert.Equal(
            new[]
            {
                "script-demo.section-a.title-scene",
                "script-demo.section-b.title-scene"
            },
            result.Project.Scenes.Select(scene => scene.Id).ToArray());
        Assert.Equal(2, result.SectionCount);
        Assert.Equal(1, result.TemplateCount);
    }

    [Fact]
    public void Compile_ReorderedEquivalentInputsProduceTheSameCanonicalJsonAndDeterministicKey()
    {
        var first = Compile(CreateValidJson(
            new SectionInput("section-b", 2, "Second section"),
            new SectionInput("section-a", 1, "First section")));
        var second = Compile(CreateValidJson(
            new SectionInput("section-a", 1, "First section"),
            new SectionInput("section-b", 2, "Second section")));

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.Equal(first.CanonicalJson, second.CanonicalJson);
        Assert.Equal(first.DeterministicKey, second.DeterministicKey);
    }

    [Fact]
    public void Compile_FailsWhenSpecProcessingPipelineRejectsGeneratedSemantics()
    {
        var temporaryDirectory = CreateTemporaryDirectory();

        try
        {
            var templatePath = Path.Combine(temporaryDirectory, "image-template.json");
            var templateCatalogPath = Path.Combine(temporaryDirectory, "catalog.json");
            var mappingCatalogPath = Path.Combine(temporaryDirectory, "mappings.json");
            var governedLibraryPath = Path.Combine(temporaryDirectory, "governed-library.json");
            var scriptPath = Path.Combine(temporaryDirectory, "script.json");

            File.WriteAllText(templatePath, CreateImageTemplateJson());
            File.WriteAllText(templateCatalogPath, CreateTemplateCatalogJson(templatePath));
            File.WriteAllText(mappingCatalogPath, CreateMappingCatalogJson("image-card"));
            File.WriteAllText(governedLibraryPath, CreateGovernedLibraryJson());

            var result = _compiler.Compile(
                CreateValidJson(new SectionInput("section-a", 1, "Headline", TemplateId: "image-card")),
                scriptPath,
                templateCatalogPath,
                mappingCatalogPath,
                governedLibraryPath);

            Assert.False(result.Success);
            Assert.Contains(result.Issues, issue => issue.Code == "semantic.scene_object.asset_ref.type_mismatch");
        }
        finally
        {
            DeleteDirectory(temporaryDirectory);
        }
    }

    private ScriptCompileResult Compile(string json)
    {
        return _compiler.Compile(
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

    private static string CreateTemplateCatalogJson(string templatePath)
    {
        return $$"""
            {
              "catalogVersion": "1.0.0",
              "templates": [
                {
                  "templateId": "image-card",
                  "status": "active",
                  "entryPath": "{{templatePath.Replace("\\", "\\\\", StringComparison.Ordinal)}}"
                }
              ]
            }
            """;
    }

    private static string CreateMappingCatalogJson(string templateId)
    {
        return $$"""
            {
              "catalogVersion": "1.0.0",
              "mappings": [
                {
                  "templateId": "{{templateId}}",
                  "status": "active",
                  "fieldMappings": [
                    {
                      "sourceField": "headline",
                      "slotId": "titleText",
                      "required": true
                    },
                    {
                      "sourceField": "illustrationAssetId",
                      "slotId": "illustrationAssetId",
                      "required": true,
                      "governedReferenceType": "asset"
                    },
                    {
                      "sourceField": "drawEffectProfileId",
                      "slotId": "drawEffectProfileId",
                      "required": true,
                      "governedReferenceType": "effect"
                    }
                  ]
                }
              ]
            }
            """;
    }

    private static string CreateGovernedLibraryJson()
    {
        return """
            {
              "registryId": "main-registry",
              "snapshotId": "reg-main-2026-04",
              "snapshotVersion": "2026.04.0",
              "assets": [
                {
                  "assetId": "svg-hero-governed",
                  "name": "Governed Hero Illustration",
                  "sourcePath": "assets/governed/svg-hero-governed.svg",
                  "assetType": "svg",
                  "status": "active"
                }
              ],
              "effectProfiles": [
                {
                  "effectProfileId": "effect-draw-default",
                  "actionType": "draw",
                  "minDurationSeconds": 0.5,
                  "maxDurationSeconds": 4.0,
                  "status": "active"
                }
              ]
            }
            """;
    }

    private static string CreateImageTemplateJson()
    {
        return """
            {
              "templateId": "image-card",
              "version": "1.0.0",
              "status": "active",
              "name": "Image Card",
              "slots": [
                {
                  "slotId": "titleText",
                  "valueType": "text",
                  "required": true,
                  "constraints": {
                    "allowedValues": []
                  }
                },
                {
                  "slotId": "illustrationAssetId",
                  "valueType": "assetId",
                  "required": true,
                  "constraints": {
                    "allowAssetId": true,
                    "allowedValues": []
                  }
                },
                {
                  "slotId": "drawEffectProfileId",
                  "valueType": "effectProfileId",
                  "required": true,
                  "constraints": {
                    "allowEffectProfileId": true,
                    "allowedValues": []
                  }
                }
              ],
              "sceneFragments": [
                {
                  "localId": "image-scene",
                  "name": "Image Scene",
                  "durationSeconds": 4,
                  "objects": [
                    {
                      "id": "hero-image",
                      "name": "Hero Image",
                      "type": "image",
                      "assetRefId": "{{slot:illustrationAssetId}}",
                      "layer": 1,
                      "isVisible": true
                    },
                    {
                      "id": "title-text",
                      "name": "Title Text",
                      "type": "text",
                      "textContent": "{{slot:titleText}}",
                      "layer": 2,
                      "isVisible": true
                    }
                  ]
                }
              ],
              "timelineEventFragments": [
                {
                  "localId": "draw-hero-image",
                  "sceneLocalId": "image-scene",
                  "sceneObjectLocalId": "hero-image",
                  "actionType": "draw",
                  "startSeconds": 0,
                  "durationSeconds": 2,
                  "easing": "linear",
                  "parameters": {
                    "effectProfileId": "{{slot:drawEffectProfileId}}"
                  }
                }
              ]
            }
            """;
    }

    private static string GetRepoPath(string relativePath)
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../", relativePath));
    }

    private static string GetScriptPath(string fileName)
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../tests/Whiteboard.Core.Tests", fileName));
    }

    private static string CreateTemporaryDirectory()
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), "whiteboard-script-compiler-tests", Guid.NewGuid().ToString("N"));
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

    private sealed record SectionInput(
        string SectionId,
        int Order,
        string? Headline,
        string TemplateId = "title-card-basic",
        string IllustrationAssetId = "svg-hero-governed",
        string DrawEffectProfileId = "effect-draw-default");
}
