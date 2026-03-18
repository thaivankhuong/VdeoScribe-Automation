using System;
using System.Collections.Generic;
using System.IO;
using Whiteboard.Cli.Models;
using Whiteboard.Cli.Services;
using Xunit;

namespace Whiteboard.Cli.Tests;

public sealed class PipelineOrchestratorIntegrationTests
{
    [Fact]
    public void PipelineOrchestrator_CanRunEndToEnd_WithJsonSpec()
    {
        var specPath = CreatePrimarySpecFile();

        try
        {
            var orchestrator = new PipelineOrchestrator();
            var request = new CliRunRequest
            {
                SpecPath = specPath,
                OutputPath = "out/video.mp4",
                FrameIndex = 0
            };

            var result = orchestrator.Run(request);

            Assert.True(result.Success);
            Assert.Equal(specPath, result.SpecPath);
            Assert.Equal(0, result.FrameIndex);
            Assert.Equal(1, result.SceneCount);
            Assert.Equal(2, result.ObjectCount);
            Assert.Equal(1, result.ExportedFrameCount);
            Assert.Equal("out/video.mp4", result.OutputPath);
            Assert.False(string.IsNullOrWhiteSpace(result.ExportStatus));
            Assert.False(string.IsNullOrWhiteSpace(result.DeterministicKey));
        }
        finally
        {
            DeleteSpecFile(specPath);
        }
    }

    [Fact]
    public void PipelineOrchestrator_WithSameSpec_ProducesDeterministicStructure()
    {
        var specPath = CreatePrimarySpecFile();

        try
        {
            var orchestrator = new PipelineOrchestrator();
            var request = new CliRunRequest
            {
                SpecPath = specPath,
                OutputPath = "out/video.mp4",
                FrameIndex = 0
            };

            var first = orchestrator.Run(request);
            var second = orchestrator.Run(request);

            Assert.Equal(first.Success, second.Success);
            Assert.Equal(first.SpecPath, second.SpecPath);
            Assert.Equal(first.FrameIndex, second.FrameIndex);
            Assert.Equal(first.SceneCount, second.SceneCount);
            Assert.Equal(first.ObjectCount, second.ObjectCount);
            Assert.Equal(first.OperationCount, second.OperationCount);
            Assert.Equal(first.ExportedFrameCount, second.ExportedFrameCount);
            Assert.Equal(first.OutputPath, second.OutputPath);
            Assert.Equal(first.ExportStatus, second.ExportStatus);
            Assert.Equal(first.DeterministicKey, second.DeterministicKey);
            Assert.Equal(first.Operations, second.Operations);
        }
        finally
        {
            DeleteSpecFile(specPath);
        }
    }

    [Fact]
    public void PipelineOrchestrator_WithEquivalentSpecsUsingDifferentSourceOrdering_ProducesEquivalentDeterministicOutput()
    {
        var firstSpecPath = CreatePrimarySpecFile();
        var secondSpecPath = CreateReorderedEquivalentSpecFile();

        try
        {
            var orchestrator = new PipelineOrchestrator();
            var firstRequest = new CliRunRequest
            {
                SpecPath = firstSpecPath,
                OutputPath = "out/video.mp4",
                FrameIndex = 1
            };
            var secondRequest = new CliRunRequest
            {
                SpecPath = secondSpecPath,
                OutputPath = "out/video.mp4",
                FrameIndex = 1
            };

            var first = orchestrator.Run(firstRequest);
            var second = orchestrator.Run(secondRequest);

            Assert.True(first.Success);
            Assert.True(second.Success);
            Assert.Equal(first.FrameIndex, second.FrameIndex);
            Assert.Equal(first.SceneCount, second.SceneCount);
            Assert.Equal(first.ObjectCount, second.ObjectCount);
            Assert.Equal(first.OperationCount, second.OperationCount);
            Assert.Equal(first.Operations, second.Operations);
            Assert.Equal(first.DeterministicKey, second.DeterministicKey);
        }
        finally
        {
            DeleteSpecFile(firstSpecPath);
            DeleteSpecFile(secondSpecPath);
        }
    }

    private static string CreatePrimarySpecFile()
    {
        return CreateSpecFile(ReadFixtureJson("primary-spec.json"));
    }

    private static string CreateReorderedEquivalentSpecFile()
    {
        return CreateSpecFile(ReadFixtureJson("equivalent-reordered-spec.json"));
    }

    private static string CreateSpecFile(string json)
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), "whiteboard-cli-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directoryPath);

        var specPath = Path.Combine(directoryPath, "project.json");
        File.WriteAllText(specPath, json);
        return specPath;
    }

    private static string ReadFixtureJson(string fileName)
    {
        var fixturePath = ResolveFixturePath(fileName);
        return File.ReadAllText(fixturePath);
    }

    private static string ResolveFixturePath(string fileName)
    {
        var baseDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        var candidateRoots = new List<DirectoryInfo>();

        for (var current = baseDirectory; current is not null; current = current.Parent)
        {
            candidateRoots.Add(current);
        }

        foreach (var candidate in candidateRoots)
        {
            var fixturePath = Path.Combine(
                candidate.FullName,
                "tests",
                "Whiteboard.Cli.Tests",
                "Fixtures",
                "phase03-determinism",
                fileName);

            if (File.Exists(fixturePath))
            {
                return fixturePath;
            }
        }

        throw new FileNotFoundException(
            $"Fixture '{fileName}' was not found under tests/Whiteboard.Cli.Tests/Fixtures/phase03-determinism.");
    }

    private static void DeleteSpecFile(string specPath)
    {
        if (!File.Exists(specPath))
        {
            return;
        }

        var directoryPath = Path.GetDirectoryName(specPath);
        File.Delete(specPath);

        if (!string.IsNullOrWhiteSpace(directoryPath) && Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }
}