using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Whiteboard.Cli.Models;
using Whiteboard.Cli.Services;
using Xunit;

namespace Whiteboard.Cli.Tests;

public sealed class PipelineOrchestratorIntegrationTests
{
    [Fact]
    public void PipelineOrchestrator_CanRunEndToEnd_WithJsonSpec()
    {
        var specPath = CreateSpecFile("phase03-determinism", "primary-spec.json");

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
            Assert.Equal(0, result.ExportedAudioCueCount);
            Assert.Equal("out/video.mp4", result.OutputPath);
            Assert.False(string.IsNullOrWhiteSpace(result.ExportStatus));
            Assert.False(string.IsNullOrWhiteSpace(result.ExportDeterministicKey));
            Assert.False(string.IsNullOrWhiteSpace(result.DeterministicKey));
            Assert.NotEmpty(result.Operations);
            Assert.Single(result.ExportFrames);
            Assert.Empty(result.ExportAudioCues);
            Assert.Equal("mp4", result.ExportSummary.Format);
            Assert.Equal(1280, result.ExportSummary.Width);
            Assert.Equal(720, result.ExportSummary.Height);
            Assert.Equal(30, result.ExportSummary.FrameRate, 6);
            Assert.StartsWith("camera:", result.Operations[0], StringComparison.Ordinal);
            Assert.Contains(result.Operations, operation => operation.Contains("svg-path:", StringComparison.Ordinal));
            Assert.Equal(result.Operations, result.ExportFrames[0].Operations);
        }
        finally
        {
            DeleteSpecFile(specPath);
        }
    }

    [Fact]
    public void PipelineOrchestrator_WithSameSpec_ProducesDeterministicStructure()
    {
        var specPath = CreateSpecFile("phase03-determinism", "primary-spec.json");

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
            Assert.Equal(first.ExportedAudioCueCount, second.ExportedAudioCueCount);
            Assert.Equal(first.OutputPath, second.OutputPath);
            Assert.Equal(first.ExportStatus, second.ExportStatus);
            Assert.Equal(first.ExportDeterministicKey, second.ExportDeterministicKey);
            Assert.Equal(first.DeterministicKey, second.DeterministicKey);
            Assert.Equal(first.Operations, second.Operations);
            Assert.Equal(first.ExportFrames.Select(frame => frame.FrameIndex).ToArray(), second.ExportFrames.Select(frame => frame.FrameIndex).ToArray());
        }
        finally
        {
            DeleteSpecFile(specPath);
        }
    }

    [Fact]
    public void PipelineOrchestrator_WithEquivalentSpecsUsingDifferentSourceOrdering_ProducesEquivalentDeterministicOutput()
    {
        var firstSpecPath = CreateSpecFile("phase03-determinism", "primary-spec.json");
        var secondSpecPath = CreateSpecFile("phase03-determinism", "equivalent-reordered-spec.json");

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
            Assert.Equal(first.ExportedFrameCount, second.ExportedFrameCount);
            Assert.Equal(first.ExportedAudioCueCount, second.ExportedAudioCueCount);
            Assert.Equal(first.Operations, second.Operations);
            Assert.Equal(first.ExportDeterministicKey, second.ExportDeterministicKey);
            Assert.Equal(first.DeterministicKey, second.DeterministicKey);
        }
        finally
        {
            DeleteSpecFile(firstSpecPath);
            DeleteSpecFile(secondSpecPath);
        }
    }

    [Fact]
    public void PipelineOrchestrator_WithPhase04SvgFixtures_ProducesEquivalentDeterministicOutput()
    {
        var firstSpecPath = CreateSpecFile("phase04-svg-rendering", "primary-spec.json");
        var secondSpecPath = CreateSpecFile("phase04-svg-rendering", "equivalent-reordered-spec.json");

        try
        {
            var orchestrator = new PipelineOrchestrator();
            var first = orchestrator.Run(new CliRunRequest
            {
                SpecPath = firstSpecPath,
                OutputPath = "out/video.mp4",
                FrameIndex = 15
            });
            var second = orchestrator.Run(new CliRunRequest
            {
                SpecPath = secondSpecPath,
                OutputPath = "out/video.mp4",
                FrameIndex = 15
            });

            Assert.True(first.Success);
            Assert.True(second.Success);
            Assert.Equal(first.Operations, second.Operations);
            Assert.Equal(first.ExportDeterministicKey, second.ExportDeterministicKey);
            Assert.Equal(first.DeterministicKey, second.DeterministicKey);
            Assert.Contains(first.Operations, operation => operation.Contains("svg-path:mode:partial", StringComparison.Ordinal));
        }
        finally
        {
            DeleteSpecFile(firstSpecPath);
            DeleteSpecFile(secondSpecPath);
        }
    }

    [Fact]
    public void PipelineOrchestrator_WithAudioCue_SurfacesExportPackageMetadata()
    {
        var specPath = CreateSpecFileFromJson(
            """
            {
              "meta": {
                "projectId": "cli-export-audio",
                "name": "CLI Export Audio"
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
                  }
                ],
                "audioAssets": [
                  {
                    "id": "audio-1",
                    "name": "Narration",
                    "sourcePath": "assets/narration.mp3",
                    "type": "audio",
                    "defaultVolume": 0.6
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
                      "isVisible": true,
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
                    "id": "event-1",
                    "sceneId": "scene-1",
                    "sceneObjectId": "object-1",
                    "actionType": "draw",
                    "startSeconds": 0,
                    "durationSeconds": 2,
                    "parameters": {
                      "pathOrder": "0"
                    }
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
                      "zoom": 1,
                      "interpolation": "linear"
                    }
                  ]
                },
                "audioCues": [
                  {
                    "id": "cue-1",
                    "audioAssetId": "audio-1",
                    "startSeconds": 0.5,
                    "durationSeconds": 2.25,
                    "volume": 0.8
                  }
                ]
              }
            }
            """,
            includeAudioAssets: true);

        try
        {
            var orchestrator = new PipelineOrchestrator();
            var result = orchestrator.Run(new CliRunRequest
            {
                SpecPath = specPath,
                OutputPath = "out/video.mp4",
                FrameIndex = 0
            });

            Assert.True(result.Success);
            Assert.Equal(1, result.ExportedFrameCount);
            Assert.Equal(1, result.ExportedAudioCueCount);
            Assert.Single(result.ExportFrames);
            Assert.Single(result.ExportAudioCues);
            Assert.Equal("cue-1", result.ExportAudioCues[0].CueId);
            Assert.Equal("assets/narration.mp3", result.ExportAudioCues[0].SourcePath);
            Assert.Equal("mp4", result.ExportSummary.Format);
            Assert.Equal(1, result.ExportSummary.FrameCount);
            Assert.Equal(1, result.ExportSummary.AudioCueCount);
            Assert.Equal(30, result.ExportSummary.FrameRate, 6);
            Assert.Equal(0, result.ExportFrames[0].StartSeconds, 6);
            Assert.Equal(result.Operations, result.ExportFrames[0].Operations);
            Assert.False(string.IsNullOrWhiteSpace(result.ExportDeterministicKey));
            Assert.Contains("audio:", result.ExportDeterministicKey, StringComparison.Ordinal);
        }
        finally
        {
            DeleteSpecFile(specPath);
        }
    }

    [Fact]
    public void PipelineOrchestrator_WithPhase05ExportFixtures_ProducesEquivalentExportPackages()
    {
        var firstSpecPath = CreateSpecFile("phase05-export-packaging", "primary-spec.json");
        var secondSpecPath = CreateSpecFile("phase05-export-packaging", "equivalent-reordered-spec.json");

        try
        {
            var orchestrator = new PipelineOrchestrator();
            var first = orchestrator.Run(new CliRunRequest
            {
                SpecPath = firstSpecPath,
                OutputPath = "out/video.mp4",
                FrameIndex = 15
            });
            var second = orchestrator.Run(new CliRunRequest
            {
                SpecPath = secondSpecPath,
                OutputPath = "out/video.mp4",
                FrameIndex = 15
            });

            Assert.True(first.Success);
            Assert.True(second.Success);
            Assert.Equal(1, first.ExportedFrameCount);
            Assert.Equal(2, first.ExportedAudioCueCount);
            Assert.Equal(first.Operations, second.Operations);
            Assert.Equal(first.ExportDeterministicKey, second.ExportDeterministicKey);
            Assert.Equal(first.DeterministicKey, second.DeterministicKey);
            Assert.Equal(first.ExportSummary.FrameCount, second.ExportSummary.FrameCount);
            Assert.Equal(first.ExportSummary.AudioCueCount, second.ExportSummary.AudioCueCount);
            Assert.Equal(first.ExportSummary.TotalDurationSeconds, second.ExportSummary.TotalDurationSeconds, 6);
            Assert.Equal(first.ExportFrames[0].Operations, second.ExportFrames[0].Operations);
            Assert.Equal(first.ExportAudioCues.Select(cue => cue.CueId).ToArray(), second.ExportAudioCues.Select(cue => cue.CueId).ToArray());
            Assert.Equal(["cue-voice", "cue-music"], first.ExportAudioCues.Select(cue => cue.CueId).ToArray());
            Assert.Equal(["assets/narration.mp3", "assets/music.mp3"], first.ExportAudioCues.Select(cue => cue.SourcePath).ToArray());
        }
        finally
        {
            DeleteSpecFile(firstSpecPath);
            DeleteSpecFile(secondSpecPath);
        }
    }

    [Fact]
    public void BatchPipelineOrchestrator_WithPhase06Fixtures_ProducesEquivalentSummaryArtifacts()
    {
        var firstManifestPath = CreateBatchFixtureManifest("primary-manifest.json");
        var secondManifestPath = CreateBatchFixtureManifest("equivalent-reordered-manifest.json");
        var firstSummaryPath = Path.Combine(Path.GetDirectoryName(firstManifestPath)!, "summary.json");
        var secondSummaryPath = Path.Combine(Path.GetDirectoryName(secondManifestPath)!, "summary.json");

        try
        {
            var orchestrator = new BatchPipelineOrchestrator();
            var first = orchestrator.Run(new CliBatchRunRequest
            {
                ManifestPath = firstManifestPath,
                SummaryOutputPath = firstSummaryPath
            });
            var second = orchestrator.Run(new CliBatchRunRequest
            {
                ManifestPath = secondManifestPath,
                SummaryOutputPath = secondSummaryPath
            });

            Assert.True(first.Success);
            Assert.True(second.Success);
            Assert.Equal(2, first.JobCount);
            Assert.Equal(2, first.SuccessCount);
            Assert.Equal(0, first.FailureCount);
            Assert.Equal(new[] { "job-a", "job-b" }, first.Jobs.Select(job => job.JobId).ToArray());
            Assert.Equal(first.DeterministicKey, second.DeterministicKey);
            Assert.Equal(first.Jobs.Select(job => job.DeterministicKey).ToArray(), second.Jobs.Select(job => job.DeterministicKey).ToArray());
            Assert.True(File.Exists(firstSummaryPath));
            Assert.True(File.Exists(secondSummaryPath));
            Assert.Equal(File.ReadAllText(firstSummaryPath), File.ReadAllText(secondSummaryPath));

            var summary = JsonSerializer.Deserialize<CliBatchRunResult>(File.ReadAllText(firstSummaryPath), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(summary);
            Assert.Equal(first.JobCount, summary!.JobCount);
            Assert.Equal(first.DeterministicKey, summary.DeterministicKey);
            Assert.Equal(first.Jobs.Select(job => job.JobId).ToArray(), summary.Jobs.Select(job => job.JobId).ToArray());
        }
        finally
        {
            DeleteSpecFile(firstManifestPath);
            DeleteSpecFile(secondManifestPath);
        }
    }

    private static string CreateBatchFixtureManifest(string manifestFileName)
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), "whiteboard-cli-batch-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directoryPath);
        Directory.CreateDirectory(Path.Combine(directoryPath, "assets"));

        WriteSvgAssets(directoryPath);
        WriteAudioAssets(directoryPath);

        foreach (var fileName in new[] { "job-a-spec.json", "job-b-spec.json", manifestFileName })
        {
            File.WriteAllText(
                Path.Combine(directoryPath, fileName),
                ReadFixtureJson("phase06-cli-batch", fileName));
        }

        return Path.Combine(directoryPath, manifestFileName);
    }

    private static string CreateSpecFile(string fixtureFolder, string fileName)
    {
        return CreateSpecFileFromJson(
            ReadFixtureJson(fixtureFolder, fileName),
            includeAudioAssets: string.Equals(fixtureFolder, "phase05-export-packaging", StringComparison.Ordinal));
    }

    private static string CreateSpecFileFromJson(string json, bool includeAudioAssets = false)
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), "whiteboard-cli-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directoryPath);
        Directory.CreateDirectory(Path.Combine(directoryPath, "assets"));

        WriteSvgAssets(directoryPath);

        if (includeAudioAssets)
        {
            WriteAudioAssets(directoryPath);
        }

        var specPath = Path.Combine(directoryPath, "project.json");
        File.WriteAllText(specPath, json);
        return specPath;
    }

    private static string ReadFixtureJson(string fixtureFolder, string fileName)
    {
        var fixturePath = ResolveFixturePath(fixtureFolder, fileName);
        return File.ReadAllText(fixturePath);
    }

    private static string ResolveFixturePath(string fixtureFolder, string fileName)
    {
        var baseDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        var candidateRoots = new List<DirectoryInfo>();

        for (var current = baseDirectory; current is not null; current = current.Parent)
        {
            candidateRoots.Add(current);
        }

        foreach (var candidate in candidateRoots)
        {
            var fixturePath = Path.Combine(
                candidate.FullName,
                "tests",
                "Whiteboard.Cli.Tests",
                "Fixtures",
                fixtureFolder,
                fileName);

            if (File.Exists(fixturePath))
            {
                return fixturePath;
            }
        }

        throw new FileNotFoundException(
            $"Fixture '{fileName}' was not found under tests/Whiteboard.Cli.Tests/Fixtures/{fixtureFolder}.");
    }

    private static void WriteSvgAssets(string directoryPath)
    {
        File.WriteAllText(
            Path.Combine(directoryPath, "assets", "idea.svg"),
            """
            <svg xmlns="http://www.w3.org/2000/svg">
              <path d="M 0 0 L 10 0" />
              <path d="M 10 0 L 10 10" />
            </svg>
            """);

        File.WriteAllText(
            Path.Combine(directoryPath, "assets", "arrow.svg"),
            """
            <svg xmlns="http://www.w3.org/2000/svg">
              <path d="M 0 5 L 12 5" />
            </svg>
            """);
    }

    private static void WriteAudioAssets(string directoryPath)
    {
        File.WriteAllText(Path.Combine(directoryPath, "assets", "narration.mp3"), "placeholder-audio");
        File.WriteAllText(Path.Combine(directoryPath, "assets", "music.mp3"), "placeholder-audio");
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


