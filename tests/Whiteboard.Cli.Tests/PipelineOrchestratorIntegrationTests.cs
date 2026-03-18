using System;
using System.IO;
using Whiteboard.Cli.Models;
using Whiteboard.Cli.Services;
using Xunit;

namespace Whiteboard.Cli.Tests;

public sealed class PipelineOrchestratorIntegrationTests
{
    [Fact]
    public void PipelineOrchestrator_CanRunEndToEnd_WithJsonSpec()
    {
        var specPath = CreatePrimarySpecFile();

        try
        {
            var orchestrator = new PipelineOrchestrator();
            var request = new CliRunRequest
            {
                SpecPath = specPath,
                OutputPath = "out/video.mp4",
                FrameIndex = 0
            };

            var result = orchestrator.Run(request);

            Assert.True(result.Success);
            Assert.Equal(specPath, result.SpecPath);
            Assert.Equal(0, result.FrameIndex);
            Assert.Equal(1, result.SceneCount);
            Assert.Equal(2, result.ObjectCount);
            Assert.Equal(1, result.ExportedFrameCount);
            Assert.Equal("out/video.mp4", result.OutputPath);
            Assert.False(string.IsNullOrWhiteSpace(result.ExportStatus));
            Assert.False(string.IsNullOrWhiteSpace(result.DeterministicKey));
        }
        finally
        {
            DeleteSpecFile(specPath);
        }
    }

    [Fact]
    public void PipelineOrchestrator_WithSameSpec_ProducesDeterministicStructure()
    {
        var specPath = CreatePrimarySpecFile();

        try
        {
            var orchestrator = new PipelineOrchestrator();
            var request = new CliRunRequest
            {
                SpecPath = specPath,
                OutputPath = "out/video.mp4",
                FrameIndex = 0
            };

            var first = orchestrator.Run(request);
            var second = orchestrator.Run(request);

            Assert.Equal(first.Success, second.Success);
            Assert.Equal(first.SpecPath, second.SpecPath);
            Assert.Equal(first.FrameIndex, second.FrameIndex);
            Assert.Equal(first.SceneCount, second.SceneCount);
            Assert.Equal(first.ObjectCount, second.ObjectCount);
            Assert.Equal(first.OperationCount, second.OperationCount);
            Assert.Equal(first.ExportedFrameCount, second.ExportedFrameCount);
            Assert.Equal(first.OutputPath, second.OutputPath);
            Assert.Equal(first.ExportStatus, second.ExportStatus);
            Assert.Equal(first.DeterministicKey, second.DeterministicKey);
            Assert.Equal(first.Operations, second.Operations);
        }
        finally
        {
            DeleteSpecFile(specPath);
        }
    }

    [Fact]
    public void PipelineOrchestrator_WithEquivalentSpecsUsingDifferentSourceOrdering_ProducesEquivalentDeterministicOutput()
    {
        var firstSpecPath = CreatePrimarySpecFile();
        var secondSpecPath = CreateReorderedEquivalentSpecFile();

        try
        {
            var orchestrator = new PipelineOrchestrator();
            var firstRequest = new CliRunRequest
            {
                SpecPath = firstSpecPath,
                OutputPath = "out/video.mp4",
                FrameIndex = 1
            };
            var secondRequest = new CliRunRequest
            {
                SpecPath = secondSpecPath,
                OutputPath = "out/video.mp4",
                FrameIndex = 1
            };

            var first = orchestrator.Run(firstRequest);
            var second = orchestrator.Run(secondRequest);

            Assert.True(first.Success);
            Assert.True(second.Success);
            Assert.Equal(first.FrameIndex, second.FrameIndex);
            Assert.Equal(first.SceneCount, second.SceneCount);
            Assert.Equal(first.ObjectCount, second.ObjectCount);
            Assert.Equal(first.OperationCount, second.OperationCount);
            Assert.Equal(first.Operations, second.Operations);
            Assert.Equal(first.DeterministicKey, second.DeterministicKey);
        }
        finally
        {
            DeleteSpecFile(firstSpecPath);
            DeleteSpecFile(secondSpecPath);
        }
    }

    private static string CreatePrimarySpecFile()
    {
        return CreateSpecFile(GetPrimarySpecJson());
    }

    private static string CreateReorderedEquivalentSpecFile()
    {
        return CreateSpecFile(GetReorderedEquivalentSpecJson());
    }

    private static string CreateSpecFile(string json)
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), "whiteboard-cli-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directoryPath);

        var specPath = Path.Combine(directoryPath, "project.json");
        File.WriteAllText(specPath, json);
        return specPath;
    }

    private static string GetPrimarySpecJson()
    {
        return """
            {
              "meta": {
                "projectId": "cli-project-001",
                "name": "CLI Integration Test"
              },
              "output": {
                "width": 1280,
                "height": 720,
                "frameRate": 30
              },
              "assets": {
                "svgAssets": [
                  {
                    "id": "svg-2",
                    "name": "Arrow",
                    "sourcePath": "assets/arrow.svg",
                    "type": "svg"
                  },
                  {
                    "id": "svg-1",
                    "name": "Idea Bulb",
                    "sourcePath": "assets/idea.svg",
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
                      "id": "object-2",
                      "name": "Arrow",
                      "type": "svg",
                      "assetRefId": "svg-2",
                      "layer": 2,
                      "transform": {
                        "position": {
                          "x": 300,
                          "y": 220
                        },
                        "size": {
                          "width": 120,
                          "height": 120
                        }
                      }
                    },
                    {
                      "id": "object-1",
                      "name": "Bulb",
                      "type": "svg",
                      "assetRefId": "svg-1",
                      "layer": 1,
                      "isVisible": false,
                      "transform": {
                        "position": {
                          "x": 100,
                          "y": 200
                        },
                        "size": {
                          "width": 300,
                          "height": 300
                        }
                      }
                    }
                  ]
                }
              ],
              "timeline": {
                "events": [
                  {
                    "id": "event-2",
                    "sceneId": "scene-1",
                    "sceneObjectId": "object-2",
                    "actionType": "reveal",
                    "startSeconds": 0,
                    "durationSeconds": 2
                  },
                  {
                    "id": "event-1",
                    "sceneId": "scene-1",
                    "sceneObjectId": "object-1",
                    "actionType": "draw",
                    "startSeconds": 0,
                    "durationSeconds": 2
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
                      "zoom": 1
                    }
                  ]
                }
              }
            }
            """;
    }

    private static string GetReorderedEquivalentSpecJson()
    {
        return """
            {
              "meta": {
                "projectId": "cli-project-001",
                "name": "CLI Integration Test"
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
                    "name": "Idea Bulb",
                    "sourcePath": "assets/idea.svg",
                    "type": "svg"
                  },
                  {
                    "id": "svg-2",
                    "name": "Arrow",
                    "sourcePath": "assets/arrow.svg",
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
                      "layer": 1,
                      "isVisible": false,
                      "transform": {
                        "position": {
                          "x": 100,
                          "y": 200
                        },
                        "size": {
                          "width": 300,
                          "height": 300
                        }
                      }
                    },
                    {
                      "id": "object-2",
                      "name": "Arrow",
                      "type": "svg",
                      "assetRefId": "svg-2",
                      "layer": 2,
                      "transform": {
                        "position": {
                          "x": 300,
                          "y": 220
                        },
                        "size": {
                          "width": 120,
                          "height": 120
                        }
                      }
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
                    "durationSeconds": 2
                  },
                  {
                    "id": "event-2",
                    "sceneId": "scene-1",
                    "sceneObjectId": "object-2",
                    "actionType": "reveal",
                    "startSeconds": 0,
                    "durationSeconds": 2
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
                      "zoom": 1
                    }
                  ]
                }
              }
            }
            """;
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
}

