namespace Aktavara.WorkflowIntelligence.Core.Models;

/// <summary>
/// Represents the complete context packet to be sent to an AI assistant.
/// Contains the user's question, current activity context, possible workflows,
/// safe actions, and guidance to enable intelligent workflow assistance.
/// </summary>
public class AssistantContextPacket
{
    /// <summary>
    /// Gets or sets the user's question or request that prompted this context.
    /// </summary>
    public string UserQuestion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current activity context for the user.
    /// Includes recent events, active entities, and user state.
    /// </summary>
    public ActivityContext? ActivityContextSummary { get; set; }

    /// <summary>
    /// Gets or sets the possible workflows that match the current activity.
    /// Ordered by confidence score (highest first).
    /// </summary>
    public List<WorkflowMatchResult> WorkflowCandidates { get; set; } = new();

    /// <summary>
    /// Gets or sets the actions that are safe to recommend to the user.
    /// These are filtered based on current state and permissions.
    /// </summary>
    public List<SafeAction> SafeActions { get; set; } = new();

    /// <summary>
    /// Gets or sets references to help guides that are relevant to the current context.
    /// </summary>
    public List<HelpGuideReference> HelpGuideReferences { get; set; } = new();

    /// <summary>
    /// Gets or sets raw evidence references that support the analysis.
    /// These are snippets from activity logs or records that provide evidence.
    /// </summary>
    public List<EvidenceReference> RawEvidenceReferences { get; set; } = new();

    /// <summary>
    /// Gets or sets the time this context packet was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the username for whom this context was generated.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the session identifier if applicable.
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Gets or sets a summary of the context for logging or display.
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets confidence metrics about the overall analysis.
    /// </summary>
    public Dictionary<string, double> ConfidenceMetrics { get; set; } = new();

    /// <summary>
    /// Gets or sets additional diagnostic or metadata information.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets the best matching workflow candidate if any.
    /// </summary>
    public WorkflowMatchResult? GetBestWorkflowMatch() =>
        WorkflowCandidates.OrderByDescending(w => w.ConfidenceScore).FirstOrDefault();

    /// <summary>
    /// Gets all candidates that meet the confidence threshold.
    /// </summary>
    public List<WorkflowMatchResult> GetStrongMatches(double threshold = 0.6) =>
        WorkflowCandidates.Where(w => w.ConfidenceScore >= threshold).ToList();

    /// <summary>
    /// Gets the number of actionable items in this context.
    /// </summary>
    public int GetActionableItemCount() =>
        SafeActions.Count + HelpGuideReferences.Count;

    /// <summary>
    /// Generates a human-readable summary of this context packet.
    /// </summary>
    public string GenerateSummary()
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(UserQuestion))
            parts.Add($"Question: {UserQuestion}");

        if (WorkflowCandidates.Count > 0)
        {
            var best = GetBestWorkflowMatch();
            parts.Add($"Best match: {best?.WorkflowName} ({best?.ConfidenceScore:P0})");
        }

        if (SafeActions.Count > 0)
            parts.Add($"Recommended actions: {SafeActions.Count}");

        if (HelpGuideReferences.Count > 0)
            parts.Add($"Help guides: {HelpGuideReferences.Count}");

        return string.Join(" | ", parts);
    }
}

/// <summary>
/// Represents an action that is safe to recommend to the user.
/// </summary>
public class SafeAction
{
    /// <summary>
    /// Gets or sets the unique identifier for this action.
    /// </summary>
    public string ActionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the action.
    /// </summary>
    public string ActionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of what this action does.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the workflow this action belongs to.
    /// </summary>
    public string? WorkflowId { get; set; }

    /// <summary>
    /// Gets or sets how this action should be executed.
    /// </summary>
    public WorkflowActionExecutionMode ExecutionMode { get; set; }

    /// <summary>
    /// Gets or sets the confidence (0-1) that this action is appropriate right now.
    /// </summary>
    public double RelevanceScore { get; set; }

    /// <summary>
    /// Gets or sets the rationale for recommending this action.
    /// </summary>
    public string Rationale { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets any prerequisites or warnings about this action.
    /// </summary>
    public List<string> Prerequisites { get; set; } = new();
}

/// <summary>
/// Represents a reference to a help guide relevant to the current context.
/// </summary>
public class HelpGuideReference
{
    /// <summary>
    /// Gets or sets the unique identifier of the help guide.
    /// </summary>
    public string GuideId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the title of the help guide.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a brief summary or excerpt from the guide.
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL or path to access the full guide.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the workflow this guide is associated with.
    /// </summary>
    public string? WorkflowId { get; set; }

    /// <summary>
    /// Gets or sets the specific state this guide applies to.
    /// May be null if the guide applies to the whole workflow.
    /// </summary>
    public string? StateId { get; set; }

    /// <summary>
    /// Gets or sets the relevance score (0-1) of this guide to the current context.
    /// </summary>
    public double RelevanceScore { get; set; }
}

/// <summary>
/// Represents a reference to raw evidence from activity logs or records.
/// </summary>
public class EvidenceReference
{
    /// <summary>
    /// Gets or sets a unique identifier for this evidence.
    /// </summary>
    public string EvidenceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of evidence (Event, Record, Error, etc.).
    /// </summary>
    public string EvidenceType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the text snippet or content of the evidence.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp of the evidence.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the source of the evidence (log file, database, etc.).
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the relevance score (0-1) of this evidence.
    /// </summary>
    public double RelevanceScore { get; set; }

    /// <summary>
    /// Gets or sets whether this evidence supports or contradicts the analysis.
    /// </summary>
    public bool IsSupporting { get; set; } = true;

    /// <summary>
    /// Gets or sets notes about why this evidence is relevant.
    /// </summary>
    public string Notes { get; set; } = string.Empty;
}
