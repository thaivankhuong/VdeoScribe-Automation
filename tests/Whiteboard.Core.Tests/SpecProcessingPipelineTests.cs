using System.Text.Json;
using Whiteboard.Core.Enums;
using Whiteboard.Core.Validation;
using Whiteboard.Core.ValueObjects;
using Xunit;

namespace Whiteboard.Core.Tests;

public sealed class SpecProcessingPipelineTests
{
    private readonly SpecProcessingPipeline _pipeline = new();

    [Fact]
    public void Ordering_SortsIssuesByGatePathSeverityCodeAndOccurrence()
    {
        var issues = new[]
        {
            new ValidationIssue(ValidationGate.Readiness, "$.timeline.events[0]", ValidationSeverity.Error, "timeline.event.out_of_range", "Out of range", 0),
            new ValidationIssue(ValidationGate.Contract, "$.meta.name", ValidationSeverity.Error, "contract.required", "Name required", 0),
            new ValidationIssue(ValidationGate.Schema, "$.scenes[0].durationSeconds", ValidationSeverity.Warning, "schema.range", "Duration warning", 0),
            new ValidationIssue(ValidationGate.Schema, "$.scenes[0].durationSeconds", ValidationSeverity.Error, "schema.range", "Duration error", 0),
            new ValidationIssue(ValidationGate.Schema, "$.scenes[0].durationSeconds", ValidationSeverity.Error, "schema.range", "Duration error second", 1),
            new ValidationIssue(ValidationGate.Schema, "$.scenes[0].id", ValidationSeverity.Error, "schema.required", "Scene id required", 0)
        };

        var ordered = ValidationIssueOrdering.Sort(issues);

        Assert.Collection(
            ordered,
            issue => Assert.Equal((ValidationGate.Contract, "$.meta.name", ValidationSeverity.Error, "contract.required", 0), (issue.Gate, issue.Path, issue.Severity, issue.Code, issue.OccurrenceIndex)),
            issue => Assert.Equal((ValidationGate.Schema, "$.scenes[0].durationSeconds", ValidationSeverity.Error, "schema.range", 0), (issue.Gate, issue.Path, issue.Severity, issue.Code, issue.OccurrenceIndex)),
            issue => Assert.Equal((ValidationGate.Schema, "$.scenes[0].durationSeconds", ValidationSeverity.Error, "schema.range", 1), (issue.Gate, issue.Path, issue.Severity, issue.Code, issue.OccurrenceIndex)),
            issue => Assert.Equal((ValidationGate.Schema, "$.scenes[0].durationSeconds", ValidationSeverity.Warning, "schema.range", 0), (issue.Gate, issue.Path, issue.Severity, issue.Code, issue.OccurrenceIndex)),
            issue => Assert.Equal((ValidationGate.Schema, "$.scenes[0].id", ValidationSeverity.Error, "schema.required", 0), (issue.Gate, issue.Path, issue.Severity, issue.Code, issue.OccurrenceIndex)),
            issue => Assert.Equal((ValidationGate.Readiness, "$.timeline.events[0]", ValidationSeverity.Error, "timeline.event.out_of_range", 0), (issue.Gate, issue.Path, issue.Severity, issue.Code, issue.OccurrenceIndex)));
    }

    [Fact]
    public void Ordering_IsStableAcrossRepeatedSorts()
    {
        var issues = new[]
        {
            new ValidationIssue(ValidationGate.Semantic, "$.scenes[0].objects[1].assetRefId", ValidationSeverity.Error, "semantic.asset.missing", "Missing asset", 1),
            new ValidationIssue(ValidationGate.Semantic, "$.scenes[0].objects[1].assetRefId", ValidationSeverity.Error, "semantic.asset.missing", "Missing asset", 0),
            new ValidationIssue(ValidationGate.Schema, "$.output.frameRate", ValidationSeverity.Error, "schema.range", "Frame rate invalid", 0)
        };

        var first = ValidationIssueOrdering.Sort(issues);
        var second = ValidationIssueOrdering.Sort(issues);

        Assert.Equal(first, second);
    }

    [Fact]
    public void GateOrder_StopsAfterSchemaFailure()
    {
        var result = _pipeline.Process(CreateSchemaInvalidJson(), "specs/invalid-schema.json");

        Assert.False(result.IsSuccess);
        Assert.Null(result.Project);
        Assert.Equal(
            new[] { ValidationGate.Contract, ValidationGate.Schema },
            result.Gates.Select(gate => gate.Gate).ToArray());
        Assert.All(result.Issues, issue => Assert.Equal(ValidationGate.Schema, issue.Gate));
    }

    [Fact]
    public void GateOrder_StopsAfterSemanticFailure()
    {
        var result = _pipeline.Process(CreateSemanticInvalidJson(), "specs/invalid-semantic.json");

        Assert.False(result.IsSuccess);
        Assert.Null(result.Project);
        Assert.Equal(
            new[] { ValidationGate.Contract, ValidationGate.Schema, ValidationGate.Normalization, ValidationGate.Semantic },
            result.Gates.Select(gate => gate.Gate).ToArray());
        Assert.Contains(result.Issues, issue => issue.Gate == ValidationGate.Semantic && issue.Code == "semantic.scene_object.asset_ref.required");
    }

    [Fact]
    public void GateOrder_ExecutesAllGatesForValidSpec()
    {
        var result = _pipeline.Process(CreateValidJson(), "specs/valid.json");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Project);
        Assert.Equal(
            new[] { ValidationGate.Contract, ValidationGate.Schema, ValidationGate.Normalization, ValidationGate.Semantic, ValidationGate.Readiness },
            result.Gates.Select(gate => gate.Gate).ToArray());
    }

    [Fact]
    public void Normalization_EquivalentValidInputsProduceIdenticalCanonicalOutput()
    {
        var first = _pipeline.Process(CreateNormalizationVariantOne(), "specs/project.json");
        var second = _pipeline.Process(CreateNormalizationVariantTwo(), "specs/project.json");

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.NotNull(first.Project);
        Assert.NotNull(second.Project);
        Assert.Equal(first.Project.CanonicalJson, second.Project.CanonicalJson);

        using var document = JsonDocument.Parse(first.Project.CanonicalJson);
        var root = document.RootElement;
        var sceneIds = root.GetProperty("Scenes").EnumerateArray().Select(scene => scene.GetProperty("Id").GetString()).ToArray();
        var svgIds = root.GetProperty("Assets").GetProperty("SvgAssets").EnumerateArray().Select(asset => asset.GetProperty("Id").GetString()).ToArray();

        Assert.Equal(new[] { "scene-a", "scene-b" }, sceneIds);
        Assert.Equal(new[] { "svg-a", "svg-b" }, svgIds);
    }

    [Fact]
    public void RegistrySnapshotPinning_RequiresSnapshotMetadataWhenPinned()
    {
        var result = _pipeline.Process(CreateRegistryPinningMissingSnapshotJson(), "specs/registry-missing.json");

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Issues, issue => issue.Code == "semantic.asset_registry.snapshot.required");
        Assert.Contains(result.Issues, issue => issue.Code == "semantic.asset_registry.id.required");
    }

    [Fact]
    public void RegistrySnapshotPinning_RejectsUnknownAndDeprecatedSnapshots()
    {
        var unknownSnapshot = _pipeline.Process(CreateRegistrySnapshotUnknownJson(), "specs/registry-unknown.json");
        var deprecatedSnapshot = _pipeline.Process(CreateRegistrySnapshotDeprecatedJson(), "specs/registry-deprecated.json");

        Assert.False(unknownSnapshot.IsSuccess);
        Assert.Contains(unknownSnapshot.Issues, issue => issue.Code == "semantic.asset_registry.snapshot.unknown");

        Assert.False(deprecatedSnapshot.IsSuccess);
        Assert.Contains(deprecatedSnapshot.Issues, issue => issue.Code == "semantic.asset_registry.snapshot.deprecated");
    }

    [Fact]
    public void EffectProfileValidation_RejectsMissingProfileAndActionMismatch()
    {
        var missingProfile = _pipeline.Process(CreateEffectProfileMissingJson(), "specs/effect-profile-missing.json");
        var mismatchedAction = _pipeline.Process(CreateEffectProfileActionMismatchJson(), "specs/effect-profile-mismatch.json");

        Assert.False(missingProfile.IsSuccess);
        Assert.Contains(missingProfile.Issues, issue => issue.Code == "semantic.effect_profile.missing");

        Assert.False(mismatchedAction.IsSuccess);
        Assert.Contains(mismatchedAction.Issues, issue => issue.Code == "semantic.effect_profile.action_mismatch");
    }

    [Fact]
    public void EffectProfileValidation_RejectsOutOfRangeParameters()
    {
        var result = _pipeline.Process(CreateEffectProfileOutOfRangeJson(), "specs/effect-profile-range.json");

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Issues, issue => issue.Code == "semantic.effect_profile.parameter_out_of_range");
    }

    private static string CreateValidJson()
    {
        return """
            {
              "meta": {
                "projectId": "project-001",
                "name": "Deterministic Project"
              },
              "output": {
                "width": 1280,
                "height": 720,
                "frameRate": 30,
                "backgroundColorHex": "#ffffff"
              },
              "assets": {
                "svgAssets": [
                  {
                    "id": "svg-1",
                    "name": "Bulb",
                    "sourcePath": "assets/bulb.svg",
                    "type": "svg"
                  }
                ]
              },
              "scenes": [
                {
                  "id": "scene-1",
                  "name": "Intro",
                  "durationSeconds": 5,
                  "objects": [
                    {
                      "id": "object-1",
                      "name": "Bulb",
                      "type": "svg",
                      "assetRefId": "svg-1",
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
                    "durationSeconds": 3,
                    "parameters": {
                      "strokeOrder": "sequential"
                    }
                  }
                ]
              }
            }
            """;
    }

    private static string CreateRegistryPinningMissingSnapshotJson()
    {
        return """
            {
              "meta": {
                "projectId": "registry-missing",
                "name": "Registry Missing",
                "assetRegistrySnapshotId": "reg-main-2026-04"
              },
              "output": {
                "width": 1280,
                "height": 720,
                "frameRate": 30
              },
              "assets": {
                "svgAssets": [
                  {
                    "id": "svg-1",
                    "name": "Bulb",
                    "sourcePath": "assets/bulb.svg",
                    "type": "svg"
                  }
                ]
              },
              "scenes": [
                {
                  "id": "scene-1",
                  "name": "Intro",
                  "durationSeconds": 5,
                  "objects": [
                    {
                      "id": "object-1",
                      "name": "Bulb",
                      "type": "svg",
                      "assetRefId": "svg-1",
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

    private static string CreateRegistrySnapshotUnknownJson()
    {
        return """
            {
              "meta": {
                "projectId": "registry-unknown",
                "name": "Registry Unknown",
                "assetRegistrySnapshotId": "reg-main-2026-99"
              },
              "output": {
                "width": 1280,
                "height": 720,
                "frameRate": 30
              },
              "assets": {
                "registrySnapshot": {
                  "registryId": "main-registry",
                  "snapshotId": "reg-main-2026-99",
                  "snapshotVersion": "2026.99.0"
                },
                "svgAssets": [
                  {
                    "id": "svg-1",
                    "name": "Bulb",
                    "sourcePath": "assets/bulb.svg",
                    "type": "svg"
                  }
                ]
              },
              "scenes": [
                {
                  "id": "scene-1",
                  "name": "Intro",
                  "durationSeconds": 5,
                  "objects": [
                    {
                      "id": "object-1",
                      "name": "Bulb",
                      "type": "svg",
                      "assetRefId": "svg-1",
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

    private static string CreateRegistrySnapshotDeprecatedJson()
    {
        return """
            {
              "meta": {
                "projectId": "registry-deprecated",
                "name": "Registry Deprecated",
                "assetRegistrySnapshotId": "reg-main-2026-03"
              },
              "output": {
                "width": 1280,
                "height": 720,
                "frameRate": 30
              },
              "assets": {
                "registrySnapshot": {
                  "registryId": "main-registry",
                  "snapshotId": "reg-main-2026-03",
                  "snapshotVersion": "2026.03.0"
                },
                "svgAssets": [
                  {
                    "id": "svg-1",
                    "name": "Bulb",
                    "sourcePath": "assets/bulb.svg",
                    "type": "svg"
                  }
                ]
              },
              "scenes": [
                {
                  "id": "scene-1",
                  "name": "Intro",
                  "durationSeconds": 5,
                  "objects": [
                    {
                      "id": "object-1",
                      "name": "Bulb",
                      "type": "svg",
                      "assetRefId": "svg-1",
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

    private static string CreateEffectProfileMissingJson()
    {
        return """
            {
              "meta": {
                "projectId": "effect-profile-missing",
                "name": "Effect Profile Missing"
              },
              "output": {
                "width": 1280,
                "height": 720,
                "frameRate": 30
              },
              "assets": {
                "svgAssets": [
                  {
                    "id": "svg-1",
                    "name": "Bulb",
                    "sourcePath": "assets/bulb.svg",
                    "type": "svg"
                  }
                ]
              },
              "scenes": [
                {
                  "id": "scene-1",
                  "name": "Intro",
                  "durationSeconds": 5,
                  "objects": [
                    {
                      "id": "object-1",
                      "name": "Bulb",
                      "type": "svg",
                      "assetRefId": "svg-1",
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
                    "durationSeconds": 2,
                    "parameters": {
                      "effectProfileId": "effect-draw-default"
                    }
                  }
                ]
              }
            }
            """;
    }

    private static string CreateEffectProfileActionMismatchJson()
    {
        return """
            {
              "meta": {
                "projectId": "effect-profile-mismatch",
                "name": "Effect Profile Mismatch"
              },
              "output": {
                "width": 1280,
                "height": 720,
                "frameRate": 30
              },
              "assets": {
                "svgAssets": [
                  {
                    "id": "svg-1",
                    "name": "Bulb",
                    "sourcePath": "assets/bulb.svg",
                    "type": "svg"
                  }
                ]
              },
              "scenes": [
                {
                  "id": "scene-1",
                  "name": "Intro",
                  "durationSeconds": 5,
                  "objects": [
                    {
                      "id": "object-1",
                      "name": "Bulb",
                      "type": "svg",
                      "assetRefId": "svg-1",
                      "layer": 1
                    }
                  ]
                }
              ],
              "timeline": {
                "effectProfiles": [
                  {
                    "id": "effect-move-only",
                    "actionType": "move",
                    "minDurationSeconds": 0.5,
                    "maxDurationSeconds": 4
                  }
                ],
                "events": [
                  {
                    "id": "event-1",
                    "sceneId": "scene-1",
                    "sceneObjectId": "object-1",
                    "actionType": "draw",
                    "startSeconds": 0,
                    "durationSeconds": 2,
                    "parameters": {
                      "effectProfileId": "effect-move-only"
                    }
                  }
                ]
              }
            }
            """;
    }

    private static string CreateEffectProfileOutOfRangeJson()
    {
        return """
            {
              "meta": {
                "projectId": "effect-profile-range",
                "name": "Effect Profile Range"
              },
              "output": {
                "width": 1280,
                "height": 720,
                "frameRate": 30
              },
              "assets": {
                "svgAssets": [
                  {
                    "id": "svg-1",
                    "name": "Bulb",
                    "sourcePath": "assets/bulb.svg",
                    "type": "svg"
                  }
                ]
              },
              "scenes": [
                {
                  "id": "scene-1",
                  "name": "Intro",
                  "durationSeconds": 5,
                  "objects": [
                    {
                      "id": "object-1",
                      "name": "Bulb",
                      "type": "svg",
                      "assetRefId": "svg-1",
                      "layer": 1
                    }
                  ]
                }
              ],
              "timeline": {
                "effectProfiles": [
                  {
                    "id": "effect-fade-default",
                    "actionType": "fade",
                    "minDurationSeconds": 0.5,
                    "maxDurationSeconds": 4,
                    "parameterBounds": {
                      "opacity": {
                        "key": "opacity",
                        "minValue": 0,
                        "maxValue": 1
                      }
                    }
                  }
                ],
                "events": [
                  {
                    "id": "event-1",
                    "sceneId": "scene-1",
                    "sceneObjectId": "object-1",
                    "actionType": "fade",
                    "startSeconds": 0,
                    "durationSeconds": 2,
                    "parameters": {
                      "effectProfileId": "effect-fade-default",
                      "opacity": "1.5"
                    }
                  }
                ]
              }
            }
            """;
    }

    private static string CreateSchemaInvalidJson()
    {
        return """
            {
              "output": {
                "width": 0,
                "height": 720,
                "frameRate": 0
              },
              "scenes": [
                {
                  "id": "",
                  "name": "Broken",
                  "durationSeconds": 0,
                  "objects": [
                    {
                      "id": "",
                      "name": "Object",
                      "type": "svg"
                    }
                  ]
                }
              ],
              "timeline": {
                "events": [
                  {
                    "id": "",
                    "sceneId": "",
                    "sceneObjectId": "",
                    "actionType": "draw",
                    "startSeconds": -1,
                    "durationSeconds": 0
                  }
                ]
              }
            }
            """;
    }

    private static string CreateSemanticInvalidJson()
    {
        return """
            {
              "meta": {
                "projectId": "semantic-project",
                "name": "Semantic Failure"
              },
              "output": {
                "width": 1280,
                "height": 720,
                "frameRate": 30
              },
              "scenes": [
                {
                  "id": "scene-1",
                  "name": "Intro",
                  "durationSeconds": 5,
                  "objects": [
                    {
                      "id": "object-1",
                      "name": "Bulb",
                      "type": "svg"
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
                    "durationSeconds": 3
                  }
                ]
              }
            }
            """;
    }

    [Fact]
    public void Camera_KeyframesRejectUnsupportedInterpolationAndEasingPolicies()
    {
        var result = _pipeline.Process(CreateCameraPolicyInvalidJson(), "specs/invalid-camera-policy.json");

        Assert.False(result.IsSuccess);
        Assert.Equal(
            new[]
            {
                "schema.camera_keyframe.easing.unsupported",
                "schema.camera_keyframe.interpolation.unsupported",
                "schema.camera_keyframe.policy.invalid",
                "schema.camera_keyframe.easing.unsupported"
            },
            result.Issues.Select(issue => issue.Code).ToArray());
    }

    [Fact]
    public void Camera_NormalizationOrdersDuplicateTimestampsDeterministically()
    {
        var result = _pipeline.Process(CreateCameraDuplicateTimeJson(), "specs/camera-duplicates.json");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Project);

        var keyframes = result.Project.Project.Timeline.CameraTrack.Keyframes;
        Assert.Equal(3, keyframes.Count);
        Assert.Collection(
            keyframes,
            keyframe =>
            {
                Assert.Equal(0.5, keyframe.TimeSeconds);
                Assert.Equal(new Position2D(0, 0), keyframe.Position);
                Assert.Equal(1.0, keyframe.Zoom);
                Assert.Equal(EasingType.Linear, keyframe.Interpolation);
            },
            keyframe =>
            {
                Assert.Equal(0.5, keyframe.TimeSeconds);
                Assert.Equal(new Position2D(10, 0), keyframe.Position);
                Assert.Equal(1.1, keyframe.Zoom);
                Assert.Equal(EasingType.Linear, keyframe.Interpolation);
            },
            keyframe =>
            {
                Assert.Equal(0.5, keyframe.TimeSeconds);
                Assert.Equal(new Position2D(10, 10), keyframe.Position);
                Assert.Equal(1.1, keyframe.Zoom);
                Assert.Equal(EasingType.Step, keyframe.Interpolation);
            });
    }

    private static string CreateNormalizationVariantOne()
    {
        return """
            {
              "meta": {
                "projectId": "project-variant",
                "name": "Normalization Test"
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
                    },
                    {
                      "id": "object-a",
                      "name": "Object A",
                      "type": "svg",
                      "assetRefId": "svg-a",
                      "layer": 1
                    }
                  ]
                },
                {
                  "id": "scene-a",
                  "name": "Scene A",
                  "durationSeconds": 4,
                  "objects": [
                    {
                      "id": "object-c",
                      "name": "Object C",
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
                    "durationSeconds": 2,
                    "parameters": {
                      "z": "last",
                      "a": "first"
                    }
                  },
                  {
                    "id": "event-a",
                    "sceneId": "scene-a",
                    "sceneObjectId": "object-c",
                    "actionType": "draw",
                    "startSeconds": 0,
                    "durationSeconds": 2,
                    "parameters": {
                      "m": "middle"
                    }
                  }
                ]
              }
            }
            """;
    }

    private static string CreateCameraPolicyInvalidJson()
    {
        return """
            {
              "meta": {
                "projectId": "camera-policy",
                "name": "Camera Policy"
              },
              "output": {
                "width": 1280,
                "height": 720,
                "frameRate": 30
              },
              "assets": {
                "svgAssets": [
                  {
                    "id": "svg-1",
                    "name": "Bulb",
                    "sourcePath": "assets/bulb.svg",
                    "type": "svg"
                  }
                ]
              },
              "scenes": [
                {
                  "id": "scene-1",
                  "name": "Intro",
                  "durationSeconds": 5,
                  "objects": [
                    {
                      "id": "object-1",
                      "name": "Bulb",
                      "type": "svg",
                      "assetRefId": "svg-1",
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
                ],
                "cameraTrack": {
                  "keyframes": [
                    {
                      "timeSeconds": 0,
                      "position": {
                        "x": 0,
                        "y": 0
                      },
                      "zoom": 1.0,
                      "interpolation": "easeInOut",
                      "easing": "easeIn"
                    },
                    {
                      "timeSeconds": 1,
                      "position": {
                        "x": 10,
                        "y": 5
                      },
                      "zoom": 1.2,
                      "interpolation": "step",
                      "easing": "easeOut"
                    }
                  ]
                }
              }
            }
            """;
    }

    private static string CreateCameraDuplicateTimeJson()
    {
        return """
            {
              "meta": {
                "projectId": "camera-duplicates",
                "name": "Camera Duplicates"
              },
              "output": {
                "width": 1280,
                "height": 720,
                "frameRate": 30
              },
              "assets": {
                "svgAssets": [
                  {
                    "id": "svg-1",
                    "name": "Bulb",
                    "sourcePath": "assets/bulb.svg",
                    "type": "svg"
                  }
                ]
              },
              "scenes": [
                {
                  "id": "scene-1",
                  "name": "Intro",
                  "durationSeconds": 5,
                  "objects": [
                    {
                      "id": "object-1",
                      "name": "Bulb",
                      "type": "svg",
                      "assetRefId": "svg-1",
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
                ],
                "cameraTrack": {
                  "keyframes": [
                    {
                      "timeSeconds": 0.5,
                      "position": {
                        "x": 10,
                        "y": 10
                      },
                      "zoom": 1.1,
                      "interpolation": "step",
                      "easing": "linear"
                    },
                    {
                      "timeSeconds": 0.5,
                      "position": {
                        "x": 0,
                        "y": 0
                      },
                      "zoom": 1.0,
                      "interpolation": "linear",
                      "easing": "linear"
                    },
                    {
                      "timeSeconds": 0.5,
                      "position": {
                        "x": 10,
                        "y": 0
                      },
                      "zoom": 1.1,
                      "interpolation": "linear",
                      "easing": "linear"
                    }
                  ]
                }
              }
            }
            """;
    }

    private static string CreateNormalizationVariantTwo()
    {
        return """
            {
              "timeline": {
                "events": [
                  {
                    "parameters": {
                      "m": "middle"
                    },
                    "durationSeconds": 2,
                    "startSeconds": 0,
                    "actionType": "draw",
                    "sceneObjectId": "object-c",
                    "sceneId": "scene-a",
                    "id": "event-a"
                  },
                  {
                    "parameters": {
                      "a": "first",
                      "z": "last"
                    },
                    "durationSeconds": 2,
                    "startSeconds": 2,
                    "actionType": "draw",
                    "sceneObjectId": "object-b",
                    "sceneId": "scene-b",
                    "id": "event-b"
                  }
                ]
              },
              "scenes": [
                {
                  "objects": [
                    {
                      "assetRefId": "svg-a",
                      "layer": 1,
                      "type": "svg",
                      "name": "Object C",
                      "id": "object-c"
                    }
                  ],
                  "durationSeconds": 4,
                  "name": "Scene A",
                  "id": "scene-a"
                },
                {
                  "objects": [
                    {
                      "assetRefId": "svg-a",
                      "layer": 1,
                      "type": "svg",
                      "name": "Object A",
                      "id": "object-a"
                    },
                    {
                      "assetRefId": "svg-b",
                      "layer": 2,
                      "type": "svg",
                      "name": "Object B",
                      "id": "object-b"
                    }
                  ],
                  "durationSeconds": 8,
                  "name": "Scene B",
                  "id": "scene-b"
                }
              ],
              "assets": {
                "svgAssets": [
                  {
                    "sourcePath": "assets/a.svg",
                    "name": "Alpha",
                    "type": "svg",
                    "id": "svg-a"
                  },
                  {
                    "sourcePath": "assets/b.svg",
                    "name": "Beta",
                    "type": "svg",
                    "id": "svg-b"
                  }
                ]
              },
              "output": {
                "backgroundColorHex": "#FFFFFF",
                "frameRate": 30,
                "height": 1080,
                "width": 1920
              },
              "meta": {
                "name": "Normalization Test",
                "projectId": "project-variant"
              }
            }
            """;
    }
}
