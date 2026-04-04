using System;
using Whiteboard.Cli.Models;
using Whiteboard.Cli.Services;
using Xunit;

namespace Whiteboard.Cli.Tests;

public sealed class CliCommandParserTests
{
    [Fact]
    public void Parse_LegacyRunMode_ParsesSpecAndClampsNegativeDebugFrameIndex()
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
    public void Parse_RunMode_LeavesFrameIndexUnsetWhenDebugOptionIsMissing()
    {
        var parser = new CliCommandParser();

        var command = parser.Parse(["run", "--spec", "project.json", "--output", "out/video.mp4"]);

        Assert.Equal(CliCommandMode.Run, command.Mode);
        Assert.NotNull(command.RunRequest);
        Assert.Equal("project.json", command.RunRequest!.SpecPath);
        Assert.Equal("out/video.mp4", command.RunRequest.OutputPath);
        Assert.Null(command.RunRequest.FrameIndex);
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

        var command = parser.Parse(["batch", "--manifest", "phase19-batch-manifest.json", "--summary-output", "phase19-summary.json"]);

        Assert.Equal(CliCommandMode.Batch, command.Mode);
        Assert.NotNull(command.BatchRequest);
        Assert.Equal("phase19-batch-manifest.json", command.BatchRequest!.ManifestPath);
        Assert.Equal("phase19-summary.json", command.BatchRequest.SummaryOutputPath);
    }

    [Fact]
    public void Parse_TemplateValidate_UsesDefaultCatalogWhenArgumentIsOmitted()
    {
        var parser = new CliCommandParser();

        var command = parser.Parse(["template", "validate", "--template", "title-card-basic", "--slots", "slots.json"]);

        Assert.Equal(CliCommandMode.TemplateValidate, command.Mode);
        Assert.NotNull(command.TemplateValidateRequest);
        Assert.Equal("title-card-basic", command.TemplateValidateRequest!.TemplateId);
        Assert.Equal(".planning/templates/index.json", command.TemplateValidateRequest.CatalogPath);
        Assert.Equal("slots.json", command.TemplateValidateRequest.SlotValuesPath);
    }

    [Fact]
    public void Parse_TemplateInstantiate_ParsesRequiredAndOptionalArguments()
    {
        var parser = new CliCommandParser();

        var command = parser.Parse([
            "template",
            "instantiate",
            "--template",
            "title-card-basic",
            "--catalog",
            "catalog.json",
            "--slots",
            "slot-values.json",
            "--output",
            "output.json",
            "--instance-id",
            "title-card-001",
            "--time-offset-seconds",
            "2.5",
            "--layer-offset",
            "4"
        ]);

        Assert.Equal(CliCommandMode.TemplateInstantiate, command.Mode);
        Assert.NotNull(command.TemplateInstantiateRequest);
        Assert.Equal("title-card-basic", command.TemplateInstantiateRequest!.TemplateId);
        Assert.Equal("catalog.json", command.TemplateInstantiateRequest.CatalogPath);
        Assert.Equal("slot-values.json", command.TemplateInstantiateRequest.SlotValuesPath);
        Assert.Equal("output.json", command.TemplateInstantiateRequest.OutputPath);
        Assert.Equal("title-card-001", command.TemplateInstantiateRequest.InstanceId);
        Assert.Equal(2.5, command.TemplateInstantiateRequest.TimeOffsetSeconds);
        Assert.Equal(4, command.TemplateInstantiateRequest.LayerOffset);
    }

    [Fact]
    public void Parse_ScriptCompile_ParsesInputSpecOutputAndReportOutput()
    {
        var parser = new CliCommandParser();

        var command = parser.Parse([
            "script",
            "compile",
            "--input",
            "script.json",
            "--spec-output",
            "compiled-spec.json",
            "--report-output",
            "compile-report.json"
        ]);

        Assert.Equal(CliCommandMode.ScriptCompile, command.Mode);
        Assert.NotNull(command.ScriptCompileRequest);
        Assert.Equal("script.json", command.ScriptCompileRequest!.InputPath);
        Assert.Equal("compiled-spec.json", command.ScriptCompileRequest.SpecOutputPath);
        Assert.Equal("compile-report.json", command.ScriptCompileRequest.ReportOutputPath);
    }

    [Fact]
    public void Parse_ScriptCompile_RequiresSpecOutput()
    {
        var parser = new CliCommandParser();

        var exception = Assert.Throws<ArgumentException>(() => parser.Parse([
            "script",
            "compile",
            "--input",
            "script.json",
            "--report-output",
            "compile-report.json"
        ]));

        Assert.Contains("--spec-output", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_ScriptCompile_RequiresReportOutput()
    {
        var parser = new CliCommandParser();

        var exception = Assert.Throws<ArgumentException>(() => parser.Parse([
            "script",
            "compile",
            "--input",
            "script.json",
            "--spec-output",
            "compiled-spec.json"
        ]));

        Assert.Contains("--report-output", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_ScriptCompile_RejectsDuplicateReportOutput()
    {
        var parser = new CliCommandParser();

        var exception = Assert.Throws<ArgumentException>(() => parser.Parse([
            "script",
            "compile",
            "--input",
            "script.json",
            "--spec-output",
            "compiled-spec.json",
            "--report-output",
            "compile-report-a.json",
            "--report-output",
            "compile-report-b.json"
        ]));

        Assert.Contains("Duplicate '--report-output'", exception.Message, StringComparison.Ordinal);
    }
}
