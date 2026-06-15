namespace Aktavara.WorkflowIntelligence.Core.Models.Api;

/// <summary>
/// Request body for POST /api/help-guides/suggest endpoint.
/// </summary>
public class SuggestGuideMappingRequest
{
    public string WorkflowId { get; set; } = string.Empty;
    public string WorkflowName { get; set; } = string.Empty;
    public string StepId { get; set; } = string.Empty;
    public string CurrentStateName { get; set; } = string.Empty;
    public List<string>? MatchedRules { get; set; }
    public List<string>? MatchedEvidence { get; set; }
}
