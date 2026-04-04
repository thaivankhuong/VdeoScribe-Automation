using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Whiteboard.Core.Assets;
using Whiteboard.Core.Enums;
using Whiteboard.Core.Models;
using Whiteboard.Core.Scene;
using Whiteboard.Core.Templates;
using Whiteboard.Core.Timeline;
using Whiteboard.Core.Validation;

namespace Whiteboard.Core.Compilation;

public sealed class ScriptCompiler : IScriptCompiler
{
    private const string DefaultTemplateCatalogPath = ".planning/templates/index.json";
    private const string DefaultMappingCatalogPath = ".planning/script-compiler/template-mappings.json";
    private const string DefaultGovernedLibraryPath = ".planning/script-compiler/governed-library.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    private readonly IScriptMappingPipeline _scriptMappingPipeline;
    private readonly ITemplateComposer _templateComposer;
    private readonly ISpecProcessingPipeline _specProcessingPipeline;

    public ScriptCompiler(
        IScriptMappingPipeline? scriptMappingPipeline = null,
        ITemplateComposer? templateComposer = null,
        ISpecProcessingPipeline? specProcessingPipeline = null)
    {
        _scriptMappingPipeline = scriptMappingPipeline ?? new ScriptMappingPipeline();
        _templateComposer = templateComposer ?? new TemplateComposer();
        _specProcessingPipeline = specProcessingPipeline ?? new SpecProcessingPipeline();
    }

    public ScriptCompileResult Compile(
        string json,
        string sourcePath,
        string templateCatalogPath,
        string mappingCatalogPath,
        string governedLibraryPath)
    {
        var compilationPlan = _scriptMappingPipeline.Process(
            json,
            sourcePath,
            templateCatalogPath,
            mappingCatalogPath,
            governedLibraryPath);

        var document = compilationPlan.Document;
        if (!compilationPlan.Success || document is null)
        {
            return CreateFailureResult(compilationPlan, document, compilationPlan.Issues);
        }

        var resolvedGovernedLibraryPath = ResolveInputPath(sourcePath, governedLibraryPath, DefaultGovernedLibraryPath);
        if (!TryReadGovernedLibrary(resolvedGovernedLibraryPath, out var governedLibrary, out var governedLibraryIssues))
        {
            return CreateFailureResult(compilationPlan, document, governedLibraryIssues);
        }

        var compositionIssues = new List<ValidationIssue>();
        var composedSections = new List<ComposedSectionResult>();

        foreach (var sectionPlan in compilationPlan.Sections)
        {
            var compositionResult = _templateComposer.Compose(sectionPlan.InstantiationRequest);
            if (!compositionResult.Success)
            {
                compositionIssues.AddRange(compositionResult.Issues);
                continue;
            }

            composedSections.Add(new ComposedSectionResult(sectionPlan, compositionResult));
        }

        if (compositionIssues.Count > 0)
        {
            return CreateFailureResult(compilationPlan, document, ValidationIssueOrdering.Sort(compositionIssues));
        }

        var project = BuildProject(document, governedLibrary!, composedSections);
        var projectJson = JsonSerializer.Serialize(project, SerializerOptions);
        var specProcessingResult = _specProcessingPipeline.Process(projectJson, sourcePath);
        if (!specProcessingResult.IsSuccess || specProcessingResult.Project is null)
        {
            return CreateFailureResult(compilationPlan, document, specProcessingResult.Issues);
        }

        var canonicalJson = specProcessingResult.Project.CanonicalJson;
        return new ScriptCompileResult
        {
            Success = true,
            ScriptId = document.ScriptId,
            ProjectName = document.ProjectName,
            TemplateCount = compilationPlan.Sections
                .Select(section => section.TemplateId)
                .Distinct(StringComparer.Ordinal)
                .Count(),
            SectionCount = compilationPlan.Sections.Count,
            Project = specProcessingResult.Project.Project,
            CanonicalJson = canonicalJson,
            DeterministicKey = BuildDeterministicKey(canonicalJson),
            CompilationPlan = compilationPlan,
            Issues = []
        };
    }

    private static VideoProject BuildProject(
        ScriptCompilationDocument document,
        ScriptGovernedLibrary governedLibrary,
        IReadOnlyList<ComposedSectionResult> composedSections)
    {
        var sceneEntries = composedSections
            .SelectMany(
                section => section.Result.Fragment.Scenes.Select(
                    (scene, index) => new OrderedScene(
                        section.Plan.Section.Order,
                        section.Plan.Section.SectionId,
                        index,
                        CloneScene(scene))))
            .OrderBy(entry => entry.Order)
            .ThenBy(entry => entry.SectionId, StringComparer.Ordinal)
            .ThenBy(entry => entry.Index)
            .ToList();

        var eventEntries = composedSections
            .SelectMany(section => section.Result.Fragment.TimelineEvents.Select(CloneTimelineEvent))
            .OrderBy(@event => @event.SceneId, StringComparer.Ordinal)
            .ThenBy(@event => @event.StartSeconds)
            .ThenBy(@event => @event.Id, StringComparer.Ordinal)
            .ToList();

        var referencedAssetIds = sceneEntries
            .SelectMany(entry => entry.Scene.Objects)
            .Where(sceneObject => !string.IsNullOrWhiteSpace(sceneObject.AssetRefId))
            .Select(sceneObject => sceneObject.AssetRefId!)
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        var referencedEffectProfileIds = eventEntries
            .SelectMany(@event => @event.Parameters)
            .Where(parameter => string.Equals(parameter.Key, "effectProfileId", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(parameter.Value))
            .Select(parameter => parameter.Value)
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        return new VideoProject
        {
            Meta = new ProjectMeta
            {
                ProjectId = document.ScriptId,
                Name = document.ProjectName,
                AssetRegistrySnapshotId = document.AssetRegistrySnapshotId
            },
            Output = new OutputSpec
            {
                Width = document.Output.Width,
                Height = document.Output.Height,
                FrameRate = document.Output.FrameRate,
                BackgroundColorHex = document.Output.BackgroundColorHex
            },
            Assets = new AssetCollection
            {
                RegistrySnapshot = new AssetRegistrySnapshot
                {
                    RegistryId = governedLibrary.RegistryId,
                    SnapshotId = governedLibrary.SnapshotId,
                    SnapshotVersion = governedLibrary.SnapshotVersion
                },
                SvgAssets = governedLibrary.Assets
                    .Where(asset =>
                        referencedAssetIds.Contains(asset.AssetId) &&
                        string.Equals(asset.AssetType, "svg", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(asset.Status, "active", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(asset => asset.AssetId, StringComparer.Ordinal)
                    .Select(asset => new SvgAsset
                    {
                        Id = asset.AssetId,
                        Name = string.IsNullOrWhiteSpace(asset.Name) ? asset.AssetId : asset.Name,
                        SourcePath = asset.SourcePath ?? string.Empty,
                        Type = AssetType.Svg
                    })
                    .ToList()
            },
            Scenes = sceneEntries
                .Select(entry => entry.Scene)
                .ToList(),
            Timeline = new TimelineDefinition
            {
                Events = eventEntries,
                EffectProfiles = governedLibrary.EffectProfiles
                    .Where(profile =>
                        referencedEffectProfileIds.Contains(profile.EffectProfileId) &&
                        string.Equals(profile.Status, "active", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(profile => profile.EffectProfileId, StringComparer.Ordinal)
                    .Select(profile => new EffectProfile
                    {
                        Id = profile.EffectProfileId,
                        ActionType = ParseActionType(profile.ActionType),
                        MinDurationSeconds = profile.MinDurationSeconds,
                        MaxDurationSeconds = profile.MaxDurationSeconds,
                        ParameterBounds = profile.ParameterBounds
                            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                            .ToDictionary(
                                pair => pair.Key,
                                pair => new EffectParameterBound
                                {
                                    Key = string.IsNullOrWhiteSpace(pair.Value.Key) ? pair.Key : pair.Value.Key,
                                    MinValue = pair.Value.MinValue,
                                    MaxValue = pair.Value.MaxValue
                                },
                                StringComparer.Ordinal)
                    })
                    .ToList()
            }
        };
    }

    private static SceneDefinition CloneScene(SceneDefinition scene)
    {
        return new SceneDefinition
        {
            Id = scene.Id,
            Name = scene.Name,
            DurationSeconds = scene.DurationSeconds,
            Objects = scene.Objects.Select(CloneSceneObject).ToList()
        };
    }

    private static SceneObject CloneSceneObject(SceneObject sceneObject)
    {
        return new SceneObject
        {
            Id = sceneObject.Id,
            Name = sceneObject.Name,
            Type = sceneObject.Type,
            AssetRefId = sceneObject.AssetRefId,
            TextContent = sceneObject.TextContent,
            Layer = sceneObject.Layer,
            IsVisible = sceneObject.IsVisible,
            Transform = sceneObject.Transform with { }
        };
    }

    private static TimelineEvent CloneTimelineEvent(TimelineEvent timelineEvent)
    {
        return new TimelineEvent
        {
            Id = timelineEvent.Id,
            SceneId = timelineEvent.SceneId,
            SceneObjectId = timelineEvent.SceneObjectId,
            ActionType = timelineEvent.ActionType,
            StartSeconds = timelineEvent.StartSeconds,
            DurationSeconds = timelineEvent.DurationSeconds,
            Easing = timelineEvent.Easing,
            Parameters = timelineEvent.Parameters
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal)
        };
    }

    private static TimelineActionType ParseActionType(string rawValue)
    {
        return Enum.TryParse<TimelineActionType>(rawValue, ignoreCase: true, out var actionType)
            ? actionType
            : TimelineActionType.Draw;
    }

    private static bool TryReadGovernedLibrary(
        string path,
        out ScriptGovernedLibrary? governedLibrary,
        out IReadOnlyList<ValidationIssue> issues)
    {
        var issueList = new List<ValidationIssue>();
        governedLibrary = null;

        if (!File.Exists(path))
        {
            issueList.Add(new ValidationIssue(
                ValidationGate.Semantic,
                "$.governedLibraryPath",
                ValidationSeverity.Error,
                "script.contract.required",
                $"Required file '{path}' was not found."));
            issues = issueList;
            return false;
        }

        try
        {
            governedLibrary = JsonSerializer.Deserialize<ScriptGovernedLibrary>(File.ReadAllText(path), SerializerOptions);
        }
        catch (JsonException exception)
        {
            issueList.Add(new ValidationIssue(
                ValidationGate.Semantic,
                "$.governedLibraryPath",
                ValidationSeverity.Error,
                "script.contract.required",
                exception.Message));
            issues = issueList;
            return false;
        }

        if (governedLibrary is null)
        {
            issueList.Add(new ValidationIssue(
                ValidationGate.Semantic,
                "$.governedLibraryPath",
                ValidationSeverity.Error,
                "script.contract.required",
                $"Required file '{path}' could not be deserialized."));
            issues = issueList;
            return false;
        }

        issues = [];
        return true;
    }

    private static ScriptCompileResult CreateFailureResult(
        ScriptCompilationPlan compilationPlan,
        ScriptCompilationDocument? document,
        IReadOnlyList<ValidationIssue> issues)
    {
        var orderedIssues = ValidationIssueOrdering.Sort(issues);
        return new ScriptCompileResult
        {
            Success = false,
            ScriptId = document?.ScriptId ?? string.Empty,
            ProjectName = document?.ProjectName ?? string.Empty,
            TemplateCount = compilationPlan.Sections
                .Select(section => section.TemplateId)
                .Distinct(StringComparer.Ordinal)
                .Count(),
            SectionCount = compilationPlan.Sections.Count,
            CompilationPlan = compilationPlan,
            Issues = orderedIssues
        };
    }

    private static string ResolveInputPath(string sourcePath, string providedPath, string defaultPath)
    {
        var candidate = string.IsNullOrWhiteSpace(providedPath) ? defaultPath : providedPath;
        if (Path.IsPathRooted(candidate))
        {
            return Path.GetFullPath(candidate);
        }

        if (candidate.StartsWith(".planning", StringComparison.OrdinalIgnoreCase))
        {
            return Path.GetFullPath(Path.Combine(FindRepoRoot(sourcePath), candidate));
        }

        var sourceDirectory = Path.GetDirectoryName(Path.GetFullPath(sourcePath)) ?? Environment.CurrentDirectory;
        return Path.GetFullPath(Path.Combine(sourceDirectory, candidate));
    }

    private static string FindRepoRoot(string path)
    {
        var current = new DirectoryInfo(Path.GetDirectoryName(Path.GetFullPath(path)) ?? Environment.CurrentDirectory);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, ".planning")))
            {
                return current.FullName;
            }

            if (string.Equals(current.Name, ".planning", StringComparison.OrdinalIgnoreCase))
            {
                return current.Parent?.FullName ?? current.FullName;
            }

            current = current.Parent;
        }

        return Environment.CurrentDirectory;
    }

    private static string BuildDeterministicKey(string canonicalJson)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalJson));
        return $"sha256:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    private sealed record OrderedScene(int Order, string SectionId, int Index, SceneDefinition Scene);

    private sealed record ComposedSectionResult(
        ScriptSectionCompilationPlan Plan,
        TemplateInstantiationResult Result);
}
