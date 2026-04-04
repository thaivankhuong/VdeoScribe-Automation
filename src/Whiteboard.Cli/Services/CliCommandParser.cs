using System;
using System.Globalization;
using Whiteboard.Cli.Models;

namespace Whiteboard.Cli.Services;

public enum CliCommandMode
{
    Run,
    Batch,
    TemplateValidate,
    TemplateInstantiate
}

public sealed record CliCommandParseResult
{
    public CliCommandMode Mode { get; init; }
    public CliRunRequest? RunRequest { get; init; }
    public CliBatchRunRequest? BatchRequest { get; init; }
    public CliTemplateValidateRequest? TemplateValidateRequest { get; init; }
    public CliTemplateInstantiateRequest? TemplateInstantiateRequest { get; init; }
}

public sealed class CliCommandParser
{
    private const string DefaultCatalogPath = ".planning/templates/index.json";

    public CliCommandParseResult Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (args.Length == 0)
        {
            throw new ArgumentException("No CLI arguments were provided.", nameof(args));
        }

        if (IsTemplateMode(args))
        {
            return ParseTemplate(args);
        }

        return IsBatchMode(args)
            ? ParseBatch(args)
            : ParseRun(args);
    }

    private static bool IsBatchMode(string[] args)
    {
        return args[0].Equals("batch", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTemplateMode(string[] args)
    {
        return args[0].Equals("template", StringComparison.OrdinalIgnoreCase);
    }

    private static CliCommandParseResult ParseTemplate(string[] args)
    {
        if (args.Length < 2)
        {
            throw new ArgumentException("Template mode requires either 'validate' or 'instantiate'.");
        }

        return args[1].ToLowerInvariant() switch
        {
            "validate" => ParseTemplateValidate(args),
            "instantiate" => ParseTemplateInstantiate(args),
            _ => throw new ArgumentException($"Unknown template command '{args[1]}'. Use 'validate' or 'instantiate'.")
        };
    }

    private static CliCommandParseResult ParseRun(string[] args)
    {
        string? specPath = null;
        string? outputPath = null;
        var frameIndex = 0;
        var startIndex = args[0].Equals("run", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

        for (var i = startIndex; i < args.Length; i++)
        {
            var arg = args[i];

            switch (arg)
            {
                case "--spec":
                    specPath = ReadRequiredValue(args, ref i, arg);
                    break;
                case "--output":
                    outputPath = ReadRequiredValue(args, ref i, arg);
                    break;
                case "--frame-index":
                    var value = ReadRequiredValue(args, ref i, arg);
                    if (!int.TryParse(value, out frameIndex))
                    {
                        throw new ArgumentException("'--frame-index' must be a valid integer.");
                    }

                    if (frameIndex < 0)
                    {
                        frameIndex = 0;
                    }

                    break;
                default:
                    throw new ArgumentException($"Unknown argument '{arg}'. Use --help for usage.");
            }
        }

        if (string.IsNullOrWhiteSpace(specPath))
        {
            throw new ArgumentException("'--spec' is required.");
        }

        return new CliCommandParseResult
        {
            Mode = CliCommandMode.Run,
            RunRequest = new CliRunRequest
            {
                SpecPath = specPath,
                OutputPath = outputPath,
                FrameIndex = frameIndex
            }
        };
    }

    private static CliCommandParseResult ParseBatch(string[] args)
    {
        string? manifestPath = null;
        string? summaryOutputPath = null;

        for (var i = 1; i < args.Length; i++)
        {
            var arg = args[i];

            switch (arg)
            {
                case "--manifest":
                    manifestPath = ReadRequiredValue(args, ref i, arg);
                    break;
                case "--summary-output":
                    summaryOutputPath = ReadRequiredValue(args, ref i, arg);
                    break;
                default:
                    throw new ArgumentException($"Unknown argument '{arg}'. Use --help for usage.");
            }
        }

        if (string.IsNullOrWhiteSpace(manifestPath))
        {
            throw new ArgumentException("'--manifest' is required for batch mode.");
        }

        if (string.IsNullOrWhiteSpace(summaryOutputPath))
        {
            throw new ArgumentException("'--summary-output' is required for batch mode.");
        }

        return new CliCommandParseResult
        {
            Mode = CliCommandMode.Batch,
            BatchRequest = new CliBatchRunRequest
            {
                ManifestPath = manifestPath,
                SummaryOutputPath = summaryOutputPath
            }
        };
    }

    private static CliCommandParseResult ParseTemplateValidate(string[] args)
    {
        string? templateId = null;
        string? catalogPath = DefaultCatalogPath;
        string? slotValuesPath = null;

        for (var i = 2; i < args.Length; i++)
        {
            var arg = args[i];

            switch (arg)
            {
                case "--template":
                    templateId = ReadRequiredValue(args, ref i, arg);
                    break;
                case "--catalog":
                    catalogPath = ReadRequiredValue(args, ref i, arg);
                    break;
                case "--slots":
                    slotValuesPath = ReadRequiredValue(args, ref i, arg);
                    break;
                default:
                    throw new ArgumentException($"Unknown argument '{arg}'. Use --help for usage.");
            }
        }

        if (string.IsNullOrWhiteSpace(templateId))
        {
            throw new ArgumentException("'--template' is required for template validate.");
        }

        return new CliCommandParseResult
        {
            Mode = CliCommandMode.TemplateValidate,
            TemplateValidateRequest = new CliTemplateValidateRequest
            {
                TemplateId = templateId,
                CatalogPath = catalogPath,
                SlotValuesPath = slotValuesPath
            }
        };
    }

    private static CliCommandParseResult ParseTemplateInstantiate(string[] args)
    {
        string? templateId = null;
        string? catalogPath = DefaultCatalogPath;
        string? slotValuesPath = null;
        string? outputPath = null;
        string? instanceId = null;
        double timeOffsetSeconds = 0;
        int layerOffset = 0;

        for (var i = 2; i < args.Length; i++)
        {
            var arg = args[i];

            switch (arg)
            {
                case "--template":
                    templateId = ReadRequiredValue(args, ref i, arg);
                    break;
                case "--catalog":
                    catalogPath = ReadRequiredValue(args, ref i, arg);
                    break;
                case "--slots":
                    slotValuesPath = ReadRequiredValue(args, ref i, arg);
                    break;
                case "--output":
                    outputPath = ReadRequiredValue(args, ref i, arg);
                    break;
                case "--instance-id":
                    instanceId = ReadRequiredValue(args, ref i, arg);
                    break;
                case "--time-offset-seconds":
                    if (!double.TryParse(ReadRequiredValue(args, ref i, arg), NumberStyles.Float, CultureInfo.InvariantCulture, out timeOffsetSeconds))
                    {
                        throw new ArgumentException("'--time-offset-seconds' must be a valid number.");
                    }

                    break;
                case "--layer-offset":
                    if (!int.TryParse(ReadRequiredValue(args, ref i, arg), out layerOffset))
                    {
                        throw new ArgumentException("'--layer-offset' must be a valid integer.");
                    }

                    break;
                default:
                    throw new ArgumentException($"Unknown argument '{arg}'. Use --help for usage.");
            }
        }

        if (string.IsNullOrWhiteSpace(templateId))
        {
            throw new ArgumentException("'--template' is required for template instantiate.");
        }

        if (string.IsNullOrWhiteSpace(slotValuesPath))
        {
            throw new ArgumentException("'--slots' is required for template instantiate.");
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("'--output' is required for template instantiate.");
        }

        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException("'--instance-id' is required for template instantiate.");
        }

        return new CliCommandParseResult
        {
            Mode = CliCommandMode.TemplateInstantiate,
            TemplateInstantiateRequest = new CliTemplateInstantiateRequest
            {
                TemplateId = templateId,
                CatalogPath = catalogPath,
                SlotValuesPath = slotValuesPath,
                OutputPath = outputPath,
                InstanceId = instanceId,
                TimeOffsetSeconds = timeOffsetSeconds,
                LayerOffset = layerOffset
            }
        };
    }

    private static string ReadRequiredValue(string[] args, ref int index, string option)
    {
        var valueIndex = index + 1;

        if (valueIndex >= args.Length)
        {
            throw new ArgumentException($"Missing value for '{option}'.");
        }

        index = valueIndex;
        return args[valueIndex];
    }
}