namespace Aktavara.WorkflowIntelligence.Core.Models.Api;

/// <summary>
/// Response from POST /api/workflows/infer/name endpoint.
/// </summary>
public class InferredNameSuggestion
{
    public string SuggestedName { get; set; } = string.Empty;
    public string SuggestedDescription { get; set; } = string.Empty;
    public List<string> AlternativeNames { get; set; } = new();
}
