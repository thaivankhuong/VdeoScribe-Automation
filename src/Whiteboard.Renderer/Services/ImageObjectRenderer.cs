using System;
using System.Globalization;
using System.Linq;
using Whiteboard.Core.Enums;
using Whiteboard.Engine.Models;
using Whiteboard.Renderer.Contracts;
using Whiteboard.Renderer.Models;

namespace Whiteboard.Renderer.Services;

public sealed class ImageObjectRenderer : IObjectRenderer
{
    public bool CanRender(ResolvedObjectState objectState)
    {
        return objectState.Type == SceneObjectType.Image;
    }

    public void RenderObject(ResolvedObjectState objectState, RenderFrameRequest request, IRenderSurface surface)
    {
        if (string.IsNullOrWhiteSpace(objectState.AssetRefId))
        {
            throw new InvalidOperationException($"Image object '{objectState.SceneObjectId}' must declare an assetRefId.");
        }

        if (!request.ImageAssets.TryGetValue(objectState.AssetRefId, out var imageAsset))
        {
            throw new InvalidOperationException($"Missing image asset '{objectState.AssetRefId}' for image object '{objectState.SceneObjectId}'.");
        }

        var transform = objectState.Transform;
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
            $"image-object:object:{objectState.SceneObjectId}:asset:{imageAsset.Id}:ordering:{orderingKey}:active:{isActive.ToString().ToLowerInvariant()}:progress:{Format(progress)}:x:{Format(transform.Position.X)}:y:{Format(transform.Position.Y)}:width:{Format(transform.Size.Width)}:height:{Format(transform.Size.Height)}:rotation:{Format(transform.RotationDegrees)}:scaleX:{Format(transform.ScaleX)}:scaleY:{Format(transform.ScaleY)}:opacity:{Format(transform.Opacity)}"));
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

    private static string Format(double value)
    {
        return value.ToString("0.000", CultureInfo.InvariantCulture);
    }
}
