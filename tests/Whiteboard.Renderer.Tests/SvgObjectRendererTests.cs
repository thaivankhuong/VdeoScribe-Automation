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

public sealed class SvgObjectRendererTests
{
    [Fact]
    public void SvgObjectRenderer_RendersOrderedFullAndPartialPathOperations()
    {
        using var fixture = CreateFixture("""
            <svg xmlns="http://www.w3.org/2000/svg">
              <path d="M 0 0 L 10 0" />
              <path d="M 10 0 L 20 10" />
            </svg>
            """);
        var renderer = new SvgObjectRenderer();
        var surface = new InMemoryRenderSurface();

        renderer.RenderObject(CreateSvgObjectState(), fixture.Request, surface);

        Assert.Equal(2, surface.Operations.Count);
        Assert.Contains("svg-path:mode:full", surface.Operations[0], StringComparison.Ordinal);
        Assert.Contains("ordering:scene-1:0:obj-1:path:0", surface.Operations[0], StringComparison.Ordinal);
        Assert.Contains("svg-path:mode:partial", surface.Operations[1], StringComparison.Ordinal);
        Assert.Contains("ordering:scene-1:0:obj-1:path:1", surface.Operations[1], StringComparison.Ordinal);
    }

    [Fact]
    public void SvgObjectRenderer_EmitsSourceDimensionsAndStyleMetadata()
    {
        using var fixture = CreateFixture("""
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 10">
              <path fill="#F9A825" stroke="#224466" stroke-width="3" stroke-linecap="square" stroke-linejoin="bevel" fill-opacity="0.7" stroke-opacity="0.5" d="M 0 0 L 10 0" />
              <path fill="#E53935" d="M 10 0 L 20 10" />
            </svg>
            """);
        var renderer = new SvgObjectRenderer();
        var surface = new InMemoryRenderSurface();

        renderer.RenderObject(CreateSvgObjectState(), fixture.Request, surface);

        var fill = Convert.ToBase64String(Encoding.UTF8.GetBytes("#F9A825"));
        var stroke = Convert.ToBase64String(Encoding.UTF8.GetBytes("#224466"));
        var fillOnlyStroke = Convert.ToBase64String(Encoding.UTF8.GetBytes("none"));

        Assert.Contains("assetWidth:20.000:assetHeight:10.000", surface.Operations[0], StringComparison.Ordinal);
        Assert.Contains($"fill64:{fill}", surface.Operations[0], StringComparison.Ordinal);
        Assert.Contains($"stroke64:{stroke}", surface.Operations[0], StringComparison.Ordinal);
        Assert.Contains("strokeWidth:3.000", surface.Operations[0], StringComparison.Ordinal);
        Assert.Contains("fillOpacity:0.700", surface.Operations[0], StringComparison.Ordinal);
        Assert.Contains("strokeOpacity:0.500", surface.Operations[0], StringComparison.Ordinal);
        Assert.Contains($"stroke64:{fillOnlyStroke}", surface.Operations[1], StringComparison.Ordinal);
    }

    [Fact]
    public void SvgObjectRenderer_MissingAsset_ThrowsFileNotFoundException()
    {
        var renderer = new SvgObjectRenderer();
        var surface = new InMemoryRenderSurface();
        var request = new RenderFrameRequest
        {
            FrameState = CreateFrameState(),
            SurfaceSize = new RenderSurfaceSize(1280, 720),
            SvgAssets = new Dictionary<string, SvgRenderAsset>()
        };

        var exception = Assert.Throws<FileNotFoundException>(() => renderer.RenderObject(CreateSvgObjectState(), request, surface));

        Assert.Contains("Missing SVG asset", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SvgObjectRenderer_MalformedSvg_EmitsDeterministicObjectFailure()
    {
        using var fixture = CreateFixture("<svg><path></svg>");
        var renderer = new SvgObjectRenderer();
        var surface = new InMemoryRenderSurface();

        renderer.RenderObject(CreateSvgObjectState(), fixture.Request, surface);

        Assert.Single(surface.Operations);
        Assert.Equal("svg-object-error:object:obj-1:asset:svg-1:reason:malformed-svg", surface.Operations[0]);
    }

    [Fact]
    public void SvgObjectRenderer_SingleDrawWindow_DistributesProgressAcrossSvgPathsSequentially()
    {
        using var fixture = CreateFixture("""
            <svg xmlns="http://www.w3.org/2000/svg">
              <path d="M 0 0 L 10 0" />
              <path d="M 12 0 L 22 0" />
              <path d="M 24 0 L 34 0" />
            </svg>
            """);
        var renderer = new SvgObjectRenderer();
        var surface = new InMemoryRenderSurface();

        renderer.RenderObject(
            new ResolvedObjectState
            {
                SceneObjectId = "obj-1",
                Type = SceneObjectType.Svg,
                AssetRefId = "svg-1",
                Layer = 0,
                IsVisible = true,
                DrawProgress = 0.5,
                DrawPathCount = 1,
                ActiveDrawPathIndex = 0,
                DrawOrderingKey = "scene-1:0:obj-1",
                DrawPaths =
                [
                    new ResolvedDrawPathState
                    {
                        PathIndex = 0,
                        Progress = 0.5,
                        IsActive = true,
                        OrderingKey = "scene-1:0:obj-1:path:0"
                    }
                ],
                Transform = new TransformSpec
                {
                    Position = new Position2D(20, 30),
                    Size = new Size2D(120, 120),
                    Opacity = 1
                }
            },
            fixture.Request,
            surface);

        Assert.Equal(2, surface.Operations.Count);
        Assert.Contains("svg-path:mode:full", surface.Operations[0], StringComparison.Ordinal);
        Assert.Contains("path:0", surface.Operations[0], StringComparison.Ordinal);
        Assert.Contains("active:false", surface.Operations[0], StringComparison.Ordinal);
        Assert.Contains("svg-path:mode:partial", surface.Operations[1], StringComparison.Ordinal);
        Assert.Contains("path:1", surface.Operations[1], StringComparison.Ordinal);
        Assert.Contains("active:true", surface.Operations[1], StringComparison.Ordinal);
        Assert.Contains("progress:0.500", surface.Operations[1], StringComparison.Ordinal);
    }

    [Fact]
    public void TextObjectRenderer_RendersStableTextOperation()
    {
        var renderer = new TextObjectRenderer();
        var surface = new InMemoryRenderSurface();
        var request = new RenderFrameRequest
        {
            FrameState = CreateFrameState(),
            SurfaceSize = new RenderSurfaceSize(1280, 720),
            FontAssets = new Dictionary<string, FontRenderAsset>
            {
                ["font-1"] = new()
                {
                    Id = "font-1",
                    FamilyName = "Caveat",
                    SourcePath = "assets/caveat.ttf",
                    ColorHex = "#E64A3B"
                }
            }
        };

        renderer.RenderObject(
            new ResolvedObjectState
            {
                SceneObjectId = "text-1",
                Type = SceneObjectType.Text,
                AssetRefId = "font-1",
                TextContent = "Hello world",
                Layer = 1,
                IsVisible = true,
                DrawOrderingKey = "scene-1:1:text-1",
                Transform = new TransformSpec
                {
                    Position = new Position2D(50, 80),
                    Size = new Size2D(240, 32),
                    RotationDegrees = 3,
                    ScaleX = 1,
                    ScaleY = 1,
                    Opacity = 0.85
                }
            },
            request,
            surface);

        Assert.Single(surface.Operations);
        Assert.Contains("text-object:object:text-1", surface.Operations[0], StringComparison.Ordinal);
        Assert.Contains("asset:font-1", surface.Operations[0], StringComparison.Ordinal);
        Assert.Contains("ordering:scene-1:1:text-1:path:0", surface.Operations[0], StringComparison.Ordinal);
        Assert.Contains($"color64:{Convert.ToBase64String(Encoding.UTF8.GetBytes("#E64A3B"))}", surface.Operations[0], StringComparison.Ordinal);
        Assert.Contains("active:false", surface.Operations[0], StringComparison.Ordinal);
        Assert.Contains("progress:1.000", surface.Operations[0], StringComparison.Ordinal);
    }

    [Fact]
    public void ImageObjectRenderer_RendersStableImageOperationWithOrderingMetadata()
    {
        var renderer = new ImageObjectRenderer();
        var surface = new InMemoryRenderSurface();
        var request = new RenderFrameRequest
        {
            FrameState = CreateFrameState(),
            SurfaceSize = new RenderSurfaceSize(1280, 720),
            ImageAssets = new Dictionary<string, ImageRenderAsset>
            {
                ["image-1"] = new()
                {
                    Id = "image-1",
                    Name = "Image Object",
                    SourcePath = "assets/image.svg"
                }
            }
        };

        renderer.RenderObject(
            new ResolvedObjectState
            {
                SceneObjectId = "image-1",
                Type = SceneObjectType.Image,
                AssetRefId = "image-1",
                Layer = 2,
                IsVisible = true,
                DrawProgress = 0.5,
                DrawPathCount = 1,
                ActiveDrawPathIndex = 0,
                DrawOrderingKey = "scene-1:2:image-1",
                DrawPaths =
                [
                    new ResolvedDrawPathState
                    {
                        PathIndex = 0,
                        Progress = 0.5,
                        IsActive = true,
                        OrderingKey = "scene-1:2:image-1:path:0"
                    }
                ],
                Transform = new TransformSpec
                {
                    Position = new Position2D(60, 90),
                    Size = new Size2D(180, 120),
                    RotationDegrees = 2,
                    ScaleX = 1,
                    ScaleY = 1,
                    Opacity = 0.8
                }
            },
            request,
            surface);

        Assert.Single(surface.Operations);
        Assert.Contains("image-object:object:image-1", surface.Operations[0], StringComparison.Ordinal);
        Assert.Contains("ordering:scene-1:2:image-1:path:0", surface.Operations[0], StringComparison.Ordinal);
        Assert.Contains("active:true", surface.Operations[0], StringComparison.Ordinal);
        Assert.Contains("progress:0.500", surface.Operations[0], StringComparison.Ordinal);
    }

    [Fact]
    public void TextObjectRenderer_MissingFontAsset_ThrowsInvalidOperationException()
    {
        var renderer = new TextObjectRenderer();
        var surface = new InMemoryRenderSurface();
        var request = new RenderFrameRequest
        {
            FrameState = CreateFrameState(),
            SurfaceSize = new RenderSurfaceSize(1280, 720)
        };

        var exception = Assert.Throws<InvalidOperationException>(() => renderer.RenderObject(
            new ResolvedObjectState
            {
                SceneObjectId = "text-1",
                Type = SceneObjectType.Text,
                AssetRefId = "font-missing",
                TextContent = "Hello world",
                Layer = 1,
                IsVisible = true
            },
            request,
            surface));

        Assert.Contains("Missing font asset", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SceneRenderer_UsesTextRendererAndFallbackRendererInStableOrder()
    {
        var renderer = new SceneRenderer();
        var surface = new InMemoryRenderSurface();
        var request = new RenderFrameRequest
        {
            FrameState = CreateFrameState(),
            SurfaceSize = new RenderSurfaceSize(1280, 720)
        };
        var scene = new ResolvedSceneState
        {
            SceneId = "scene-1",
            Objects =
            [
                new ResolvedObjectState { SceneObjectId = "group-z", Type = SceneObjectType.Group, Layer = 3, IsVisible = true },
                new ResolvedObjectState { SceneObjectId = "text-b", Type = SceneObjectType.Text, Layer = 2, IsVisible = true, TextContent = "B" },
                new ResolvedObjectState { SceneObjectId = "text-a", Type = SceneObjectType.Text, Layer = 1, IsVisible = true, TextContent = "A" }
            ]
        };

        renderer.RenderScene(scene, request, surface);

        Assert.Equal(3, surface.Operations.Count);
        Assert.StartsWith("text-object:object:text-a", surface.Operations[0], StringComparison.Ordinal);
        Assert.StartsWith("text-object:object:text-b", surface.Operations[1], StringComparison.Ordinal);
        Assert.Equal("unsupported-object:object:group-z:type:group", surface.Operations[2]);
    }

    private static Fixture CreateFixture(string svgMarkup)
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), "whiteboard-svg-renderer-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directoryPath);

        var svgPath = Path.Combine(directoryPath, "shape.svg");
        File.WriteAllText(svgPath, svgMarkup);

        return new Fixture(
            directoryPath,
            new RenderFrameRequest
            {
                FrameState = CreateFrameState(),
                SurfaceSize = new RenderSurfaceSize(1280, 720),
                SvgAssets = new Dictionary<string, SvgRenderAsset>
                {
                    ["svg-1"] = new()
                    {
                        Id = "svg-1",
                        Name = "Shape",
                        SourcePath = svgPath
                    }
                }
            });
    }

    private static ResolvedFrameState CreateFrameState()
    {
        return new ResolvedFrameState
        {
            FrameContext = FrameContext.FromFrameIndex(0, 30),
            Camera = new ResolvedCameraState
            {
                Position = new Position2D(0, 0),
                Zoom = 1,
                Interpolation = EasingType.Linear
            },
            Scenes = []
        };
    }

    private static ResolvedObjectState CreateSvgObjectState()
    {
        return new ResolvedObjectState
        {
            SceneObjectId = "obj-1",
            Type = SceneObjectType.Svg,
            AssetRefId = "svg-1",
            Layer = 0,
            IsVisible = true,
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
                Opacity = 1
            }
        };
    }

    private sealed class Fixture(string directoryPath, RenderFrameRequest request) : IDisposable
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










