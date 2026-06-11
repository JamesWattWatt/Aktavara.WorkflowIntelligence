namespace Aktavara.WorkflowIntelligence.Core.Models;

/// <summary>
/// A serializable summary of a workflow match result, suitable for including
/// in an AssistantContextPacket to be sent to an LLM.
/// </summary>
public class WorkflowMatchSummary
{
    /// <summary>
    /// The workflow identifier.
    /// </summary>
    public string WorkflowId { get; set; } = string.Empty;

    /// <summary>
    /// The human-readable workflow name.
    /// </summary>
    public string WorkflowName { get; set; } = string.Empty;

    /// <summary>
    /// The confidence score (0-1).
    /// </summary>
    public double ConfidenceScore { get; set; }

    /// <summary>
    /// The human-readable confidence level (High/Medium/Low).
    /// </summary>
    public string ConfidenceLevel { get; set; } = string.Empty;

    /// <summary>
    /// The current state name from the workflow.
    /// </summary>
    public string? CurrentStateName { get; set; }

    /// <summary>
    /// List of rule descriptions that matched.
    /// </summary>
    public List<string> MatchedRules { get; set; } = new();

    /// <summary>
    /// Human-readable evidence descriptions for matched events.
    /// Format: "SearchRecords(Path) at 09:20:37", etc.
    /// </summary>
    public List<string> MatchedEvidence { get; set; } = new();

    /// <summary>
    /// List of required rule descriptions that did not match.
    /// </summary>
    public List<string> MissingRules { get; set; } = new();

    /// <summary>
    /// Hint for the next step in the workflow from the current state.
    /// </summary>
    public string? NextStepHint { get; set; }

    /// <summary>
    /// Score breakdown components (e.g., "Matched Rules": 0.95, "Sequence Bonus": 0.15, etc.).
    /// </summary>
    public Dictionary<string, double> ScoreBreakdown { get; set; } = new();
}
