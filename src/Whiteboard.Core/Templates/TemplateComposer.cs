using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Whiteboard.Core.Scene;
using Whiteboard.Core.Timeline;
using Whiteboard.Core.Validation;
using Whiteboard.Core.ValueObjects;

namespace Whiteboard.Core.Templates;

public sealed class TemplateComposer : ITemplateComposer
{
    private const string SlotPlaceholderTokenPrefix = "{{slot:";
    private const string SlotValueMissingCode = "template.compose.slot_value_missing";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    private readonly ITemplateSlotBindingValidator _slotBindingValidator;
    private readonly TemplateSlotValueResolver _slotValueResolver;

    public TemplateComposer()
        : this(new TemplateSlotBindingValidator(), new TemplateSlotValueResolver())
    {
    }

    public TemplateComposer(
        ITemplateSlotBindingValidator slotBindingValidator,
        TemplateSlotValueResolver slotValueResolver)
    {
        _slotBindingValidator = slotBindingValidator;
        _slotValueResolver = slotValueResolver;
    }

    public TemplateInstantiationResult Compose(TemplateInstantiationRequest request)
    {
        // TemplateSlotValueResolver expands {{slot:slotId}} tokens before materialized fragments are returned.
        var issues = new List<ValidationIssue>();
        var instanceId = request.InstanceId.Trim();

        if (string.IsNullOrWhiteSpace(instanceId))
        {
            issues.Add(new ValidationIssue(
                ValidationGate.Contract,
                "$.instanceId",
                ValidationSeverity.Error,
                "template.compose.instance_id.required",
                "InstanceId is required for template composition."));
        }

        var validationResult = _slotBindingValidator.Validate(request.Template, request.SlotValues);
        issues.AddRange(validationResult.Issues);
        if (issues.Count > 0)
        {
            return CreateFailureResult(request, instanceId, validationResult.SlotBindings, issues);
        }

        var sceneIds = new HashSet<string>(StringComparer.Ordinal);
        var sceneObjectIds = new HashSet<string>(StringComparer.Ordinal);
        var timelineEventIds = new HashSet<string>(StringComparer.Ordinal);
        var scenes = new List<SceneDefinition>();
        var timelineEvents = new List<TimelineEvent>();

        for (var sceneIndex = 0; sceneIndex < request.Template.SceneFragments.Count; sceneIndex++)
        {
            var fragment = request.Template.SceneFragments[sceneIndex];
            var sceneId = CreateSceneId(instanceId, fragment.LocalId);
            if (!sceneIds.Add(sceneId))
            {
                issues.Add(CreateCollisionIssue($"$.sceneFragments[{sceneIndex}].localId", sceneId));
                continue;
            }

            var objects = new List<SceneObject>();
            for (var objectIndex = 0; objectIndex < fragment.Objects.Count; objectIndex++)
            {
                var templateObject = fragment.Objects[objectIndex];
                var sceneObjectId = CreateSceneObjectId(instanceId, fragment.LocalId, templateObject.Id);
                if (!sceneObjectIds.Add(sceneObjectId))
                {
                    issues.Add(CreateCollisionIssue($"$.sceneFragments[{sceneIndex}].objects[{objectIndex}].id", sceneObjectId));
                    continue;
                }

                objects.Add(new SceneObject
                {
                    Id = sceneObjectId,
                    Name = _slotValueResolver.ResolveString(
                        templateObject.Name,
                        validationResult.SlotBindings,
                        $"$.sceneFragments[{sceneIndex}].objects[{objectIndex}].name",
                        issues) ?? string.Empty,
                    Type = templateObject.Type,
                    AssetRefId = _slotValueResolver.ResolveString(
                        templateObject.AssetRefId,
                        validationResult.SlotBindings,
                        $"$.sceneFragments[{sceneIndex}].objects[{objectIndex}].assetRefId",
                        issues),
                    TextContent = _slotValueResolver.ResolveString(
                        templateObject.TextContent,
                        validationResult.SlotBindings,
                        $"$.sceneFragments[{sceneIndex}].objects[{objectIndex}].textContent",
                        issues),
                    Layer = templateObject.Layer + request.LayerOffset,
                    IsVisible = templateObject.IsVisible,
                    Transform = CloneTransform(templateObject.Transform)
                });
            }

            scenes.Add(new SceneDefinition
            {
                Id = sceneId,
                Name = _slotValueResolver.ResolveString(
                    fragment.Name,
                    validationResult.SlotBindings,
                    $"$.sceneFragments[{sceneIndex}].name",
                    issues) ?? string.Empty,
                DurationSeconds = fragment.DurationSeconds,
                Objects = objects
            });
        }

        for (var eventIndex = 0; eventIndex < request.Template.TimelineEventFragments.Count; eventIndex++)
        {
            var fragment = request.Template.TimelineEventFragments[eventIndex];
            var eventId = CreateEventId(instanceId, fragment.LocalId);
            if (!timelineEventIds.Add(eventId))
            {
                issues.Add(CreateCollisionIssue($"$.timelineEventFragments[{eventIndex}].localId", eventId));
                continue;
            }

            timelineEvents.Add(new TimelineEvent
            {
                Id = eventId,
                SceneId = CreateSceneId(instanceId, fragment.SceneLocalId),
                SceneObjectId = CreateSceneObjectId(instanceId, fragment.SceneLocalId, fragment.SceneObjectLocalId),
                ActionType = fragment.ActionType,
                StartSeconds = fragment.StartSeconds + request.TimeOffsetSeconds,
                DurationSeconds = fragment.DurationSeconds,
                Easing = fragment.Easing,
                Parameters = ResolveParameters(fragment.Parameters, validationResult.SlotBindings, eventIndex, issues, _slotValueResolver)
            });
        }

        if (issues.Count > 0)
        {
            return CreateFailureResult(request, instanceId, validationResult.SlotBindings, issues);
        }

        var fragmentResult = new ComposedTemplateFragment
        {
            Scenes = scenes,
            TimelineEvents = timelineEvents
        };

        var canonicalPayload = new
        {
            templateId = request.Template.TemplateId,
            version = request.Template.Version,
            instanceId,
            slotBindings = validationResult.SlotBindings,
            scenes,
            timelineEvents
        };
        var canonicalJson = JsonSerializer.Serialize(canonicalPayload, SerializerOptions);

        return new TemplateInstantiationResult
        {
            Success = true,
            TemplateId = request.Template.TemplateId,
            Version = request.Template.Version,
            InstanceId = instanceId,
            SlotBindings = validationResult.SlotBindings,
            Fragment = fragmentResult,
            CanonicalJson = canonicalJson,
            DeterministicKey = BuildDeterministicKey(canonicalJson),
            Issues = []
        };
    }

    private static TemplateInstantiationResult CreateFailureResult(
        TemplateInstantiationRequest request,
        string instanceId,
        IReadOnlyDictionary<string, string> slotBindings,
        IEnumerable<ValidationIssue> issues)
    {
        return new TemplateInstantiationResult
        {
            Success = false,
            TemplateId = request.Template.TemplateId,
            Version = request.Template.Version,
            InstanceId = instanceId,
            SlotBindings = slotBindings,
            Fragment = new ComposedTemplateFragment(),
            CanonicalJson = string.Empty,
            DeterministicKey = string.Empty,
            Issues = ValidationIssueOrdering.Sort(issues)
        };
    }

    private static Dictionary<string, string> ResolveParameters(
        IReadOnlyDictionary<string, string> parameters,
        IReadOnlyDictionary<string, string> slotBindings,
        int eventIndex,
        ICollection<ValidationIssue> issues,
        TemplateSlotValueResolver slotValueResolver)
    {
        return parameters
            .OrderBy(parameter => parameter.Key, StringComparer.Ordinal)
            .ToDictionary(
                parameter => parameter.Key,
                parameter => slotValueResolver.ResolveString(
                    parameter.Value,
                    slotBindings,
                    $"$.timelineEventFragments[{eventIndex}].parameters.{parameter.Key}",
                    issues) ?? string.Empty,
                StringComparer.Ordinal);
    }

    private static ValidationIssue CreateCollisionIssue(string path, string id)
    {
        return new ValidationIssue(
            ValidationGate.Semantic,
            path,
            ValidationSeverity.Error,
            "template.compose.id_collision",
            $"Generated id '{id}' collides after namespacing.");
    }

    private static TransformSpec CloneTransform(TransformSpec transform)
    {
        return new TransformSpec
        {
            Position = new Position2D(transform.Position.X, transform.Position.Y),
            Size = new Size2D(transform.Size.Width, transform.Size.Height),
            RotationDegrees = transform.RotationDegrees,
            ScaleX = transform.ScaleX,
            ScaleY = transform.ScaleY,
            Opacity = transform.Opacity
        };
    }

    private static string CreateSceneId(string instanceId, string localId) => $"{instanceId}.{localId}";

    private static string CreateEventId(string instanceId, string localId) => $"{instanceId}.{localId}";

    private static string CreateSceneObjectId(string instanceId, string sceneLocalId, string objectId) => $"{instanceId}.{sceneLocalId}.{objectId}";

    private static string BuildDeterministicKey(string canonicalJson)
    {
        _ = SlotPlaceholderTokenPrefix;
        _ = SlotValueMissingCode;
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalJson));
        return $"sha256:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }
}
