namespace Aktavara.WorkflowIntelligence.Api.Services;

using Aktavara.WorkflowIntelligence.Core.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Simple mapping entry that extracts values from JsonElement before document disposal.
/// </summary>
internal class MappingEntry
{
    public string WorkflowId { get; set; } = string.Empty;
    public string StepId { get; set; } = string.Empty;
    public string GuideFile { get; set; } = string.Empty;
    public string? SectionId { get; set; }
}

public class HelpGuideMappingWriter : IHelpGuideMappingWriter
{
    private readonly string _mappingFilePath;
    private readonly IHelpGuideStore _helpGuideStore;
    private readonly ILogger<HelpGuideMappingWriter> _logger;
    private static readonly object _lockObject = new();

    public HelpGuideMappingWriter(
        IConfiguration configuration,
        IHelpGuideStore helpGuideStore,
        ILogger<HelpGuideMappingWriter> logger)
    {
        _helpGuideStore = helpGuideStore;
        _logger = logger;
        var helpGuidesPath = configuration.GetValue<string>("WorkflowIntelligence:HelpGuidesPath") ?? "help-guides/";
        _mappingFilePath = Path.Combine(helpGuidesPath, "workflow-guide-mapping.json");
    }

    public async Task SaveMappingAsync(
        string workflowId,
        string stepId,
        string guideFile,
        string? sectionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedStepId = NormalizeStepId(stepId);

            lock (_lockObject)
            {
                var mappingEntries = LoadMappingArray();

                // Remove existing mapping for this workflow:step
                mappingEntries.RemoveAll(m =>
                    m.WorkflowId == workflowId &&
                    m.StepId == normalizedStepId);

                // Add new mapping
                mappingEntries.Add(new MappingEntry
                {
                    WorkflowId = workflowId,
                    StepId = normalizedStepId,
                    GuideFile = guideFile,
                    SectionId = sectionId
                });

                // Convert back to dictionaries for JSON serialization
                var mappingDicts = mappingEntries.Select(m => new Dictionary<string, object?>
                {
                    { "workflowId", m.WorkflowId },
                    { "stepId", m.StepId },
                    { "guideFile", m.GuideFile },
                    { "sectionId", m.SectionId }
                }).ToList();

                // Write the file with "mappings" wrapper
                var root = new Dictionary<string, object>
                {
                    { "mappings", mappingDicts }
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                var json = JsonSerializer.Serialize(root, options);
                File.WriteAllText(_mappingFilePath, json);

                _logger.LogInformation("Saved help guide mapping: {WorkflowId}:{StepId} -> {GuideFile}", workflowId, normalizedStepId, guideFile);
            }

            // Reload the store to pick up the new mapping
            _helpGuideStore.Reload();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving help guide mapping for {WorkflowId}:{StepId}", workflowId, stepId);
            throw;
        }
    }

    private List<MappingEntry> LoadMappingArray()
    {
        try
        {
            if (!File.Exists(_mappingFilePath))
            {
                return new List<MappingEntry>();
            }

            var json = File.ReadAllText(_mappingFilePath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("mappings", out var mappingsArray))
            {
                return new List<MappingEntry>();
            }

            // Extract all values while JsonDocument is still in scope
            var entries = new List<MappingEntry>();
            foreach (var mapping in mappingsArray.EnumerateArray())
            {
                var entry = new MappingEntry
                {
                    WorkflowId = mapping.TryGetProperty("workflowId", out var wf) ? wf.GetString() ?? string.Empty : string.Empty,
                    StepId = mapping.TryGetProperty("stepId", out var st) ? st.GetString() ?? string.Empty : string.Empty,
                    GuideFile = mapping.TryGetProperty("guideFile", out var gf) ? gf.GetString() ?? string.Empty : string.Empty,
                    SectionId = mapping.TryGetProperty("sectionId", out var si) && si.ValueKind != JsonValueKind.Null ? si.GetString() : null
                };
                entries.Add(entry);
            }

            return entries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading help guide mappings");
            return new List<MappingEntry>();
        }
    }

    private static string NormalizeStepId(string stepId)
    {
        return stepId.ToLowerInvariant().Trim().Replace(" ", "_").Replace("-", "_");
    }
}
