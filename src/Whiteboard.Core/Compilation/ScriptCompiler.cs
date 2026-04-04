using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
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

    private static readonly Regex ScriptSectionPathPattern = new(@"^\$\.sections\[(\d+)\]", RegexOptions.Compiled);
    private static readonly Regex ScenePathPattern = new(@"^\$\.scenes\[(\d+)\]", RegexOptions.Compiled);
    private static readonly Regex TimelineEventPathPattern = new(@"^\$\.timeline\.events\[(\d+)\]", RegexOptions.Compiled);

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
        var resolvedGovernedLibraryPath = ResolveInputPath(sourcePath, governedLibraryPath, DefaultGovernedLibraryPath);

        ScriptGovernedLibrary? governedLibrary = null;
        IReadOnlyList<ValidationIssue> governedLibraryIssues = [];
        if (!string.IsNullOrWhiteSpace(sourcePath))
        {
            _ = TryReadGovernedLibrary(resolvedGovernedLibraryPath, out governedLibrary, out governedLibraryIssues);
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

        var orderedScenes = composedSections
            .SelectMany(
                section => section.Result.Fragment.Scenes.Select(
                    (scene, index) => new OrderedScene(
                        section.Plan.Section.Order,
                        section.Plan.Section.SectionId,
                        section.Plan.TemplateId,
                        index,
                        CloneScene(scene))))
            .OrderBy(entry => entry.Order)
            .ThenBy(entry => entry.SectionId, StringComparer.Ordinal)
            .ThenBy(entry => entry.Index)
            .ToList();

        var orderedEvents = composedSections
            .SelectMany(section => section.Result.Fragment.TimelineEvents.Select(@event => new OrderedEvent(
                section.Plan.Section.SectionId,
                section.Plan.TemplateId,
                CloneTimelineEvent(@event))))
            .OrderBy(entry => entry.Event.SceneId, StringComparer.Ordinal)
            .ThenBy(entry => entry.Event.StartSeconds)
            .ThenBy(entry => entry.Event.Id, StringComparer.Ordinal)
            .ToList();

        VideoProject? project = null;
        string specOutputJson = string.Empty;
        string specOutputDeterministicKey = string.Empty;
        IReadOnlyList<ValidationIssue> specIssues = [];

        if (document is not null &&
            compilationPlan.Success &&
            governedLibraryIssues.Count == 0 &&
            compositionIssues.Count == 0 &&
            governedLibrary is not null)
        {
            var builtProject = BuildProject(document, governedLibrary, orderedScenes, orderedEvents);
            var projectJson = JsonSerializer.Serialize(builtProject, SerializerOptions);
            var specProcessingResult = _specProcessingPipeline.Process(projectJson, sourcePath);

            if (specProcessingResult.IsSuccess && specProcessingResult.Project is not null)
            {
                project = specProcessingResult.Project.Project;
                specOutputJson = specProcessingResult.Project.CanonicalJson;
                specOutputDeterministicKey = BuildDeterministicKey(specOutputJson);
            }
            else
            {
                specIssues = BuildSpecValidationIssues(specProcessingResult.Issues);
            }
        }

        var orderedValidationIssues = ValidationIssueOrdering.Sort(
            compilationPlan.Issues
                .Concat(governedLibraryIssues)
                .Concat(compositionIssues)
                .Concat(specIssues));

        var diagnostics = BuildDiagnostics(
            orderedValidationIssues,
            document,
            compilationPlan,
            orderedScenes,
            orderedEvents);
        var report = BuildReport(
            sourcePath,
            document,
            compilationPlan,
            composedSections,
            governedLibrary,
            orderedScenes,
            orderedEvents,
            project,
            diagnostics,
            specOutputDeterministicKey);
        var reportJson = SerializeReport(report);

        return new ScriptCompileResult
        {
            Success = project is not null && orderedValidationIssues.All(issue => issue.Severity != ValidationSeverity.Error),
            ScriptId = document?.ScriptId ?? string.Empty,
            ProjectName = document?.ProjectName ?? string.Empty,
            TemplateCount = CountTemplates(document, compilationPlan),
            SectionCount = document?.Sections.Count ?? compilationPlan.Sections.Count,
            Project = project,
            SpecOutputJson = specOutputJson,
            CanonicalJson = specOutputJson,
            DeterministicKey = string.IsNullOrWhiteSpace(specOutputDeterministicKey)
                ? BuildDeterministicKey(reportJson)
                : specOutputDeterministicKey,
            Report = report,
            Diagnostics = diagnostics,
            CompilationPlan = compilationPlan,
            Issues = orderedValidationIssues
        };
    }

    private static int CountTemplates(ScriptCompilationDocument? document, ScriptCompilationPlan compilationPlan)
    {
        return (document?.Sections ?? compilationPlan.Sections.Select(section => section.Section).ToList())
            .Select(section => section.TemplateId)
            .Where(templateId => !string.IsNullOrWhiteSpace(templateId))
            .Distinct(StringComparer.Ordinal)
            .Count();
    }

    private static IReadOnlyList<ValidationIssue> BuildSpecValidationIssues(IReadOnlyList<ValidationIssue> issues)
    {
        if (issues.Count == 0)
        {
            return
            [
                new ValidationIssue(
                    ValidationGate.Semantic,
                    "$.spec",
                    ValidationSeverity.Error,
                    "script.spec.validation.failed",
                    "Generated project spec failed validation.")
            ];
        }

        return
        [
            new ValidationIssue(
                ValidationGate.Semantic,
                "$.spec",
                ValidationSeverity.Error,
                "script.spec.validation.failed",
                "Generated project spec failed validation."),
            ..issues
        ];
    }

    private static ScriptCompileReport BuildReport(
        string sourcePath,
        ScriptCompilationDocument? document,
        ScriptCompilationPlan compilationPlan,
        IReadOnlyList<ComposedSectionResult> composedSections,
        ScriptGovernedLibrary? governedLibrary,
        IReadOnlyList<OrderedScene> orderedScenes,
        IReadOnlyList<OrderedEvent> orderedEvents,
        VideoProject? project,
        IReadOnlyList<ScriptCompileDiagnostic> diagnostics,
        string specOutputDeterministicKey)
    {
        var sectionReports = BuildSectionReports(document, compilationPlan, composedSections);
        return new ScriptCompileReport
        {
            Script = new ScriptCompileReportScript
            {
                ScriptId = document?.ScriptId ?? string.Empty,
                Version = document?.Version ?? string.Empty,
                ProjectName = document?.ProjectName ?? string.Empty,
                AssetRegistrySnapshotId = document?.AssetRegistrySnapshotId ?? string.Empty,
                SourcePath = Path.GetFullPath(sourcePath)
            },
            Templates = (document?.Sections ?? [])
                .Where(section => !string.IsNullOrWhiteSpace(section.TemplateId))
                .GroupBy(section => section.TemplateId, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => new ScriptCompileReportTemplate
                {
                    TemplateId = group.Key,
                    SectionCount = group.Count(),
                    SectionIds = group
                        .Select(section => section.SectionId)
                        .OrderBy(sectionId => sectionId, StringComparer.Ordinal)
                        .ToArray()
                })
                .ToArray(),
            Sections = sectionReports,
            GovernedResources = new ScriptCompileReportGovernedResources
            {
                RequestedSnapshotId = document?.AssetRegistrySnapshotId ?? string.Empty,
                RegistryId = governedLibrary?.RegistryId ?? string.Empty,
                SnapshotId = governedLibrary?.SnapshotId ?? string.Empty,
                SnapshotVersion = governedLibrary?.SnapshotVersion ?? string.Empty,
                AssetIds = sectionReports
                    .SelectMany(section => section.AssetIds)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(assetId => assetId, StringComparer.Ordinal)
                    .ToArray(),
                EffectProfileIds = sectionReports
                    .SelectMany(section => section.EffectProfileIds)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(effectId => effectId, StringComparer.Ordinal)
                    .ToArray()
            },
            Spec = new ScriptCompileReportSpec
            {
                Success = project is not null,
                SpecOutputGenerated = !string.IsNullOrWhiteSpace(specOutputDeterministicKey),
                SceneCount = orderedScenes.Count,
                TimelineEventCount = orderedEvents.Count,
                SvgAssetCount = project?.Assets.SvgAssets.Count ?? 0,
                EffectProfileCount = project?.Timeline.EffectProfiles.Count ?? 0,
                SpecOutputDeterministicKey = specOutputDeterministicKey
            },
            Diagnostics = diagnostics
        };
    }

    private static IReadOnlyList<ScriptCompileReportSection> BuildSectionReports(
        ScriptCompilationDocument? document,
        ScriptCompilationPlan compilationPlan,
        IReadOnlyList<ComposedSectionResult> composedSections)
    {
        if (document is null)
        {
            return [];
        }

        var planBySectionId = compilationPlan.Sections.ToDictionary(
            section => section.Section.SectionId,
            StringComparer.Ordinal);
        var composedBySectionId = composedSections.ToDictionary(
            section => section.Plan.Section.SectionId,
            StringComparer.Ordinal);

        return document.Sections
            .OrderBy(section => section.Order)
            .ThenBy(section => section.SectionId, StringComparer.Ordinal)
            .Select(section =>
            {
                planBySectionId.TryGetValue(section.SectionId, out var plan);
                composedBySectionId.TryGetValue(section.SectionId, out var composed);

                var slotBindings = plan?.SlotBindings ??
                    new Dictionary<string, string>(StringComparer.Ordinal);
                var assetIds = composed is not null
                    ? composed.Result.Fragment.Scenes
                        .SelectMany(scene => scene.Objects)
                        .Where(sceneObject => !string.IsNullOrWhiteSpace(sceneObject.AssetRefId))
                        .Select(sceneObject => sceneObject.AssetRefId!)
                        .Distinct(StringComparer.Ordinal)
                        .OrderBy(assetId => assetId, StringComparer.Ordinal)
                        .ToArray()
                    : GetFallbackIds(plan?.GovernedAssetId, section.IllustrationAssetId);
                var effectProfileIds = composed is not null
                    ? composed.Result.Fragment.TimelineEvents
                        .SelectMany(@event => @event.Parameters)
                        .Where(parameter => string.Equals(parameter.Key, "effectProfileId", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(parameter.Value))
                        .Select(parameter => parameter.Value)
                        .Distinct(StringComparer.Ordinal)
                        .OrderBy(effectId => effectId, StringComparer.Ordinal)
                        .ToArray()
                    : GetFallbackIds(plan?.GovernedEffectProfileId, section.DrawEffectProfileId);

                return new ScriptCompileReportSection
                {
                    SectionId = section.SectionId,
                    Order = section.Order,
                    TemplateId = plan?.TemplateId ?? section.TemplateId,
                    SlotBindings = slotBindings,
                    AssetIds = assetIds,
                    EffectProfileIds = effectProfileIds,
                    SceneIds = composed?.Result.Fragment.Scenes
                        .Select(scene => scene.Id)
                        .OrderBy(sceneId => sceneId, StringComparer.Ordinal)
                        .ToArray() ?? [],
                    TimelineEventIds = composed?.Result.Fragment.TimelineEvents
                        .Select(@event => @event.Id)
                        .OrderBy(eventId => eventId, StringComparer.Ordinal)
                        .ToArray() ?? []
                };
            })
            .ToArray();
    }

    private static IReadOnlyList<ScriptCompileDiagnostic> BuildDiagnostics(
        IReadOnlyList<ValidationIssue> issues,
        ScriptCompilationDocument? document,
        ScriptCompilationPlan compilationPlan,
        IReadOnlyList<OrderedScene> orderedScenes,
        IReadOnlyList<OrderedEvent> orderedEvents)
    {
        var diagnostics = issues
            .Select(issue =>
            {
                var scope = ResolveScope(issue.Path, document, compilationPlan, orderedScenes, orderedEvents);
                return new ScriptCompileDiagnostic
                {
                    Severity = issue.Severity.ToString().ToLowerInvariant(),
                    Code = issue.Code,
                    Message = issue.Message,
                    Path = issue.Path,
                    Gate = issue.Gate.ToString().ToLowerInvariant(),
                    SectionId = scope.SectionId,
                    TemplateId = scope.TemplateId
                };
            });

        return ScriptCompileDiagnostic.Sort(diagnostics);
    }

    private static DiagnosticScope ResolveScope(
        string issuePath,
        ScriptCompilationDocument? document,
        ScriptCompilationPlan compilationPlan,
        IReadOnlyList<OrderedScene> orderedScenes,
        IReadOnlyList<OrderedEvent> orderedEvents)
    {
        var sectionMatch = ScriptSectionPathPattern.Match(issuePath);
        if (sectionMatch.Success &&
            document is not null &&
            int.TryParse(sectionMatch.Groups[1].Value, out var sectionIndex) &&
            sectionIndex >= 0 &&
            sectionIndex < document.Sections.Count)
        {
            var section = document.Sections[sectionIndex];
            var plan = compilationPlan.Sections.FirstOrDefault(candidate =>
                string.Equals(candidate.Section.SectionId, section.SectionId, StringComparison.Ordinal));

            return new DiagnosticScope(section.SectionId, plan?.TemplateId ?? section.TemplateId);
        }

        var sceneMatch = ScenePathPattern.Match(issuePath);
        if (sceneMatch.Success &&
            int.TryParse(sceneMatch.Groups[1].Value, out var sceneIndex) &&
            sceneIndex >= 0 &&
            sceneIndex < orderedScenes.Count)
        {
            var scene = orderedScenes[sceneIndex];
            return new DiagnosticScope(scene.SectionId, scene.TemplateId);
        }

        var eventMatch = TimelineEventPathPattern.Match(issuePath);
        if (eventMatch.Success &&
            int.TryParse(eventMatch.Groups[1].Value, out var eventIndex) &&
            eventIndex >= 0 &&
            eventIndex < orderedEvents.Count)
        {
            var @event = orderedEvents[eventIndex];
            return new DiagnosticScope(@event.SectionId, @event.TemplateId);
        }

        return DiagnosticScope.Empty;
    }

    private static string[] GetFallbackIds(string? primaryValue, string? secondaryValue)
    {
        var value = string.IsNullOrWhiteSpace(primaryValue) ? secondaryValue : primaryValue;
        return string.IsNullOrWhiteSpace(value) ? [] : [value];
    }

    private static string SerializeReport(ScriptCompileReport report)
    {
        return JsonSerializer.Serialize(report, SerializerOptions);
    }

    private static VideoProject BuildProject(
        ScriptCompilationDocument document,
        ScriptGovernedLibrary governedLibrary,
        IReadOnlyList<OrderedScene> orderedScenes,
        IReadOnlyList<OrderedEvent> orderedEvents)
    {
        var referencedAssetIds = orderedScenes
            .SelectMany(entry => entry.Scene.Objects)
            .Where(sceneObject => !string.IsNullOrWhiteSpace(sceneObject.AssetRefId))
            .Select(sceneObject => sceneObject.AssetRefId!)
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        var referencedEffectProfileIds = orderedEvents
            .SelectMany(entry => entry.Event.Parameters)
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
            Scenes = orderedScenes
                .Select(entry => entry.Scene)
                .ToList(),
            Timeline = new TimelineDefinition
            {
                Events = orderedEvents
                    .Select(entry => entry.Event)
                    .ToList(),
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

    private sealed record OrderedScene(int Order, string SectionId, string TemplateId, int Index, SceneDefinition Scene);

    private sealed record OrderedEvent(string SectionId, string TemplateId, TimelineEvent Event);

    private sealed record ComposedSectionResult(
        ScriptSectionCompilationPlan Plan,
        TemplateInstantiationResult Result);

    private sealed record DiagnosticScope(string SectionId, string TemplateId)
    {
        public static DiagnosticScope Empty { get; } = new(string.Empty, string.Empty);
    }
}
