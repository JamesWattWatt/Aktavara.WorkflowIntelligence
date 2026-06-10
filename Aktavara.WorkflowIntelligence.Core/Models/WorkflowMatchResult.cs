namespace Aktavara.WorkflowIntelligence.Core.Models;

/// <summary>
/// Represents the confidence level of a workflow match.
/// </summary>
public enum ConfidenceLevel
{
    /// <summary>High confidence in workflow match (>= 0.85)</summary>
    High,

    /// <summary>Medium confidence in workflow match (>= 0.55 and < 0.85)</summary>
    Medium,

    /// <summary>Low confidence in workflow match (< 0.55)</summary>
    Low
}

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
    public double ConfidenceScore { get; set; }

    /// <summary>
    /// Gets the categorized confidence level based on score.
    /// </summary>
    public ConfidenceLevel ConfidenceLevel => ConfidenceScore >= 0.85
        ? ConfidenceLevel.High
        : ConfidenceScore >= 0.55
            ? ConfidenceLevel.Medium
            : ConfidenceLevel.Low;

    /// <summary>
    /// Gets or sets the current state identifier in the workflow.
    /// Indicates where in the workflow progression the user is.
    /// </summary>
    public string? CurrentStateId { get; set; }

    /// <summary>
    /// Gets or sets the current state name for display.
    /// </summary>
    public string? CurrentStateName { get; set; }

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
    /// Shows which signature rules matched and their individual contributions.
    /// </summary>
    public Dictionary<string, double> RuleScores { get; set; } = new();

    /// <summary>
    /// Detailed breakdown of score calculation for debugging.
    /// </summary>
    public WorkflowScoreBreakdown ScoreBreakdown { get; set; } = new();

    /// <summary>
    /// Gets or sets timestamp when this match was calculated.
    /// </summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets additional match details and diagnostic information.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Determines if this is a strong match (high confidence).
    /// </summary>
    public bool IsStrongMatch => ConfidenceLevel == ConfidenceLevel.High;

    /// <summary>
    /// Determines if this is a weak match (low confidence).
    /// </summary>
    public bool IsWeakMatch => ConfidenceLevel == ConfidenceLevel.Low;

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
        $"{WorkflowName}: {ConfidenceScore:P0} confidence ({ConfidenceLevel}), State: {CurrentStateName ?? "unknown"}";
}

/// <summary>
/// Detailed breakdown of how the confidence score was calculated.
/// Useful for understanding and debugging match results.
/// </summary>
public class WorkflowScoreBreakdown
{
    /// <summary>
    /// Sum of weights for all matched rules.
    /// </summary>
    public double MatchedRulesWeight { get; set; }

    /// <summary>
    /// Total penalties applied for missing required rules.
    /// </summary>
    public double MissingRulesPenalty { get; set; }

    /// <summary>
    /// Bonus added for events appearing in expected sequence.
    /// </summary>
    public double SequenceBonus { get; set; }

    /// <summary>
    /// Bonus added for correlation between workspace, entities, and saves.
    /// </summary>
    public double EntityCorrelationBonus { get; set; }

    /// <summary>
    /// Penalty applied for events outside preferred time window.
    /// </summary>
    public double StalenesssPenalty { get; set; }

    /// <summary>
    /// Final score before clamping to [0, 1].
    /// </summary>
    public double RawScore { get; set; }

    /// <summary>
    /// Final clamped confidence score [0, 1].
    /// </summary>
    public double FinalScore { get; set; }

    /// <summary>
    /// Details about what contributed to each component.
    /// </summary>
    public Dictionary<string, string> Details { get; set; } = new();
}
