using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Whiteboard.Renderer.Contracts;
using Whiteboard.Renderer.Models;

namespace Whiteboard.Renderer.Services;

public sealed partial class FrameRenderer : IFrameRenderer
{
    private const string HandGuidancePathData = "M 0 0 L -4 2 L -10 14 L -6 15 L -1 7 L 2 19 L 6 18 L 4 7 L 9 16 L 12 14 L 7 5 L 13 11 L 15 8 L 8 1 L 12 -6 L 9 -8 L 3 -2 L 0 0 Z";

    private readonly ISceneRenderer _sceneRenderer;

    public FrameRenderer(ISceneRenderer? sceneRenderer = null)
    {
        _sceneRenderer = sceneRenderer ?? new SceneRenderer();
    }

    public RenderFrameResult Render(RenderFrameRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.FrameState);

        var surface = new InMemoryRenderSurface();
        surface.AddOperation(BuildCameraOperation(request));

        try
        {
            foreach (var scene in request.FrameState.Scenes)
            {
                _sceneRenderer.RenderScene(scene, request, surface);
            }

            var objectCount = request.FrameState.Scenes.Sum(s => s.Objects.Count);
            var artifact = BuildArtifact(request, surface.Operations);

            return new RenderFrameResult
            {
                FrameIndex = request.FrameState.FrameContext.FrameIndex,
                Success = true,
                Message = "Rendered deterministic SVG frame operations and artifact payload.",
                SceneCount = request.FrameState.Scenes.Count,
                ObjectCount = objectCount,
                Operations = surface.Operations,
                Artifact = artifact
            };
        }
        catch (FileNotFoundException exception)
        {
            return CreateFailureResult(request, surface, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return CreateFailureResult(request, surface, exception.Message);
        }
    }

    private static RenderFrameResult CreateFailureResult(
        RenderFrameRequest request,
        InMemoryRenderSurface surface,
        string message)
    {
        var objectCount = request.FrameState.Scenes.Sum(s => s.Objects.Count);

        return new RenderFrameResult
        {
            FrameIndex = request.FrameState.FrameContext.FrameIndex,
            Success = false,
            Message = message,
            SceneCount = request.FrameState.Scenes.Count,
            ObjectCount = objectCount,
            Operations = surface.Operations
        };
    }

    private static RenderFrameArtifact BuildArtifact(RenderFrameRequest request, IReadOnlyList<string> operations)
    {
        var camera = TryParseCameraOperation(
            operations.FirstOrDefault(operation => operation.StartsWith("camera:", StringComparison.Ordinal)),
            request,
            out var parsedCamera)
            ? parsedCamera
            : new CameraOperationData(
                request.FrameState.Camera.Position.X,
                request.FrameState.Camera.Position.Y,
                request.FrameState.Camera.Zoom,
                request.SurfaceSize.Width,
                request.SurfaceSize.Height,
                request.FrameState.Camera.Interpolation.ToString().ToLowerInvariant());

        var guidanceOverlay = request.EnableHandGuidanceOverlay
            ? ResolveHandGuidanceOverlay(operations)
            : null;

        var builder = new StringBuilder();
        builder.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"")
            .Append(camera.SurfaceWidth)
            .Append("\" height=\"")
            .Append(camera.SurfaceHeight)
            .Append("\" viewBox=\"0 0 ")
            .Append(camera.SurfaceWidth)
            .Append(' ')
            .Append(camera.SurfaceHeight)
            .Append("\">")
            .Append("<rect width=\"100%\" height=\"100%\" fill=\"#FFFFFF\"/>")
            .Append("<g data-camera-x=\"")
            .Append(Format(camera.X))
            .Append("\" data-camera-y=\"")
            .Append(Format(camera.Y))
            .Append("\" data-camera-zoom=\"")
            .Append(Format(camera.Zoom))
            .Append("\" data-camera-interpolation=\"")
            .Append(EscapeXml(camera.Interpolation))
            .Append("\" transform=\"translate(")
            .Append(Format(-camera.X))
            .Append(' ')
            .Append(Format(-camera.Y))
            .Append(") scale(")
            .Append(Format(camera.Zoom))
            .Append(")\">");

        var errorIndex = 0;
        foreach (var operation in operations)
        {
            if (TryParsePathOperation(operation, out var pathOperation))
            {
                AppendPathObject(builder, pathOperation);
                continue;
            }

            if (TryParseTextOperation(operation, out var textOperation))
            {
                AppendTextObject(builder, textOperation);
                continue;
            }

            if (TryParseImageOperation(operation, out var imageOperation))
            {
                AppendImageObject(builder, request, imageOperation);
                continue;
            }

            if (TryParseObjectError(operation, out var errorOperation))
            {
                errorIndex++;
                builder.Append("<text x=\"12\" y=\"")
                    .Append(24 + (errorIndex * 18))
                    .Append("\" fill=\"#C62828\" font-size=\"12\" font-family=\"monospace\">")
                    .Append(EscapeXml($"{errorOperation.ObjectId}: {errorOperation.Reason}"))
                    .Append("</text>");
            }
        }

        if (guidanceOverlay is HandGuidanceOverlayData overlay)
        {
            AppendResolvedHandGuidance(builder, request, overlay);
        }

        builder.Append("</g></svg>");

        return new RenderFrameArtifact
        {
            Format = "svg",
            FileExtension = ".svg",
            ContentType = "image/svg+xml",
            Payload = Encoding.UTF8.GetBytes(builder.ToString())
        };
    }

    private static void AppendPathObject(StringBuilder builder, PathOperationData pathOperation)
    {
        var tracedProgress = ClampProgress(pathOperation.Progress);
        var renderScaleX = ResolveRenderScale(pathOperation.Width, pathOperation.AssetWidth, pathOperation.ScaleX);
        var renderScaleY = ResolveRenderScale(pathOperation.Height, pathOperation.AssetHeight, pathOperation.ScaleY);
        var strokeLineCap = string.IsNullOrWhiteSpace(pathOperation.LineCap) ? "round" : pathOperation.LineCap;
        var strokeLineJoin = string.IsNullOrWhiteSpace(pathOperation.LineJoin) ? "round" : pathOperation.LineJoin;

        builder.Append("<path data-object=\"")
            .Append(EscapeXml(pathOperation.ObjectId))
            .Append("\" data-asset=\"")
            .Append(EscapeXml(pathOperation.AssetId))
            .Append("\" data-mode=\"")
            .Append(EscapeXml(pathOperation.Mode))
            .Append("\" data-progress=\"")
            .Append(Format(pathOperation.Progress))
            .Append("\" data-ordering=\"")
            .Append(EscapeXml(pathOperation.OrderingKey))
            .Append("\" data-active=\"")
            .Append(pathOperation.IsActive.ToString().ToLowerInvariant())
            .Append("\" opacity=\"")
            .Append(Format(pathOperation.Opacity))
            .Append('"');

        if (string.Equals(pathOperation.Mode, "partial", StringComparison.Ordinal))
        {
            var revealStroke = ResolvePartialStroke(pathOperation);
            var revealStrokeOpacity = ResolvePartialStrokeOpacity(pathOperation);
            var revealStrokeWidth = ResolvePartialStrokeWidth(pathOperation);

            builder.Append(" fill=\"none\" stroke=\"")
                .Append(EscapeXml(revealStroke))
                .Append("\" stroke-width=\"")
                .Append(Format(revealStrokeWidth))
                .Append("\" stroke-linecap=\"")
                .Append(EscapeXml(strokeLineCap))
                .Append("\" stroke-linejoin=\"")
                .Append(EscapeXml(strokeLineJoin))
                .Append("\" stroke-opacity=\"")
                .Append(Format(revealStrokeOpacity))
                .Append("\" pathLength=\"1\" stroke-dasharray=\"")
                .Append(Format(tracedProgress))
                .Append(" 1\" stroke-dashoffset=\"0\"");
        }
        else
        {
            builder.Append(" fill=\"")
                .Append(EscapeXml(pathOperation.Fill))
                .Append("\" stroke=\"")
                .Append(EscapeXml(pathOperation.Stroke))
                .Append("\" fill-opacity=\"")
                .Append(Format(pathOperation.FillOpacity))
                .Append("\" stroke-opacity=\"")
                .Append(Format(pathOperation.StrokeOpacity))
                .Append("\" stroke-width=\"")
                .Append(Format(pathOperation.StrokeWidth))
                .Append("\" stroke-linecap=\"")
                .Append(EscapeXml(strokeLineCap))
                .Append("\" stroke-linejoin=\"")
                .Append(EscapeXml(strokeLineJoin))
                .Append('"');
        }

        builder.Append(" transform=\"translate(")
            .Append(Format(pathOperation.X))
            .Append(' ')
            .Append(Format(pathOperation.Y))
            .Append(") rotate(")
            .Append(Format(pathOperation.Rotation))
            .Append(") scale(")
            .Append(Format(renderScaleX))
            .Append(' ')
            .Append(Format(renderScaleY))
            .Append(")\" d=\"")
            .Append(EscapeXml(pathOperation.PathData))
            .Append("\"/>");
    }

    private static double ResolveRenderScale(double requestedSize, double assetSize, double baseScale)
    {
        var effectiveAssetSize = assetSize <= 0 ? 1d : assetSize;
        var sizeScale = requestedSize > 0 ? requestedSize / effectiveAssetSize : 1d;
        return sizeScale * baseScale;
    }

    private static string ResolvePartialStroke(PathOperationData pathOperation)
    {
        if (!string.Equals(pathOperation.Stroke, "none", StringComparison.OrdinalIgnoreCase))
        {
            return pathOperation.Stroke;
        }

        if (!string.Equals(pathOperation.Fill, "none", StringComparison.OrdinalIgnoreCase))
        {
            return pathOperation.Fill;
        }

        return "#111111";
    }

    private static double ResolvePartialStrokeOpacity(PathOperationData pathOperation)
    {
        if (!string.Equals(pathOperation.Stroke, "none", StringComparison.OrdinalIgnoreCase))
        {
            return pathOperation.StrokeOpacity;
        }

        if (!string.Equals(pathOperation.Fill, "none", StringComparison.OrdinalIgnoreCase))
        {
            return pathOperation.FillOpacity;
        }

        return 1d;
    }

    private static double ResolvePartialStrokeWidth(PathOperationData pathOperation)
    {
        if (!string.Equals(pathOperation.Stroke, "none", StringComparison.OrdinalIgnoreCase)
            && pathOperation.StrokeWidth > 0)
        {
            return pathOperation.StrokeWidth;
        }

        return Math.Max(pathOperation.StrokeWidth, 2d);
    }
    private static void AppendHandGuidance(StringBuilder builder, HandGuidanceOverlayData overlay)
    {
        builder.Append("<g data-guidance=\"hand\" data-guidance-object=\"")
            .Append(EscapeXml(overlay.ObjectId))
            .Append("\" data-guidance-progress=\"")
            .Append(Format(overlay.Progress))
            .Append("\" data-guidance-renderer=\"fallback\" transform=\"translate(")
            .Append(Format(overlay.X))
            .Append(' ')
            .Append(Format(overlay.Y))
            .Append(") rotate(")
            .Append(Format(overlay.AngleDegrees))
            .Append(")\">")
            .Append("<path fill=\"#F4D5B1\" stroke=\"#111111\" stroke-width=\"1.2\" stroke-linejoin=\"round\" d=\"")
            .Append(HandGuidancePathData)
            .Append("\"/>")
            .Append("<circle cx=\"0\" cy=\"0\" r=\"1.2\" fill=\"#111111\" opacity=\"0.18\"/>")
            .Append("</g>");
    }

    private static HandGuidanceOverlayData? ResolveHandGuidanceOverlay(IReadOnlyList<string> operations)
    {
        HandGuidanceOverlayData? selectedOverlay = null;

        for (var index = 0; index < operations.Count; index++)
        {
            var operation = operations[index];

            if (TryParsePathOperation(operation, out var pathOperation)
                && TryBuildHandGuidanceOverlay(pathOperation, out var overlay))
            {
                selectedOverlay = SelectPreferredHandGuidanceOverlay(selectedOverlay, overlay with { OperationIndex = index });
                continue;
            }

            if (TryParseTextOperation(operation, out var textOperation)
                && TryBuildHandGuidanceOverlay(textOperation, out var parsedTextOverlay))
            {
                selectedOverlay = SelectPreferredHandGuidanceOverlay(selectedOverlay, parsedTextOverlay with { OperationIndex = index });
                continue;
            }

            if (TryParseImageOperation(operation, out var imageOperation)
                && TryBuildHandGuidanceOverlay(imageOperation, out var parsedImageOverlay))
            {
                selectedOverlay = SelectPreferredHandGuidanceOverlay(selectedOverlay, parsedImageOverlay with { OperationIndex = index });
            }
        }

        return selectedOverlay;
    }

    private static HandGuidanceOverlayData SelectPreferredHandGuidanceOverlay(
        HandGuidanceOverlayData? selectedOverlay,
        HandGuidanceOverlayData candidate)
    {
        if (selectedOverlay is null)
        {
            return candidate;
        }

        var orderingComparison = CompareOrderingKeys(candidate.OrderingKey, selectedOverlay.Value.OrderingKey);
        if (orderingComparison < 0)
        {
            return candidate;
        }

        if (orderingComparison > 0)
        {
            return selectedOverlay.Value;
        }

        return candidate.OperationIndex < selectedOverlay.Value.OperationIndex
            ? candidate
            : selectedOverlay.Value;
    }

    private static int CompareOrderingKeys(string left, string right)
    {
        var hasLeft = !string.IsNullOrWhiteSpace(left);
        var hasRight = !string.IsNullOrWhiteSpace(right);
        if (!hasLeft && !hasRight)
        {
            return 0;
        }

        if (!hasLeft)
        {
            return 1;
        }

        if (!hasRight)
        {
            return -1;
        }

        var leftTokens = left.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var rightTokens = right.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var length = Math.Min(leftTokens.Length, rightTokens.Length);

        for (var index = 0; index < length; index++)
        {
            if (int.TryParse(leftTokens[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out var leftNumber)
                && int.TryParse(rightTokens[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out var rightNumber))
            {
                var numberComparison = leftNumber.CompareTo(rightNumber);
                if (numberComparison != 0)
                {
                    return numberComparison;
                }

                continue;
            }

            var tokenComparison = StringComparer.Ordinal.Compare(leftTokens[index], rightTokens[index]);
            if (tokenComparison != 0)
            {
                return tokenComparison;
            }
        }

        return leftTokens.Length.CompareTo(rightTokens.Length);
    }

    private static bool TryBuildHandGuidanceOverlay(PathOperationData pathOperation, out HandGuidanceOverlayData overlay)
    {
        overlay = default;

        if (!pathOperation.IsActive
            || !string.Equals(pathOperation.Mode, "partial", StringComparison.Ordinal))
        {
            return false;
        }

        var progress = ClampProgress(pathOperation.Progress);
        if (progress <= 0 || progress >= 1)
        {
            return false;
        }

        if (!TryResolveHandGuidancePose(pathOperation, progress, out var pose))
        {
            return false;
        }

        overlay = new HandGuidanceOverlayData(pathOperation.ObjectId, progress, pose.X, pose.Y, pose.AngleDegrees, pathOperation.OrderingKey, -1);
        return true;
    }

    private static bool TryBuildHandGuidanceOverlay(TextOperationData textOperation, out HandGuidanceOverlayData overlay)
    {
        overlay = default;

        var progress = ClampProgress(textOperation.Progress);
        if (!textOperation.IsActive || progress <= 0d || progress >= 1d)
        {
            return false;
        }

        var localX = ResolveTextRenderWidth(textOperation) * progress;
        var localY = ResolveTextRenderHeight(textOperation) * TextHandBaselineFactor;
        var worldPoint = TransformPoint(
            textOperation.X,
            textOperation.Y,
            textOperation.Rotation,
            textOperation.ScaleX,
            textOperation.ScaleY,
            localX,
            localY);

        overlay = new HandGuidanceOverlayData(
            textOperation.ObjectId,
            progress,
            worldPoint.X,
            worldPoint.Y,
            textOperation.Rotation + (textOperation.ScaleX < 0 ? 180d : 0d),
            textOperation.OrderingKey,
            -1);
        return true;
    }

    private static bool TryBuildHandGuidanceOverlay(ImageOperationData imageOperation, out HandGuidanceOverlayData overlay)
    {
        overlay = default;

        var progress = ClampProgress(imageOperation.Progress);
        if (!imageOperation.IsActive || progress <= 0d || progress >= 1d)
        {
            return false;
        }

        var renderWidth = imageOperation.Width > 0d ? imageOperation.Width : 1d;
        var renderHeight = imageOperation.Height > 0d ? imageOperation.Height : 1d;
        var aspectRatio = renderHeight <= 0d ? 1d : renderWidth / renderHeight;

        var localX = renderWidth * progress;
        var localYFactor = aspectRatio >= 1.6d
            ? 0.5d + (Math.Sin(progress * Math.PI) * 0.06d)
            : 0.16d + (0.68d * progress);
        var localY = renderHeight * ClampProgress(localYFactor);
        var worldPoint = TransformPoint(
            imageOperation.X,
            imageOperation.Y,
            imageOperation.Rotation,
            imageOperation.ScaleX,
            imageOperation.ScaleY,
            localX,
            localY);

        var sweepAngle = aspectRatio >= 1.6d
            ? (-10d + (progress * 20d))
            : 18d;

        overlay = new HandGuidanceOverlayData(
            imageOperation.ObjectId,
            progress,
            worldPoint.X,
            worldPoint.Y,
            imageOperation.Rotation + sweepAngle,
            imageOperation.OrderingKey,
            -1);
        return true;
    }

    private static bool TryResolveHandGuidancePose(PathOperationData pathOperation, double progress, out HandGuidancePose pose)
    {
        pose = default;

        if (!TryBuildPathSegments(pathOperation.PathData, out var segments) || segments.Count == 0)
        {
            return false;
        }

        var totalLength = segments.Sum(segment => segment.Length);
        if (totalLength <= 0)
        {
            return false;
        }

        var targetDistance = totalLength * progress;
        var traversedLength = 0d;
        var selectedSegment = segments[^1];
        var localProgress = 1d;

        foreach (var segment in segments)
        {
            if (targetDistance <= traversedLength + segment.Length)
            {
                selectedSegment = segment;
                localProgress = segment.Length <= 0
                    ? 0
                    : (targetDistance - traversedLength) / segment.Length;
                break;
            }

            traversedLength += segment.Length;
        }

        localProgress = ClampProgress(localProgress);
        var localX = selectedSegment.StartX + ((selectedSegment.EndX - selectedSegment.StartX) * localProgress);
        var localY = selectedSegment.StartY + ((selectedSegment.EndY - selectedSegment.StartY) * localProgress);
        var worldPoint = TransformPoint(pathOperation, localX, localY);

        var directionX = selectedSegment.EndX - selectedSegment.StartX;
        var directionY = selectedSegment.EndY - selectedSegment.StartY;
        var renderScaleX = ResolveRenderScale(pathOperation.Width, pathOperation.AssetWidth, pathOperation.ScaleX);
        var renderScaleY = ResolveRenderScale(pathOperation.Height, pathOperation.AssetHeight, pathOperation.ScaleY);
        var transformedDirectionX = directionX * renderScaleX;
        var transformedDirectionY = directionY * renderScaleY;
        var localAngleDegrees = Math.Abs(transformedDirectionX) < 0.0001 && Math.Abs(transformedDirectionY) < 0.0001
            ? 0
            : Math.Atan2(transformedDirectionY, transformedDirectionX) * (180d / Math.PI);

        pose = new HandGuidancePose(
            worldPoint.X,
            worldPoint.Y,
            pathOperation.Rotation + localAngleDegrees);
        return true;
    }

    private static (double X, double Y) TransformPoint(PathOperationData pathOperation, double localX, double localY)
    {
        return TransformPoint(
            pathOperation.X,
            pathOperation.Y,
            pathOperation.Rotation,
            ResolveRenderScale(pathOperation.Width, pathOperation.AssetWidth, pathOperation.ScaleX),
            ResolveRenderScale(pathOperation.Height, pathOperation.AssetHeight, pathOperation.ScaleY),
            localX,
            localY);
    }

    private static (double X, double Y) TransformPoint(
        double x,
        double y,
        double rotation,
        double scaleX,
        double scaleY,
        double localX,
        double localY)
    {
        var scaledX = localX * scaleX;
        var scaledY = localY * scaleY;
        var radians = rotation * (Math.PI / 180d);
        var rotatedX = (scaledX * Math.Cos(radians)) - (scaledY * Math.Sin(radians));
        var rotatedY = (scaledX * Math.Sin(radians)) + (scaledY * Math.Cos(radians));
        return (x + rotatedX, y + rotatedY);
    }

    private static bool TryBuildPathSegments(string pathData, out IReadOnlyList<PathLineSegment> segments)
    {
        segments = [];
        if (string.IsNullOrWhiteSpace(pathData))
        {
            return false;
        }

        var tokens = PathTokenRegex()
            .Matches(pathData)
            .Select(match => match.Value)
            .ToArray();
        if (tokens.Length == 0)
        {
            return false;
        }

        var builtSegments = new List<PathLineSegment>();
        var index = 0;
        var currentX = 0d;
        var currentY = 0d;
        var startX = 0d;
        var startY = 0d;
        char? command = null;

        while (index < tokens.Length)
        {
            if (TryGetCommandToken(tokens[index], out var explicitCommand))
            {
                command = explicitCommand;
                index++;
            }
            else if (command is null)
            {
                return false;
            }

            switch (command)
            {
                case 'M':
                case 'm':
                    if (!TryReadCoordinatePair(tokens, ref index, out var moveX, out var moveY))
                    {
                        segments = builtSegments;
                        return builtSegments.Count > 0;
                    }

                    if (command == 'm')
                    {
                        moveX += currentX;
                        moveY += currentY;
                    }

                    currentX = moveX;
                    currentY = moveY;
                    startX = moveX;
                    startY = moveY;

                    while (TryReadCoordinatePair(tokens, ref index, out var lineX, out var lineY))
                    {
                        if (command == 'm')
                        {
                            lineX += currentX;
                            lineY += currentY;
                        }

                        AddSegment(builtSegments, currentX, currentY, lineX, lineY);
                        currentX = lineX;
                        currentY = lineY;
                    }
                    break;

                case 'L':
                case 'l':
                    while (TryReadCoordinatePair(tokens, ref index, out var nextX, out var nextY))
                    {
                        if (command == 'l')
                        {
                            nextX += currentX;
                            nextY += currentY;
                        }

                        AddSegment(builtSegments, currentX, currentY, nextX, nextY);
                        currentX = nextX;
                        currentY = nextY;
                    }
                    break;

                case 'H':
                case 'h':
                    while (TryReadDouble(tokens, ref index, out var horizontalX))
                    {
                        var resolvedX = command == 'h' ? currentX + horizontalX : horizontalX;
                        AddSegment(builtSegments, currentX, currentY, resolvedX, currentY);
                        currentX = resolvedX;
                    }
                    break;

                case 'V':
                case 'v':
                    while (TryReadDouble(tokens, ref index, out var verticalY))
                    {
                        var resolvedY = command == 'v' ? currentY + verticalY : verticalY;
                        AddSegment(builtSegments, currentX, currentY, currentX, resolvedY);
                        currentY = resolvedY;
                    }
                    break;

                case 'C':
                case 'c':
                case 'S':
                case 's':
                case 'Q':
                case 'q':
                case 'T':
                case 't':
                case 'A':
                case 'a':
                    var parameterCount = ResolveApproximateParameterCount(command.Value);
                    while (TryReadParameters(tokens, ref index, parameterCount, out var parameters))
                    {
                        var endX = parameters[^2];
                        var endY = parameters[^1];
                        if (char.IsLower(command.Value))
                        {
                            endX += currentX;
                            endY += currentY;
                        }

                        AddSegment(builtSegments, currentX, currentY, endX, endY);
                        currentX = endX;
                        currentY = endY;
                    }
                    break;

                case 'Z':
                case 'z':
                    AddSegment(builtSegments, currentX, currentY, startX, startY);
                    currentX = startX;
                    currentY = startY;
                    break;

                default:
                    segments = builtSegments;
                    return builtSegments.Count > 0;
            }
        }

        segments = builtSegments;
        return builtSegments.Count > 0;
    }

    private static int ResolveApproximateParameterCount(char command)
    {
        return char.ToUpperInvariant(command) switch
        {
            'C' => 6,
            'S' => 4,
            'Q' => 4,
            'T' => 2,
            'A' => 7,
            _ => 0
        };
    }

    private static bool TryReadParameters(string[] tokens, ref int index, int count, out double[] parameters)
    {
        parameters = new double[count];
        var startIndex = index;

        for (var parameterIndex = 0; parameterIndex < count; parameterIndex++)
        {
            if (!TryReadDouble(tokens, ref index, out var value))
            {
                index = startIndex;
                parameters = [];
                return false;
            }

            parameters[parameterIndex] = value;
        }

        return true;
    }

    private static bool TryReadCoordinatePair(string[] tokens, ref int index, out double x, out double y)
    {
        var startIndex = index;
        if (!TryReadDouble(tokens, ref index, out x) || !TryReadDouble(tokens, ref index, out y))
        {
            index = startIndex;
            x = 0;
            y = 0;
            return false;
        }

        return true;
    }

    private static bool TryReadDouble(string[] tokens, ref int index, out double value)
    {
        value = 0;
        if (index >= tokens.Length || TryGetCommandToken(tokens[index], out _))
        {
            return false;
        }

        if (!double.TryParse(tokens[index], NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            return false;
        }

        index++;
        return true;
    }

    private static bool TryGetCommandToken(string token, out char command)
    {
        command = default;
        return token.Length == 1 && char.IsLetter(token[0]) && (command = token[0]) != default;
    }

    private static void AddSegment(List<PathLineSegment> segments, double startX, double startY, double endX, double endY)
    {
        var length = Math.Sqrt(Math.Pow(endX - startX, 2) + Math.Pow(endY - startY, 2));
        if (length <= 0)
        {
            return;
        }

        segments.Add(new PathLineSegment(startX, startY, endX, endY, length));
    }

    private static string BuildCameraOperation(RenderFrameRequest request)
    {
        var camera = request.FrameState.Camera;

        return string.Create(
            CultureInfo.InvariantCulture,
            $"camera:x:{Format(camera.Position.X)}:y:{Format(camera.Position.Y)}:zoom:{Format(camera.Zoom)}:surfaceWidth:{request.SurfaceSize.Width}:surfaceHeight:{request.SurfaceSize.Height}:interpolation:{camera.Interpolation.ToString().ToLowerInvariant()}");
    }

    private static bool TryParseCameraOperation(
        string? operation,
        RenderFrameRequest request,
        out CameraOperationData camera)
    {
        if (string.IsNullOrWhiteSpace(operation))
        {
            camera = new CameraOperationData(
                request.FrameState.Camera.Position.X,
                request.FrameState.Camera.Position.Y,
                request.FrameState.Camera.Zoom,
                request.SurfaceSize.Width,
                request.SurfaceSize.Height,
                request.FrameState.Camera.Interpolation.ToString().ToLowerInvariant());
            return false;
        }

        var match = CameraOperationRegex().Match(operation);
        if (!match.Success)
        {
            camera = new CameraOperationData(
                request.FrameState.Camera.Position.X,
                request.FrameState.Camera.Position.Y,
                request.FrameState.Camera.Zoom,
                request.SurfaceSize.Width,
                request.SurfaceSize.Height,
                request.FrameState.Camera.Interpolation.ToString().ToLowerInvariant());
            return false;
        }

        camera = new CameraOperationData(
            ParseDouble(match, "x"),
            ParseDouble(match, "y"),
            ParseDouble(match, "zoom"),
            ParseInt(match, "surfaceWidth"),
            ParseInt(match, "surfaceHeight"),
            match.Groups["interpolation"].Value);
        return true;
    }

    private static bool TryParsePathOperation(string operation, out PathOperationData pathOperation)
    {
        var match = PathOperationRegex().Match(operation);
        if (!match.Success)
        {
            pathOperation = default;
            return false;
        }

        pathOperation = new PathOperationData(
            match.Groups["mode"].Value,
            match.Groups["objectId"].Value,
            match.Groups["assetId"].Value,
            ParseInt(match, "pathIndex"),
            match.Groups["orderingKey"].Value,
            bool.Parse(match.Groups["isActive"].Value),
            ParseDouble(match, "progress"),
            ParseDouble(match, "x"),
            ParseDouble(match, "y"),
            ParseDouble(match, "width"),
            ParseDouble(match, "height"),
            ParseDouble(match, "assetWidth"),
            ParseDouble(match, "assetHeight"),
            ParseDouble(match, "rotation"),
            ParseDouble(match, "scaleX"),
            ParseDouble(match, "scaleY"),
            ParseDouble(match, "opacity"),
            DecodeBase64Utf8(match.Groups["fill"].Value),
            DecodeBase64Utf8(match.Groups["stroke"].Value),
            ParseDouble(match, "strokeWidth"),
            DecodeBase64Utf8(match.Groups["lineCap"].Value),
            DecodeBase64Utf8(match.Groups["lineJoin"].Value),
            ParseDouble(match, "fillOpacity"),
            ParseDouble(match, "strokeOpacity"),
            match.Groups["pathData"].Value);
        return true;
    }

    private static bool TryParseObjectError(string operation, out ObjectErrorData error)
    {
        var match = ObjectErrorRegex().Match(operation);
        if (!match.Success)
        {
            error = default;
            return false;
        }

        error = new ObjectErrorData(
            match.Groups["objectId"].Value,
            match.Groups["assetId"].Value,
            match.Groups["reason"].Value);
        return true;
    }

    private static int ParseInt(Match match, string groupName)
    {
        return int.Parse(match.Groups[groupName].Value, CultureInfo.InvariantCulture);
    }

    private static double ParseDouble(Match match, string groupName)
    {
        return double.Parse(match.Groups[groupName].Value, CultureInfo.InvariantCulture);
    }

    private static string EscapeXml(string value)
    {
        return value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);
    }

    private static double ClampProgress(double value)
    {
        return Math.Max(0, Math.Min(1, value));
    }

    private static string Format(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    [GeneratedRegex("^camera:x:(?<x>[^:]+):y:(?<y>[^:]+):zoom:(?<zoom>[^:]+):surfaceWidth:(?<surfaceWidth>\\d+):surfaceHeight:(?<surfaceHeight>\\d+):interpolation:(?<interpolation>[^:]+)$")]
    private static partial Regex CameraOperationRegex();

    [GeneratedRegex("^svg-path:mode:(?<mode>[^:]+):object:(?<objectId>[^:]+):asset:(?<assetId>[^:]+):path:(?<pathIndex>\\d+):ordering:(?<orderingKey>.*):active:(?<isActive>true|false):progress:(?<progress>[^:]+):x:(?<x>[^:]+):y:(?<y>[^:]+):width:(?<width>[^:]+):height:(?<height>[^:]+):assetWidth:(?<assetWidth>[^:]+):assetHeight:(?<assetHeight>[^:]+):rotation:(?<rotation>[^:]+):scaleX:(?<scaleX>[^:]+):scaleY:(?<scaleY>[^:]+):opacity:(?<opacity>[^:]+):fill64:(?<fill>[^:]*):stroke64:(?<stroke>[^:]*):strokeWidth:(?<strokeWidth>[^:]+):lineCap64:(?<lineCap>[^:]*):lineJoin64:(?<lineJoin>[^:]*):fillOpacity:(?<fillOpacity>[^:]+):strokeOpacity:(?<strokeOpacity>[^:]+):d:(?<pathData>.*)$")]
    private static partial Regex PathOperationRegex();

    [GeneratedRegex("-?(?:\\d+\\.?\\d*|\\.\\d+)(?:[eE][+-]?\\d+)?|[A-Za-z]")]
    private static partial Regex PathTokenRegex();

    [GeneratedRegex("^svg-object-error:object:(?<objectId>[^:]+):asset:(?<assetId>[^:]+):reason:(?<reason>[^:]+)$")]
    private static partial Regex ObjectErrorRegex();

    private readonly record struct CameraOperationData(
        double X,
        double Y,
        double Zoom,
        int SurfaceWidth,
        int SurfaceHeight,
        string Interpolation);

    private readonly record struct PathOperationData(
        string Mode,
        string ObjectId,
        string AssetId,
        int PathIndex,
        string OrderingKey,
        bool IsActive,
        double Progress,
        double X,
        double Y,
        double Width,
        double Height,
        double AssetWidth,
        double AssetHeight,
        double Rotation,
        double ScaleX,
        double ScaleY,
        double Opacity,
        string Fill,
        string Stroke,
        double StrokeWidth,
        string LineCap,
        string LineJoin,
        double FillOpacity,
        double StrokeOpacity,
        string PathData);

    private readonly record struct ObjectErrorData(string ObjectId, string AssetId, string Reason);
    private readonly record struct PathLineSegment(double StartX, double StartY, double EndX, double EndY, double Length);
    private readonly record struct HandGuidancePose(double X, double Y, double AngleDegrees);
    private readonly record struct HandGuidanceOverlayData(
        string ObjectId,
        double Progress,
        double X,
        double Y,
        double AngleDegrees,
        string OrderingKey,
        int OperationIndex);
}









