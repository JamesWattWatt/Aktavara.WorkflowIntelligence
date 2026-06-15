namespace Aktavara.WorkflowIntelligence.Api.Services;

using Aktavara.WorkflowIntelligence.Core.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;

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
                var mappingArray = LoadMappingArray();

                // Remove existing mapping for this workflow:step
                mappingArray.RemoveAll(m =>
                    m.GetProperty("workflowId").GetString() == workflowId &&
                    m.GetProperty("stepId").GetString() == normalizedStepId);

                // Add new mapping
                var newMapping = new Dictionary<string, object>
                {
                    { "workflowId", workflowId },
                    { "stepId", normalizedStepId },
                    { "guideFile", guideFile },
                };

                if (sectionId != null)
                {
                    newMapping["sectionId"] = sectionId;
                }
                else
                {
                    newMapping["sectionId"] = null!;
                }

                mappingArray.Add(JsonSerializer.SerializeToElement(newMapping));

                // Write the file with "mappings" wrapper
                var root = new Dictionary<string, object>
                {
                    { "mappings", mappingArray }
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.Never
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

    private List<JsonElement> LoadMappingArray()
    {
        try
        {
            if (!File.Exists(_mappingFilePath))
            {
                return new List<JsonElement>();
            }

            var json = File.ReadAllText(_mappingFilePath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("mappings", out var mappingsArray))
            {
                return new List<JsonElement>();
            }

            return mappingsArray.EnumerateArray().ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading help guide mappings");
            return new List<JsonElement>();
        }
    }

    private static string NormalizeStepId(string stepId)
    {
        return stepId.ToLowerInvariant().Trim().Replace(" ", "_").Replace("-", "_");
    }
}
