using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Whiteboard.Core.Scene;
using Whiteboard.Core.Validation;
using Whiteboard.Core.ValueObjects;

namespace Whiteboard.Core.Templates;

public sealed partial class TemplateContractPipeline : ITemplateContractPipeline
{
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

    public TemplateContractProcessingResult Process(string json, string sourcePath)
    {
        var gateResults = new List<ValidationGateResult>();

        var contractIssues = ValidateContract(json, sourcePath);
        gateResults.Add(CreateGateResult(ValidationGate.Contract, contractIssues));
        if (contractIssues.Count > 0)
        {
            return CreateResult(gateResults, null);
        }

        var schemaIssues = ValidateSchema(json, out var parsedTemplate);
        gateResults.Add(CreateGateResult(ValidationGate.Schema, schemaIssues));
        if (schemaIssues.Count > 0 || parsedTemplate is null)
        {
            return CreateResult(gateResults, null);
        }

        var normalizedTemplate = NormalizeTemplate(parsedTemplate, sourcePath);
        gateResults.Add(CreateGateResult(ValidationGate.Normalization, []));

        var semanticIssues = ValidateSemantic(json, normalizedTemplate.Template);
        gateResults.Add(CreateGateResult(ValidationGate.Semantic, semanticIssues));
        if (semanticIssues.Count > 0)
        {
            return CreateResult(gateResults, null);
        }

        return CreateResult(gateResults, normalizedTemplate);
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
            issues.Add(new ValidationIssue(ValidationGate.Contract, "$", ValidationSeverity.Error, "contract.template.required", "Template JSON is required."));
        }

        return issues;
    }

    private static List<ValidationIssue> ValidateSchema(string json, out SceneTemplateDefinition? parsedTemplate)
    {
        var issues = new List<ValidationIssue>();
        parsedTemplate = null;

        try
        {
            parsedTemplate = JsonSerializer.Deserialize<SceneTemplateDefinition>(json, SerializerOptions);
        }
        catch (JsonException exception)
        {
            issues.Add(new ValidationIssue(ValidationGate.Schema, "$", ValidationSeverity.Error, "schema.template.invalid", exception.Message));
            return issues;
        }

        if (parsedTemplate is null)
        {
            issues.Add(new ValidationIssue(ValidationGate.Schema, "$", ValidationSeverity.Error, "schema.template.deserialize.null", "Template JSON could not be deserialized into SceneTemplateDefinition."));
            return issues;
        }

        return issues;
    }

    private static NormalizedSceneTemplateDefinition NormalizeTemplate(SceneTemplateDefinition template, string sourcePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(sourcePath);
        var normalizedTemplate = new SceneTemplateDefinition
        {
            TemplateId = template.TemplateId.Trim(),
            Version = string.IsNullOrWhiteSpace(template.Version) ? "1.0.0" : template.Version.Trim(),
            Status = string.IsNullOrWhiteSpace(template.Status) ? string.Empty : template.Status.Trim().ToLowerInvariant(),
            Name = string.IsNullOrWhiteSpace(template.Name) ? fileName : template.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(template.Description) ? null : template.Description.Trim(),
            Slots = NormalizeSlots(template.Slots),
            SceneFragments = NormalizeSceneFragments(template.SceneFragments),
            TimelineEventFragments = NormalizeTimelineFragments(template.TimelineEventFragments)
        };

        var canonicalJson = JsonSerializer.Serialize(normalizedTemplate, SerializerOptions);
        return new NormalizedSceneTemplateDefinition(normalizedTemplate, canonicalJson);
    }

    private static List<TemplateSlotDefinition> NormalizeSlots(List<TemplateSlotDefinition>? slots)
    {
        slots ??= [];

        return slots
            .Select(slot => new TemplateSlotDefinition
            {
                SlotId = slot.SlotId.Trim(),
                ValueType = slot.ValueType.Trim(),
                Required = slot.Required,
                DefaultValue = string.IsNullOrWhiteSpace(slot.DefaultValue) ? null : slot.DefaultValue.Trim(),
                Constraints = NormalizeConstraint(slot.Constraints)
            })
            .OrderBy(slot => slot.SlotId, StringComparer.Ordinal)
            .ThenBy(slot => slot.ValueType, StringComparer.Ordinal)
            .ToList();
    }

    private static TemplateSlotConstraint NormalizeConstraint(TemplateSlotConstraint? constraint)
    {
        constraint ??= new TemplateSlotConstraint();

        return new TemplateSlotConstraint
        {
            AllowAssetId = constraint.AllowAssetId,
            AllowEffectProfileId = constraint.AllowEffectProfileId,
            MinValue = constraint.MinValue,
            MaxValue = constraint.MaxValue,
            AllowedValues = (constraint.AllowedValues ?? [])
                .Select(value => value?.Trim() ?? string.Empty)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToList()
        };
    }

    private static List<TemplateSceneFragment> NormalizeSceneFragments(List<TemplateSceneFragment>? sceneFragments)
    {
        sceneFragments ??= [];

        return sceneFragments
            .Select(fragment => new TemplateSceneFragment
            {
                LocalId = fragment.LocalId.Trim(),
                Name = fragment.Name.Trim(),
                DurationSeconds = fragment.DurationSeconds,
                Objects = NormalizeSceneObjects(fragment.Objects)
            })
            .OrderBy(fragment => fragment.LocalId, StringComparer.Ordinal)
            .ThenBy(fragment => fragment.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static List<SceneObject> NormalizeSceneObjects(List<SceneObject>? objects)
    {
        objects ??= [];

        return objects
            .Select(sceneObject => new SceneObject
            {
                Id = sceneObject.Id.Trim(),
                Name = sceneObject.Name.Trim(),
                Type = sceneObject.Type,
                AssetRefId = string.IsNullOrWhiteSpace(sceneObject.AssetRefId) ? null : sceneObject.AssetRefId.Trim(),
                TextContent = string.IsNullOrWhiteSpace(sceneObject.TextContent) ? null : sceneObject.TextContent.Trim(),
                Layer = sceneObject.Layer,
                IsVisible = sceneObject.IsVisible,
                Transform = new TransformSpec
                {
                    Position = new Position2D(sceneObject.Transform.Position.X, sceneObject.Transform.Position.Y),
                    Size = new Size2D(sceneObject.Transform.Size.Width, sceneObject.Transform.Size.Height),
                    RotationDegrees = sceneObject.Transform.RotationDegrees,
                    ScaleX = sceneObject.Transform.ScaleX,
                    ScaleY = sceneObject.Transform.ScaleY,
                    Opacity = sceneObject.Transform.Opacity
                }
            })
            .OrderBy(sceneObject => sceneObject.Layer)
            .ThenBy(sceneObject => sceneObject.Id, StringComparer.Ordinal)
            .ThenBy(sceneObject => sceneObject.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static List<TemplateTimelineEventFragment> NormalizeTimelineFragments(List<TemplateTimelineEventFragment>? timelineFragments)
    {
        timelineFragments ??= [];

        return timelineFragments
            .Select(fragment => new TemplateTimelineEventFragment
            {
                LocalId = fragment.LocalId.Trim(),
                SceneLocalId = fragment.SceneLocalId.Trim(),
                SceneObjectLocalId = fragment.SceneObjectLocalId.Trim(),
                ActionType = fragment.ActionType,
                StartSeconds = fragment.StartSeconds,
                DurationSeconds = fragment.DurationSeconds,
                Easing = fragment.Easing,
                Parameters = (fragment.Parameters ?? [])
                    .OrderBy(parameter => parameter.Key, StringComparer.Ordinal)
                    .ToDictionary(
                        parameter => parameter.Key.Trim(),
                        parameter => parameter.Value?.Trim() ?? string.Empty,
                        StringComparer.Ordinal)
            })
            .OrderBy(fragment => fragment.LocalId, StringComparer.Ordinal)
            .ThenBy(fragment => fragment.SceneLocalId, StringComparer.Ordinal)
            .ThenBy(fragment => fragment.SceneObjectLocalId, StringComparer.Ordinal)
            .ToList();
    }

    private static List<ValidationIssue> ValidateSemantic(string json, SceneTemplateDefinition template)
    {
        var issues = new List<ValidationIssue>();

        if (!string.Equals(template.Status, "active", StringComparison.Ordinal) &&
            !string.Equals(template.Status, "deprecated", StringComparison.Ordinal))
        {
            issues.Add(new ValidationIssue(ValidationGate.Semantic, "$.status", ValidationSeverity.Error, "template.status.invalid", "Template status must be 'active' or 'deprecated'."));
        }

        if (string.IsNullOrWhiteSpace(template.TemplateId))
        {
            issues.Add(new ValidationIssue(ValidationGate.Semantic, "$.templateId", ValidationSeverity.Error, "template.slot.definition.required", "Template templateId is required."));
        }

        if (string.IsNullOrWhiteSpace(template.Version))
        {
            issues.Add(new ValidationIssue(ValidationGate.Semantic, "$.version", ValidationSeverity.Error, "template.slot.definition.required", "Template version is required."));
        }

        var slotIds = new HashSet<string>(StringComparer.Ordinal);
        var duplicateIndex = 0;
        for (var slotIndex = 0; slotIndex < template.Slots.Count; slotIndex++)
        {
            var slot = template.Slots[slotIndex];
            if (string.IsNullOrWhiteSpace(slot.SlotId) || string.IsNullOrWhiteSpace(slot.ValueType))
            {
                issues.Add(new ValidationIssue(ValidationGate.Semantic, $"$.slots[{slotIndex}]", ValidationSeverity.Error, "template.slot.definition.required", "Template slots must define slotId and valueType."));
                continue;
            }

            if (!slotIds.Add(slot.SlotId))
            {
                issues.Add(new ValidationIssue(ValidationGate.Semantic, "$.slots", ValidationSeverity.Error, "template.slot.id.duplicate", $"Duplicate template slot id '{slot.SlotId}'.", duplicateIndex));
                duplicateIndex++;
            }

            if ((slot.Constraints.AllowAssetId || slot.Constraints.AllowEffectProfileId) &&
                !string.IsNullOrWhiteSpace(slot.DefaultValue))
            {
                issues.Add(new ValidationIssue(ValidationGate.Semantic, $"$.slots[{slotIndex}].defaultValue", ValidationSeverity.Error, "template.slot.default_governed_disallowed", "Governed slot defaults are not allowed for assetId or effectProfileId values."));
            }
        }

        using var document = JsonDocument.Parse(json);
        FindSourcePathIssues(document.RootElement, "$", issues);

        foreach (var placeholder in EnumeratePlaceholders(template))
        {
            if (slotIds.Contains(placeholder.SlotId))
            {
                continue;
            }

            issues.Add(new ValidationIssue(ValidationGate.Semantic, placeholder.Path, ValidationSeverity.Error, "template.fragment.placeholder.undeclared", $"Template placeholder '{placeholder.SlotId}' must match a declared slot."));
        }

        return issues;
    }

    private static void FindSourcePathIssues(JsonElement element, string path, ICollection<ValidationIssue> issues)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var propertyPath = $"{path}.{property.Name}";
                    if (string.Equals(property.Name, "sourcePath", StringComparison.Ordinal))
                    {
                        issues.Add(new ValidationIssue(ValidationGate.Semantic, propertyPath, ValidationSeverity.Error, "template.reference.path_fallback.disallowed", "Template contracts cannot use sourcePath fallbacks; governed references must use stable IDs."));
                    }

                    FindSourcePathIssues(property.Value, propertyPath, issues);
                }

                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    FindSourcePathIssues(item, $"{path}[{index}]", issues);
                    index++;
                }

                break;
        }
    }

    private static IEnumerable<(string Path, string SlotId)> EnumeratePlaceholders(SceneTemplateDefinition template)
    {
        for (var sceneIndex = 0; sceneIndex < template.SceneFragments.Count; sceneIndex++)
        {
            var sceneFragment = template.SceneFragments[sceneIndex];
            foreach (var placeholder in EnumeratePlaceholdersFromJson(
                         JsonSerializer.Serialize(sceneFragment, SerializerOptions),
                         $"$.sceneFragments[{sceneIndex}]"))
            {
                yield return placeholder;
            }
        }

        for (var timelineIndex = 0; timelineIndex < template.TimelineEventFragments.Count; timelineIndex++)
        {
            var timelineFragment = template.TimelineEventFragments[timelineIndex];
            foreach (var placeholder in EnumeratePlaceholdersFromJson(
                         JsonSerializer.Serialize(timelineFragment, SerializerOptions),
                         $"$.timelineEventFragments[{timelineIndex}]"))
            {
                yield return placeholder;
            }
        }
    }

    private static IEnumerable<(string Path, string SlotId)> EnumeratePlaceholdersFromJson(string json, string path)
    {
        foreach (Match match in SlotPlaceholderRegex().Matches(json))
        {
            var slotId = match.Groups["slotId"].Value.Trim();
            if (string.IsNullOrWhiteSpace(slotId))
            {
                continue;
            }

            yield return (path, slotId);
        }
    }

    private static ValidationGateResult CreateGateResult(ValidationGate gate, IEnumerable<ValidationIssue> issues)
    {
        return new ValidationGateResult(gate, ValidationIssueOrdering.Sort(issues));
    }

    private static TemplateContractProcessingResult CreateResult(
        IReadOnlyList<ValidationGateResult> gateResults,
        NormalizedSceneTemplateDefinition? template)
    {
        var issues = ValidationIssueOrdering.Sort(gateResults.SelectMany(result => result.Issues));
        return new TemplateContractProcessingResult(gateResults, issues, template);
    }

    [GeneratedRegex(@"\{\{slot:(?<slotId>[^}]+)\}\}", RegexOptions.CultureInvariant)]
    private static partial Regex SlotPlaceholderRegex();
}
