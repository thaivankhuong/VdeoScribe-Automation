using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Whiteboard.Cli.Models;
using Whiteboard.Cli.Services;
using Whiteboard.Core.ValueObjects;
using Whiteboard.Export.Models;
using Whiteboard.Export.Services;
using Whiteboard.Renderer.Contracts;
using Whiteboard.Renderer.Models;
using Xunit;

namespace Whiteboard.Cli.Tests;

public sealed class PipelineOrchestratorIntegrationTests
{
    [Fact]
    public void PipelineOrchestrator_CanRunEndToEnd_WithJsonSpec()
    {
        var specPath = CreateSpecFile("phase03-determinism", "primary-spec.json");
        var outputPath = CreateOutputPath(specPath);

        try
        {
            var orchestrator = new PipelineOrchestrator();
            var request = new CliRunRequest
            {
                SpecPath = specPath,
                OutputPath = outputPath
            };

            var result = orchestrator.Run(request);

            Assert.True(result.Success);
            Assert.Equal(specPath, result.SpecPath);
            Assert.Null(result.FrameIndex);
            Assert.Equal(0, result.FirstFrameIndex);
            Assert.Equal(149, result.LastFrameIndex);
            Assert.Equal(150, result.PlannedFrameCount);
            Assert.Equal(150, result.RenderedFrameCount);
            Assert.Equal(5, result.ProjectDurationSeconds, 6);
            Assert.Equal(1, result.SceneCount);
            Assert.Equal(2, result.ObjectCount);
            Assert.Equal(150, result.ExportedFrameCount);
            Assert.Equal(0, result.ExportedAudioCueCount);
            Assert.Equal(outputPath, result.OutputPath);
            Assert.False(string.IsNullOrWhiteSpace(result.ExportStatus));
            Assert.False(string.IsNullOrWhiteSpace(result.ExportDeterministicKey));
            Assert.False(string.IsNullOrWhiteSpace(result.DeterministicKey));
            Assert.False(string.IsNullOrWhiteSpace(result.ExportPackageRootPath));
            Assert.False(string.IsNullOrWhiteSpace(result.ExportManifestPath));
            Assert.NotEmpty(result.Operations);
            Assert.Equal(150, result.ExportFrames.Count);
            Assert.Empty(result.ExportAudioCues);
            Assert.Equal("mp4", result.ExportSummary.Format);
            Assert.Equal(1280, result.ExportSummary.Width);
            Assert.Equal(720, result.ExportSummary.Height);
            Assert.Equal(30, result.ExportSummary.FrameRate, 6);
            Assert.Equal(150, result.ExportSummary.FrameCount);
            Assert.Equal(5, result.ExportSummary.TotalDurationSeconds, 6);
            Assert.Equal(0, result.ExportFrames[0].FrameIndex);
            Assert.Equal(149, result.ExportFrames[^1].FrameIndex);
            Assert.StartsWith("frames/frame-000000.svg", result.ExportFrames[0].ArtifactRelativePath, StringComparison.Ordinal);
            Assert.True(File.Exists(result.ExportManifestPath));
            Assert.True(File.Exists(Path.Combine(result.ExportPackageRootPath, result.ExportFrames[0].ArtifactRelativePath.Replace('/', Path.DirectorySeparatorChar))));
            Assert.StartsWith("camera:", result.Operations[0], StringComparison.Ordinal);
            Assert.Contains(result.Operations, operation => operation.Contains("svg-path:", StringComparison.Ordinal));
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
        var outputPath = CreateOutputPath(specPath);

        try
        {
            var orchestrator = new PipelineOrchestrator();
            var request = new CliRunRequest
            {
                SpecPath = specPath,
                OutputPath = outputPath
            };

            var first = orchestrator.Run(request);
            var second = orchestrator.Run(request);

            AssertRunResultsEquivalent(first, second);
            Assert.Equal(File.ReadAllText(first.ExportManifestPath), File.ReadAllText(second.ExportManifestPath));
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
        var firstOutputPath = CreateOutputPath(firstSpecPath);
        var secondOutputPath = CreateOutputPath(secondSpecPath);

        try
        {
            var orchestrator = new PipelineOrchestrator();
            var firstRequest = new CliRunRequest
            {
                SpecPath = firstSpecPath,
                OutputPath = firstOutputPath
            };
            var secondRequest = new CliRunRequest
            {
                SpecPath = secondSpecPath,
                OutputPath = secondOutputPath
            };

            var first = orchestrator.Run(firstRequest);
            var second = orchestrator.Run(secondRequest);

            Assert.True(first.Success);
            Assert.True(second.Success);
            Assert.Equal(first.PlannedFrameCount, second.PlannedFrameCount);
            Assert.Equal(first.RenderedFrameCount, second.RenderedFrameCount);
            Assert.Equal(first.ProjectDurationSeconds, second.ProjectDurationSeconds, 6);
            Assert.Equal(first.ExportFrames.Select(frame => frame.FrameIndex).ToArray(), second.ExportFrames.Select(frame => frame.FrameIndex).ToArray());
            Assert.Equal(first.ExportFrames.Select(frame => frame.ArtifactRelativePath).ToArray(), second.ExportFrames.Select(frame => frame.ArtifactRelativePath).ToArray());
            Assert.Equal(first.ExportFrames.Select(frame => frame.ArtifactDeterministicKey).ToArray(), second.ExportFrames.Select(frame => frame.ArtifactDeterministicKey).ToArray());
            Assert.Equal(first.ExportDeterministicKey, second.ExportDeterministicKey);
            Assert.Equal(first.DeterministicKey, second.DeterministicKey);
            Assert.Equal(File.ReadAllText(first.ExportManifestPath), File.ReadAllText(second.ExportManifestPath));
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
        var firstOutputPath = CreateOutputPath(firstSpecPath);
        var secondOutputPath = CreateOutputPath(secondSpecPath);

        try
        {
            var orchestrator = new PipelineOrchestrator();
            var first = orchestrator.Run(new CliRunRequest
            {
                SpecPath = firstSpecPath,
                OutputPath = firstOutputPath
            });
            var second = orchestrator.Run(new CliRunRequest
            {
                SpecPath = secondSpecPath,
                OutputPath = secondOutputPath
            });

            Assert.True(first.Success);
            Assert.True(second.Success);
            Assert.Equal(150, first.ExportedFrameCount);
            Assert.Equal(first.ExportDeterministicKey, second.ExportDeterministicKey);
            Assert.Equal(first.DeterministicKey, second.DeterministicKey);
            Assert.Equal(File.ReadAllText(first.ExportManifestPath), File.ReadAllText(second.ExportManifestPath));
            Assert.Contains(first.Operations, operation => operation.Contains("svg-path:mode:partial", StringComparison.Ordinal));
        }
        finally
        {
            DeleteSpecFile(firstSpecPath);
            DeleteSpecFile(secondSpecPath);
        }
    }

    [Fact]
    public void PipelineOrchestrator_WithPhase07Fixtures_ProducesEquivalentPackagesAcrossRepeatedRuns()
    {
        var firstSpecPath = CreateSpecFile("phase07-full-timeline", "primary-spec.json");
        var secondSpecPath = CreateSpecFile("phase07-full-timeline", "primary-spec.json");
        var firstOutputPath = CreateOutputPath(firstSpecPath, "first-video.mp4");
        var secondOutputPath = CreateOutputPath(secondSpecPath, "second-video.mp4");

        try
        {
            var orchestrator = new PipelineOrchestrator();
            var first = orchestrator.Run(new CliRunRequest
            {
                SpecPath = firstSpecPath,
                OutputPath = firstOutputPath
            });
            var second = orchestrator.Run(new CliRunRequest
            {
                SpecPath = secondSpecPath,
                OutputPath = secondOutputPath
            });

            Assert.True(first.Success);
            Assert.True(second.Success);
            Assert.Equal(150, first.ExportedFrameCount);
            Assert.Equal(2, first.ExportedAudioCueCount);
            Assert.Equal(5, first.ProjectDurationSeconds, 6);
            AssertRunArtifactPackagesEquivalent(first, second);
        }
        finally
        {
            DeleteSpecFile(firstSpecPath);
            DeleteSpecFile(secondSpecPath);
        }
    }

    [Fact]
    public void PipelineOrchestrator_WithPhase07EquivalentFixtures_ProducesEquivalentArtifactManifestAndFiles()
    {
        var firstSpecPath = CreateSpecFile("phase07-full-timeline", "primary-spec.json");
        var secondSpecPath = CreateSpecFile("phase07-full-timeline", "equivalent-reordered-spec.json");
        var firstOutputPath = CreateOutputPath(firstSpecPath, "primary-video.mp4");
        var secondOutputPath = CreateOutputPath(secondSpecPath, "equivalent-video.mp4");

        try
        {
            var orchestrator = new PipelineOrchestrator();
            var first = orchestrator.Run(new CliRunRequest
            {
                SpecPath = firstSpecPath,
                OutputPath = firstOutputPath
            });
            var second = orchestrator.Run(new CliRunRequest
            {
                SpecPath = secondSpecPath,
                OutputPath = secondOutputPath
            });

            Assert.True(first.Success);
            Assert.True(second.Success);
            Assert.Equal(150, first.ExportSummary.FrameCount);
            Assert.Equal(2, first.ExportSummary.AudioCueCount);
            Assert.Equal(5, first.ExportSummary.TotalDurationSeconds, 6);
            Assert.Equal("frames/frame-000149.svg", first.ExportFrames[^1].ArtifactRelativePath);
            Assert.Equal(new[] { "cue-music", "cue-voice" }, first.ExportAudioCues.Select(cue => cue.CueId).ToArray());
            AssertRunArtifactPackagesEquivalent(first, second);
        }
        finally
        {
            DeleteSpecFile(firstSpecPath);
            DeleteSpecFile(secondSpecPath);
        }
    }

    [Fact]
    public void PipelineOrchestrator_WithPhase07AudioOverhangFixture_CoversFullRenderSequence()
    {
        var specPath = CreateSpecFile("phase07-full-timeline", "audio-overhang-spec.json");
        var outputPath = CreateOutputPath(specPath, "audio-tail-video.mp4");

        try
        {
            var orchestrator = new PipelineOrchestrator();
            var result = orchestrator.Run(new CliRunRequest
            {
                SpecPath = specPath,
                OutputPath = outputPath
            });

            using var manifest = JsonDocument.Parse(File.ReadAllText(result.ExportManifestPath));
            var manifestRoot = manifest.RootElement;
            var manifestFrames = manifestRoot.GetProperty("frames");
            var lastManifestFrame = manifestFrames.EnumerateArray().Last();

            Assert.True(result.Success);
            Assert.Equal(0, result.FirstFrameIndex);
            Assert.Equal(89, result.LastFrameIndex);
            Assert.Equal(90, result.PlannedFrameCount);
            Assert.Equal(90, result.RenderedFrameCount);
            Assert.Equal(90, result.ExportedFrameCount);
            Assert.Equal(1, result.ExportedAudioCueCount);
            Assert.Equal(3, result.ProjectDurationSeconds, 6);
            Assert.Equal(3, result.ExportSummary.TotalDurationSeconds, 6);
            Assert.Equal(90, manifestFrames.GetArrayLength());
            Assert.Equal(3, manifestRoot.GetProperty("totalDurationSeconds").GetDouble(), 6);
            Assert.Equal(89, lastManifestFrame.GetProperty("frameIndex").GetInt32());
            Assert.Equal(89d / 30d, lastManifestFrame.GetProperty("startSeconds").GetDouble(), 6);
            Assert.Equal("frames/frame-000089.svg", lastManifestFrame.GetProperty("relativeArtifactPath").GetString());
            Assert.True(File.Exists(Path.Combine(result.ExportPackageRootPath, result.ExportFrames[^1].ArtifactRelativePath.Replace('/', Path.DirectorySeparatorChar))));
        }
        finally
        {
            DeleteSpecFile(specPath);
        }
    }

    [Fact]
    public void PipelineOrchestrator_WithPhase08Fixtures_ProducesEquivalentPlayableMediaWitnessAcrossRepeatedRuns()
    {
        var firstSpecPath = CreateSpecFile("phase08-playable-media", "primary-spec.json");
        var secondSpecPath = CreateSpecFile("phase08-playable-media", "primary-spec.json");
        var firstOutputPath = CreateOutputPath(firstSpecPath, "first-video.mp4");
        var secondOutputPath = CreateOutputPath(secondSpecPath, "second-video.mp4");
        var fakeExecutablePath = Path.Combine(Path.GetDirectoryName(firstSpecPath)!, "ffmpeg.exe");
        File.WriteAllText(fakeExecutablePath, "fake-ffmpeg");
        var previousEnabled = Environment.GetEnvironmentVariable("WHITEBOARD_ENABLE_PLAYABLE_MEDIA");
        var previousPath = Environment.GetEnvironmentVariable("WHITEBOARD_FFMPEG_PATH");
        Environment.SetEnvironmentVariable("WHITEBOARD_ENABLE_PLAYABLE_MEDIA", "1");
        Environment.SetEnvironmentVariable("WHITEBOARD_FFMPEG_PATH", fakeExecutablePath);

        try
        {
            var orchestrator = new PipelineOrchestrator(exportPipeline: new ExportPipeline(new PlayableMediaEncoder(new DeterministicWitnessProcessRunner())));
            var first = orchestrator.Run(new CliRunRequest
            {
                SpecPath = firstSpecPath,
                OutputPath = firstOutputPath
            });
            var second = orchestrator.Run(new CliRunRequest
            {
                SpecPath = secondSpecPath,
                OutputPath = secondOutputPath
            });

            Assert.True(first.Success);
            Assert.True(second.Success);
            Assert.Equal("encoded", first.PlayableMediaStatus);
            Assert.Equal("muxed", first.PlayableMediaAudioStatus);
            Assert.Equal(2, first.PlayableMediaAudioCueCount);
            AssertRunArtifactPackagesEquivalent(first, second);
            AssertPlayableMediaRunWitnessEquivalent(first, second);
        }
        finally
        {
            Environment.SetEnvironmentVariable("WHITEBOARD_ENABLE_PLAYABLE_MEDIA", previousEnabled);
            Environment.SetEnvironmentVariable("WHITEBOARD_FFMPEG_PATH", previousPath);
            DeleteSpecFile(firstSpecPath);
            DeleteSpecFile(secondSpecPath);
        }
    }

    [Fact]
    public void PipelineOrchestrator_WithPhase08EquivalentFixtures_ProducesEquivalentPlayableMediaWitnessAcrossReorderedInputs()
    {
        var firstSpecPath = CreateSpecFile("phase08-playable-media", "primary-spec.json");
        var secondSpecPath = CreateSpecFile("phase08-playable-media", "equivalent-reordered-spec.json");
        var firstOutputPath = CreateOutputPath(firstSpecPath, "primary-video.mp4");
        var secondOutputPath = CreateOutputPath(secondSpecPath, "equivalent-video.mp4");
        var fakeExecutablePath = Path.Combine(Path.GetDirectoryName(firstSpecPath)!, "ffmpeg.exe");
        File.WriteAllText(fakeExecutablePath, "fake-ffmpeg");
        var previousEnabled = Environment.GetEnvironmentVariable("WHITEBOARD_ENABLE_PLAYABLE_MEDIA");
        var previousPath = Environment.GetEnvironmentVariable("WHITEBOARD_FFMPEG_PATH");
        Environment.SetEnvironmentVariable("WHITEBOARD_ENABLE_PLAYABLE_MEDIA", "1");
        Environment.SetEnvironmentVariable("WHITEBOARD_FFMPEG_PATH", fakeExecutablePath);

        try
        {
            var orchestrator = new PipelineOrchestrator(exportPipeline: new ExportPipeline(new PlayableMediaEncoder(new DeterministicWitnessProcessRunner())));
            var first = orchestrator.Run(new CliRunRequest
            {
                SpecPath = firstSpecPath,
                OutputPath = firstOutputPath
            });
            var second = orchestrator.Run(new CliRunRequest
            {
                SpecPath = secondSpecPath,
                OutputPath = secondOutputPath
            });

            Assert.True(first.Success);
            Assert.True(second.Success);
            Assert.Equal("encoded", first.PlayableMediaStatus);
            Assert.Equal("muxed", first.PlayableMediaAudioStatus);
            Assert.Equal(new[] { "cue-music", "cue-voice" }, first.ExportAudioCues.Select(cue => cue.CueId).ToArray());
            AssertRunArtifactPackagesEquivalent(first, second);
            AssertPlayableMediaRunWitnessEquivalent(first, second);
        }
        finally
        {
            Environment.SetEnvironmentVariable("WHITEBOARD_ENABLE_PLAYABLE_MEDIA", previousEnabled);
            Environment.SetEnvironmentVariable("WHITEBOARD_FFMPEG_PATH", previousPath);
            DeleteSpecFile(firstSpecPath);
            DeleteSpecFile(secondSpecPath);
        }
    }

    [Fact]
    public void PipelineOrchestrator_WithPhase08MissingEncoderTool_FailsWithoutAmbiguousSuccess()
    {
        var specPath = CreateSpecFile("phase08-playable-media", "audio-mux-spec.json");
        var outputPath = CreateOutputPath(specPath, "missing-encoder-video.mp4");
        var missingExecutablePath = Path.Combine(Path.GetDirectoryName(specPath)!, "missing-ffmpeg.exe");
        var previousEnabled = Environment.GetEnvironmentVariable("WHITEBOARD_ENABLE_PLAYABLE_MEDIA");
        var previousPath = Environment.GetEnvironmentVariable("WHITEBOARD_FFMPEG_PATH");
        Environment.SetEnvironmentVariable("WHITEBOARD_ENABLE_PLAYABLE_MEDIA", "1");
        Environment.SetEnvironmentVariable("WHITEBOARD_FFMPEG_PATH", missingExecutablePath);

        try
        {
            var orchestrator = new PipelineOrchestrator();
            var result = orchestrator.Run(new CliRunRequest
            {
                SpecPath = specPath,
                OutputPath = outputPath
            });

            Assert.False(result.Success);
            Assert.Equal("failed", result.PlayableMediaStatus);
            Assert.Equal("failed", result.PlayableMediaAudioStatus);
            Assert.Equal(1, result.PlayableMediaAudioCueCount);
            Assert.Equal("Export packaging or playable media encoding failed after renderer completed.", result.Message);
            Assert.True(File.Exists(result.ExportManifestPath));
            Assert.Equal(48, result.ExportedFrameCount);
            Assert.Equal(1, result.ExportedAudioCueCount);
            Assert.Contains("playable media encoding failed", result.ExportStatus, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable("WHITEBOARD_ENABLE_PLAYABLE_MEDIA", previousEnabled);
            Environment.SetEnvironmentVariable("WHITEBOARD_FFMPEG_PATH", previousPath);
            DeleteSpecFile(specPath);
        }
    }

    [Fact]
    public void PipelineOrchestrator_WithPhase08PlayableMediaFixture_SurfacesMuxedAudioMetadata()
    {
        var specPath = CreateSpecFile("phase08-playable-media", "audio-mux-spec.json");
        var outputPath = CreateOutputPath(specPath, "audio-mux-video.mp4");
        var fakeExecutablePath = Path.Combine(Path.GetDirectoryName(specPath)!, "ffmpeg.exe");
        File.WriteAllText(fakeExecutablePath, "fake-ffmpeg");
        var previousEnabled = Environment.GetEnvironmentVariable("WHITEBOARD_ENABLE_PLAYABLE_MEDIA");
        var previousPath = Environment.GetEnvironmentVariable("WHITEBOARD_FFMPEG_PATH");
        Environment.SetEnvironmentVariable("WHITEBOARD_ENABLE_PLAYABLE_MEDIA", "1");
        Environment.SetEnvironmentVariable("WHITEBOARD_FFMPEG_PATH", fakeExecutablePath);

        try
        {
            var orchestrator = new PipelineOrchestrator(exportPipeline: new ExportPipeline(new PlayableMediaEncoder(new RecordingProcessRunner())));
            var result = orchestrator.Run(new CliRunRequest
            {
                SpecPath = specPath,
                OutputPath = outputPath
            });

            Assert.True(result.Success);
            Assert.Equal("encoded", result.PlayableMediaStatus);
            Assert.Equal("muxed", result.PlayableMediaAudioStatus);
            Assert.Equal(1, result.PlayableMediaAudioCueCount);
            Assert.Equal(outputPath, result.PlayableMediaPath);
            Assert.True(result.PlayableMediaByteCount > 0);
            Assert.Equal("Integrated SVG pipeline executed full render sequence through playable media encoding with audio muxing.", result.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable("WHITEBOARD_ENABLE_PLAYABLE_MEDIA", previousEnabled);
            Environment.SetEnvironmentVariable("WHITEBOARD_FFMPEG_PATH", previousPath);
            DeleteSpecFile(specPath);
        }
    }

    [Fact]
    public void PipelineOrchestrator_WithAudioOverhang_CoversFullRenderSequence()
    {
        var specPath = CreateSpecFileFromJson(
            """
            {
              "meta": {
                "projectId": "cli-phase07-audio-tail",
                "name": "CLI Phase 07 Audio Tail"
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
                  "durationSeconds": 2,
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
                    "durationSeconds": 1.5,
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
                    },
                    {
                      "timeSeconds": 1,
                      "position": {
                        "x": 8,
                        "y": 6
                      },
                      "zoom": 1.1,
                      "interpolation": "linear"
                    }
                  ]
                },
                "audioCues": [
                  {
                    "id": "cue-1",
                    "audioAssetId": "audio-1",
                    "startSeconds": 1,
                    "durationSeconds": 2,
                    "volume": 0.8
                  }
                ]
              }
            }
            """,
            includeAudioAssets: true);
        var outputPath = CreateOutputPath(specPath);

        try
        {
            var orchestrator = new PipelineOrchestrator();
            var result = orchestrator.Run(new CliRunRequest
            {
                SpecPath = specPath,
                OutputPath = outputPath
            });

            Assert.True(result.Success);
            Assert.Null(result.FrameIndex);
            Assert.Equal(0, result.FirstFrameIndex);
            Assert.Equal(89, result.LastFrameIndex);
            Assert.Equal(90, result.PlannedFrameCount);
            Assert.Equal(90, result.RenderedFrameCount);
            Assert.Equal(90, result.ExportedFrameCount);
            Assert.Equal(1, result.ExportedAudioCueCount);
            Assert.Equal(3, result.ProjectDurationSeconds, 6);
            Assert.Equal(3, result.ExportSummary.TotalDurationSeconds, 6);
            Assert.Equal(89d / 30d, result.ExportFrames[^1].StartSeconds, 6);
            Assert.Equal("cue-1", result.ExportAudioCues[0].CueId);
            Assert.True(File.Exists(result.ExportManifestPath));
            Assert.True(File.Exists(Path.Combine(result.ExportPackageRootPath, result.ExportFrames[^1].ArtifactRelativePath.Replace('/', Path.DirectorySeparatorChar))));
        }
        finally
        {
            DeleteSpecFile(specPath);
        }
    }

    [Fact]
    public void PipelineOrchestrator_DebugFrameIndex_RemainsAvailableAsSecondaryPath()
    {
        var specPath = CreateSpecFile("phase03-determinism", "primary-spec.json");
        var outputPath = CreateOutputPath(specPath, "debug-video.mp4");

        try
        {
            var orchestrator = new PipelineOrchestrator();
            var result = orchestrator.Run(new CliRunRequest
            {
                SpecPath = specPath,
                OutputPath = outputPath,
                FrameIndex = 15
            });

            Assert.True(result.Success);
            Assert.Equal(15, result.FrameIndex);
            Assert.Equal(150, result.PlannedFrameCount);
            Assert.Equal(1, result.RenderedFrameCount);
            Assert.Equal(1, result.ExportedFrameCount);
            Assert.Single(result.ExportFrames);
            Assert.Equal(15, result.ExportFrames[0].FrameIndex);
            Assert.Equal("frames/frame-000015.svg", result.ExportFrames[0].ArtifactRelativePath);
            Assert.Equal(0, result.FirstFrameIndex);
            Assert.Equal(149, result.LastFrameIndex);
            Assert.True(File.Exists(result.ExportManifestPath));
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
        var firstOutputPath = CreateOutputPath(firstSpecPath);
        var secondOutputPath = CreateOutputPath(secondSpecPath);

        try
        {
            var orchestrator = new PipelineOrchestrator();
            var first = orchestrator.Run(new CliRunRequest
            {
                SpecPath = firstSpecPath,
                OutputPath = firstOutputPath
            });
            var second = orchestrator.Run(new CliRunRequest
            {
                SpecPath = secondSpecPath,
                OutputPath = secondOutputPath
            });

            Assert.True(first.Success);
            Assert.True(second.Success);
            Assert.Equal(150, first.ExportedFrameCount);
            Assert.Equal(2, first.ExportedAudioCueCount);
            Assert.Equal(150, first.ExportSummary.FrameCount);
            Assert.Equal(2, first.ExportSummary.AudioCueCount);
            Assert.Equal(5, first.ExportSummary.TotalDurationSeconds, 6);
            Assert.Equal(first.ExportDeterministicKey, second.ExportDeterministicKey);
            Assert.Equal(first.DeterministicKey, second.DeterministicKey);
            Assert.Equal(first.ExportFrames.Select(frame => frame.ArtifactRelativePath).ToArray(), second.ExportFrames.Select(frame => frame.ArtifactRelativePath).ToArray());
            Assert.Equal(["cue-voice", "cue-music"], first.ExportAudioCues.Select(cue => cue.CueId).ToArray());
            Assert.Equal(["assets/narration.mp3", "assets/music.mp3"], first.ExportAudioCues.Select(cue => cue.SourcePath).ToArray());
            Assert.Equal(File.ReadAllText(first.ExportManifestPath), File.ReadAllText(second.ExportManifestPath));
        }
        finally
        {
            DeleteSpecFile(firstSpecPath);
            DeleteSpecFile(secondSpecPath);
        }
    }

    [Fact]
    public void BatchPipelineOrchestrator_WithPhase06Fixtures_FailsDeterministicallyWhenPlayableMediaIsNotConfigured()
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

            Assert.False(first.Success);
            Assert.False(second.Success);
            Assert.Equal(2, first.JobCount);
            Assert.Equal(0, first.SuccessCount);
            Assert.Equal(2, first.FailureCount);
            Assert.Equal(new[] { "job-a", "job-b" }, first.Jobs.Select(job => job.JobId).ToArray());
            Assert.All(first.Jobs, job =>
            {
                Assert.Null(job.FrameIndex);
                Assert.Equal(150, job.PlannedFrameCount);
                Assert.Equal(150, job.RenderedFrameCount);
                Assert.Equal("not-configured", job.PlayableMediaStatus);
                Assert.Contains("did not produce a finished playable media artifact", job.Message, StringComparison.Ordinal);
            });
            Assert.Equal(first.DeterministicKey, second.DeterministicKey);
            Assert.Equal(first.Jobs.Select(job => job.JobId).ToArray(), second.Jobs.Select(job => job.JobId).ToArray());
            Assert.Equal(first.Jobs.Select(job => job.DeterministicKey).ToArray(), second.Jobs.Select(job => job.DeterministicKey).ToArray());
            Assert.Equal(first.Jobs.Select(job => job.PlayableMediaStatus).ToArray(), second.Jobs.Select(job => job.PlayableMediaStatus).ToArray());
            Assert.True(File.Exists(firstSummaryPath));
            Assert.True(File.Exists(secondSummaryPath));

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

    [Fact]
    public void BatchPipelineOrchestrator_WithPhase09Fixtures_ProducesEquivalentPlayableMediaSummaryArtifacts()
    {
        var firstManifestPath = CreatePlayableMediaBatchFixtureManifest("primary-manifest.json");
        var secondManifestPath = CreatePlayableMediaBatchFixtureManifest("equivalent-reordered-manifest.json");
        var firstSummaryPath = Path.Combine(Path.GetDirectoryName(firstManifestPath)!, "summary.json");
        var secondSummaryPath = Path.Combine(Path.GetDirectoryName(secondManifestPath)!, "summary.json");
        var firstExecutablePath = Path.Combine(Path.GetDirectoryName(firstManifestPath)!, "ffmpeg.exe");
        var secondExecutablePath = Path.Combine(Path.GetDirectoryName(secondManifestPath)!, "ffmpeg.exe");
        File.WriteAllText(firstExecutablePath, "fake-ffmpeg");
        File.WriteAllText(secondExecutablePath, "fake-ffmpeg");
        var previousEnabled = Environment.GetEnvironmentVariable("WHITEBOARD_ENABLE_PLAYABLE_MEDIA");
        var previousPath = Environment.GetEnvironmentVariable("WHITEBOARD_FFMPEG_PATH");

        try
        {
            Environment.SetEnvironmentVariable("WHITEBOARD_ENABLE_PLAYABLE_MEDIA", "1");
            Environment.SetEnvironmentVariable("WHITEBOARD_FFMPEG_PATH", firstExecutablePath);
            var first = new BatchPipelineOrchestrator(new PipelineOrchestrator(exportPipeline: new ExportPipeline(new PlayableMediaEncoder(new DeterministicWitnessProcessRunner())))).Run(new CliBatchRunRequest
            {
                ManifestPath = firstManifestPath,
                SummaryOutputPath = firstSummaryPath
            });

            Environment.SetEnvironmentVariable("WHITEBOARD_ENABLE_PLAYABLE_MEDIA", "1");
            Environment.SetEnvironmentVariable("WHITEBOARD_FFMPEG_PATH", secondExecutablePath);
            var second = new BatchPipelineOrchestrator(new PipelineOrchestrator(exportPipeline: new ExportPipeline(new PlayableMediaEncoder(new DeterministicWitnessProcessRunner())))).Run(new CliBatchRunRequest
            {
                ManifestPath = secondManifestPath,
                SummaryOutputPath = secondSummaryPath
            });

            Assert.True(first.Success);
            Assert.True(second.Success);
            Assert.Equal(2, first.JobCount);
            Assert.All(first.Jobs, job =>
            {
                Assert.Equal("encoded", job.PlayableMediaStatus);
                Assert.True(job.PlayableMediaByteCount > 0);
                Assert.True(File.Exists(Path.Combine(Path.GetDirectoryName(firstManifestPath)!, job.PlayableMediaPath.Replace('/', Path.DirectorySeparatorChar))));
                Assert.True(File.Exists(Path.Combine(Path.GetDirectoryName(firstManifestPath)!, job.ExportManifestPath.Replace('/', Path.DirectorySeparatorChar))));
            });
            AssertBatchPlayableMediaResultsEquivalent(first, firstManifestPath, second, secondManifestPath);
            Assert.Equal(File.ReadAllText(firstSummaryPath), File.ReadAllText(secondSummaryPath));
        }
        finally
        {
            Environment.SetEnvironmentVariable("WHITEBOARD_ENABLE_PLAYABLE_MEDIA", previousEnabled);
            Environment.SetEnvironmentVariable("WHITEBOARD_FFMPEG_PATH", previousPath);
            DeleteSpecFile(firstManifestPath);
            DeleteSpecFile(secondManifestPath);
        }
    }

    [Fact]
    public void BatchPipelineOrchestrator_WithPhase09Fixtures_FailsWhenPlayableMediaIsNotConfigured()
    {
        var manifestPath = CreatePlayableMediaBatchFixtureManifest("primary-manifest.json");
        var summaryPath = Path.Combine(Path.GetDirectoryName(manifestPath)!, "summary.json");
        var previousEnabled = Environment.GetEnvironmentVariable("WHITEBOARD_ENABLE_PLAYABLE_MEDIA");
        var previousPath = Environment.GetEnvironmentVariable("WHITEBOARD_FFMPEG_PATH");

        try
        {
            Environment.SetEnvironmentVariable("WHITEBOARD_ENABLE_PLAYABLE_MEDIA", null);
            Environment.SetEnvironmentVariable("WHITEBOARD_FFMPEG_PATH", null);
            var result = new BatchPipelineOrchestrator().Run(new CliBatchRunRequest
            {
                ManifestPath = manifestPath,
                SummaryOutputPath = summaryPath
            });

            Assert.False(result.Success);
            Assert.Equal(2, result.JobCount);
            Assert.Equal(0, result.SuccessCount);
            Assert.Equal(2, result.FailureCount);
            Assert.All(result.Jobs, job =>
            {
                Assert.False(job.Success);
                Assert.Equal("not-configured", job.PlayableMediaStatus);
                Assert.Contains("did not produce a finished playable media artifact", job.Message, StringComparison.Ordinal);
                Assert.True(File.Exists(Path.Combine(Path.GetDirectoryName(manifestPath)!, job.ExportManifestPath.Replace('/', Path.DirectorySeparatorChar))));
            });
            Assert.True(File.Exists(summaryPath));
        }
        finally
        {
            Environment.SetEnvironmentVariable("WHITEBOARD_ENABLE_PLAYABLE_MEDIA", previousEnabled);
            Environment.SetEnvironmentVariable("WHITEBOARD_FFMPEG_PATH", previousPath);
            DeleteSpecFile(manifestPath);
        }
    }

    [Fact]
    public void PipelineOrchestrator_WithPhase08Fixture_EmitsHandGuidanceOverlayInFrameArtifacts()
    {
        var specPath = CreateSpecFile("phase08-playable-media", "primary-spec.json");
        var outputPath = CreateOutputPath(specPath, "guided-video.mp4");

        try
        {
            var orchestrator = new PipelineOrchestrator();
            var result = orchestrator.Run(new CliRunRequest
            {
                SpecPath = specPath,
                OutputPath = outputPath
            });

            Assert.True(result.Success);
            var firstArtifactPath = Path.Combine(result.ExportPackageRootPath, result.ExportFrames[0].ArtifactRelativePath.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(firstArtifactPath));

            var markup = File.ReadAllText(firstArtifactPath);
            Assert.Contains("stroke-dasharray=\"", markup, StringComparison.Ordinal);
            Assert.Contains("data-guidance=\"hand\"", markup, StringComparison.Ordinal);
            Assert.Contains("data-guidance-object=\"object-1\"", markup, StringComparison.Ordinal);
        }
        finally
        {
            DeleteSpecFile(specPath);
        }
    }

    [Fact]
    public void PipelineOrchestrator_WithPhase11Fixture_EmitsHandAssetGuidanceAndRenderedTextInFrameArtifacts()
    {
        var specPath = CreateSpecFile("phase11-hand-assets", "primary-spec.json");
        var outputPath = CreateOutputPath(specPath, "phase11-video.mp4");

        try
        {
            var orchestrator = new PipelineOrchestrator();
            var result = orchestrator.Run(new CliRunRequest
            {
                SpecPath = specPath,
                OutputPath = outputPath
            });

            Assert.True(result.Success);
            var firstArtifactPath = Path.Combine(result.ExportPackageRootPath, result.ExportFrames[0].ArtifactRelativePath.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(firstArtifactPath));

            var markup = File.ReadAllText(firstArtifactPath);
            Assert.Contains("data-guidance=\"hand\"", markup, StringComparison.Ordinal);
            Assert.Contains("data-guidance-renderer=\"asset\"", markup, StringComparison.Ordinal);
            Assert.Contains("data-guidance-asset=\"hand-1\"", markup, StringComparison.Ordinal);
            Assert.Contains("data-object=\"text-1\"", markup, StringComparison.Ordinal);
            Assert.Contains("Think bigger", markup, StringComparison.Ordinal);
            Assert.DoesNotContain(result.Operations, operation => string.Equals(operation, "unsupported-object:object:text-1:type:text", StringComparison.Ordinal));
        }
        finally
        {
            DeleteSpecFile(specPath);
        }
    }

    [Fact]
    public void PipelineOrchestrator_WithPhase11Fixtures_ProducesEquivalentPlayableMediaWitnessAcrossRepeatedRuns()
    {
        var firstSpecPath = CreateSpecFile("phase11-hand-assets", "primary-spec.json");
        var secondSpecPath = CreateSpecFile("phase11-hand-assets", "primary-spec.json");
        var firstOutputPath = CreateOutputPath(firstSpecPath, "first-video.mp4");
        var secondOutputPath = CreateOutputPath(secondSpecPath, "second-video.mp4");
        var fakeExecutablePath = Path.Combine(Path.GetDirectoryName(firstSpecPath)!, "ffmpeg.exe");
        File.WriteAllText(fakeExecutablePath, "fake-ffmpeg");
        var previousEnabled = Environment.GetEnvironmentVariable("WHITEBOARD_ENABLE_PLAYABLE_MEDIA");
        var previousPath = Environment.GetEnvironmentVariable("WHITEBOARD_FFMPEG_PATH");
        Environment.SetEnvironmentVariable("WHITEBOARD_ENABLE_PLAYABLE_MEDIA", "1");
        Environment.SetEnvironmentVariable("WHITEBOARD_FFMPEG_PATH", fakeExecutablePath);

        try
        {
            var orchestrator = new PipelineOrchestrator(exportPipeline: new ExportPipeline(new PlayableMediaEncoder(new DeterministicWitnessProcessRunner())));
            var first = orchestrator.Run(new CliRunRequest
            {
                SpecPath = firstSpecPath,
                OutputPath = firstOutputPath
            });
            var second = orchestrator.Run(new CliRunRequest
            {
                SpecPath = secondSpecPath,
                OutputPath = secondOutputPath
            });

            Assert.True(first.Success);
            Assert.True(second.Success);
            Assert.Equal("encoded", first.PlayableMediaStatus);
            Assert.Equal("muxed", first.PlayableMediaAudioStatus);
            AssertRunArtifactPackagesEquivalent(first, second);
            AssertPlayableMediaRunWitnessEquivalent(first, second);
        }
        finally
        {
            Environment.SetEnvironmentVariable("WHITEBOARD_ENABLE_PLAYABLE_MEDIA", previousEnabled);
            Environment.SetEnvironmentVariable("WHITEBOARD_FFMPEG_PATH", previousPath);
            DeleteSpecFile(firstSpecPath);
            DeleteSpecFile(secondSpecPath);
        }
    }

    [Fact]
    public void PipelineOrchestrator_WithPhase11EquivalentFixtures_ProducesEquivalentPlayableMediaWitnessAcrossReorderedInputs()
    {
        var firstSpecPath = CreateSpecFile("phase11-hand-assets", "primary-spec.json");
        var secondSpecPath = CreateSpecFile("phase11-hand-assets", "equivalent-reordered-spec.json");
        var firstOutputPath = CreateOutputPath(firstSpecPath, "primary-video.mp4");
        var secondOutputPath = CreateOutputPath(secondSpecPath, "equivalent-video.mp4");
        var fakeExecutablePath = Path.Combine(Path.GetDirectoryName(firstSpecPath)!, "ffmpeg.exe");
        File.WriteAllText(fakeExecutablePath, "fake-ffmpeg");
        var previousEnabled = Environment.GetEnvironmentVariable("WHITEBOARD_ENABLE_PLAYABLE_MEDIA");
        var previousPath = Environment.GetEnvironmentVariable("WHITEBOARD_FFMPEG_PATH");
        Environment.SetEnvironmentVariable("WHITEBOARD_ENABLE_PLAYABLE_MEDIA", "1");
        Environment.SetEnvironmentVariable("WHITEBOARD_FFMPEG_PATH", fakeExecutablePath);

        try
        {
            var orchestrator = new PipelineOrchestrator(exportPipeline: new ExportPipeline(new PlayableMediaEncoder(new DeterministicWitnessProcessRunner())));
            var first = orchestrator.Run(new CliRunRequest
            {
                SpecPath = firstSpecPath,
                OutputPath = firstOutputPath
            });
            var second = orchestrator.Run(new CliRunRequest
            {
                SpecPath = secondSpecPath,
                OutputPath = secondOutputPath
            });

            Assert.True(first.Success);
            Assert.True(second.Success);
            AssertRunArtifactPackagesEquivalent(first, second);
            AssertPlayableMediaRunWitnessEquivalent(first, second);
        }
        finally
        {
            Environment.SetEnvironmentVariable("WHITEBOARD_ENABLE_PLAYABLE_MEDIA", previousEnabled);
            Environment.SetEnvironmentVariable("WHITEBOARD_FFMPEG_PATH", previousPath);
            DeleteSpecFile(firstSpecPath);
            DeleteSpecFile(secondSpecPath);
        }
    }

    [Fact]
    public void PipelineOrchestrator_WithPhase12AuthoredWitnessSpec_HandsResolvedAssetsToRendererWithoutFallbackImages()
    {
        var specPath = ResolveRepoRelativePath("artifacts", "source-parity-demo", "project-engine.json");
        var outputDirectory = Path.Combine(Path.GetTempPath(), "whiteboard-cli-phase12-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "phase12-video.mp4");
        var recordingRenderer = new RecordingFrameRenderer();
        var exportPipeline = new RecordingExportPipeline();

        try
        {
            var orchestrator = new PipelineOrchestrator(
                frameRenderer: recordingRenderer,
                exportPipeline: exportPipeline);
            var result = orchestrator.Run(new CliRunRequest
            {
                SpecPath = specPath,
                OutputPath = outputPath
            });

            Assert.True(result.Success);
            Assert.NotNull(exportPipeline.LastRequest);
            Assert.NotEmpty(recordingRenderer.Requests);
            Assert.Equal(result.RenderedFrameCount, recordingRenderer.Requests.Count);
            Assert.Equal(recordingRenderer.Requests.Count, exportPipeline.LastRequest!.Frames.Count);

            var firstRequest = recordingRenderer.Requests[0];
            Assert.Equal(new[]
            {
                "svg-arrow",
                "svg-body",
                "svg-clock-group",
                "svg-footer",
                "svg-left",
                "svg-title"
            }, firstRequest.SvgAssets.Keys.OrderBy(key => key, StringComparer.Ordinal).ToArray());
            Assert.Equal(new[] { "hand-1" }, firstRequest.HandAssets.Keys.OrderBy(key => key, StringComparer.Ordinal).ToArray());
            Assert.Empty(firstRequest.ImageAssets);
            Assert.All(firstRequest.SvgAssets.Values, asset => Assert.True(Path.IsPathRooted(asset.SourcePath)));
            Assert.All(firstRequest.HandAssets.Values, asset => Assert.True(Path.IsPathRooted(asset.SourcePath)));
            Assert.Equal(
                Path.GetFullPath(Path.Combine(Path.GetDirectoryName(specPath)!, "engine-assets", "arrow-main.svg")),
                firstRequest.SvgAssets["svg-arrow"].SourcePath);
            Assert.Equal(
                Path.GetFullPath(Path.Combine(Path.GetDirectoryName(specPath)!, "assets", "hand.svg")),
                firstRequest.HandAssets["hand-1"].SourcePath);
            Assert.All(
                recordingRenderer.Requests,
                request =>
                {
                    Assert.Equal(firstRequest.SvgAssets.Keys.OrderBy(key => key, StringComparer.Ordinal).ToArray(), request.SvgAssets.Keys.OrderBy(key => key, StringComparer.Ordinal).ToArray());
                    Assert.Equal(firstRequest.HandAssets.Keys.OrderBy(key => key, StringComparer.Ordinal).ToArray(), request.HandAssets.Keys.OrderBy(key => key, StringComparer.Ordinal).ToArray());
                    Assert.Empty(request.ImageAssets);
                });
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void PipelineOrchestrator_WithPhase12AuthoredWitnessSpec_ResolvesParityMotionTransformsForRenderer()
    {
        var specPath = ResolveRepoRelativePath("artifacts", "source-parity-demo", "project-engine.json");
        var outputDirectory = Path.Combine(Path.GetTempPath(), "whiteboard-cli-phase13-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "phase13-video.mp4");
        var recordingRenderer = new RecordingFrameRenderer();
        var exportPipeline = new RecordingExportPipeline();

        try
        {
            var orchestrator = new PipelineOrchestrator(
                frameRenderer: recordingRenderer,
                exportPipeline: exportPipeline);
            var result = orchestrator.Run(new CliRunRequest
            {
                SpecPath = specPath,
                OutputPath = outputPath
            });

            Assert.True(result.Success);
            Assert.Equal(264, recordingRenderer.Requests.Count);

            var firstRequest = recordingRenderer.Requests.First(request => request.FrameState.FrameContext.FrameIndex == 0);
            var lastRequest = recordingRenderer.Requests.Last();

            var firstLeft = firstRequest.FrameState.Scenes.SelectMany(scene => scene.Objects).Single(obj => obj.SceneObjectId == "object-left");
            var lastLeft = lastRequest.FrameState.Scenes.SelectMany(scene => scene.Objects).Single(obj => obj.SceneObjectId == "object-left");
            Assert.NotEqual(firstLeft.Transform.Position, lastLeft.Transform.Position);
            Assert.NotEqual(firstLeft.Transform.Size, lastLeft.Transform.Size);
            Assert.NotEqual(firstLeft.Transform.ScaleX, lastLeft.Transform.ScaleX);
            Assert.NotEqual(firstLeft.Transform.ScaleY, lastLeft.Transform.ScaleY);
            Assert.NotEqual(firstLeft.Transform.Opacity, lastLeft.Transform.Opacity);
            Assert.Equal(new Position2D(18, 120), lastLeft.Transform.Position);
            Assert.Equal(new Size2D(560, 520), lastLeft.Transform.Size);
            Assert.Equal(1, lastLeft.Transform.ScaleX, 3);
            Assert.Equal(1, lastLeft.Transform.ScaleY, 3);
            Assert.Equal(0, lastLeft.Transform.RotationDegrees, 3);
            Assert.Equal(1, lastLeft.Transform.Opacity, 3);

            var firstArrow = firstRequest.FrameState.Scenes.SelectMany(scene => scene.Objects).Single(obj => obj.SceneObjectId == "object-arrow");
            var lastArrow = lastRequest.FrameState.Scenes.SelectMany(scene => scene.Objects).Single(obj => obj.SceneObjectId == "object-arrow");
            Assert.Equal(new Position2D(500, 70), firstArrow.Transform.Position);
            Assert.Equal(new Size2D(284, 138), firstArrow.Transform.Size);
            Assert.Equal(-7, firstArrow.Transform.RotationDegrees, 3);
            Assert.Equal(0.92, firstArrow.Transform.ScaleX, 3);
            Assert.Equal(0.92, firstArrow.Transform.ScaleY, 3);
            Assert.Equal(0.88, firstArrow.Transform.Opacity, 3);
            Assert.Equal(new Position2D(520, 56), lastArrow.Transform.Position);
            Assert.Equal(new Size2D(308, 150), lastArrow.Transform.Size);
            Assert.Equal(0, lastArrow.Transform.RotationDegrees, 3);
            Assert.Equal(1, lastArrow.Transform.ScaleX, 3);
            Assert.Equal(1, lastArrow.Transform.ScaleY, 3);
            Assert.Equal(1, lastArrow.Transform.Opacity, 3);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }
    [Fact]
    public void PipelineOrchestrator_WithPhase12AuthoredWitnessSpec_TransitionsHandGuidanceAcrossObjectsInAuthoredOrder()
    {
        var specPath = ResolveRepoRelativePath("artifacts", "source-parity-demo", "project-engine.json");
        var outputDirectory = Path.Combine(Path.GetTempPath(), "whiteboard-cli-phase13-hand-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "phase13-hand-video.mp4");

        try
        {
            var orchestrator = new PipelineOrchestrator();
            var result = orchestrator.Run(new CliRunRequest
            {
                SpecPath = specPath,
                OutputPath = outputPath
            });

            Assert.True(result.Success);
            Assert.Equal(264, result.ExportedFrameCount);

            var guidanceSequence = result.ExportFrames
                .Select(frame => Path.Combine(result.ExportPackageRootPath, frame.ArtifactRelativePath.Replace('/', Path.DirectorySeparatorChar)))
                .Select(ReadGuidanceObjectId)
                .Where(objectId => !string.IsNullOrWhiteSpace(objectId))
                .Distinct()
                .ToArray();

            Assert.Equal(
                new[]
                {
                    "object-left",
                    "object-arrow",
                    "object-title",
                    "object-clock-group",
                    "object-body",
                    "object-footer"
                },
                guidanceSequence);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }
    [Fact]
    public void PipelineOrchestrator_WithPhase12AuthoredWitnessSpec_EmitsRepresentativeMotionHandWitnessFrames()
    {
        var specPath = ResolveRepoRelativePath("artifacts", "source-parity-demo", "project-engine.json");
        var outputDirectory = Path.Combine(Path.GetTempPath(), "whiteboard-cli-phase13-representative-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "phase13-representative-video.mp4");

        try
        {
            var orchestrator = new PipelineOrchestrator();
            var result = orchestrator.Run(new CliRunRequest
            {
                SpecPath = specPath,
                OutputPath = outputPath
            });

            Assert.True(result.Success);
            Assert.Equal(264, result.ExportedFrameCount);

            var expectedFrames = new Dictionary<int, string>
            {
                [27] = "object-left",
                [72] = "object-arrow",
                [93] = "object-title",
                [130] = "object-clock-group",
                [185] = "object-body",
                [214] = "object-footer"
            };

            foreach (var expectedFrame in expectedFrames)
            {
                var frame = Assert.Single(result.ExportFrames, entry => entry.FrameIndex == expectedFrame.Key);
                var artifactPath = Path.Combine(result.ExportPackageRootPath, frame.ArtifactRelativePath.Replace('/', Path.DirectorySeparatorChar));
                Assert.True(File.Exists(artifactPath));
                Assert.Equal(expectedFrame.Value, ReadGuidanceObjectId(artifactPath));
            }
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void PipelineOrchestrator_WithPhase12AuthoredWitnessSpec_ProducesEquivalentArtifactsAcrossRepeatedRuns()
    {
        var specPath = ResolveRepoRelativePath("artifacts", "source-parity-demo", "project-engine.json");
        var firstOutputDirectory = Path.Combine(Path.GetTempPath(), "whiteboard-cli-phase12-tests", Guid.NewGuid().ToString("N"));
        var secondOutputDirectory = Path.Combine(Path.GetTempPath(), "whiteboard-cli-phase12-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(firstOutputDirectory);
        Directory.CreateDirectory(secondOutputDirectory);
        var firstOutputPath = Path.Combine(firstOutputDirectory, "phase12-first.mp4");
        var secondOutputPath = Path.Combine(secondOutputDirectory, "phase12-second.mp4");

        try
        {
            var orchestrator = new PipelineOrchestrator();
            var first = orchestrator.Run(new CliRunRequest
            {
                SpecPath = specPath,
                OutputPath = firstOutputPath
            });
            var second = orchestrator.Run(new CliRunRequest
            {
                SpecPath = specPath,
                OutputPath = secondOutputPath
            });

            Assert.True(first.Success);
            Assert.True(second.Success);
            Assert.Equal("source-parity-authored-witness", first.ExportSummary.ProjectId);
            Assert.Equal(first.ExportSummary.ProjectId, second.ExportSummary.ProjectId);
            Assert.Equal(264, first.ExportedFrameCount);
            Assert.Equal(first.ExportedFrameCount, second.ExportedFrameCount);
            Assert.Empty(first.ExportAudioCues);
            Assert.Empty(second.ExportAudioCues);
            AssertRunArtifactPackagesEquivalent(first, second);
        }
        finally
        {
            if (Directory.Exists(firstOutputDirectory))
            {
                Directory.Delete(firstOutputDirectory, recursive: true);
            }

            if (Directory.Exists(secondOutputDirectory))
            {
                Directory.Delete(secondOutputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void PipelineOrchestrator_WithPhase12RepoSpecs_KeepsShortcutFixturesMarkedLegacy()
    {
        var loader = new ProjectSpecLoader();
        var authored = loader.Load(ResolveRepoRelativePath("artifacts", "source-parity-demo", "project-engine.json"));
        var segmentedLegacy = loader.Load(ResolveRepoRelativePath("artifacts", "source-parity-demo", "project-image-hand.json"));
        var cropLegacy = loader.Load(ResolveRepoRelativePath("artifacts", "source-parity-demo", "project.json"));

        Assert.Equal("source-parity-authored-witness", authored.Meta.ProjectId);
        Assert.DoesNotContain("legacy", authored.Meta.ProjectId, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("legacy", authored.Meta.Name, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("legacy", segmentedLegacy.Meta.ProjectId, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("comparison", segmentedLegacy.Meta.ProjectId, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("legacy", segmentedLegacy.Meta.Name, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("comparison", segmentedLegacy.Meta.Name, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("legacy", cropLegacy.Meta.ProjectId, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("comparison", cropLegacy.Meta.ProjectId, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("legacy", cropLegacy.Meta.Name, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("comparison", cropLegacy.Meta.Name, StringComparison.OrdinalIgnoreCase);
    }
    private static string? ReadGuidanceObjectId(string artifactPath)
    {
        var markup = File.ReadAllText(artifactPath);
        const string token = "data-guidance-object=\"";
        var start = markup.IndexOf(token, StringComparison.Ordinal);
        if (start < 0)
        {
            return null;
        }

        start += token.Length;
        var end = markup.IndexOf('"', start);
        return end < 0 ? null : markup[start..end];
    }

    private static void AssertRunResultsEquivalent(CliRunResult expected, CliRunResult actual)
    {
        Assert.Equal(expected.Success, actual.Success);
        Assert.Equal(expected.Message, actual.Message);
        Assert.Equal(expected.SpecPath, actual.SpecPath);
        Assert.Equal(expected.FrameIndex, actual.FrameIndex);
        Assert.Equal(expected.FirstFrameIndex, actual.FirstFrameIndex);
        Assert.Equal(expected.LastFrameIndex, actual.LastFrameIndex);
        Assert.Equal(expected.PlannedFrameCount, actual.PlannedFrameCount);
        Assert.Equal(expected.RenderedFrameCount, actual.RenderedFrameCount);
        Assert.Equal(expected.ProjectDurationSeconds, actual.ProjectDurationSeconds, 6);
        Assert.Equal(expected.SceneCount, actual.SceneCount);
        Assert.Equal(expected.ObjectCount, actual.ObjectCount);
        Assert.Equal(expected.OperationCount, actual.OperationCount);
        Assert.Equal(expected.ExportedFrameCount, actual.ExportedFrameCount);
        Assert.Equal(expected.ExportedAudioCueCount, actual.ExportedAudioCueCount);
        Assert.Equal(expected.OutputPath, actual.OutputPath);
        Assert.Equal(expected.ExportStatus, actual.ExportStatus);
        Assert.Equal(expected.ExportDeterministicKey, actual.ExportDeterministicKey);
        Assert.Equal(expected.DeterministicKey, actual.DeterministicKey);
        Assert.Equal(expected.Operations, actual.Operations);
        Assert.Equal(expected.ExportFrames.Select(frame => frame.FrameIndex).ToArray(), actual.ExportFrames.Select(frame => frame.FrameIndex).ToArray());
        Assert.Equal(expected.ExportFrames.Select(frame => frame.ArtifactRelativePath).ToArray(), actual.ExportFrames.Select(frame => frame.ArtifactRelativePath).ToArray());
        Assert.Equal(expected.ExportFrames.Select(frame => frame.ArtifactDeterministicKey).ToArray(), actual.ExportFrames.Select(frame => frame.ArtifactDeterministicKey).ToArray());
        Assert.Equal(expected.ExportAudioCues.Select(cue => cue.CueId).ToArray(), actual.ExportAudioCues.Select(cue => cue.CueId).ToArray());
    }

    private static void AssertRunArtifactPackagesEquivalent(CliRunResult expected, CliRunResult actual)
    {
        Assert.Equal(expected.Success, actual.Success);
        Assert.Equal(expected.Message, actual.Message);
        Assert.Equal(expected.FrameIndex, actual.FrameIndex);
        Assert.Equal(expected.FirstFrameIndex, actual.FirstFrameIndex);
        Assert.Equal(expected.LastFrameIndex, actual.LastFrameIndex);
        Assert.Equal(expected.PlannedFrameCount, actual.PlannedFrameCount);
        Assert.Equal(expected.RenderedFrameCount, actual.RenderedFrameCount);
        Assert.Equal(expected.ProjectDurationSeconds, actual.ProjectDurationSeconds, 6);
        Assert.Equal(expected.SceneCount, actual.SceneCount);
        Assert.Equal(expected.ObjectCount, actual.ObjectCount);
        Assert.Equal(expected.OperationCount, actual.OperationCount);
        Assert.Equal(expected.ExportedFrameCount, actual.ExportedFrameCount);
        Assert.Equal(expected.ExportedAudioCueCount, actual.ExportedAudioCueCount);
        Assert.Equal(expected.ExportStatus, actual.ExportStatus);
        Assert.Equal(expected.ExportDeterministicKey, actual.ExportDeterministicKey);
        Assert.Equal(expected.DeterministicKey, actual.DeterministicKey);
        Assert.Equal(expected.Operations, actual.Operations);
        Assert.Equal(expected.ExportSummary.ProjectId, actual.ExportSummary.ProjectId);
        Assert.Equal(expected.ExportSummary.Format, actual.ExportSummary.Format);
        Assert.Equal(expected.ExportSummary.Width, actual.ExportSummary.Width);
        Assert.Equal(expected.ExportSummary.Height, actual.ExportSummary.Height);
        Assert.Equal(expected.ExportSummary.FrameRate, actual.ExportSummary.FrameRate, 6);
        Assert.Equal(expected.ExportSummary.FrameCount, actual.ExportSummary.FrameCount);
        Assert.Equal(expected.ExportSummary.AudioCueCount, actual.ExportSummary.AudioCueCount);
        Assert.Equal(expected.ExportSummary.TotalOperations, actual.ExportSummary.TotalOperations);
        Assert.Equal(expected.ExportSummary.TotalDurationSeconds, actual.ExportSummary.TotalDurationSeconds, 6);
        Assert.Equal(expected.ExportFrames.Select(frame => frame.FrameIndex).ToArray(), actual.ExportFrames.Select(frame => frame.FrameIndex).ToArray());
        Assert.Equal(expected.ExportFrames.Select(frame => frame.ArtifactRelativePath).ToArray(), actual.ExportFrames.Select(frame => frame.ArtifactRelativePath).ToArray());
        Assert.Equal(expected.ExportFrames.Select(frame => frame.ArtifactDeterministicKey).ToArray(), actual.ExportFrames.Select(frame => frame.ArtifactDeterministicKey).ToArray());
        Assert.Equal(expected.ExportAudioCues.Select(cue => cue.CueId).ToArray(), actual.ExportAudioCues.Select(cue => cue.CueId).ToArray());
        Assert.Equal(File.ReadAllText(expected.ExportManifestPath), File.ReadAllText(actual.ExportManifestPath));

        for (var index = 0; index < expected.ExportFrames.Count; index++)
        {
            var expectedFrame = expected.ExportFrames[index];
            var actualFrame = actual.ExportFrames[index];
            var expectedArtifactPath = Path.Combine(expected.ExportPackageRootPath, expectedFrame.ArtifactRelativePath.Replace('/', Path.DirectorySeparatorChar));
            var actualArtifactPath = Path.Combine(actual.ExportPackageRootPath, actualFrame.ArtifactRelativePath.Replace('/', Path.DirectorySeparatorChar));

            Assert.True(File.Exists(expectedArtifactPath));
            Assert.True(File.Exists(actualArtifactPath));
            Assert.Equal(File.ReadAllBytes(expectedArtifactPath), File.ReadAllBytes(actualArtifactPath));
        }
    }

    private static void AssertPlayableMediaRunWitnessEquivalent(CliRunResult expected, CliRunResult actual)
    {
        Assert.Equal(expected.PlayableMediaStatus, actual.PlayableMediaStatus);
        Assert.Equal(expected.PlayableMediaAudioStatus, actual.PlayableMediaAudioStatus);
        Assert.Equal(expected.PlayableMediaAudioCueCount, actual.PlayableMediaAudioCueCount);
        Assert.Equal(expected.PlayableMediaByteCount, actual.PlayableMediaByteCount);
        Assert.Equal(expected.PlayableMediaDeterministicKey, actual.PlayableMediaDeterministicKey);
        Assert.True(File.Exists(expected.PlayableMediaPath));
        Assert.True(File.Exists(actual.PlayableMediaPath));
        Assert.Equal(File.ReadAllBytes(expected.PlayableMediaPath), File.ReadAllBytes(actual.PlayableMediaPath));
    }

    private static void AssertBatchRunResultsEquivalent(CliBatchRunResult expected, CliBatchRunResult actual)
    {
        Assert.Equal(expected.JobCount, actual.JobCount);
        Assert.Equal(expected.SuccessCount, actual.SuccessCount);
        Assert.Equal(expected.FailureCount, actual.FailureCount);
        Assert.Equal(expected.Success, actual.Success);
        Assert.Equal(expected.DeterministicKey, actual.DeterministicKey);
        Assert.Equal(expected.Jobs.Select(job => job.JobId).ToArray(), actual.Jobs.Select(job => job.JobId).ToArray());

        for (var index = 0; index < expected.Jobs.Count; index++)
        {
            var expectedJob = expected.Jobs[index];
            var actualJob = actual.Jobs[index];
            Assert.Equal(expectedJob.JobId, actualJob.JobId);
            Assert.Equal(expectedJob.SpecPath, actualJob.SpecPath);
            Assert.Equal(expectedJob.OutputPath, actualJob.OutputPath);
            Assert.Equal(expectedJob.FrameIndex, actualJob.FrameIndex);
            Assert.Equal(expectedJob.Success, actualJob.Success);
            Assert.Equal(expectedJob.ExportStatus, actualJob.ExportStatus);
            Assert.Equal(expectedJob.ExportPackageRootPath, actualJob.ExportPackageRootPath);
            Assert.Equal(expectedJob.ExportManifestPath, actualJob.ExportManifestPath);
            Assert.Equal(expectedJob.PlayableMediaPath, actualJob.PlayableMediaPath);
            Assert.Equal(expectedJob.PlayableMediaStatus, actualJob.PlayableMediaStatus);
            Assert.Equal(expectedJob.PlayableMediaDeterministicKey, actualJob.PlayableMediaDeterministicKey);
            Assert.Equal(expectedJob.PlayableMediaByteCount, actualJob.PlayableMediaByteCount);
            Assert.Equal(expectedJob.PlayableMediaAudioStatus, actualJob.PlayableMediaAudioStatus);
            Assert.Equal(expectedJob.PlayableMediaAudioCueCount, actualJob.PlayableMediaAudioCueCount);
            Assert.Equal(expectedJob.DeterministicKey, actualJob.DeterministicKey);
            Assert.Equal(expectedJob.ExportDeterministicKey, actualJob.ExportDeterministicKey);
        }
    }

    private static void AssertBatchPlayableMediaResultsEquivalent(
        CliBatchRunResult expected,
        string expectedManifestPath,
        CliBatchRunResult actual,
        string actualManifestPath)
    {
        Assert.Equal(expected.JobCount, actual.JobCount);
        Assert.Equal(expected.SuccessCount, actual.SuccessCount);
        Assert.Equal(expected.FailureCount, actual.FailureCount);
        Assert.Equal(expected.Success, actual.Success);
        Assert.Equal(expected.DeterministicKey, actual.DeterministicKey);
        Assert.Equal(expected.Jobs.Select(job => job.JobId).ToArray(), actual.Jobs.Select(job => job.JobId).ToArray());

        var expectedRootDirectory = Path.GetDirectoryName(expectedManifestPath)!;
        var actualRootDirectory = Path.GetDirectoryName(actualManifestPath)!;
        for (var index = 0; index < expected.Jobs.Count; index++)
        {
            var expectedJob = expected.Jobs[index];
            var actualJob = actual.Jobs[index];
            Assert.Equal(expectedJob.JobId, actualJob.JobId);
            Assert.Equal(expectedJob.SpecPath, actualJob.SpecPath);
            Assert.Equal(expectedJob.OutputPath, actualJob.OutputPath);
            Assert.Equal(expectedJob.FrameIndex, actualJob.FrameIndex);
            Assert.Equal(expectedJob.Success, actualJob.Success);
            Assert.Equal(expectedJob.ExportStatus, actualJob.ExportStatus);
            Assert.Equal(expectedJob.ExportPackageRootPath, actualJob.ExportPackageRootPath);
            Assert.Equal(expectedJob.ExportManifestPath, actualJob.ExportManifestPath);
            Assert.Equal(expectedJob.PlayableMediaPath, actualJob.PlayableMediaPath);
            Assert.Equal(expectedJob.PlayableMediaStatus, actualJob.PlayableMediaStatus);
            Assert.Equal(expectedJob.PlayableMediaDeterministicKey, actualJob.PlayableMediaDeterministicKey);
            Assert.Equal(expectedJob.PlayableMediaByteCount, actualJob.PlayableMediaByteCount);
            Assert.Equal(expectedJob.PlayableMediaAudioStatus, actualJob.PlayableMediaAudioStatus);
            Assert.Equal(expectedJob.PlayableMediaAudioCueCount, actualJob.PlayableMediaAudioCueCount);
            Assert.Equal(expectedJob.DeterministicKey, actualJob.DeterministicKey);
            Assert.Equal(expectedJob.ExportDeterministicKey, actualJob.ExportDeterministicKey);

            var expectedMediaPath = Path.Combine(expectedRootDirectory, expectedJob.PlayableMediaPath.Replace('/', Path.DirectorySeparatorChar));
            var actualMediaPath = Path.Combine(actualRootDirectory, actualJob.PlayableMediaPath.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(expectedMediaPath));
            Assert.True(File.Exists(actualMediaPath));
            Assert.Equal(File.ReadAllBytes(expectedMediaPath), File.ReadAllBytes(actualMediaPath));
        }
    }

    private static string CreateBatchFixtureManifest(string manifestFileName)
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), "whiteboard-cli-batch-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directoryPath);
        Directory.CreateDirectory(Path.Combine(directoryPath, "assets"));

        WriteSvgAssets(directoryPath);
        WriteAudioAssets(directoryPath);

        foreach (var fileName in new[] { "job-a-spec.json", "job-b-spec.json" })
        {
            File.WriteAllText(
                Path.Combine(directoryPath, fileName),
                ReadFixtureJson("phase06-cli-batch", fileName));
        }

        var jobs = string.Equals(manifestFileName, "primary-manifest.json", StringComparison.Ordinal)
            ? new[]
            {
                new CliBatchJob { JobId = "job-b", SpecPath = "job-b-spec.json", OutputPath = "out/job-b.mp4" },
                new CliBatchJob { JobId = "job-a", SpecPath = "job-a-spec.json", OutputPath = "out/job-a.mp4" }
            }
            : new[]
            {
                new CliBatchJob { JobId = "job-a", SpecPath = "job-a-spec.json", OutputPath = "out/job-a.mp4" },
                new CliBatchJob { JobId = "job-b", SpecPath = "job-b-spec.json", OutputPath = "out/job-b.mp4" }
            };

        var manifestPath = Path.Combine(directoryPath, manifestFileName);
        File.WriteAllText(
            manifestPath,
            JsonSerializer.Serialize(new CliBatchManifest { Jobs = jobs }, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            }));

        return manifestPath;
    }

    private static string CreatePlayableMediaBatchFixtureManifest(string manifestFileName)
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), "whiteboard-cli-batch-media-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directoryPath);
        Directory.CreateDirectory(Path.Combine(directoryPath, "assets"));

        WriteSvgAssets(directoryPath);
        WriteAudioAssets(directoryPath);

        foreach (var fileName in new[] { "job-a-spec.json", "job-b-spec.json", "primary-manifest.json", "equivalent-reordered-manifest.json" })
        {
            File.WriteAllText(
                Path.Combine(directoryPath, fileName),
                ReadFixtureJson("phase09-batch-media", fileName));
        }

        return Path.Combine(directoryPath, manifestFileName);
    }

    private static string CreateSpecFile(string fixtureFolder, string fileName)
    {
        return CreateSpecFileFromJson(
            ReadFixtureJson(fixtureFolder, fileName),
            includeAudioAssets: FixtureRequiresAudioAssets(fixtureFolder),
            includeFontAssets: FixtureRequiresFontAssets(fixtureFolder),
            includeHandAssets: FixtureRequiresHandAssets(fixtureFolder));
    }

    private static bool FixtureRequiresAudioAssets(string fixtureFolder)
    {
        return string.Equals(fixtureFolder, "phase05-export-packaging", StringComparison.Ordinal)
            || string.Equals(fixtureFolder, "phase07-full-timeline", StringComparison.Ordinal)
            || string.Equals(fixtureFolder, "phase08-playable-media", StringComparison.Ordinal)
            || string.Equals(fixtureFolder, "phase11-hand-assets", StringComparison.Ordinal);
    }

    private static bool FixtureRequiresFontAssets(string fixtureFolder)
    {
        return string.Equals(fixtureFolder, "phase11-hand-assets", StringComparison.Ordinal);
    }

    private static bool FixtureRequiresHandAssets(string fixtureFolder)
    {
        return string.Equals(fixtureFolder, "phase11-hand-assets", StringComparison.Ordinal);
    }

    private static string CreateSpecFileFromJson(
        string json,
        bool includeAudioAssets = false,
        bool includeFontAssets = false,
        bool includeHandAssets = false)
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), "whiteboard-cli-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directoryPath);
        Directory.CreateDirectory(Path.Combine(directoryPath, "assets"));

        WriteSvgAssets(directoryPath);

        if (includeAudioAssets)
        {
            WriteAudioAssets(directoryPath);
        }

        if (includeFontAssets)
        {
            WriteFontAssets(directoryPath);
        }

        if (includeHandAssets)
        {
            WriteHandAssets(directoryPath);
        }

        var specPath = Path.Combine(directoryPath, "project.json");
        File.WriteAllText(specPath, json);
        return specPath;
    }

    private static string CreateOutputPath(string specPath, string fileName = "video.mp4")
    {
        var directoryPath = Path.Combine(Path.GetDirectoryName(specPath)!, "out");
        Directory.CreateDirectory(directoryPath);
        return Path.Combine(directoryPath, fileName);
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

    private static void WriteFontAssets(string directoryPath)
    {
        File.WriteAllText(Path.Combine(directoryPath, "assets", "caveat.ttf"), "placeholder-font");
    }

    private static void WriteHandAssets(string directoryPath)
    {
        File.WriteAllText(
            Path.Combine(directoryPath, "assets", "hand.svg"),
            """
            <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24">
              <path fill="#F4D5B1" stroke="#111111" stroke-width="1" d="M 6 2 L 11 2 L 16 8 L 16 18 L 9 22 L 4 18 L 4 7 Z" />
            </svg>
            """);
    }

    private sealed class DeterministicWitnessProcessRunner : Export.Contracts.IProcessRunner
    {
        public ProcessRunResult Run(ProcessRunRequest request)
        {
            var outputPath = request.Arguments.Last();
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? Environment.CurrentDirectory);
            var payload = string.Join("\n", request.Arguments.Select(argument => NormalizeArgument(request, argument)));
            File.WriteAllText(outputPath, payload);
            return new ProcessRunResult
            {
                Success = true,
                ExitCode = 0,
                StandardOutput = "ok",
                StandardError = string.Empty
            };
        }

        private static string NormalizeArgument(ProcessRunRequest request, string argument)
        {
            if (!Path.IsPathRooted(argument))
            {
                return argument.Replace('\\', '/');
            }

            var fullPath = Path.GetFullPath(argument);
            var outputPath = Path.GetFullPath(request.Arguments.Last());
            if (string.Equals(fullPath, outputPath, StringComparison.OrdinalIgnoreCase))
            {
                return $"output{Path.GetExtension(fullPath).ToLowerInvariant()}";
            }

            var workingDirectory = string.IsNullOrWhiteSpace(request.WorkingDirectory)
                ? Environment.CurrentDirectory
                : Path.GetFullPath(request.WorkingDirectory);

            return fullPath.StartsWith(workingDirectory, StringComparison.OrdinalIgnoreCase)
                ? Path.GetRelativePath(workingDirectory, fullPath).Replace('\\', '/')
                : Path.GetFileName(fullPath);
        }
    }

    private sealed class RecordingProcessRunner : Export.Contracts.IProcessRunner
    {
        public ProcessRunResult Run(ProcessRunRequest request)
        {
            var outputPath = request.Arguments.Last();
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? Environment.CurrentDirectory);
            File.WriteAllText(outputPath, "fake-video-payload");
            return new ProcessRunResult
            {
                Success = true,
                ExitCode = 0,
                StandardOutput = "ok",
                StandardError = string.Empty
            };
        }
    }

    private static string ResolveRepoRelativePath(params string[] segments)
    {
        var baseDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        var candidateRoots = new List<DirectoryInfo>();

        for (var current = baseDirectory; current is not null; current = current.Parent)
        {
            candidateRoots.Add(current);
        }

        foreach (var candidate in candidateRoots)
        {
            var repoPath = Path.Combine(new[] { candidate.FullName }.Concat(segments).ToArray());
            if (File.Exists(repoPath))
            {
                return repoPath;
            }
        }

        throw new FileNotFoundException($"Repo file '{Path.Combine(segments)}' was not found.");
    }

    private sealed class RecordingFrameRenderer : IFrameRenderer
    {
        public List<RenderFrameRequest> Requests { get; } = [];

        public RenderFrameResult Render(RenderFrameRequest request)
        {
            Requests.Add(request);
            return new RenderFrameResult
            {
                FrameIndex = request.FrameState.FrameContext.FrameIndex,
                Success = true,
                Message = "recorded",
                SceneCount = request.FrameState.Scenes.Count,
                ObjectCount = request.FrameState.Scenes.Sum(scene => scene.Objects.Count),
                Artifact = new RenderFrameArtifact
                {
                    Format = "svg",
                    FileExtension = ".svg",
                    ContentType = "image/svg+xml",
                    Payload = Array.Empty<byte>()
                }
            };
        }
    }

    private sealed class RecordingExportPipeline : Whiteboard.Export.Contracts.IExportPipeline
    {
        public ExportRequest? LastRequest { get; private set; }

        public ExportResult Export(ExportRequest request)
        {
            LastRequest = request;
            var outputDirectory = Path.GetDirectoryName(request.Target.OutputPath) ?? Environment.CurrentDirectory;
            var totalOperations = request.Frames.Sum(frame => frame.Operations.Count);
            return new ExportResult
            {
                Success = true,
                Message = "recorded",
                OutputPath = request.Target.OutputPath,
                PackageRootPath = outputDirectory,
                ManifestPath = Path.Combine(outputDirectory, "frame-manifest.json"),
                ExportedFrameCount = request.Frames.Count,
                ExportedAudioCueCount = request.AudioCues.Count,
                TotalOperations = totalOperations,
                Frames = request.Frames
                    .Select(frame => new ExportFramePackage
                    {
                        FrameIndex = frame.FrameIndex,
                        StartSeconds = request.FrameTimings.FirstOrDefault(timing => timing.FrameIndex == frame.FrameIndex)?.StartSeconds ?? 0d,
                        DurationSeconds = request.FrameTimings.FirstOrDefault(timing => timing.FrameIndex == frame.FrameIndex)?.DurationSeconds ?? 0d,
                        SceneCount = frame.SceneCount,
                        ObjectCount = frame.ObjectCount,
                        ArtifactRelativePath = $"frames/frame-{frame.FrameIndex:000000}.svg",
                        ArtifactFormat = frame.Artifact.Format,
                        ArtifactContentType = frame.Artifact.ContentType,
                        ArtifactByteCount = frame.Artifact.Payload.Length,
                        ArtifactDeterministicKey = $"artifact-{frame.FrameIndex:000000}",
                        Operations = frame.Operations
                    })
                    .ToArray(),
                Summary = new ExportPackageSummary
                {
                    ProjectId = request.ProjectId,
                    Format = request.Target.Format,
                    Width = request.Target.Width,
                    Height = request.Target.Height,
                    FrameRate = request.Target.FrameRate,
                    FrameCount = request.Frames.Count,
                    AudioCueCount = request.AudioCues.Count,
                    TotalOperations = totalOperations,
                    TotalDurationSeconds = request.FrameTimings.Sum(timing => timing.DurationSeconds)
                },
                DeterministicKey = "recording-export"
            };
        }
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























