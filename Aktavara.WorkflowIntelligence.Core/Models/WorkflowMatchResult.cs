namespace Aktavara.WorkflowIntelligence.Core.Models;

/// <summary>
/// Represents the result of matching activity events against a workflow definition.
/// Contains the workflow that matched, the confidence score, and what evidence was found or missing.
/// </summary>
public class WorkflowMatchResult
{
    /// <summary>
    /// Gets or sets the unique identifier of the matched workflow.
    /// </summary>
    public string WorkflowId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the matched workflow.
    /// </summary>
    public string WorkflowName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the confidence score (0-1) indicating how well the activities match this workflow.
    /// 1.0 = perfect match, 0.0 = no match.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Gets or sets the current state identifier in the workflow.
    /// Indicates where in the workflow progression the user is.
    /// </summary>
    public string? CurrentStateId { get; set; }

    /// <summary>
    /// Gets or sets the next state identifier the workflow progression suggests.
    /// May be null if at a terminal state.
    /// </summary>
    public string? NextStateId { get; set; }

    /// <summary>
    /// Gets or sets the activity events that matched this workflow's signature.
    /// These are the key events that contribute to this match.
    /// </summary>
    public List<ActivityEvent> MatchedEvidence { get; set; } = new();

    /// <summary>
    /// Gets or sets the activity events that were expected but not found.
    /// These indicate gaps in the expected workflow progression.
    /// </summary>
    public List<string> MissingEvidence { get; set; } = new();

    /// <summary>
    /// Gets or sets warnings or notes about this match.
    /// Examples: "Missing critical step", "Unusual activity sequence".
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Gets or sets the rule matches that contributed to this result.
    /// Shows which signature rules matched and their individual confidence contributions.
    /// </summary>
    public Dictionary<string, double> RuleScores { get; set; } = new();

    /// <summary>
    /// Gets or sets timestamp when this match was calculated.
    /// </summary>
    public DateTime MatchedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets additional match details and diagnostic information.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Determines if this is a strong match (above standard threshold).
    /// </summary>
    public bool IsStrongMatch => Confidence >= 0.7;

    /// <summary>
    /// Determines if this is a weak match (below standard threshold).
    /// </summary>
    public bool IsWeakMatch => Confidence < 0.5;

    /// <summary>
    /// Gets the percentage of missing evidence relative to total expected evidence.
    /// </summary>
    public double GetMissingEvidencePercentage()
    {
        var total = MatchedEvidence.Count + MissingEvidence.Count;
        return total == 0 ? 0 : (double)MissingEvidence.Count / total * 100;
    }

    /// <summary>
    /// Gets a summary string describing this match result.
    /// </summary>
    public string GetSummary() =>
        $"{WorkflowName}: {Confidence:P0} confidence, {MatchedEvidence.Count} matched events, {MissingEvidence.Count} missing";
}
