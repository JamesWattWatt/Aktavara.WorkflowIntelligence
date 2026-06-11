namespace Aktavara.WorkflowIntelligence.Core.Models.Api;

/// <summary>
/// Represents a workflow candidate from matching pipeline for API responses.
/// </summary>
public class WorkflowCandidateResult
{
    public string WorkflowId { get; set; } = string.Empty;
    public string WorkflowName { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public string ConfidenceLevel { get; set; } = string.Empty;
    public string CurrentStateName { get; set; } = string.Empty;
    public List<string> MatchedRules { get; set; } = new();
    public List<string> MatchedEvidence { get; set; } = new();
    public List<string> MissingRules { get; set; } = new();
    public string? NextStepHint { get; set; }
    public Dictionary<string, double> ScoreBreakdown { get; set; } = new();
}
