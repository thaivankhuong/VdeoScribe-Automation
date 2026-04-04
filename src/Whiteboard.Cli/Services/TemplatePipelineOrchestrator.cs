using System.Text.Json;
using System.Text.Json.Serialization;
using Whiteboard.Cli.Contracts;
using Whiteboard.Cli.Models;
using Whiteboard.Core.Templates;
using Whiteboard.Core.Validation;

namespace Whiteboard.Cli.Services;

public sealed class TemplatePipelineOrchestrator : ITemplatePipelineOrchestrator
{
    private const string DefaultCatalogPath = ".planning/templates/index.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    private readonly ITemplateContractPipeline _templateContractPipeline;
    private readonly ITemplateSlotBindingValidator _slotBindingValidator;
    private readonly ITemplateComposer _templateComposer;

    public TemplatePipelineOrchestrator(
        ITemplateContractPipeline? templateContractPipeline = null,
        ITemplateSlotBindingValidator? templateSlotBindingValidator = null,
        ITemplateComposer? templateComposer = null)
    {
        _templateContractPipeline = templateContractPipeline ?? new TemplateContractPipeline();
        _slotBindingValidator = templateSlotBindingValidator ?? new TemplateSlotBindingValidator();
        _templateComposer = templateComposer ?? new TemplateComposer(_slotBindingValidator, new TemplateSlotValueResolver());
    }

    public CliTemplateValidateResult Validate(CliTemplateValidateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var resolution = ResolveTemplate(request.TemplateId, request.CatalogPath);
        if (!resolution.Success || resolution.Template is null)
        {
            return new CliTemplateValidateResult
            {
                Success = false,
                TemplateId = request.TemplateId,
                SlotValidationStatus = string.IsNullOrWhiteSpace(request.SlotValuesPath) ? "skipped" : "failed",
                Issues = resolution.Issues
            };
        }

        var slotValidationStatus = "skipped";
        var issues = new List<ValidationIssue>();
        if (!string.IsNullOrWhiteSpace(request.SlotValuesPath))
        {
            var slotValidation = _slotBindingValidator.Validate(
                resolution.Template.Template,
                LoadSlotValues(request.SlotValuesPath!));
            issues.AddRange(slotValidation.Issues);
            slotValidationStatus = slotValidation.Success ? "passed" : "failed";
        }

        return new CliTemplateValidateResult
        {
            Success = issues.Count == 0,
            TemplateId = resolution.Template.Template.TemplateId,
            Version = resolution.Template.Template.Version,
            Status = resolution.Template.Template.Status,
            SlotValidationStatus = slotValidationStatus,
            Issues = ValidationIssueOrdering.Sort(issues)
        };
    }

    public CliTemplateInstantiateResult Instantiate(CliTemplateInstantiateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var resolution = ResolveTemplate(request.TemplateId, request.CatalogPath);
        if (!resolution.Success || resolution.Template is null)
        {
            return new CliTemplateInstantiateResult
            {
                Success = false,
                TemplateId = request.TemplateId,
                InstanceId = request.InstanceId,
                OutputPath = request.OutputPath,
                Issues = resolution.Issues
            };
        }

        var slotValues = LoadSlotValues(request.SlotValuesPath);
        var slotValidation = _slotBindingValidator.Validate(resolution.Template.Template, slotValues);
        if (!slotValidation.Success)
        {
            return new CliTemplateInstantiateResult
            {
                Success = false,
                TemplateId = resolution.Template.Template.TemplateId,
                Version = resolution.Template.Template.Version,
                InstanceId = request.InstanceId,
                OutputPath = request.OutputPath,
                SlotValidationStatus = "failed",
                SlotBindings = slotValidation.SlotBindings,
                Issues = slotValidation.Issues
            };
        }

        var compositionResult = _templateComposer.Compose(new TemplateInstantiationRequest
        {
            Template = resolution.Template.Template,
            SlotValues = slotValidation.SlotBindings,
            InstanceId = request.InstanceId,
            TimeOffsetSeconds = request.TimeOffsetSeconds,
            LayerOffset = request.LayerOffset
        });

        if (!compositionResult.Success)
        {
            return new CliTemplateInstantiateResult
            {
                Success = false,
                TemplateId = compositionResult.TemplateId,
                Version = compositionResult.Version,
                InstanceId = compositionResult.InstanceId,
                OutputPath = request.OutputPath,
                SlotValidationStatus = "passed",
                SlotBindings = compositionResult.SlotBindings,
                Issues = compositionResult.Issues
            };
        }

        var outputPath = Path.GetFullPath(request.OutputPath);
        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var outputPayload = new
        {
            templateId = compositionResult.TemplateId,
            version = compositionResult.Version,
            instanceId = compositionResult.InstanceId,
            slotBindings = compositionResult.SlotBindings,
            scenes = compositionResult.Fragment.Scenes,
            timelineEvents = compositionResult.Fragment.TimelineEvents,
            deterministicKey = compositionResult.DeterministicKey
        };
        File.WriteAllText(outputPath, JsonSerializer.Serialize(outputPayload, SerializerOptions));

        return new CliTemplateInstantiateResult
        {
            Success = true,
            TemplateId = compositionResult.TemplateId,
            Version = compositionResult.Version,
            InstanceId = compositionResult.InstanceId,
            OutputPath = outputPath,
            SlotValidationStatus = "passed",
            DeterministicKey = compositionResult.DeterministicKey,
            SlotBindings = compositionResult.SlotBindings,
            Issues = []
        };
    }

    private TemplateResolutionResult ResolveTemplate(string templateId, string? catalogPath)
    {
        var normalizedTemplateId = templateId.Trim();
        var normalizedCatalogPath = Path.GetFullPath(string.IsNullOrWhiteSpace(catalogPath) ? DefaultCatalogPath : catalogPath);

        if (!File.Exists(normalizedCatalogPath))
        {
            return CreateFailureResult(
                new ValidationIssue(
                    ValidationGate.Contract,
                    "$.catalogPath",
                    ValidationSeverity.Error,
                    "template.catalog.template_missing",
                    $"Template catalog '{normalizedCatalogPath}' was not found."));
        }

        var catalog = JsonSerializer.Deserialize<TemplateCatalog>(File.ReadAllText(normalizedCatalogPath), SerializerOptions);
        if (catalog is null)
        {
            return CreateFailureResult(
                new ValidationIssue(
                    ValidationGate.Schema,
                    "$.catalog",
                    ValidationSeverity.Error,
                    "template.catalog.template_missing",
                    $"Template catalog '{normalizedCatalogPath}' could not be read."));
        }

        var entry = catalog.Templates.FirstOrDefault(candidate => string.Equals(candidate.TemplateId, normalizedTemplateId, StringComparison.Ordinal));
        if (entry is null)
        {
            return CreateFailureResult(
                new ValidationIssue(
                    ValidationGate.Semantic,
                    "$.templates",
                    ValidationSeverity.Error,
                    "template.catalog.template_missing",
                    $"Template '{normalizedTemplateId}' could not be resolved from catalog '{normalizedCatalogPath}'."));
        }

        if (string.Equals(entry.Status, "deprecated", StringComparison.OrdinalIgnoreCase))
        {
            return CreateFailureResult(
                new ValidationIssue(
                    ValidationGate.Semantic,
                    "$.templates",
                    ValidationSeverity.Error,
                    "template.catalog.template_deprecated",
                    $"Template '{normalizedTemplateId}' is deprecated and cannot be instantiated."));
        }

        var templatePath = ResolveEntryPath(normalizedCatalogPath, entry.EntryPath);
        var templateJson = File.ReadAllText(templatePath);
        var processingResult = _templateContractPipeline.Process(templateJson, templatePath);
        if (!processingResult.IsSuccess || processingResult.Template is null)
        {
            return new TemplateResolutionResult
            {
                Success = false,
                CatalogPath = normalizedCatalogPath,
                TemplatePath = templatePath,
                Issues = processingResult.Issues
            };
        }

        return new TemplateResolutionResult
        {
            Success = true,
            CatalogPath = normalizedCatalogPath,
            TemplatePath = templatePath,
            Template = processingResult.Template,
            Issues = []
        };
    }

    private static Dictionary<string, string> LoadSlotValues(string slotValuesPath)
    {
        var normalizedSlotValuesPath = Path.GetFullPath(slotValuesPath);
        var json = File.ReadAllText(normalizedSlotValuesPath);
        var slotValues = JsonSerializer.Deserialize<Dictionary<string, string>>(json, SerializerOptions);

        return slotValues ?? new Dictionary<string, string>(StringComparer.Ordinal);
    }

    private static string ResolveEntryPath(string catalogPath, string entryPath)
    {
        if (Path.IsPathRooted(entryPath))
        {
            return Path.GetFullPath(entryPath);
        }

        if (entryPath.StartsWith(".planning", StringComparison.OrdinalIgnoreCase))
        {
            var repoRoot = FindRepoRootFromCatalog(catalogPath);
            return Path.GetFullPath(Path.Combine(repoRoot, entryPath));
        }

        var catalogDirectory = Path.GetDirectoryName(catalogPath) ?? Environment.CurrentDirectory;
        return Path.GetFullPath(Path.Combine(catalogDirectory, entryPath));
    }

    private static string FindRepoRootFromCatalog(string catalogPath)
    {
        var current = new DirectoryInfo(Path.GetDirectoryName(catalogPath) ?? Environment.CurrentDirectory);
        while (current is not null)
        {
            if (string.Equals(current.Name, ".planning", StringComparison.OrdinalIgnoreCase))
            {
                return current.Parent?.FullName ?? current.FullName;
            }

            current = current.Parent;
        }

        return Environment.CurrentDirectory;
    }

    private static TemplateResolutionResult CreateFailureResult(ValidationIssue issue)
    {
        return new TemplateResolutionResult
        {
            Success = false,
            Issues = [issue]
        };
    }

    private sealed record TemplateResolutionResult
    {
        public bool Success { get; init; }
        public string CatalogPath { get; init; } = string.Empty;
        public string TemplatePath { get; init; } = string.Empty;
        public NormalizedSceneTemplateDefinition? Template { get; init; }
        public IReadOnlyList<ValidationIssue> Issues { get; init; } = [];
    }
}
