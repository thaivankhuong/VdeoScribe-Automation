using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Whiteboard.Core.Enums;
using Whiteboard.Core.ValueObjects;
using Whiteboard.Engine.Context;
using Whiteboard.Engine.Models;
using Whiteboard.Renderer.Models;
using Whiteboard.Renderer.Services;
using Xunit;

namespace Whiteboard.Renderer.Tests;

public sealed class FrameRendererContractTests
{
    [Fact]
    public void RenderFrameRequest_CanBeCreatedFromResolvedFrameState()
    {
        using var fixture = CreateFixture();

        Assert.Equal(0, fixture.Request.FrameState.FrameContext.FrameIndex);
        Assert.Equal(1280, fixture.Request.SurfaceSize.Width);
        Assert.Equal(720, fixture.Request.SurfaceSize.Height);
        Assert.Single(fixture.Request.SvgAssets);
        Assert.Single(fixture.Request.HandAssets);
        Assert.True(fixture.Request.EnableHandGuidanceOverlay);
    }

    [Fact]
    public void FrameRenderer_CanAcceptRequest_AndProduceSvgOperationsArtifactAndHandAssetGuidance()
    {
        using var fixture = CreateFixture();
        var renderer = new FrameRenderer();

        var result = renderer.Render(fixture.Request);
        var artifactMarkup = Encoding.UTF8.GetString(result.Artifact.Payload);

        Assert.True(result.Success, result.Message);
        Assert.Equal("Rendered deterministic SVG frame operations and artifact payload.", result.Message);
        Assert.Equal(0, result.FrameIndex);
        Assert.Equal(1, result.SceneCount);
        Assert.Equal(1, result.ObjectCount);
        Assert.Equal(3, result.Operations.Count);
        Assert.StartsWith("camera:", result.Operations[0], StringComparison.Ordinal);
        Assert.Contains("svg-path:mode:full", result.Operations[1], StringComparison.Ordinal);
        Assert.Contains("svg-path:mode:partial", result.Operations[2], StringComparison.Ordinal);
        Assert.Equal("svg", result.Artifact.Format);
        Assert.Equal(".svg", result.Artifact.FileExtension);
        Assert.Equal("image/svg+xml", result.Artifact.ContentType);
        Assert.NotEmpty(result.Artifact.Payload);
        Assert.Contains("<svg", artifactMarkup, StringComparison.Ordinal);
        Assert.Contains("data-mode=\"full\"", artifactMarkup, StringComparison.Ordinal);
        Assert.Contains("data-mode=\"partial\"", artifactMarkup, StringComparison.Ordinal);
        Assert.Contains("fill=\"#F9A825\"", artifactMarkup, StringComparison.Ordinal);
        Assert.Contains("stroke=\"#224466\"", artifactMarkup, StringComparison.Ordinal);
        Assert.Contains("fill-opacity=\"0.7\"", artifactMarkup, StringComparison.Ordinal);
        Assert.Contains("stroke-opacity=\"0.5\"", artifactMarkup, StringComparison.Ordinal);
        Assert.Contains("stroke=\"#E53935\"", artifactMarkup, StringComparison.Ordinal);
        Assert.Contains("transform=\"translate(100 100) rotate(5) scale(10 20)\"", artifactMarkup, StringComparison.Ordinal);
        Assert.Contains("pathLength=\"1\"", artifactMarkup, StringComparison.Ordinal);
        Assert.Contains("stroke-dasharray=\"0.5 1\"", artifactMarkup, StringComparison.Ordinal);
        Assert.Contains("stroke-dashoffset=\"0\"", artifactMarkup, StringComparison.Ordinal);
        Assert.Contains("data-guidance=\"hand\"", artifactMarkup, StringComparison.Ordinal);
        Assert.Contains("data-guidance-object=\"obj-1\"", artifactMarkup, StringComparison.Ordinal);
        Assert.Contains("data-guidance-renderer=\"asset\"", artifactMarkup, StringComparison.Ordinal);
        Assert.Contains("data-guidance-asset=\"hand-1\"", artifactMarkup, StringComparison.Ordinal);
        Assert.Contains("<image href=\"data:image/svg+xml;base64,", artifactMarkup, StringComparison.Ordinal);    }

    [Fact]
    public void FrameRenderer_WhenHandGuidanceOverlayDisabled_DoesNotEmitGuidanceMarkup()
    {
        using var fixture = CreateFixture(enableHandGuidanceOverlay: false);
        var renderer = new FrameRenderer();

        var result = renderer.Render(fixture.Request);
        var artifactMarkup = Encoding.UTF8.GetString(result.Artifact.Payload);

        Assert.True(result.Success, result.Message);
        Assert.DoesNotContain("data-guidance=\"hand\"", artifactMarkup, StringComparison.Ordinal);
    }

    [Fact]
    public void FrameRenderer_WhenTextIsActivelyDrawing_ClipsTextAndUsesTextHandGuidance()
    {
        using var fixture = CreateFixture(includeAssetManifest: false);
        var renderer = new FrameRenderer();
        var request = fixture.Request with
        {
            FrameState = CreateTextFrameState(),
            SvgAssets = new Dictionary<string, SvgRenderAsset>()
        };

        var result = renderer.Render(request);
        var artifactMarkup = Encoding.UTF8.GetString(result.Artifact.Payload);

        Assert.True(result.Success, result.Message);
        Assert.Contains("data-object=\"text-1\"", artifactMarkup, StringComparison.Ordinal);
        Assert.Contains("data-progress=\"0.5\"", artifactMarkup, StringComparison.Ordinal);
        Assert.Contains("data-ordering=\"scene-1:0:text-1:path:0\"", artifactMarkup, StringComparison.Ordinal);
        Assert.Contains("clipPath id=\"text-clip-text-1\"", artifactMarkup, StringComparison.Ordinal);
        Assert.Contains("data-guidance=\"hand\"", artifactMarkup, StringComparison.Ordinal);
        Assert.Contains("data-guidance-object=\"text-1\"", artifactMarkup, StringComparison.Ordinal);
        Assert.Contains("Write this", artifactMarkup, StringComparison.Ordinal);
    }

    [Fact]
    public void FrameRenderer_WhenImageIsActivelyDrawing_ClipsImageAndUsesHandGuidance()
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), "whiteboard-renderer-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directoryPath);

        try
        {
            var imagePath = Path.Combine(directoryPath, "object.svg");
            File.WriteAllText(
                imagePath,
                """
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 10 10">
                  <rect x="0" y="0" width="10" height="10" fill="#4DB6AC"/>
                </svg>
                """);

            var handPath = Path.Combine(directoryPath, "hand.svg");
            File.WriteAllText(
                handPath,
                """
                <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24">
                  <path fill="#F4D5B1" stroke="#111111" stroke-width="1" d="M 6 2 L 11 2 L 16 8 L 16 18 L 9 22 L 4 18 L 4 7 Z" />
                </svg>
                """);

            var request = new RenderFrameRequest
            {
                FrameState = CreateImageFrameState(),
                SurfaceSize = new RenderSurfaceSize(1280, 720),
                ImageAssets = new Dictionary<string, ImageRenderAsset>
                {
                    ["image-1"] = new()
                    {
                        Id = "image-1",
                        Name = "Image Object",
                        SourcePath = imagePath
                    }
                },
                HandAssets = new Dictionary<string, HandRenderAsset>
                {
                    ["hand-1"] = new()
                    {
                        Id = "hand-1",
                        Name = "Default Hand",
                        SourcePath = handPath,
                        TipOffset = new Position2D(8, 18)
                    }
                }
            };

            var renderer = new FrameRenderer();
            var result = renderer.Render(request);
            var artifactMarkup = Encoding.UTF8.GetString(result.Artifact.Payload);

            Assert.True(result.Success, result.Message);
            Assert.Contains("data-object=\"image-1\"", artifactMarkup, StringComparison.Ordinal);
            Assert.Contains("data-ordering=\"scene-1:0:image-1:path:0\"", artifactMarkup, StringComparison.Ordinal);
            Assert.Contains("clipPath id=\"image-clip-image-1\"", artifactMarkup, StringComparison.Ordinal);
            Assert.Contains("data-guidance=\"hand\"", artifactMarkup, StringComparison.Ordinal);
            Assert.Contains("data-guidance-object=\"image-1\"", artifactMarkup, StringComparison.Ordinal);
            Assert.Contains("data-guidance-renderer=\"asset\"", artifactMarkup, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, recursive: true);
            }
        }
    }

    [Fact]
    public void FrameRenderer_ProducesDeterministicStructure_ForSameInput()
    {
        using var fixture = CreateFixture();
        var renderer = new FrameRenderer();

        var first = renderer.Render(fixture.Request);
        var second = renderer.Render(fixture.Request);

        Assert.Equal(first.Success, second.Success);
        Assert.Equal(first.Message, second.Message);
        Assert.Equal(first.FrameIndex, second.FrameIndex);
        Assert.Equal(first.SceneCount, second.SceneCount);
        Assert.Equal(first.ObjectCount, second.ObjectCount);
        Assert.Equal(first.Operations, second.Operations);
        Assert.Equal(first.Artifact.Format, second.Artifact.Format);
        Assert.Equal(first.Artifact.ContentType, second.Artifact.ContentType);
        Assert.Equal(first.Artifact.Payload, second.Artifact.Payload);
    }
    [Fact]
    public void FrameRenderer_WhenEarlierActiveTextAndLaterActiveSvgOverlap_PrefersEarlierOrderingForHandGuidance()
    {
        using var fixture = CreateFixture();
        var renderer = new FrameRenderer();
        var request = fixture.Request with
        {
            FrameState = CreateOverlappingTextAndSvgFrameState()
        };

        var result = renderer.Render(request);
        var artifactMarkup = Encoding.UTF8.GetString(result.Artifact.Payload);

        Assert.True(result.Success, result.Message);
        Assert.Contains("data-object=\"text-1\"", artifactMarkup, StringComparison.Ordinal);
        Assert.Contains("data-object=\"obj-1\"", artifactMarkup, StringComparison.Ordinal);
        Assert.Contains("data-guidance-object=\"text-1\"", artifactMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("data-guidance-object=\"obj-1\"", artifactMarkup, StringComparison.Ordinal);
    }

    [Fact]
    public void FrameRenderer_WithEquivalentRequests_ProducesEquivalentRenderSummary()
    {
        using var firstFixture = CreateFixture();
        using var secondFixture = CreateFixture();
        var renderer = new FrameRenderer();

        var first = renderer.Render(firstFixture.Request);
        var second = renderer.Render(secondFixture.Request);

        Assert.Equal(first.Success, second.Success);
        Assert.Equal(first.Message, second.Message);
        Assert.Equal(first.FrameIndex, second.FrameIndex);
        Assert.Equal(first.SceneCount, second.SceneCount);
        Assert.Equal(first.ObjectCount, second.ObjectCount);
        Assert.Equal(first.Operations, second.Operations);
        Assert.Equal(first.Artifact.Payload, second.Artifact.Payload);
    }

    [Fact]
    public void FrameRenderer_MissingSvgAsset_FailsFastDeterministically()
    {
        using var fixture = CreateFixture(includeAssetManifest: false);
        var renderer = new FrameRenderer();

        var result = renderer.Render(fixture.Request);

        Assert.False(result.Success);
        Assert.Contains("Missing SVG asset", result.Message, StringComparison.Ordinal);
        Assert.Single(result.Operations);
        Assert.StartsWith("camera:", result.Operations[0], StringComparison.Ordinal);
        Assert.Empty(result.Artifact.Payload);
    }

    [Fact]
    public void FrameRenderer_MissingHandAsset_FailsFastDeterministically()
    {
        using var fixture = CreateFixture(includeHandAssetManifest: true, includeHandAssetFile: false);
        var renderer = new FrameRenderer();

        var result = renderer.Render(fixture.Request);

        Assert.False(result.Success);
        Assert.Contains("Missing hand asset", result.Message, StringComparison.Ordinal);
        Assert.Equal(3, result.Operations.Count);
        Assert.Empty(result.Artifact.Payload);
    }

    private static RenderFixture CreateFixture(
        bool includeAssetManifest = true,
        bool enableHandGuidanceOverlay = true,
        bool includeHandAssetManifest = true,
        bool includeHandAssetFile = true)
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), "whiteboard-renderer-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directoryPath);

        var svgPath = Path.Combine(directoryPath, "shape.svg");
        File.WriteAllText(
            svgPath,
            """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 10">
              <path fill="#F9A825" stroke="#224466" stroke-width="3" stroke-linecap="square" stroke-linejoin="bevel" fill-opacity="0.7" stroke-opacity="0.5" d="M 0 0 L 10 0" />
              <path fill="#E53935" d="M 10 0 L 20 10" />
            </svg>
            """);

        var handPath = Path.Combine(directoryPath, "hand.svg");
        if (includeHandAssetFile)
        {
            File.WriteAllText(
                handPath,
                """
                <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24">
                  <path fill="#F4D5B1" stroke="#111111" stroke-width="1" d="M 6 2 L 11 2 L 16 8 L 16 18 L 9 22 L 4 18 L 4 7 Z" />
                </svg>
                """);
        }

        var request = new RenderFrameRequest
        {
            FrameState = CreateResolvedFrameState(),
            SurfaceSize = new RenderSurfaceSize(1280, 720),
            EnableHandGuidanceOverlay = enableHandGuidanceOverlay,
            SvgAssets = includeAssetManifest
                ? new Dictionary<string, SvgRenderAsset>
                {
                    ["svg-1"] = new()
                    {
                        Id = "svg-1",
                        Name = "Shape",
                        SourcePath = svgPath
                    }
                }
                : new Dictionary<string, SvgRenderAsset>(),
            HandAssets = includeHandAssetManifest
                ? new Dictionary<string, HandRenderAsset>
                {
                    ["hand-1"] = new()
                    {
                        Id = "hand-1",
                        Name = "Default Hand",
                        SourcePath = handPath,
                        TipOffset = new Position2D(8, 18)
                    }
                }
                : new Dictionary<string, HandRenderAsset>()
        };

        return new RenderFixture(directoryPath, request);
    }

    private static ResolvedFrameState CreateResolvedFrameState()
    {
        return new ResolvedFrameState
        {
            FrameContext = FrameContext.FromFrameIndex(0, 30),
            Camera = new ResolvedCameraState
            {
                FrameTimeSeconds = 0,
                Position = new Position2D(10, 20),
                Zoom = 1.25,
                Interpolation = EasingType.Linear
            },
            TimelineEvents =
            [
                new ResolvedTimelineEvent
                {
                    EventId = "evt-1",
                    SceneId = "scene-1",
                    SceneObjectId = "obj-1",
                    ActionType = TimelineActionType.Draw,
                    IsActive = true
                }
            ],
            Scenes =
            [
                new ResolvedSceneState
                {
                    SceneId = "scene-1",
                    Objects =
                    [
                        new ResolvedObjectState
                        {
                            SceneObjectId = "obj-1",
                            Type = SceneObjectType.Svg,
                            AssetRefId = "svg-1",
                            Layer = 0,
                            IsVisible = true,
                            RevealProgress = 0.75,
                            DrawProgress = 0.75,
                            DrawPathCount = 2,
                            ActiveDrawPathIndex = 1,
                            DrawOrderingKey = "scene-1:0:obj-1",
                            DrawPaths =
                            [
                                new ResolvedDrawPathState
                                {
                                    PathIndex = 0,
                                    Progress = 1,
                                    IsActive = false,
                                    OrderingKey = "scene-1:0:obj-1:path:0"
                                },
                                new ResolvedDrawPathState
                                {
                                    PathIndex = 1,
                                    Progress = 0.5,
                                    IsActive = true,
                                    OrderingKey = "scene-1:0:obj-1:path:1"
                                }
                            ],
                            Transform = new TransformSpec
                            {
                                Position = new Position2D(100, 100),
                                Size = new Size2D(200, 200),
                                RotationDegrees = 5,
                                ScaleX = 1,
                                ScaleY = 1,
                                Opacity = 0.9
                            }
                        }
                    ]
                }
            ]
        };
    }

    private static ResolvedFrameState CreateImageFrameState()
    {
        return new ResolvedFrameState
        {
            FrameContext = FrameContext.FromFrameIndex(12, 24),
            Camera = new ResolvedCameraState
            {
                FrameTimeSeconds = 0.5,
                Position = new Position2D(0, 0),
                Zoom = 1,
                Interpolation = EasingType.Linear
            },
            Scenes =
            [
                new ResolvedSceneState
                {
                    SceneId = "scene-1",
                    Objects =
                    [
                        new ResolvedObjectState
                        {
                            SceneObjectId = "image-1",
                            Type = SceneObjectType.Image,
                            AssetRefId = "image-1",
                            Layer = 0,
                            IsVisible = true,
                            DrawProgress = 0.5,
                            DrawPathCount = 1,
                            ActiveDrawPathIndex = 0,
                            DrawOrderingKey = "scene-1:0:image-1",
                            DrawPaths =
                            [
                                new ResolvedDrawPathState
                                {
                                    PathIndex = 0,
                                    Progress = 0.5,
                                    IsActive = true,
                                    OrderingKey = "scene-1:0:image-1:path:0"
                                }
                            ],
                            Transform = new TransformSpec
                            {
                                Position = new Position2D(280, 180),
                                Size = new Size2D(240, 120),
                                Opacity = 1
                            }
                        }
                    ]
                }
            ]
        };
    }

    private static ResolvedFrameState CreateTextFrameState()
    {
        return new ResolvedFrameState
        {
            FrameContext = FrameContext.FromFrameIndex(12, 24),
            Camera = new ResolvedCameraState
            {
                FrameTimeSeconds = 0.5,
                Position = new Position2D(0, 0),
                Zoom = 1,
                Interpolation = EasingType.Linear
            },
            Scenes =
            [
                new ResolvedSceneState
                {
                    SceneId = "scene-1",
                    Objects =
                    [
                        new ResolvedObjectState
                        {
                            SceneObjectId = "text-1",
                            Type = SceneObjectType.Text,
                            TextContent = "Write this",
                            Layer = 0,
                            IsVisible = true,
                            DrawProgress = 0.5,
                            DrawPathCount = 1,
                            ActiveDrawPathIndex = 0,
                            DrawOrderingKey = "scene-1:0:text-1",
                            DrawPaths =
                            [
                                new ResolvedDrawPathState
                                {
                                    PathIndex = 0,
                                    Progress = 0.5,
                                    IsActive = true,
                                    OrderingKey = "scene-1:0:text-1:path:0"
                                }
                            ],
                            Transform = new TransformSpec
                            {
                                Position = new Position2D(320, 180),
                                Size = new Size2D(220, 42),
                                Opacity = 1
                            }
                        }
                    ]
                }
            ]
        };
    }
    private static ResolvedFrameState CreateOverlappingTextAndSvgFrameState()
    {
        return new ResolvedFrameState
        {
            FrameContext = FrameContext.FromFrameIndex(12, 24),
            Camera = new ResolvedCameraState
            {
                FrameTimeSeconds = 0.5,
                Position = new Position2D(0, 0),
                Zoom = 1,
                Interpolation = EasingType.Linear
            },
            Scenes =
            [
                new ResolvedSceneState
                {
                    SceneId = "scene-1",
                    Objects =
                    [
                        new ResolvedObjectState
                        {
                            SceneObjectId = "text-1",
                            Type = SceneObjectType.Text,
                            TextContent = "Write this",
                            Layer = 0,
                            IsVisible = true,
                            DrawProgress = 0.4,
                            DrawPathCount = 1,
                            ActiveDrawPathIndex = 0,
                            DrawOrderingKey = "scene-1:0:text-1",
                            DrawPaths =
                            [
                                new ResolvedDrawPathState
                                {
                                    PathIndex = 0,
                                    Progress = 0.4,
                                    IsActive = true,
                                    OrderingKey = "scene-1:0:text-1:path:0"
                                }
                            ],
                            Transform = new TransformSpec
                            {
                                Position = new Position2D(120, 120),
                                Size = new Size2D(220, 42),
                                Opacity = 1
                            }
                        },
                        new ResolvedObjectState
                        {
                            SceneObjectId = "obj-1",
                            Type = SceneObjectType.Svg,
                            AssetRefId = "svg-1",
                            Layer = 1,
                            IsVisible = true,
                            RevealProgress = 0.75,
                            DrawProgress = 0.75,
                            DrawPathCount = 2,
                            ActiveDrawPathIndex = 1,
                            DrawOrderingKey = "scene-1:1:obj-1",
                            DrawPaths =
                            [
                                new ResolvedDrawPathState
                                {
                                    PathIndex = 0,
                                    Progress = 1,
                                    IsActive = false,
                                    OrderingKey = "scene-1:1:obj-1:path:0"
                                },
                                new ResolvedDrawPathState
                                {
                                    PathIndex = 1,
                                    Progress = 0.5,
                                    IsActive = true,
                                    OrderingKey = "scene-1:1:obj-1:path:1"
                                }
                            ],
                            Transform = new TransformSpec
                            {
                                Position = new Position2D(100, 100),
                                Size = new Size2D(200, 200),
                                RotationDegrees = 5,
                                ScaleX = 1,
                                ScaleY = 1,
                                Opacity = 0.9
                            }
                        }
                    ]
                }
            ]
        };
    }

    private sealed class RenderFixture(string directoryPath, RenderFrameRequest request) : IDisposable
    {
        public RenderFrameRequest Request { get; } = request;

        public void Dispose()
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, recursive: true);
            }
        }
    }
}





