using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Whiteboard.Renderer.Services;

public sealed partial class FrameRenderer
{
    private static bool TryParseTextOperation(string operation, out TextOperationData textOperation)
    {
        var match = TextOperationRegex().Match(operation);
        if (!match.Success)
        {
            textOperation = default;
            return false;
        }

        textOperation = new TextOperationData(
            match.Groups["objectId"].Value,
            match.Groups["assetId"].Value,
            match.Groups["orderingKey"].Value,
            DecodeBase64Utf8(match.Groups["fontFamily"].Value),
            DecodeBase64Utf8(match.Groups["color"].Value),
            DecodeBase64Utf8(match.Groups["content"].Value),
            bool.Parse(match.Groups["isActive"].Value),
            ParseDouble(match, "progress"),
            ParseDouble(match, "x"),
            ParseDouble(match, "y"),
            ParseDouble(match, "width"),
            ParseDouble(match, "height"),
            ParseDouble(match, "rotation"),
            ParseDouble(match, "scaleX"),
            ParseDouble(match, "scaleY"),
            ParseDouble(match, "opacity"),
            ParseDouble(match, "fontSize"));
        return true;
    }

    private static string DecodeBase64Utf8(string value)
    {
        return Encoding.UTF8.GetString(Convert.FromBase64String(value));
    }

    private static bool TryParseSvgLength(string? value, out double result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var match = SvgLengthRegex().Match(value.Trim());
        return match.Success
            && double.TryParse(match.Groups["value"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }

    [GeneratedRegex("^text-object:object:(?<objectId>[^:]+):asset:(?<assetId>[^:]+):ordering:(?<orderingKey>.*):fontFamily64:(?<fontFamily>[^:]+):color64:(?<color>[^:]+):content64:(?<content>[^:]+):active:(?<isActive>true|false):progress:(?<progress>[^:]+):x:(?<x>[^:]+):y:(?<y>[^:]+):width:(?<width>[^:]+):height:(?<height>[^:]+):rotation:(?<rotation>[^:]+):scaleX:(?<scaleX>[^:]+):scaleY:(?<scaleY>[^:]+):opacity:(?<opacity>[^:]+):fontSize:(?<fontSize>[^:]+)$")]
    private static partial Regex TextOperationRegex();

    [GeneratedRegex("^(?<value>-?(?:\\d+\\.?\\d*|\\.\\d+))")]
    private static partial Regex SvgLengthRegex();

    private readonly record struct TextOperationData(
        string ObjectId,
        string AssetId,
        string OrderingKey,
        string FontFamily,
        string Color,
        string Content,
        bool IsActive,
        double Progress,
        double X,
        double Y,
        double Width,
        double Height,
        double Rotation,
        double ScaleX,
        double ScaleY,
        double Opacity,
        double FontSize);

    private readonly record struct HandAssetVisualData(string DataUri, double Width, double Height);
}


