namespace Aktavara.WorkflowIntelligence.Core.Models;

/// <summary>
/// Represents an inferred workflow suggestion from activity analysis.
/// </summary>
public class InferredWorkflowSuggestion
{
    public string SuggestedName { get; set; } = string.Empty;
    public string SuggestedDescription { get; set; } = string.Empty;
    public string SuggestedRiskLevel { get; set; } = "Medium";
    public List<string> SuggestedTags { get; set; } = new();
    public List<WorkflowSignatureRule> SuggestedRules { get; set; } = new();
    public List<WorkflowStateDefinition> SuggestedStates { get; set; } = new();
    public double SuggestedThreshold { get; set; } = 0.7;
    public List<WorkflowVariant> Variants { get; set; } = new();
    public int EvidenceSessions { get; set; }
    public int EvidenceEvents { get; set; }
    public List<string> InferenceNotes { get; set; } = new();
}
