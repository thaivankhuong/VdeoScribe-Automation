using System;
using Whiteboard.Cli.Models;
using Whiteboard.Cli.Services;
using Xunit;

namespace Whiteboard.Cli.Tests;

public sealed class CliCommandParserTests
{
    [Fact]
    public void Parse_LegacyRunMode_ParsesSpecAndClampsNegativeFrameIndex()
    {
        var parser = new CliCommandParser();

        var command = parser.Parse(["--spec", "project.json", "--output", "out/video.mp4", "--frame-index", "-4"]);

        Assert.Equal(CliCommandMode.Run, command.Mode);
        Assert.NotNull(command.RunRequest);
        Assert.Equal("project.json", command.RunRequest!.SpecPath);
        Assert.Equal("out/video.mp4", command.RunRequest.OutputPath);
        Assert.Equal(0, command.RunRequest.FrameIndex);
    }

    [Fact]
    public void Parse_BatchMode_RequiresSummaryOutput()
    {
        var parser = new CliCommandParser();

        var exception = Assert.Throws<ArgumentException>(() => parser.Parse(["batch", "--manifest", "manifest.json"]));

        Assert.Contains("--summary-output", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_BatchMode_ParsesManifestAndSummaryOutput()
    {
        var parser = new CliCommandParser();

        var command = parser.Parse(["batch", "--manifest", "manifest.json", "--summary-output", "summary.json"]);

        Assert.Equal(CliCommandMode.Batch, command.Mode);
        Assert.NotNull(command.BatchRequest);
        Assert.Equal("manifest.json", command.BatchRequest!.ManifestPath);
        Assert.Equal("summary.json", command.BatchRequest.SummaryOutputPath);
    }
}
