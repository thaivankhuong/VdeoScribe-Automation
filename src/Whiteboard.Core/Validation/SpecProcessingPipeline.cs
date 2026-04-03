using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Whiteboard.Core.Assets;
using Whiteboard.Core.Enums;
using Whiteboard.Core.Models;
using Whiteboard.Core.Normalization;
using Whiteboard.Core.Scene;
using Whiteboard.Core.Timeline;

namespace Whiteboard.Core.Validation;

public sealed class SpecProcessingPipeline : ISpecProcessingPipeline
{
    private enum RegistrySnapshotStatus
    {
        Active = 0,
        Deprecated = 1
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    private static readonly IReadOnlyDictionary<string, RegistrySnapshotStatus> RegistrySnapshotPolicy =
        new Dictionary<string, RegistrySnapshotStatus>(StringComparer.Ordinal)
        {
            ["reg-main-2026-04"] = RegistrySnapshotStatus.Active,
            ["reg-main-2026-03"] = RegistrySnapshotStatus.Deprecated
        };

    public SpecProcessingResult Process(string json, string sourcePath)
    {
        var gateResults = new List<ValidationGateResult>();

        var contractIssues = ValidateContract(json, sourcePath);
        gateResults.Add(CreateGateResult(ValidationGate.Contract, contractIssues));
        if (contractIssues.Count > 0)
        {
            return CreateResult(gateResults, null);
        }

        VideoProject? parsedProject;
        var schemaIssues = ValidateSchema(json, out parsedProject);
        gateResults.Add(CreateGateResult(ValidationGate.Schema, schemaIssues));
        if (schemaIssues.Count > 0 || parsedProject is null)
        {
            return CreateResult(gateResults, null);
        }

        var normalizedProject = NormalizeProject(parsedProject, sourcePath);
        gateResults.Add(CreateGateResult(ValidationGate.Normalization, []));

        var semanticIssues = ValidateSemantic(normalizedProject.Project);
        gateResults.Add(CreateGateResult(ValidationGate.Semantic, semanticIssues));
        if (semanticIssues.Count > 0)
        {
            return CreateResult(gateResults, null);
        }

        var readinessIssues = ValidateReadiness(normalizedProject.Project);
        gateResults.Add(CreateGateResult(ValidationGate.Readiness, readinessIssues));
        if (readinessIssues.Count > 0)
        {
            return CreateResult(gateResults, null);
        }

        return CreateResult(gateResults, normalizedProject);
    }

    private static List<ValidationIssue> ValidateContract(string json, string sourcePath)
    {
        var issues = new List<ValidationIssue>();

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            issues.Add(new ValidationIssue(ValidationGate.Contract, "$.sourcePath", ValidationSeverity.Error, "contract.source_path.required", "Source path is required."));
        }

        if (string.IsNullOrWhiteSpace(json))
        {
            issues.Add(new ValidationIssue(ValidationGate.Contract, "$", ValidationSeverity.Error, "contract.spec.required", "Project spec JSON is required."));
        }

        return issues;
    }

    private static List<ValidationIssue> ValidateSchema(string json, out VideoProject? parsedProject)
    {
        var issues = new List<ValidationIssue>();
        parsedProject = null;

        try
        {
            parsedProject = JsonSerializer.Deserialize<VideoProject>(json, SerializerOptions);
        }
        catch (JsonException exception)
        {
            issues.Add(new ValidationIssue(ValidationGate.Schema, "$", ValidationSeverity.Error, "schema.json.invalid", exception.Message));
            return issues;
        }

        if (parsedProject is null)
        {
            issues.Add(new ValidationIssue(ValidationGate.Schema, "$", ValidationSeverity.Error, "schema.deserialize.null", "Spec JSON could not be deserialized into VideoProject."));
            return issues;
        }

        if (parsedProject.Output.Width <= 0)
        {
            issues.Add(new ValidationIssue(ValidationGate.Schema, "$.output.width", ValidationSeverity.Error, "schema.output.width.invalid", "Output width must be greater than zero."));
        }

        if (parsedProject.Output.Height <= 0)
        {
            issues.Add(new ValidationIssue(ValidationGate.Schema, "$.output.height", ValidationSeverity.Error, "schema.output.height.invalid", "Output height must be greater than zero."));
        }

        if (parsedProject.Output.FrameRate <= 0)
        {
            issues.Add(new ValidationIssue(ValidationGate.Schema, "$.output.frameRate", ValidationSeverity.Error, "schema.output.frame_rate.invalid", "Output frame rate must be greater than zero."));
        }

        for (var sceneIndex = 0; sceneIndex < parsedProject.Scenes.Count; sceneIndex++)
        {
            var scene = parsedProject.Scenes[sceneIndex];
            if (string.IsNullOrWhiteSpace(scene.Id))
            {
                issues.Add(new ValidationIssue(ValidationGate.Schema, $"$.scenes[{sceneIndex}].id", ValidationSeverity.Error, "schema.scene.id.required", "Scene id is required."));
            }

            if (scene.DurationSeconds <= 0)
            {
                issues.Add(new ValidationIssue(ValidationGate.Schema, $"$.scenes[{sceneIndex}].durationSeconds", ValidationSeverity.Error, "schema.scene.duration.invalid", "Scene duration must be greater than zero."));
            }

            for (var objectIndex = 0; objectIndex < scene.Objects.Count; objectIndex++)
            {
                var sceneObject = scene.Objects[objectIndex];
                if (string.IsNullOrWhiteSpace(sceneObject.Id))
                {
                    issues.Add(new ValidationIssue(ValidationGate.Schema, $"$.scenes[{sceneIndex}].objects[{objectIndex}].id", ValidationSeverity.Error, "schema.scene_object.id.required", "Scene object id is required."));
                }
            }
        }

        for (var eventIndex = 0; eventIndex < parsedProject.Timeline.Events.Count; eventIndex++)
        {
            var timelineEvent = parsedProject.Timeline.Events[eventIndex];
            if (string.IsNullOrWhiteSpace(timelineEvent.Id))
            {
                issues.Add(new ValidationIssue(ValidationGate.Schema, $"$.timeline.events[{eventIndex}].id", ValidationSeverity.Error, "schema.timeline_event.id.required", "Timeline event id is required."));
            }

            if (string.IsNullOrWhiteSpace(timelineEvent.SceneId))
            {
                issues.Add(new ValidationIssue(ValidationGate.Schema, $"$.timeline.events[{eventIndex}].sceneId", ValidationSeverity.Error, "schema.timeline_event.scene_id.required", "Timeline event sceneId is required."));
            }

            if (string.IsNullOrWhiteSpace(timelineEvent.SceneObjectId))
            {
                issues.Add(new ValidationIssue(ValidationGate.Schema, $"$.timeline.events[{eventIndex}].sceneObjectId", ValidationSeverity.Error, "schema.timeline_event.scene_object_id.required", "Timeline event sceneObjectId is required."));
            }

            if (timelineEvent.StartSeconds < 0)
            {
                issues.Add(new ValidationIssue(ValidationGate.Schema, $"$.timeline.events[{eventIndex}].startSeconds", ValidationSeverity.Error, "schema.timeline_event.start.invalid", "Timeline event startSeconds must be zero or greater."));
            }

            if (timelineEvent.DurationSeconds <= 0)
            {
                issues.Add(new ValidationIssue(ValidationGate.Schema, $"$.timeline.events[{eventIndex}].durationSeconds", ValidationSeverity.Error, "schema.timeline_event.duration.invalid", "Timeline event durationSeconds must be greater than zero."));
            }
        }

        var effectProfiles = parsedProject.Timeline.EffectProfiles ?? [];
        for (var profileIndex = 0; profileIndex < effectProfiles.Count; profileIndex++)
        {
            var profile = effectProfiles[profileIndex];
            if (string.IsNullOrWhiteSpace(profile.Id))
            {
                issues.Add(new ValidationIssue(ValidationGate.Schema, $"$.timeline.effectProfiles[{profileIndex}].id", ValidationSeverity.Error, "schema.effect_profile.id.required", "Effect profile id is required."));
            }

            if (profile.MinDurationSeconds < 0)
            {
                issues.Add(new ValidationIssue(ValidationGate.Schema, $"$.timeline.effectProfiles[{profileIndex}].minDurationSeconds", ValidationSeverity.Error, "schema.effect_profile.min_duration.invalid", "Effect profile minDurationSeconds must be zero or greater."));
            }

            if (profile.MaxDurationSeconds <= 0)
            {
                issues.Add(new ValidationIssue(ValidationGate.Schema, $"$.timeline.effectProfiles[{profileIndex}].maxDurationSeconds", ValidationSeverity.Error, "schema.effect_profile.max_duration.invalid", "Effect profile maxDurationSeconds must be greater than zero."));
            }

            if (profile.MaxDurationSeconds < profile.MinDurationSeconds)
            {
                issues.Add(new ValidationIssue(ValidationGate.Schema, $"$.timeline.effectProfiles[{profileIndex}]", ValidationSeverity.Error, "schema.effect_profile.duration_range.invalid", "Effect profile maxDurationSeconds must be greater than or equal to minDurationSeconds."));
            }

            foreach (var parameterBound in profile.ParameterBounds)
            {
                if (string.IsNullOrWhiteSpace(parameterBound.Key))
                {
                    issues.Add(new ValidationIssue(ValidationGate.Schema, $"$.timeline.effectProfiles[{profileIndex}].parameterBounds", ValidationSeverity.Error, "schema.effect_profile.parameter.key.required", "Effect profile parameter bound key is required."));
                    continue;
                }

                if (parameterBound.Value.MaxValue < parameterBound.Value.MinValue)
                {
                    issues.Add(new ValidationIssue(ValidationGate.Schema, $"$.timeline.effectProfiles[{profileIndex}].parameterBounds.{parameterBound.Key}", ValidationSeverity.Error, "schema.effect_profile.parameter.range.invalid", "Effect profile parameter bound maxValue must be greater than or equal to minValue."));
                }
            }
        }

        for (var keyframeIndex = 0; keyframeIndex < parsedProject.Timeline.CameraTrack.Keyframes.Count; keyframeIndex++)
        {
            var keyframe = parsedProject.Timeline.CameraTrack.Keyframes[keyframeIndex];
            if (keyframe.TimeSeconds < 0)
            {
                issues.Add(new ValidationIssue(ValidationGate.Schema, $"$.timeline.cameraTrack.keyframes[{keyframeIndex}].timeSeconds", ValidationSeverity.Error, "schema.camera_keyframe.time.invalid", "Camera keyframe timeSeconds must be zero or greater."));
            }

            if (keyframe.Zoom <= 0)
            {
                issues.Add(new ValidationIssue(ValidationGate.Schema, $"$.timeline.cameraTrack.keyframes[{keyframeIndex}].zoom", ValidationSeverity.Error, "schema.camera_keyframe.zoom.invalid", "Camera keyframe zoom must be greater than zero."));
            }

            if (!IsSupportedCameraInterpolation(keyframe.Interpolation))
            {
                issues.Add(new ValidationIssue(ValidationGate.Schema, $"$.timeline.cameraTrack.keyframes[{keyframeIndex}].interpolation", ValidationSeverity.Error, "schema.camera_keyframe.interpolation.unsupported", "Camera keyframe interpolation must be 'step' or 'linear'."));
            }

            if (keyframe.Easing != EasingType.Linear)
            {
                issues.Add(new ValidationIssue(ValidationGate.Schema, $"$.timeline.cameraTrack.keyframes[{keyframeIndex}].easing", ValidationSeverity.Error, "schema.camera_keyframe.easing.unsupported", "Camera keyframe easing must remain 'linear' until non-linear easing is implemented."));
            }

            if (keyframe.Interpolation == EasingType.Step && keyframe.Easing != EasingType.Linear)
            {
                issues.Add(new ValidationIssue(ValidationGate.Schema, $"$.timeline.cameraTrack.keyframes[{keyframeIndex}]", ValidationSeverity.Error, "schema.camera_keyframe.policy.invalid", "Step camera interpolation cannot be combined with non-linear easing."));
            }
        }

        for (var cueIndex = 0; cueIndex < parsedProject.Timeline.AudioCues.Count; cueIndex++)
        {
            var cue = parsedProject.Timeline.AudioCues[cueIndex];
            if (string.IsNullOrWhiteSpace(cue.Id))
            {
                issues.Add(new ValidationIssue(ValidationGate.Schema, $"$.timeline.audioCues[{cueIndex}].id", ValidationSeverity.Error, "schema.audio_cue.id.required", "Audio cue id is required."));
            }

            if (cue.StartSeconds < 0)
            {
                issues.Add(new ValidationIssue(ValidationGate.Schema, $"$.timeline.audioCues[{cueIndex}].startSeconds", ValidationSeverity.Error, "schema.audio_cue.start.invalid", "Audio cue startSeconds must be zero or greater."));
            }

            if (cue.DurationSeconds is <= 0)
            {
                issues.Add(new ValidationIssue(ValidationGate.Schema, $"$.timeline.audioCues[{cueIndex}].durationSeconds", ValidationSeverity.Error, "schema.audio_cue.duration.invalid", "Audio cue durationSeconds must be greater than zero when provided."));
            }

            if (cue.Volume < 0)
            {
                issues.Add(new ValidationIssue(ValidationGate.Schema, $"$.timeline.audioCues[{cueIndex}].volume", ValidationSeverity.Error, "schema.audio_cue.volume.invalid", "Audio cue volume must be zero or greater."));
            }
        }

        return issues;
    }

    private static NormalizedVideoProject NormalizeProject(VideoProject project, string sourcePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(sourcePath);
        var meta = project.Meta ?? new ProjectMeta();
        var normalizedProject = new VideoProject
        {
            Meta = new ProjectMeta
            {
                ProjectId = string.IsNullOrWhiteSpace(meta.ProjectId) ? sourcePath : meta.ProjectId.Trim(),
                Name = string.IsNullOrWhiteSpace(meta.Name) ? fileName : meta.Name.Trim(),
                Description = meta.Description?.Trim(),
                Version = string.IsNullOrWhiteSpace(meta.Version) ? "1.0" : meta.Version.Trim(),
                AssetRegistrySnapshotId = string.IsNullOrWhiteSpace(meta.AssetRegistrySnapshotId)
                    ? null
                    : meta.AssetRegistrySnapshotId.Trim(),
                CreatedUtc = meta.CreatedUtc,
                UpdatedUtc = meta.UpdatedUtc
            },
            Output = new OutputSpec
            {
                Width = project.Output.Width,
                Height = project.Output.Height,
                FrameRate = project.Output.FrameRate,
                BackgroundColorHex = string.IsNullOrWhiteSpace(project.Output.BackgroundColorHex) ? "#FFFFFF" : project.Output.BackgroundColorHex.Trim().ToUpperInvariant()
            },
            Assets = NormalizeAssets(project.Assets),
            Scenes = NormalizeScenes(project.Scenes),
            Timeline = NormalizeTimeline(project.Timeline)
        };

        var canonicalJson = JsonSerializer.Serialize(normalizedProject, SerializerOptions);
        return new NormalizedVideoProject(normalizedProject, canonicalJson);
    }

    private static AssetCollection NormalizeAssets(AssetCollection? assets)
    {
        assets ??= new AssetCollection();

        return new AssetCollection
        {
            RegistrySnapshot = NormalizeRegistrySnapshot(assets.RegistrySnapshot),
            SvgAssets = assets.SvgAssets
                .Select(asset => new SvgAsset
                {
                    Id = asset.Id.Trim(),
                    Name = asset.Name.Trim(),
                    SourcePath = asset.SourcePath.Trim(),
                    Type = asset.Type,
                    DefaultSize = asset.DefaultSize
                })
                .OrderBy(asset => asset.Id, StringComparer.Ordinal)
                .ThenBy(asset => asset.SourcePath, StringComparer.Ordinal)
                .ToList(),
            AudioAssets = assets.AudioAssets
                .Select(asset => new AudioAsset
                {
                    Id = asset.Id.Trim(),
                    Name = asset.Name.Trim(),
                    SourcePath = asset.SourcePath.Trim(),
                    Type = asset.Type,
                    DefaultVolume = asset.DefaultVolume
                })
                .OrderBy(asset => asset.Id, StringComparer.Ordinal)
                .ThenBy(asset => asset.SourcePath, StringComparer.Ordinal)
                .ToList(),
            FontAssets = assets.FontAssets
                .Select(asset => new FontAsset
                {
                    Id = asset.Id.Trim(),
                    FamilyName = asset.FamilyName.Trim(),
                    SourcePath = asset.SourcePath.Trim(),
                    ColorHex = string.IsNullOrWhiteSpace(asset.ColorHex) ? "#111111" : asset.ColorHex.Trim().ToUpperInvariant(),
                    Type = asset.Type
                })
                .OrderBy(asset => asset.Id, StringComparer.Ordinal)
                .ThenBy(asset => asset.SourcePath, StringComparer.Ordinal)
                .ToList(),
            HandAssets = assets.HandAssets
                .Select(asset => new HandAsset
                {
                    Id = asset.Id.Trim(),
                    Name = asset.Name.Trim(),
                    SourcePath = asset.SourcePath.Trim(),
                    Type = asset.Type,
                    TipOffset = asset.TipOffset
                })
                .OrderBy(asset => asset.Id, StringComparer.Ordinal)
                .ThenBy(asset => asset.SourcePath, StringComparer.Ordinal)
                .ToList(),
            ImageAssets = assets.ImageAssets
                .Select(asset => new ImageAsset
                {
                    Id = asset.Id.Trim(),
                    Name = asset.Name.Trim(),
                    SourcePath = asset.SourcePath.Trim(),
                    Type = asset.Type,
                    DefaultSize = asset.DefaultSize
                })
                .OrderBy(asset => asset.Id, StringComparer.Ordinal)
                .ThenBy(asset => asset.SourcePath, StringComparer.Ordinal)
                .ToList()
        };
    }

    private static AssetRegistrySnapshot NormalizeRegistrySnapshot(AssetRegistrySnapshot? registrySnapshot)
    {
        registrySnapshot ??= new AssetRegistrySnapshot();

        return new AssetRegistrySnapshot
        {
            RegistryId = registrySnapshot.RegistryId.Trim(),
            SnapshotId = registrySnapshot.SnapshotId.Trim(),
            SnapshotVersion = registrySnapshot.SnapshotVersion.Trim(),
            GeneratedUtc = registrySnapshot.GeneratedUtc,
            SourceManifestPath = string.IsNullOrWhiteSpace(registrySnapshot.SourceManifestPath)
                ? null
                : registrySnapshot.SourceManifestPath.Trim()
        };
    }

    private static List<SceneDefinition> NormalizeScenes(List<SceneDefinition>? scenes)
    {
        scenes ??= [];

        return scenes
            .Select(scene => new SceneDefinition
            {
                Id = scene.Id.Trim(),
                Name = scene.Name.Trim(),
                DurationSeconds = scene.DurationSeconds,
                Objects = (scene.Objects ?? [])
                    .Select(sceneObject => new SceneObject
                    {
                        Id = sceneObject.Id.Trim(),
                        Name = sceneObject.Name.Trim(),
                        Type = sceneObject.Type,
                        AssetRefId = string.IsNullOrWhiteSpace(sceneObject.AssetRefId) ? null : sceneObject.AssetRefId.Trim(),
                        TextContent = sceneObject.TextContent?.Trim(),
                        Layer = sceneObject.Layer,
                        IsVisible = sceneObject.IsVisible,
                        Transform = sceneObject.Transform
                    })
                    .OrderBy(sceneObject => sceneObject.Layer)
                    .ThenBy(sceneObject => sceneObject.Id, StringComparer.Ordinal)
                    .ToList()
            })
            .OrderBy(scene => scene.Id, StringComparer.Ordinal)
            .ThenBy(scene => scene.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static TimelineDefinition NormalizeTimeline(TimelineDefinition? timeline)
    {
        timeline ??= new TimelineDefinition();

        return new TimelineDefinition
        {
            Events = timeline.Events
                .Select(timelineEvent => new TimelineEvent
                {
                    Id = timelineEvent.Id.Trim(),
                    SceneId = timelineEvent.SceneId.Trim(),
                    SceneObjectId = timelineEvent.SceneObjectId.Trim(),
                    ActionType = timelineEvent.ActionType,
                    StartSeconds = timelineEvent.StartSeconds,
                    DurationSeconds = timelineEvent.DurationSeconds,
                    Easing = timelineEvent.Easing,
                    Parameters = timelineEvent.Parameters
                        .OrderBy(parameter => parameter.Key, StringComparer.Ordinal)
                        .ToDictionary(parameter => parameter.Key, parameter => parameter.Value, StringComparer.Ordinal)
                })
                .OrderBy(timelineEvent => timelineEvent.StartSeconds)
                .ThenBy(timelineEvent => timelineEvent.SceneId, StringComparer.Ordinal)
                .ThenBy(timelineEvent => timelineEvent.SceneObjectId, StringComparer.Ordinal)
                .ThenBy(timelineEvent => timelineEvent.ActionType)
                .ThenBy(timelineEvent => timelineEvent.Id, StringComparer.Ordinal)
                .ToList(),
            EffectProfiles = (timeline.EffectProfiles ?? [])
                .Select(effectProfile => new EffectProfile
                {
                    Id = effectProfile.Id.Trim(),
                    ActionType = effectProfile.ActionType,
                    MinDurationSeconds = effectProfile.MinDurationSeconds,
                    MaxDurationSeconds = effectProfile.MaxDurationSeconds,
                    ParameterBounds = (effectProfile.ParameterBounds ?? [])
                        .OrderBy(parameter => parameter.Key, StringComparer.Ordinal)
                        .ToDictionary(
                            parameter => parameter.Key,
                            parameter => new EffectParameterBound
                            {
                                Key = string.IsNullOrWhiteSpace(parameter.Value.Key)
                                    ? parameter.Key.Trim()
                                    : parameter.Value.Key.Trim(),
                                MinValue = parameter.Value.MinValue,
                                MaxValue = parameter.Value.MaxValue
                            },
                            StringComparer.Ordinal)
                })
                .OrderBy(effectProfile => effectProfile.Id, StringComparer.Ordinal)
                .ThenBy(effectProfile => effectProfile.ActionType)
                .ThenBy(effectProfile => effectProfile.MinDurationSeconds)
                .ThenBy(effectProfile => effectProfile.MaxDurationSeconds)
                .ToList(),
            CameraTrack = new CameraTrack
            {
                Keyframes = timeline.CameraTrack.Keyframes
                    .Select(keyframe => new CameraKeyframe
                    {
                        TimeSeconds = keyframe.TimeSeconds,
                        Position = keyframe.Position,
                        Zoom = keyframe.Zoom,
                        Interpolation = keyframe.Interpolation,
                        Easing = keyframe.Easing
                    })
                    .OrderBy(keyframe => keyframe.TimeSeconds)
                    .ThenBy(keyframe => keyframe.Position.X)
                    .ThenBy(keyframe => keyframe.Position.Y)
                    .ThenBy(keyframe => keyframe.Zoom)
                    .ThenBy(keyframe => keyframe.Interpolation)
                    .ThenBy(keyframe => keyframe.Easing)
                    .ToList()
            },
            AudioCues = timeline.AudioCues
                .Select(cue => new AudioCue
                {
                    Id = cue.Id.Trim(),
                    AudioAssetId = cue.AudioAssetId.Trim(),
                    StartSeconds = cue.StartSeconds,
                    DurationSeconds = cue.DurationSeconds,
                    Volume = cue.Volume
                })
                .OrderBy(cue => cue.StartSeconds)
                .ThenBy(cue => cue.AudioAssetId, StringComparer.Ordinal)
                .ThenBy(cue => cue.Id, StringComparer.Ordinal)
                .ToList()
        };
    }

    private static List<ValidationIssue> ValidateSemantic(VideoProject project)
    {
        var issues = new List<ValidationIssue>();
        ValidateRegistrySnapshotPinning(project, issues);

        var assetIds = new HashSet<string>(StringComparer.Ordinal);
        var svgAssetIds = project.Assets.SvgAssets
            .Select(asset => asset.Id)
            .ToHashSet(StringComparer.Ordinal);
        var imageAssetIds = project.Assets.ImageAssets
            .Select(asset => asset.Id)
            .ToHashSet(StringComparer.Ordinal);
        var effectProfileLookup = new Dictionary<string, EffectProfile>(StringComparer.Ordinal);

        AddDuplicateAssetIssues(project.Assets.SvgAssets.Select(asset => asset.Id), "$.assets.svgAssets", "semantic.asset.id.duplicate", "Duplicate SVG asset id.", issues);
        AddDuplicateAssetIssues(project.Assets.AudioAssets.Select(asset => asset.Id), "$.assets.audioAssets", "semantic.asset.id.duplicate", "Duplicate audio asset id.", issues);
        AddDuplicateAssetIssues(project.Assets.FontAssets.Select(asset => asset.Id), "$.assets.fontAssets", "semantic.asset.id.duplicate", "Duplicate font asset id.", issues);
        AddDuplicateAssetIssues(project.Assets.HandAssets.Select(asset => asset.Id), "$.assets.handAssets", "semantic.asset.id.duplicate", "Duplicate hand asset id.", issues);
        AddDuplicateAssetIssues(project.Assets.ImageAssets.Select(asset => asset.Id), "$.assets.imageAssets", "semantic.asset.id.duplicate", "Duplicate image asset id.", issues);
        AddDuplicateAssetIssues((project.Timeline.EffectProfiles ?? []).Select(effectProfile => effectProfile.Id), "$.timeline.effectProfiles", "semantic.effect_profile.id.duplicate", "Duplicate effect profile id.", issues);

        foreach (var assetId in project.Assets.SvgAssets.Select(asset => asset.Id)
                     .Concat(project.Assets.AudioAssets.Select(asset => asset.Id))
                     .Concat(project.Assets.FontAssets.Select(asset => asset.Id))
                     .Concat(project.Assets.HandAssets.Select(asset => asset.Id))
                     .Concat(project.Assets.ImageAssets.Select(asset => asset.Id)))
        {
            assetIds.Add(assetId);
        }

        foreach (var effectProfile in project.Timeline.EffectProfiles ?? [])
        {
            effectProfileLookup.TryAdd(effectProfile.Id, effectProfile);
        }

        var sceneLookup = new Dictionary<string, SceneDefinition>(StringComparer.Ordinal);
        for (var sceneIndex = 0; sceneIndex < project.Scenes.Count; sceneIndex++)
        {
            var scene = project.Scenes[sceneIndex];
            if (!sceneLookup.TryAdd(scene.Id, scene))
            {
                issues.Add(new ValidationIssue(ValidationGate.Semantic, $"$.scenes[{sceneIndex}].id", ValidationSeverity.Error, "semantic.scene.id.duplicate", "Scene ids must be unique."));
            }

            var sceneObjectIds = new HashSet<string>(StringComparer.Ordinal);
            for (var objectIndex = 0; objectIndex < scene.Objects.Count; objectIndex++)
            {
                var sceneObject = scene.Objects[objectIndex];
                if (!sceneObjectIds.Add(sceneObject.Id))
                {
                    issues.Add(new ValidationIssue(ValidationGate.Semantic, $"$.scenes[{sceneIndex}].objects[{objectIndex}].id", ValidationSeverity.Error, "semantic.scene_object.id.duplicate", "Scene object ids must be unique within a scene."));
                }

                if (sceneObject.Type == SceneObjectType.Svg || sceneObject.Type == SceneObjectType.Image)
                {
                    if (string.IsNullOrWhiteSpace(sceneObject.AssetRefId))
                    {
                        var message = sceneObject.Type == SceneObjectType.Image
                            ? "Image scene objects must reference an image asset."
                            : "SVG scene objects must reference an SVG asset.";
                        issues.Add(new ValidationIssue(ValidationGate.Semantic, $"$.scenes[{sceneIndex}].objects[{objectIndex}].assetRefId", ValidationSeverity.Error, "semantic.scene_object.asset_ref.required", message));
                    }
                    else if (!assetIds.Contains(sceneObject.AssetRefId))
                    {
                        issues.Add(new ValidationIssue(ValidationGate.Semantic, $"$.scenes[{sceneIndex}].objects[{objectIndex}].assetRefId", ValidationSeverity.Error, "semantic.scene_object.asset_ref.missing", "Scene object assetRefId must reference an existing asset."));
                    }
                    else if (sceneObject.Type == SceneObjectType.Svg && !svgAssetIds.Contains(sceneObject.AssetRefId))
                    {
                        issues.Add(new ValidationIssue(ValidationGate.Semantic, $"$.scenes[{sceneIndex}].objects[{objectIndex}].assetRefId", ValidationSeverity.Error, "semantic.scene_object.asset_ref.type_mismatch", "SVG scene objects must reference an existing SVG asset."));
                    }
                    else if (sceneObject.Type == SceneObjectType.Image && !imageAssetIds.Contains(sceneObject.AssetRefId))
                    {
                        issues.Add(new ValidationIssue(ValidationGate.Semantic, $"$.scenes[{sceneIndex}].objects[{objectIndex}].assetRefId", ValidationSeverity.Error, "semantic.scene_object.asset_ref.type_mismatch", "Image scene objects must reference an existing image asset."));
                    }
                }
            }
        }

        var eventIds = new HashSet<string>(StringComparer.Ordinal);
        for (var eventIndex = 0; eventIndex < project.Timeline.Events.Count; eventIndex++)
        {
            var timelineEvent = project.Timeline.Events[eventIndex];
            if (!eventIds.Add(timelineEvent.Id))
            {
                issues.Add(new ValidationIssue(ValidationGate.Semantic, $"$.timeline.events[{eventIndex}].id", ValidationSeverity.Error, "semantic.timeline_event.id.duplicate", "Timeline event ids must be unique."));
            }

            if (!sceneLookup.TryGetValue(timelineEvent.SceneId, out var scene))
            {
                issues.Add(new ValidationIssue(ValidationGate.Semantic, $"$.timeline.events[{eventIndex}].sceneId", ValidationSeverity.Error, "semantic.timeline_event.scene.missing", "Timeline event sceneId must reference an existing scene."));
                continue;
            }

            if (!scene.Objects.Any(sceneObject => string.Equals(sceneObject.Id, timelineEvent.SceneObjectId, StringComparison.Ordinal)))
            {
                issues.Add(new ValidationIssue(ValidationGate.Semantic, $"$.timeline.events[{eventIndex}].sceneObjectId", ValidationSeverity.Error, "semantic.timeline_event.scene_object.missing", "Timeline event sceneObjectId must reference an existing scene object."));
            }

            if (!TryResolveEffectProfileId(timelineEvent, out var effectProfileId))
            {
                continue;
            }

            if (!effectProfileLookup.TryGetValue(effectProfileId, out var effectProfile))
            {
                issues.Add(new ValidationIssue(ValidationGate.Semantic, $"$.timeline.events[{eventIndex}].parameters.effectProfileId", ValidationSeverity.Error, "semantic.effect_profile.missing", $"Timeline event effectProfileId '{effectProfileId}' must reference an existing effect profile."));
                continue;
            }

            if (effectProfile.ActionType != timelineEvent.ActionType)
            {
                issues.Add(new ValidationIssue(ValidationGate.Semantic, $"$.timeline.events[{eventIndex}].parameters.effectProfileId", ValidationSeverity.Error, "semantic.effect_profile.action_mismatch", $"Timeline event actionType '{timelineEvent.ActionType}' must match effect profile actionType '{effectProfile.ActionType}'."));
                continue;
            }

            foreach (var parameterBound in effectProfile.ParameterBounds.Values.OrderBy(bound => bound.Key, StringComparer.Ordinal))
            {
                if (!timelineEvent.Parameters.TryGetValue(parameterBound.Key, out var parameterValueRaw))
                {
                    continue;
                }

                if (!double.TryParse(parameterValueRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out var parameterValue))
                {
                    issues.Add(new ValidationIssue(ValidationGate.Semantic, $"$.timeline.events[{eventIndex}].parameters.{parameterBound.Key}", ValidationSeverity.Error, "semantic.effect_profile.parameter_out_of_range", $"Timeline event parameter '{parameterBound.Key}' must be a numeric value between {parameterBound.MinValue} and {parameterBound.MaxValue} (inclusive)."));
                    continue;
                }

                if (parameterValue < parameterBound.MinValue || parameterValue > parameterBound.MaxValue)
                {
                    issues.Add(new ValidationIssue(ValidationGate.Semantic, $"$.timeline.events[{eventIndex}].parameters.{parameterBound.Key}", ValidationSeverity.Error, "semantic.effect_profile.parameter_out_of_range", $"Timeline event parameter '{parameterBound.Key}' is outside allowed range [{parameterBound.MinValue}, {parameterBound.MaxValue}]."));
                }
            }
        }

        return issues;
    }

    private static void ValidateRegistrySnapshotPinning(VideoProject project, ICollection<ValidationIssue> issues)
    {
        var metaSnapshotId = project.Meta.AssetRegistrySnapshotId?.Trim();
        var registrySnapshot = project.Assets.RegistrySnapshot ?? new AssetRegistrySnapshot();
        var registrySnapshotId = registrySnapshot.SnapshotId.Trim();
        var registryVersion = registrySnapshot.SnapshotVersion.Trim();
        var registryId = registrySnapshot.RegistryId.Trim();

        var hasMetaPin = !string.IsNullOrWhiteSpace(metaSnapshotId);
        var hasSnapshotData =
            !string.IsNullOrWhiteSpace(registrySnapshotId) ||
            !string.IsNullOrWhiteSpace(registryVersion) ||
            !string.IsNullOrWhiteSpace(registryId);

        if (!hasMetaPin && !hasSnapshotData)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(registrySnapshotId))
        {
            if (hasMetaPin)
            {
                issues.Add(new ValidationIssue(ValidationGate.Semantic, "$.assets.registrySnapshot", ValidationSeverity.Error, "semantic.asset_registry.snapshot.required", "Pinned projects must provide assets.registrySnapshot data."));
            }

            issues.Add(new ValidationIssue(ValidationGate.Semantic, "$.assets.registrySnapshot.snapshotId", ValidationSeverity.Error, "semantic.asset_registry.id.required", "Asset registry snapshotId is required when registry pinning is used."));
            return;
        }

        if (string.IsNullOrWhiteSpace(registryVersion))
        {
            issues.Add(new ValidationIssue(ValidationGate.Semantic, "$.assets.registrySnapshot.snapshotVersion", ValidationSeverity.Error, "semantic.asset_registry.version.required", "Asset registry snapshotVersion is required when registry pinning is used."));
        }

        if (hasMetaPin && !string.Equals(metaSnapshotId, registrySnapshotId, StringComparison.Ordinal))
        {
            issues.Add(new ValidationIssue(ValidationGate.Semantic, "$.meta.assetRegistrySnapshotId", ValidationSeverity.Error, "semantic.asset_registry.snapshot.mismatch", "meta.assetRegistrySnapshotId must match assets.registrySnapshot.snapshotId."));
        }

        if (!RegistrySnapshotPolicy.TryGetValue(registrySnapshotId, out var registrySnapshotStatus))
        {
            issues.Add(new ValidationIssue(ValidationGate.Semantic, "$.assets.registrySnapshot.snapshotId", ValidationSeverity.Error, "semantic.asset_registry.snapshot.unknown", $"Asset registry snapshot '{registrySnapshotId}' is not recognized by the controlled registry policy."));
            return;
        }

        if (registrySnapshotStatus == RegistrySnapshotStatus.Deprecated)
        {
            issues.Add(new ValidationIssue(ValidationGate.Semantic, "$.assets.registrySnapshot.snapshotId", ValidationSeverity.Error, "semantic.asset_registry.snapshot.deprecated", $"Asset registry snapshot '{registrySnapshotId}' is deprecated and cannot be used."));
        }
    }

    private static bool TryResolveEffectProfileId(TimelineEvent timelineEvent, out string effectProfileId)
    {
        effectProfileId = string.Empty;
        if (!timelineEvent.Parameters.TryGetValue("effectProfileId", out var rawEffectProfileId))
        {
            return false;
        }

        effectProfileId = rawEffectProfileId?.Trim() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(effectProfileId);
    }

    private static List<ValidationIssue> ValidateReadiness(VideoProject project)
    {
        var issues = new List<ValidationIssue>();

        if (project.Scenes.Count == 0)
        {
            issues.Add(new ValidationIssue(ValidationGate.Readiness, "$.scenes", ValidationSeverity.Error, "readiness.scenes.required", "At least one scene is required before timeline evaluation."));
        }

        if (project.Timeline.Events.Count == 0)
        {
            issues.Add(new ValidationIssue(ValidationGate.Readiness, "$.timeline.events", ValidationSeverity.Error, "readiness.timeline_events.required", "At least one timeline event is required before timeline evaluation."));
        }

        var sceneLookup = project.Scenes.ToDictionary(scene => scene.Id, StringComparer.Ordinal);
        for (var eventIndex = 0; eventIndex < project.Timeline.Events.Count; eventIndex++)
        {
            var timelineEvent = project.Timeline.Events[eventIndex];
            if (!sceneLookup.TryGetValue(timelineEvent.SceneId, out var scene))
            {
                continue;
            }

            if (timelineEvent.StartSeconds + timelineEvent.DurationSeconds > scene.DurationSeconds)
            {
                issues.Add(new ValidationIssue(ValidationGate.Readiness, $"$.timeline.events[{eventIndex}]", ValidationSeverity.Error, "readiness.timeline_event.scene_duration_exceeded", "Timeline event extends beyond the owning scene duration."));
            }
        }

        return issues;
    }

    private static void AddDuplicateAssetIssues(IEnumerable<string> ids, string pathPrefix, string code, string message, ICollection<ValidationIssue> issues)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var duplicateIndex = 0;
        foreach (var id in ids)
        {
            if (!seen.Add(id))
            {
                issues.Add(new ValidationIssue(ValidationGate.Semantic, pathPrefix, ValidationSeverity.Error, code, message, duplicateIndex));
                duplicateIndex++;
            }
        }
    }

    private static bool IsSupportedCameraInterpolation(EasingType interpolation)
    {
        return interpolation is EasingType.Step or EasingType.Linear;
    }

    private static ValidationGateResult CreateGateResult(ValidationGate gate, IEnumerable<ValidationIssue> issues)
    {
        return new ValidationGateResult(gate, ValidationIssueOrdering.Sort(issues));
    }

    private static SpecProcessingResult CreateResult(IReadOnlyList<ValidationGateResult> gateResults, NormalizedVideoProject? project)
    {
        var issues = ValidationIssueOrdering.Sort(gateResults.SelectMany(result => result.Issues));
        return new SpecProcessingResult(gateResults, issues, project);
    }
}


