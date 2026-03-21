using System;
using System.IO;
using System.Linq;
using Whiteboard.Cli.Services;
using Xunit;

namespace Whiteboard.Cli.Tests;

public sealed class ProjectSpecLoaderTests
{
    [Fact]
    public void ProjectSpecLoader_Load_RejectsInvalidSpecWithDeterministicOrderedIssues()
    {
        var specPath = CreateSpecFile("invalid-project.json", CreateInvalidSpecJson());

        try
        {
            var loader = new ProjectSpecLoader();
            var exception = Assert.Throws<InvalidDataException>(() => loader.Load(specPath));

            Assert.Contains("Spec processing failed", exception.Message, StringComparison.Ordinal);
            var outputIndex = exception.Message.IndexOf("schema.output.frame_rate.invalid", StringComparison.Ordinal);
            var sceneIndex = exception.Message.IndexOf("schema.scene.id.required", StringComparison.Ordinal);
            var eventIndex = exception.Message.IndexOf("schema.timeline_event.id.required", StringComparison.Ordinal);

            Assert.True(outputIndex >= 0, "Expected output.frameRate validation issue in error message.");
            Assert.True(sceneIndex > outputIndex, "Expected scene id issue after output frame rate issue.");
            Assert.True(eventIndex > sceneIndex, "Expected timeline event issue after scene issue.");
            Assert.DoesNotContain("semantic.timeline_event.scene.missing", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            DeleteSpecFile(specPath);
        }
    }

    [Fact]
    public void ProjectSpecLoader_Load_ReturnsCanonicalNormalizedProject()
    {
        var specPath = CreateSpecFile("valid-project.json", CreateValidSpecJson());

        try
        {
            var loader = new ProjectSpecLoader();
            var project = loader.Load(specPath);

            Assert.Equal(new[] { "scene-a", "scene-b" }, project.Scenes.Select(scene => scene.Id).ToArray());
            Assert.Equal(new[] { "svg-a", "svg-b" }, project.Assets.SvgAssets.Select(asset => asset.Id).ToArray());
            Assert.Equal("#FFFFFF", project.Output.BackgroundColorHex);
            Assert.Equal(Path.GetFileNameWithoutExtension(specPath), project.Meta.Name);
            Assert.Equal("#E64A3B", project.Assets.FontAssets.Single().ColorHex);
        }
        finally
        {
            DeleteSpecFile(specPath);
        }
    }

    [Fact]
    public void ProjectSpecLoader_Load_RejectsSceneObjectsThatReferenceWrongAssetType()
    {
        var specPath = CreateSpecFile("type-mismatch-project.json", CreateTypeMismatchSpecJson());

        try
        {
            var loader = new ProjectSpecLoader();
            var exception = Assert.Throws<InvalidDataException>(() => loader.Load(specPath));

            Assert.Contains("semantic.scene_object.asset_ref.type_mismatch", exception.Message, StringComparison.Ordinal);
            Assert.Contains("SVG scene objects must reference an existing SVG asset.", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            DeleteSpecFile(specPath);
        }
    }

    [Fact]
    public void ProjectSpecLoader_Load_CanReadPhase12AuthoredWitnessSpec()
    {
        var specPath = ResolveRepoRelativePath("artifacts", "source-parity-demo", "project-engine.json");
        var loader = new ProjectSpecLoader();

        var project = loader.Load(specPath);

        Assert.Equal("source-parity-authored-witness", project.Meta.ProjectId);
        Assert.Equal(6, project.Assets.SvgAssets.Count);
        Assert.Single(project.Assets.HandAssets);
        Assert.Empty(project.Assets.ImageAssets);
        Assert.Equal(new[]
        {
            "svg-arrow",
            "svg-body",
            "svg-clock-group",
            "svg-footer",
            "svg-left",
            "svg-title"
        }, project.Assets.SvgAssets.Select(asset => asset.Id).ToArray());
        Assert.Equal("hand-1", project.Assets.HandAssets.Single().Id);
        Assert.Equal(new[]
        {
            "object-arrow",
            "object-body",
            "object-clock-group",
            "object-footer",
            "object-left",
            "object-title"
        }, project.Scenes.Single().Objects.Select(sceneObject => sceneObject.Id).ToArray());
    }

    private static string CreateSpecFile(string fileName, string json)
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), "whiteboard-cli-loader-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directoryPath);

        var specPath = Path.Combine(directoryPath, fileName);
        File.WriteAllText(specPath, json);
        return specPath;
    }

    private static void DeleteSpecFile(string specPath)
    {
        if (!File.Exists(specPath))
        {
            return;
        }

        var directoryPath = Path.GetDirectoryName(specPath);
        File.Delete(specPath);

        if (!string.IsNullOrWhiteSpace(directoryPath) && Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, recursive: true);
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

    private static string CreateInvalidSpecJson()
    {
        return """
            {
              "output": {
                "width": 1280,
                "height": 720,
                "frameRate": 0
              },
              "scenes": [
                {
                  "id": "",
                  "name": "Broken",
                  "durationSeconds": 5,
                  "objects": [
                    {
                      "id": "object-1",
                      "name": "Object",
                      "type": "svg",
                      "assetRefId": "svg-1"
                    }
                  ]
                }
              ],
              "timeline": {
                "events": [
                  {
                    "id": "",
                    "sceneId": "missing-scene",
                    "sceneObjectId": "missing-object",
                    "actionType": "draw",
                    "startSeconds": 0,
                    "durationSeconds": 2
                  }
                ]
              }
            }
            """;
    }

    private static string CreateValidSpecJson()
    {
        return """
            {
              "meta": {
                "projectId": "cli-loader-project"
              },
              "output": {
                "width": 1920,
                "height": 1080,
                "frameRate": 30,
                "backgroundColorHex": "#ffffff"
              },
              "assets": {
                "svgAssets": [
                  {
                    "id": "svg-b",
                    "name": "Beta",
                    "sourcePath": "assets/b.svg",
                    "type": "svg"
                  },
                  {
                    "id": "svg-a",
                    "name": "Alpha",
                    "sourcePath": "assets/a.svg",
                    "type": "svg"
                  }
                ],
                "fontAssets": [
                  {
                    "id": "font-a",
                    "name": "Accent",
                    "familyName": "Caveat",
                    "sourcePath": "assets/caveat.ttf",
                    "colorHex": "#e64a3b"
                  }
                ]
              },
              "scenes": [
                {
                  "id": "scene-b",
                  "name": "Scene B",
                  "durationSeconds": 8,
                  "objects": [
                    {
                      "id": "object-b",
                      "name": "Object B",
                      "type": "svg",
                      "assetRefId": "svg-b",
                      "layer": 2
                    }
                  ]
                },
                {
                  "id": "scene-a",
                  "name": "Scene A",
                  "durationSeconds": 4,
                  "objects": [
                    {
                      "id": "object-a",
                      "name": "Object A",
                      "type": "svg",
                      "assetRefId": "svg-a",
                      "layer": 1
                    }
                  ]
                }
              ],
              "timeline": {
                "events": [
                  {
                    "id": "event-b",
                    "sceneId": "scene-b",
                    "sceneObjectId": "object-b",
                    "actionType": "draw",
                    "startSeconds": 2,
                    "durationSeconds": 2
                  },
                  {
                    "id": "event-a",
                    "sceneId": "scene-a",
                    "sceneObjectId": "object-a",
                    "actionType": "draw",
                    "startSeconds": 0,
                    "durationSeconds": 2
                  }
                ]
              }
            }
            """;
    }

    private static string CreateTypeMismatchSpecJson()
    {
        return """
            {
              "meta": {
                "projectId": "cli-loader-type-mismatch"
              },
              "output": {
                "width": 1280,
                "height": 720,
                "frameRate": 24
              },
              "assets": {
                "imageAssets": [
                  {
                    "id": "image-1",
                    "name": "Witness Raster",
                    "sourcePath": "assets/witness.png",
                    "type": "image"
                  }
                ]
              },
              "scenes": [
                {
                  "id": "scene-1",
                  "name": "Mismatch",
                  "durationSeconds": 3,
                  "objects": [
                    {
                      "id": "object-1",
                      "name": "Wrong Type",
                      "type": "svg",
                      "assetRefId": "image-1",
                      "layer": 1
                    }
                  ]
                }
              ],
              "timeline": {
                "events": [
                  {
                    "id": "event-1",
                    "sceneId": "scene-1",
                    "sceneObjectId": "object-1",
                    "actionType": "draw",
                    "startSeconds": 0,
                    "durationSeconds": 1
                  }
                ]
              }
            }
            """;
    }
}

