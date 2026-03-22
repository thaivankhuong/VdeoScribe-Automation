using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Whiteboard.Renderer.Models;

namespace Whiteboard.Renderer.Services;

public sealed partial class FrameRenderer
{
    private static void AppendImageObject(StringBuilder builder, RenderFrameRequest request, ImageOperationData imageOperation)
    {
        if (!request.ImageAssets.TryGetValue(imageOperation.AssetId, out var imageAsset))
        {
            throw new InvalidOperationException($"Missing image asset '{imageOperation.AssetId}' for image object '{imageOperation.ObjectId}'.");
        }

        var visual = LoadEmbeddedImageVisual(imageAsset.Id, imageAsset.SourcePath);
        var progress = ClampProgress(imageOperation.Progress);
        var renderWidth = imageOperation.Width > 0 ? imageOperation.Width : visual.Width;
        var renderHeight = imageOperation.Height > 0 ? imageOperation.Height : visual.Height;

        builder.Append("<g data-object=\"")
            .Append(EscapeXml(imageOperation.ObjectId))
            .Append("\" data-asset=\"")
            .Append(EscapeXml(imageOperation.AssetId))
            .Append("\" data-progress=\"")
            .Append(Format(progress))
            .Append("\" data-ordering=\"")
            .Append(EscapeXml(imageOperation.OrderingKey))
            .Append("\" data-active=\"")
            .Append(imageOperation.IsActive.ToString().ToLowerInvariant())
            .Append("\" opacity=\"")
            .Append(Format(imageOperation.Opacity))
            .Append("\" transform=\"translate(")
            .Append(Format(imageOperation.X))
            .Append(' ')
            .Append(Format(imageOperation.Y))
            .Append(") rotate(")
            .Append(Format(imageOperation.Rotation))
            .Append(") scale(")
            .Append(Format(imageOperation.ScaleX))
            .Append(' ')
            .Append(Format(imageOperation.ScaleY))
            .Append(")\">");

        if (progress < 1d)
        {
            builder.Append("<clipPath id=\"")
                .Append(BuildImageClipId(imageOperation.ObjectId))
                .Append("\"><rect x=\"0\" y=\"0\" width=\"")
                .Append(Format(renderWidth * progress))
                .Append("\" height=\"")
                .Append(Format(renderHeight))
                .Append("\"/></clipPath>");
        }

        builder.Append("<image href=\"")
            .Append(visual.DataUri)
            .Append("\" width=\"")
            .Append(Format(renderWidth))
            .Append("\" height=\"")
            .Append(Format(renderHeight))
            .Append("\" preserveAspectRatio=\"none\"");

        if (progress < 1d)
        {
            builder.Append(" clip-path=\"url(#")
                .Append(BuildImageClipId(imageOperation.ObjectId))
                .Append(")\"");
        }

        builder.Append("/></g>");
    }

    private static EmbeddedImageVisual LoadEmbeddedImageVisual(string assetId, string sourcePath)
    {
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException($"Missing image asset '{assetId}' at '{sourcePath}'.", sourcePath);
        }

        var bytes = File.ReadAllBytes(sourcePath);
        var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        var mimeType = ResolveEmbeddedImageMimeType(extension);
        return new EmbeddedImageVisual($"data:{mimeType};base64,{Convert.ToBase64String(bytes)}", 1d, 1d);
    }

    private static string ResolveEmbeddedImageMimeType(string extension)
    {
        return extension switch
        {
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".svg" => "image/svg+xml",
            _ => throw new InvalidOperationException($"Unsupported image asset format '{extension}'.")
        };
    }

    private static string BuildImageClipId(string objectId)
    {
        return $"image-clip-{EscapeXml(objectId)}";
    }

    private sealed record EmbeddedImageVisual(string DataUri, double Width, double Height);

    private sealed record ImageOperationData(
        string ObjectId,
        string AssetId,
        string OrderingKey,
        bool IsActive,
        double Progress,
        double X,
        double Y,
        double Width,
        double Height,
        double Rotation,
        double ScaleX,
        double ScaleY,
        double Opacity);

    private static bool TryParseImageOperation(string operation, out ImageOperationData imageOperation)
    {
        var match = ImageOperationRegex().Match(operation);
        if (!match.Success)
        {
            imageOperation = default!;
            return false;
        }

        imageOperation = new ImageOperationData(
            match.Groups["objectId"].Value,
            match.Groups["assetId"].Value,
            match.Groups["orderingKey"].Value,
            bool.Parse(match.Groups["isActive"].Value),
            double.Parse(match.Groups["progress"].Value, CultureInfo.InvariantCulture),
            double.Parse(match.Groups["x"].Value, CultureInfo.InvariantCulture),
            double.Parse(match.Groups["y"].Value, CultureInfo.InvariantCulture),
            double.Parse(match.Groups["width"].Value, CultureInfo.InvariantCulture),
            double.Parse(match.Groups["height"].Value, CultureInfo.InvariantCulture),
            double.Parse(match.Groups["rotation"].Value, CultureInfo.InvariantCulture),
            double.Parse(match.Groups["scaleX"].Value, CultureInfo.InvariantCulture),
            double.Parse(match.Groups["scaleY"].Value, CultureInfo.InvariantCulture),
            double.Parse(match.Groups["opacity"].Value, CultureInfo.InvariantCulture));
        return true;
    }

    [GeneratedRegex("^image-object:object:(?<objectId>[^:]+):asset:(?<assetId>[^:]+):ordering:(?<orderingKey>.*):active:(?<isActive>true|false):progress:(?<progress>[^:]+):x:(?<x>[^:]+):y:(?<y>[^:]+):width:(?<width>[^:]+):height:(?<height>[^:]+):rotation:(?<rotation>[^:]+):scaleX:(?<scaleX>[^:]+):scaleY:(?<scaleY>[^:]+):opacity:(?<opacity>[^:]+)$")]
    private static partial Regex ImageOperationRegex();
}


