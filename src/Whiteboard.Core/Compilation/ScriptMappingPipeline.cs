using System.Text.Json;
using System.Text.Json.Serialization;
using Whiteboard.Core.Templates;
using Whiteboard.Core.Validation;

namespace Whiteboard.Core.Compilation;

public sealed class ScriptMappingPipeline : IScriptMappingPipeline
{
    private const string DefaultTemplateCatalogPath = ".planning/templates/index.json";
    private const string DefaultMappingCatalogPath = ".planning/script-compiler/template-mappings.json";
    private const string DefaultGovernedLibraryPath = ".planning/script-compiler/governed-library.json";

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

    private readonly ITemplateContractPipeline _templateContractPipeline;
    private readonly ITemplateSlotBindingValidator _slotBindingValidator;

    public ScriptMappingPipeline(
        ITemplateContractPipeline? templateContractPipeline = null,
        ITemplateSlotBindingValidator? templateSlotBindingValidator = null)
    {
        _templateContractPipeline = templateContractPipeline ?? new TemplateContractPipeline();
        _slotBindingValidator = templateSlotBindingValidator ?? new TemplateSlotBindingValidator();
    }

    public ScriptCompilationPlan Process(
        string json,
        string sourcePath,
        string templateCatalogPath,
        string mappingCatalogPath,
        string governedLibraryPath)
    {
        var gateResults = new List<ValidationGateResult>();

        var contractIssues = ValidateContract(json, sourcePath, templateCatalogPath, mappingCatalogPath, governedLibraryPath);
        gateResults.Add(CreateGateResult(ValidationGate.Contract, contractIssues));
        if (contractIssues.Count > 0)
        {
            return CreateResult(gateResults, null, []);
        }

        var schemaIssues = ValidateSchema(json, out var parsedDocument);
        gateResults.Add(CreateGateResult(ValidationGate.Schema, schemaIssues));
        if (schemaIssues.Count > 0 || parsedDocument is null)
        {
            return CreateResult(gateResults, null, []);
        }

        var normalizedDocument = NormalizeDocument(parsedDocument);
        gateResults.Add(CreateGateResult(ValidationGate.Normalization, []));

        var semanticIssues = ValidateSemantic(
            normalizedDocument,
            sourcePath,
            templateCatalogPath,
            mappingCatalogPath,
            governedLibraryPath,
            out var sectionPlans);
        gateResults.Add(CreateGateResult(ValidationGate.Semantic, semanticIssues));

        return CreateResult(
            gateResults,
            normalizedDocument,
            sectionPlans);
    }

    private static List<ValidationIssue> ValidateContract(
        string json,
        string sourcePath,
        string templateCatalogPath,
        string mappingCatalogPath,
        string governedLibraryPath)
    {
        var issues = new List<ValidationIssue>();

        if (string.IsNullOrWhiteSpace(json))
        {
            issues.Add(CreateIssue(ValidationGate.Contract, "$", "script.contract.required", "Script JSON is required."));
        }

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            issues.Add(CreateIssue(ValidationGate.Contract, "$.sourcePath", "script.contract.required", "Source path is required."));
        }

        if (string.IsNullOrWhiteSpace(templateCatalogPath))
        {
            issues.Add(CreateIssue(ValidationGate.Contract, "$.templateCatalogPath", "script.contract.required", "Template catalog path is required."));
        }

        if (string.IsNullOrWhiteSpace(mappingCatalogPath))
        {
            issues.Add(CreateIssue(ValidationGate.Contract, "$.mappingCatalogPath", "script.contract.required", "Mapping catalog path is required."));
        }

        if (string.IsNullOrWhiteSpace(governedLibraryPath))
        {
            issues.Add(CreateIssue(ValidationGate.Contract, "$.governedLibraryPath", "script.contract.required", "Governed library path is required."));
        }

        return issues;
    }

    private static List<ValidationIssue> ValidateSchema(string json, out ScriptCompilationDocument? document)
    {
        var issues = new List<ValidationIssue>();
        document = null;

        try
        {
            document = JsonSerializer.Deserialize<ScriptCompilationDocument>(json, SerializerOptions);
        }
        catch (JsonException exception)
        {
            issues.Add(CreateIssue(ValidationGate.Schema, "$", "script.schema.invalid", exception.Message));
            return issues;
        }

        if (document is null)
        {
            issues.Add(CreateIssue(ValidationGate.Schema, "$", "script.schema.invalid", "Script JSON could not be deserialized."));
        }

        return issues;
    }

    private static ScriptCompilationDocument NormalizeDocument(ScriptCompilationDocument document)
    {
        return new ScriptCompilationDocument
        {
            ScriptId = document.ScriptId.Trim(),
            Version = string.IsNullOrWhiteSpace(document.Version) ? "1.0.0" : document.Version.Trim(),
            ProjectName = document.ProjectName.Trim(),
            AssetRegistrySnapshotId = document.AssetRegistrySnapshotId.Trim(),
            Output = document.Output with
            {
                BackgroundColorHex = document.Output.BackgroundColorHex.Trim()
            },
            Sections = document.Sections
                .Select(section => new ScriptSectionDefinition
                {
                    SectionId = section.SectionId.Trim(),
                    Order = section.Order,
                    TemplateId = section.TemplateId.Trim(),
                    Headline = NormalizeOptionalValue(section.Headline),
                    SupportingText = NormalizeOptionalValue(section.SupportingText),
                    IllustrationAssetId = NormalizeOptionalValue(section.IllustrationAssetId),
                    DrawEffectProfileId = NormalizeOptionalValue(section.DrawEffectProfileId)
                })
                .OrderBy(section => section.Order)
                .ThenBy(section => section.SectionId, StringComparer.Ordinal)
                .ToList()
        };
    }

    private List<ValidationIssue> ValidateSemantic(
        ScriptCompilationDocument document,
        string sourcePath,
        string templateCatalogPath,
        string mappingCatalogPath,
        string governedLibraryPath,
        out IReadOnlyList<ScriptSectionCompilationPlan> sectionPlans)
    {
        var issues = new List<ValidationIssue>();
        var plans = new List<ScriptSectionCompilationPlan>();

        if (string.IsNullOrWhiteSpace(document.ScriptId))
        {
            issues.Add(CreateIssue(ValidationGate.Semantic, "$.scriptId", "script.contract.required", "scriptId is required."));
        }

        if (string.IsNullOrWhiteSpace(document.ProjectName))
        {
            issues.Add(CreateIssue(ValidationGate.Semantic, "$.projectName", "script.contract.required", "projectName is required."));
        }

        if (string.IsNullOrWhiteSpace(document.AssetRegistrySnapshotId))
        {
            issues.Add(CreateIssue(ValidationGate.Semantic, "$.assetRegistrySnapshotId", "script.contract.required", "assetRegistrySnapshotId is required."));
        }

        if (document.Sections.Count == 0)
        {
            issues.Add(CreateIssue(ValidationGate.Semantic, "$.sections", "script.contract.required", "At least one section is required."));
        }

        var duplicateSectionIds = document.Sections
            .GroupBy(section => section.SectionId, StringComparer.Ordinal)
            .Where(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1)
            .OrderBy(group => group.Key, StringComparer.Ordinal);
        var duplicateIndex = 0;
        foreach (var duplicateSectionId in duplicateSectionIds)
        {
            issues.Add(new ValidationIssue(
                ValidationGate.Semantic,
                "$.sections",
                ValidationSeverity.Error,
                "script.section.id.duplicate",
                $"Duplicate sectionId '{duplicateSectionId.Key}'.",
                duplicateIndex));
            duplicateIndex++;
        }

        if (issues.Count > 0)
        {
            sectionPlans = [];
            return issues;
        }

        var resolvedTemplateCatalogPath = ResolveInputPath(sourcePath, templateCatalogPath, DefaultTemplateCatalogPath);
        var resolvedMappingCatalogPath = ResolveInputPath(sourcePath, mappingCatalogPath, DefaultMappingCatalogPath);
        var resolvedGovernedLibraryPath = ResolveInputPath(sourcePath, governedLibraryPath, DefaultGovernedLibraryPath);

        var templateCatalog = ReadCatalog<TemplateCatalog>(
            resolvedTemplateCatalogPath,
            ValidationGate.Semantic,
            "$.templateCatalogPath",
            "script.template.unresolved",
            issues);
        var mappingCatalog = ReadCatalog<ScriptTemplateMappingCatalog>(
            resolvedMappingCatalogPath,
            ValidationGate.Semantic,
            "$.mappingCatalogPath",
            "script.mapping.rule.missing",
            issues);
        var governedLibrary = ReadCatalog<ScriptGovernedLibrary>(
            resolvedGovernedLibraryPath,
            ValidationGate.Semantic,
            "$.governedLibraryPath",
            "script.contract.required",
            issues);

        if (templateCatalog is null || mappingCatalog is null || governedLibrary is null)
        {
            sectionPlans = [];
            return issues;
        }

        if (!string.Equals(document.AssetRegistrySnapshotId, governedLibrary.SnapshotId, StringComparison.Ordinal))
        {
            issues.Add(CreateIssue(
                ValidationGate.Semantic,
                "$.assetRegistrySnapshotId",
                "script.contract.required",
                $"assetRegistrySnapshotId '{document.AssetRegistrySnapshotId}' does not match governed snapshot '{governedLibrary.SnapshotId}'."));
            sectionPlans = [];
            return issues;
        }

        foreach (var pair in document.Sections.Select((section, index) => (Section: section, Index: index)))
        {
            var section = pair.Section;
            var sectionPath = $"$.sections[{pair.Index}]";
            var sectionIssues = new List<ValidationIssue>();

            if (string.IsNullOrWhiteSpace(section.SectionId))
            {
                sectionIssues.Add(CreateIssue(ValidationGate.Semantic, $"{sectionPath}.sectionId", "script.contract.required", "sectionId is required."));
            }

            if (string.IsNullOrWhiteSpace(section.TemplateId))
            {
                sectionIssues.Add(CreateIssue(ValidationGate.Semantic, $"{sectionPath}.templateId", "script.contract.required", "templateId is required."));
            }

            if (sectionIssues.Count > 0)
            {
                issues.AddRange(sectionIssues);
                continue;
            }

            var templateEntry = templateCatalog.Templates.FirstOrDefault(candidate =>
                string.Equals(candidate.TemplateId, section.TemplateId, StringComparison.Ordinal));
            if (templateEntry is null || string.Equals(templateEntry.Status, "deprecated", StringComparison.OrdinalIgnoreCase))
            {
                sectionIssues.Add(CreateIssue(
                    ValidationGate.Semantic,
                    $"{sectionPath}.templateId",
                    "script.template.unresolved",
                    $"templateId '{section.TemplateId}' could not be resolved from '{resolvedTemplateCatalogPath}'."));
                issues.AddRange(sectionIssues);
                continue;
            }

            var mappingDefinition = mappingCatalog.Mappings.FirstOrDefault(candidate =>
                string.Equals(candidate.TemplateId, section.TemplateId, StringComparison.Ordinal) &&
                !string.Equals(candidate.Status, "deprecated", StringComparison.OrdinalIgnoreCase));
            if (mappingDefinition is null)
            {
                sectionIssues.Add(CreateIssue(
                    ValidationGate.Semantic,
                    $"{sectionPath}.templateId",
                    "script.mapping.rule.missing",
                    $"No committed mapping rules were found for templateId '{section.TemplateId}'."));
                issues.AddRange(sectionIssues);
                continue;
            }

            var templatePath = ResolveCatalogEntryPath(resolvedTemplateCatalogPath, templateEntry.EntryPath);
            var templateContract = _templateContractPipeline.Process(File.ReadAllText(templatePath), templatePath);
            if (!templateContract.IsSuccess || templateContract.Template is null)
            {
                sectionIssues.Add(CreateIssue(
                    ValidationGate.Semantic,
                    $"{sectionPath}.templateId",
                    "script.template.unresolved",
                    $"templateId '{section.TemplateId}' resolved to an invalid template contract."));
                issues.AddRange(sectionIssues);
                continue;
            }

            BuildSectionPlan(section, sectionPath, document.ScriptId, mappingDefinition, governedLibrary, templateContract.Template.Template, sectionIssues, plans);
            issues.AddRange(sectionIssues);
        }

        sectionPlans = plans;
        return issues;
    }

    private void BuildSectionPlan(
        ScriptSectionDefinition section,
        string sectionPath,
        string scriptId,
        ScriptTemplateMappingDefinition mappingDefinition,
        ScriptGovernedLibrary governedLibrary,
        SceneTemplateDefinition template,
        ICollection<ValidationIssue> sectionIssues,
        ICollection<ScriptSectionCompilationPlan> plans)
    {
        var mappingRules = mappingDefinition.FieldMappings
            .OrderBy(rule => rule.SlotId, StringComparer.Ordinal)
            .ThenBy(rule => rule.SourceField, StringComparer.Ordinal)
            .ToList();
        var slotIds = template.Slots
            .Select(slot => slot.SlotId)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var slot in template.Slots)
        {
            if (!slot.Required || !string.IsNullOrWhiteSpace(slot.DefaultValue))
            {
                continue;
            }

            if (mappingRules.Any(rule => string.Equals(rule.SlotId, slot.SlotId, StringComparison.Ordinal)))
            {
                continue;
            }

            sectionIssues.Add(CreateIssue(
                ValidationGate.Semantic,
                $"{sectionPath}.templateId",
                "script.mapping.rule.missing",
                $"Template slot '{slot.SlotId}' has no committed script mapping rule for templateId '{section.TemplateId}'."));
        }

        var slotBindings = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var rule in mappingRules)
        {
            if (!slotIds.Contains(rule.SlotId))
            {
                sectionIssues.Add(CreateIssue(
                    ValidationGate.Semantic,
                    $"{sectionPath}.templateId",
                    "script.mapping.rule.missing",
                    $"Mapping rule for templateId '{section.TemplateId}' targets unknown slot '{rule.SlotId}'."));
                continue;
            }

            if (!TryGetFieldValue(section, rule.SourceField, out var rawValue))
            {
                sectionIssues.Add(CreateIssue(
                    ValidationGate.Semantic,
                    $"{sectionPath}.templateId",
                    "script.mapping.rule.missing",
                    $"Mapping rule references unsupported source field '{rule.SourceField}'."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(rawValue))
            {
                if (rule.Required)
                {
                    sectionIssues.Add(CreateIssue(
                        ValidationGate.Semantic,
                        $"{sectionPath}.{rule.SourceField}",
                        "script.mapping.field.required",
                        $"Field '{rule.SourceField}' is required for templateId '{section.TemplateId}'."));
                }

                continue;
            }

            var normalizedValue = rawValue.Trim();
            slotBindings[rule.SlotId] = normalizedValue;

            if (string.Equals(rule.GovernedReferenceType, "asset", StringComparison.Ordinal))
            {
                var assetExists = governedLibrary.Assets.Any(asset =>
                    string.Equals(asset.AssetId, normalizedValue, StringComparison.Ordinal) &&
                    string.Equals(asset.Status, "active", StringComparison.OrdinalIgnoreCase));
                if (!assetExists)
                {
                    sectionIssues.Add(CreateIssue(
                        ValidationGate.Semantic,
                        $"{sectionPath}.{rule.SourceField}",
                        "script.governed.asset.missing",
                        $"Governed asset '{normalizedValue}' is not available in snapshot '{governedLibrary.SnapshotId}'."));
                }
            }
            else if (string.Equals(rule.GovernedReferenceType, "effect", StringComparison.Ordinal))
            {
                var effectExists = governedLibrary.EffectProfiles.Any(effect =>
                    string.Equals(effect.EffectProfileId, normalizedValue, StringComparison.Ordinal) &&
                    string.Equals(effect.ActionType, "draw", StringComparison.Ordinal) &&
                    string.Equals(effect.Status, "active", StringComparison.OrdinalIgnoreCase));
                if (!effectExists)
                {
                    sectionIssues.Add(CreateIssue(
                        ValidationGate.Semantic,
                        $"{sectionPath}.{rule.SourceField}",
                        "script.governed.effect.missing",
                        $"Governed effect profile '{normalizedValue}' is not available for action type 'draw' in snapshot '{governedLibrary.SnapshotId}'."));
                }
            }
        }

        if (sectionIssues.Count > 0)
        {
            return;
        }

        var slotValidation = _slotBindingValidator.Validate(template, slotBindings);
        if (!slotValidation.Success)
        {
            foreach (var validationIssue in slotValidation.Issues)
            {
                if (string.Equals(validationIssue.Code, "template.slot.unknown", StringComparison.Ordinal))
                {
                    sectionIssues.Add(CreateIssue(
                        ValidationGate.Semantic,
                        $"{sectionPath}.templateId",
                        "script.mapping.rule.missing",
                        validationIssue.Message));
                    continue;
                }

                if (string.Equals(validationIssue.Code, "template.slot.required", StringComparison.Ordinal))
                {
                    var slotId = validationIssue.Path.Split('.').Last();
                    var sourceRule = mappingRules.FirstOrDefault(rule => string.Equals(rule.SlotId, slotId, StringComparison.Ordinal));
                    if (sourceRule is null)
                    {
                        sectionIssues.Add(CreateIssue(
                            ValidationGate.Semantic,
                            $"{sectionPath}.templateId",
                            "script.mapping.rule.missing",
                            validationIssue.Message));
                    }
                    else
                    {
                        sectionIssues.Add(CreateIssue(
                            ValidationGate.Semantic,
                            $"{sectionPath}.{sourceRule.SourceField}",
                            "script.mapping.field.required",
                            $"Field '{sourceRule.SourceField}' is required for templateId '{section.TemplateId}'."));
                    }
                }
            }

            return;
        }

        var orderedSlotBindings = slotValidation.SlotBindings
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        plans.Add(new ScriptSectionCompilationPlan
        {
            Section = section,
            TemplateId = template.TemplateId,
            GovernedAssetId = orderedSlotBindings["illustrationAssetId"],
            GovernedEffectProfileId = orderedSlotBindings["drawEffectProfileId"],
            SlotBindings = orderedSlotBindings,
            InstantiationRequest = new TemplateInstantiationRequest
            {
                Template = template,
                SlotValues = orderedSlotBindings,
                InstanceId = $"{scriptId}.{section.SectionId}"
            }
        });
    }

    private static bool TryGetFieldValue(ScriptSectionDefinition section, string sourceField, out string? value)
    {
        value = null;

        if (string.Equals(sourceField, "headline", StringComparison.Ordinal))
        {
            value = section.Headline;
            return true;
        }

        if (string.Equals(sourceField, "supportingText", StringComparison.Ordinal))
        {
            value = section.SupportingText;
            return true;
        }

        if (string.Equals(sourceField, "illustrationAssetId", StringComparison.Ordinal))
        {
            value = section.IllustrationAssetId;
            return true;
        }

        if (string.Equals(sourceField, "drawEffectProfileId", StringComparison.Ordinal))
        {
            value = section.DrawEffectProfileId;
            return true;
        }

        return false;
    }

    private static T? ReadCatalog<T>(
        string path,
        ValidationGate gate,
        string issuePath,
        string issueCode,
        ICollection<ValidationIssue> issues)
    {
        if (!File.Exists(path))
        {
            issues.Add(CreateIssue(gate, issuePath, issueCode, $"Required file '{path}' was not found."));
            return default;
        }

        try
        {
            var value = JsonSerializer.Deserialize<T>(File.ReadAllText(path), SerializerOptions);
            if (value is not null)
            {
                return value;
            }
        }
        catch (JsonException exception)
        {
            issues.Add(CreateIssue(gate, issuePath, issueCode, exception.Message));
            return default;
        }

        issues.Add(CreateIssue(gate, issuePath, issueCode, $"Required file '{path}' could not be deserialized."));
        return default;
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

    private static string ResolveCatalogEntryPath(string catalogPath, string entryPath)
    {
        if (Path.IsPathRooted(entryPath))
        {
            return Path.GetFullPath(entryPath);
        }

        if (entryPath.StartsWith(".planning", StringComparison.OrdinalIgnoreCase))
        {
            return Path.GetFullPath(Path.Combine(FindRepoRoot(catalogPath), entryPath));
        }

        var catalogDirectory = Path.GetDirectoryName(catalogPath) ?? Environment.CurrentDirectory;
        return Path.GetFullPath(Path.Combine(catalogDirectory, entryPath));
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

    private static string? NormalizeOptionalValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static ValidationIssue CreateIssue(ValidationGate gate, string path, string code, string message)
    {
        return new ValidationIssue(gate, path, ValidationSeverity.Error, code, message);
    }

    private static ValidationGateResult CreateGateResult(ValidationGate gate, IEnumerable<ValidationIssue> issues)
    {
        return new ValidationGateResult(gate, ValidationIssueOrdering.Sort(issues));
    }

    private static ScriptCompilationPlan CreateResult(
        IReadOnlyList<ValidationGateResult> gateResults,
        ScriptCompilationDocument? document,
        IReadOnlyList<ScriptSectionCompilationPlan> sectionPlans)
    {
        var issues = ValidationIssueOrdering.Sort(gateResults.SelectMany(result => result.Issues));
        return new ScriptCompilationPlan
        {
            Gates = gateResults,
            Issues = issues,
            Document = document,
            Sections = sectionPlans
        };
    }
}
