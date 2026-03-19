using System;
using Whiteboard.Cli.Models;

namespace Whiteboard.Cli.Services;

public enum CliCommandMode
{
    Run,
    Batch
}

public sealed record CliCommandParseResult
{
    public CliCommandMode Mode { get; init; }
    public CliRunRequest? RunRequest { get; init; }
    public CliBatchRunRequest? BatchRequest { get; init; }
}

public sealed class CliCommandParser
{
    public CliCommandParseResult Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (args.Length == 0)
        {
            throw new ArgumentException("No CLI arguments were provided.", nameof(args));
        }

        return IsBatchMode(args)
            ? ParseBatch(args)
            : ParseRun(args);
    }

    private static bool IsBatchMode(string[] args)
    {
        return args[0].Equals("batch", StringComparison.OrdinalIgnoreCase);
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
