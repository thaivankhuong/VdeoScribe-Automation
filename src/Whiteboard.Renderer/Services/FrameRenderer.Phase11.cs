using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Whiteboard.Renderer.Models;

namespace Whiteboard.Renderer.Services;

public sealed partial class FrameRenderer
{
    private const double HandAssetGuidanceScale = 1.6d;
    private const double TextHandBaselineFactor = 0.82d;

    private static void AppendResolvedHandGuidance(StringBuilder builder, RenderFrameRequest request, HandGuidanceOverlayData overlay)
    {
        if (TryGetHandRenderAsset(request, out var handAsset))
        {
            AppendHandAssetGuidance(builder, overlay, handAsset);
            return;
        }

        AppendHandGuidance(builder, overlay);
    }

    private static void AppendTextObject(StringBuilder builder, TextOperationData textOperation)
    {
        var progress = ClampProgress(textOperation.Progress);
        var estimatedWidth = ResolveTextRenderWidth(textOperation);
        var clipHeight = ResolveTextRenderHeight(textOperation);
        var clipWidth = estimatedWidth * progress;

        builder.Append("<g data-object=\"")
            .Append(EscapeXml(textOperation.ObjectId))
            .Append("\" data-asset=\"")
            .Append(EscapeXml(textOperation.AssetId))
            .Append("\" data-progress=\"")
            .Append(Format(progress))
            .Append("\" data-ordering=\"")
            .Append(EscapeXml(textOperation.OrderingKey))
            .Append("\" data-active=\"")
            .Append(textOperation.IsActive.ToString().ToLowerInvariant())
            .Append("\" data-font-family=\"")
            .Append(EscapeXml(textOperation.FontFamily))
            .Append("\" opacity=\"")
            .Append(Format(textOperation.Opacity))
            .Append("\" transform=\"translate(")
            .Append(Format(textOperation.X))
            .Append(' ')
            .Append(Format(textOperation.Y))
            .Append(") rotate(")
            .Append(Format(textOperation.Rotation))
            .Append(") scale(")
            .Append(Format(textOperation.ScaleX))
            .Append(' ')
            .Append(Format(textOperation.ScaleY))
            .Append(")\">");

        if (progress < 1d)
        {
            builder.Append("<clipPath id=\"")
                .Append(BuildTextClipId(textOperation.ObjectId))
                .Append("\"><rect x=\"0\" y=\"0\" width=\"")
                .Append(Format(clipWidth))
                .Append("\" height=\"")
                .Append(Format(clipHeight))
                .Append("\"/></clipPath>");
        }

        builder.Append("<text fill=\"")
            .Append(EscapeXml(textOperation.Color))
            .Append("\" font-family=\"")
            .Append(EscapeXml(textOperation.FontFamily))
            .Append("\" font-size=\"")
            .Append(Format(textOperation.FontSize))
            .Append("\" dominant-baseline=\"hanging\"");

        if (progress < 1d)
        {
            builder.Append(" clip-path=\"url(#")
                .Append(BuildTextClipId(textOperation.ObjectId))
                .Append(")\"");
        }

        builder.Append('>')
            .Append(EscapeXml(textOperation.Content))
            .Append("</text></g>");
    }

    private static void AppendHandAssetGuidance(StringBuilder builder, HandGuidanceOverlayData overlay, HandRenderAsset handAsset)
    {
        var visual = LoadHandAssetVisual(handAsset);
        var x = overlay.X - (handAsset.TipOffset.X * HandAssetGuidanceScale);
        var y = overlay.Y - (handAsset.TipOffset.Y * HandAssetGuidanceScale);

        builder.Append("<g data-guidance=\"hand\" data-guidance-object=\"")
            .Append(EscapeXml(overlay.ObjectId))
            .Append("\" data-guidance-progress=\"")
            .Append(Format(overlay.Progress))
            .Append("\" data-guidance-asset=\"")
            .Append(EscapeXml(handAsset.Id))
            .Append("\" data-guidance-renderer=\"asset\" opacity=\"0.98\" transform=\"translate(")
            .Append(Format(x))
            .Append(' ')
            .Append(Format(y))
            .Append(") rotate(")
            .Append(Format(overlay.AngleDegrees))
            .Append(") scale(")
            .Append(Format(HandAssetGuidanceScale))
            .Append(' ')
            .Append(Format(HandAssetGuidanceScale))
            .Append(")\">")
            .Append("<image href=\"")
            .Append(visual.DataUri)
            .Append("\" width=\"")
            .Append(Format(visual.Width))
            .Append("\" height=\"")
            .Append(Format(visual.Height))
            .Append("\" preserveAspectRatio=\"xMidYMid meet\"/>")
            .Append("</g>");
    }

    private static bool TryGetHandRenderAsset(RenderFrameRequest request, out HandRenderAsset handAsset)
    {
        handAsset = request.HandAssets
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry => entry.Value)
            .FirstOrDefault() ?? new HandRenderAsset();

        return !string.IsNullOrWhiteSpace(handAsset.Id);
    }

    private static HandAssetVisualData LoadHandAssetVisual(HandRenderAsset handAsset)
    {
        if (!File.Exists(handAsset.SourcePath))
        {
            throw new FileNotFoundException($"Missing hand asset '{handAsset.Id}' at '{handAsset.SourcePath}'.", handAsset.SourcePath);
        }

        var bytes = File.ReadAllBytes(handAsset.SourcePath);
        var extension = Path.GetExtension(handAsset.SourcePath).ToLowerInvariant();
        var mimeType = ResolveHandAssetMimeType(extension);
        var width = 24d;
        var height = 24d;

        if (string.Equals(extension, ".svg", StringComparison.Ordinal))
        {
            try
            {
                var document = XDocument.Parse(File.ReadAllText(handAsset.SourcePath), LoadOptions.PreserveWhitespace);
                var root = document.Root;
                if (root is null || !string.Equals(root.Name.LocalName, "svg", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Hand asset '{handAsset.Id}' must be a valid SVG root element.");
                }

                ResolveSvgDimensions(root, ref width, ref height);
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"Hand asset '{handAsset.Id}' is malformed: {exception.Message}");
            }
        }

        return new HandAssetVisualData($"data:{mimeType};base64,{Convert.ToBase64String(bytes)}", width, height);
    }

    private static string ResolveHandAssetMimeType(string extension)
    {
        return extension switch
        {
            ".svg" => "image/svg+xml",
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            _ => throw new InvalidOperationException($"Unsupported hand asset format '{extension}'.")
        };
    }

    private static void ResolveSvgDimensions(XElement root, ref double width, ref double height)
    {
        if (TryParseSvgLength(root.Attribute("width")?.Value, out var parsedWidth))
        {
            width = parsedWidth;
        }

        if (TryParseSvgLength(root.Attribute("height")?.Value, out var parsedHeight))
        {
            height = parsedHeight;
        }

        var viewBox = root.Attribute("viewBox")?.Value;
        if (!string.IsNullOrWhiteSpace(viewBox))
        {
            var tokens = viewBox.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length == 4
                && double.TryParse(tokens[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var viewBoxWidth)
                && double.TryParse(tokens[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var viewBoxHeight))
            {
                width = width <= 0 ? viewBoxWidth : width;
                height = height <= 0 ? viewBoxHeight : height;
            }
        }
    }

    private static string BuildTextClipId(string objectId)
    {
        return $"text-clip-{EscapeXml(objectId)}";
    }

    private static double ResolveTextRenderWidth(TextOperationData textOperation)
    {
        if (textOperation.Width > 0)
        {
            return textOperation.Width;
        }

        return Math.Max(textOperation.FontSize, textOperation.Content.Length * textOperation.FontSize * 0.62d);
    }

    private static double ResolveTextRenderHeight(TextOperationData textOperation)
    {
        if (textOperation.Height > 0)
        {
            return textOperation.Height;
        }

        return Math.Max(textOperation.FontSize * 1.4d, textOperation.FontSize);
    }
}




