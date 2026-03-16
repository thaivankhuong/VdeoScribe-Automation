using System;
using Whiteboard.Cli.Models;
using Whiteboard.Cli.Services;

namespace Whiteboard.Cli;

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0 || HasHelpFlag(args))
            {
                PrintUsage();
                return args.Length == 0 ? 1 : 0;
            }

            var request = ParseArguments(args);
            var orchestrator = new PipelineOrchestrator();
            var result = orchestrator.Run(request);

            Console.WriteLine($"Success: {result.Success}");
            Console.WriteLine($"Message: {result.Message}");
            Console.WriteLine($"SpecPath: {result.SpecPath}");
            Console.WriteLine($"FrameIndex: {result.FrameIndex}");
            Console.WriteLine($"SceneCount: {result.SceneCount}");
            Console.WriteLine($"ObjectCount: {result.ObjectCount}");
            Console.WriteLine($"OperationCount: {result.OperationCount}");
            Console.WriteLine($"ExportedFrameCount: {result.ExportedFrameCount}");
            Console.WriteLine($"OutputPath: {result.OutputPath}");
            Console.WriteLine($"ExportStatus: {result.ExportStatus}");
            Console.WriteLine($"DeterministicKey: {result.DeterministicKey}");

            return result.Success ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"CLI error: {ex.Message}");
            return 1;
        }
    }

    private static CliRunRequest ParseArguments(string[] args)
    {
        string? specPath = null;
        string? outputPath = null;
        var frameIndex = 0;

        for (var i = 0; i < args.Length; i++)
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

        return new CliRunRequest
        {
            SpecPath = specPath,
            OutputPath = outputPath,
            FrameIndex = frameIndex
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

    private static bool HasHelpFlag(string[] args)
    {
        foreach (var arg in args)
        {
            if (arg is "--help" or "-h")
            {
                return true;
            }
        }

        return false;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage: whiteboard-cli --spec <path> [--output <path>] [--frame-index <int>]");
        Console.WriteLine("Step 12 scope: deterministic Core -> Engine -> Renderer -> Export -> CLI placeholder pipeline.");
    }
}
