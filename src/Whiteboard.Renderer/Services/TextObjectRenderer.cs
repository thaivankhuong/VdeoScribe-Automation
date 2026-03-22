using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Whiteboard.Core.Enums;
using Whiteboard.Engine.Models;
using Whiteboard.Renderer.Contracts;
using Whiteboard.Renderer.Models;

namespace Whiteboard.Renderer.Services;

public sealed class TextObjectRenderer : IObjectRenderer
{
    public bool CanRender(ResolvedObjectState objectState)
    {
        return objectState.Type == SceneObjectType.Text;
    }

    public void RenderObject(ResolvedObjectState objectState, RenderFrameRequest request, IRenderSurface surface)
    {
        if (string.IsNullOrWhiteSpace(objectState.TextContent))
        {
            throw new InvalidOperationException($"Text object '{objectState.SceneObjectId}' does not declare text content.");
        }

        var fontAssetId = "inline";
        var fontFamily = "Segoe UI";
        var colorHex = "#111111";
        if (!string.IsNullOrWhiteSpace(objectState.AssetRefId))
        {
            if (!request.FontAssets.TryGetValue(objectState.AssetRefId, out var fontAsset))
            {
                throw new InvalidOperationException($"Missing font asset '{objectState.AssetRefId}' for text object '{objectState.SceneObjectId}'.");
            }

            fontAssetId = fontAsset.Id;
            fontFamily = string.IsNullOrWhiteSpace(fontAsset.FamilyName) ? fontFamily : fontAsset.FamilyName;
            colorHex = string.IsNullOrWhiteSpace(fontAsset.ColorHex) ? colorHex : fontAsset.ColorHex;
        }

        var transform = objectState.Transform;
        var fontSize = transform.Size.Height <= 0
            ? 24d
            : transform.Size.Height;
        var progress = objectState.DrawPathCount > 0
            ? Math.Clamp(objectState.DrawProgress, 0d, 1d)
            : objectState.IsVisible ? 1d : 0d;
        var isActive = objectState.DrawPathCount > 0
            && objectState.ActiveDrawPathIndex >= 0
            && progress > 0d
            && progress < 1d;
        var orderingKey = ResolveOrderingKey(objectState);

        surface.AddOperation(string.Create(
            CultureInfo.InvariantCulture,
            $"text-object:object:{objectState.SceneObjectId}:asset:{fontAssetId}:ordering:{orderingKey}:fontFamily64:{Encode(fontFamily)}:color64:{Encode(colorHex)}:content64:{Encode(objectState.TextContent)}:active:{isActive.ToString().ToLowerInvariant()}:progress:{Format(progress)}:x:{Format(transform.Position.X)}:y:{Format(transform.Position.Y)}:width:{Format(transform.Size.Width)}:height:{Format(transform.Size.Height)}:rotation:{Format(transform.RotationDegrees)}:scaleX:{Format(transform.ScaleX)}:scaleY:{Format(transform.ScaleY)}:opacity:{Format(transform.Opacity)}:fontSize:{Format(fontSize)}"));
    }

    private static string ResolveOrderingKey(ResolvedObjectState objectState)
    {
        var orderingKey = objectState.DrawPaths
            .FirstOrDefault(path => path.IsActive)?.OrderingKey;

        if (!string.IsNullOrWhiteSpace(orderingKey))
        {
            return orderingKey;
        }

        orderingKey = objectState.DrawPaths
            .OrderBy(path => path.PathIndex)
            .Select(path => path.OrderingKey)
            .FirstOrDefault(key => !string.IsNullOrWhiteSpace(key));

        if (!string.IsNullOrWhiteSpace(orderingKey))
        {
            return orderingKey;
        }

        if (!string.IsNullOrWhiteSpace(objectState.DrawOrderingKey))
        {
            return $"{objectState.DrawOrderingKey}:path:0";
        }

        return $"{objectState.SceneObjectId}:path:0";
    }

    private static string Encode(string value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    }

    private static string Format(double value)
    {
        return value.ToString("0.000", CultureInfo.InvariantCulture);
    }
}
