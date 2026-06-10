namespace Aktavara.WorkflowIntelligence.Core.Models;

/// <summary>
/// Represents a single rule in a workflow's activity signature.
/// A signature rule defines what types of activities indicate that a workflow is in progress.
/// Multiple rules together form a workflow's activity fingerprint.
/// </summary>
public class WorkflowSignatureRule
{
    /// <summary>
    /// Gets or sets the unique identifier for this rule.
    /// </summary>
    public string RuleId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the type of event this rule looks for.
    /// </summary>
    public EventType EventType { get; set; }

    /// <summary>
    /// Gets or sets the specific record kind this rule applies to.
    /// May be null if the rule applies to all record kinds.
    /// </summary>
    public RecordKind? RecordKind { get; set; }

    /// <summary>
    /// Gets or sets the workspace kind this rule applies to.
    /// May be null if the rule applies to all workspaces.
    /// </summary>
    public string? WorkspaceKind { get; set; }

    /// <summary>
    /// Gets or sets whether this event type is required for the workflow match.
    /// Required events must be present for the workflow to match.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Gets or sets the weight/importance of this rule in the overall signature.
    /// Higher weights contribute more significantly to the match confidence score.
    /// </summary>
    public double Weight { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the penalty applied if this required event is missing.
    /// Applied to the confidence score if the event is expected but not found.
    /// </summary>
    public double MissingPenalty { get; set; } = 0.1;

    /// <summary>
    /// Gets or sets the maximum age (in minutes) an event can be and still count toward this rule.
    /// May be null if there is no age restriction.
    /// </summary>
    public int? MaxAgeMinutes { get; set; }

    /// <summary>
    /// Gets or sets a human-readable description of what this rule detects.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Determines whether this rule matches the given activity event.
    /// </summary>
    /// <param name="activityEvent">The event to check.</param>
    /// <param name="eventAgeMinutes">The age of the event in minutes.</param>
    /// <returns>True if the event matches this rule; otherwise false.</returns>
    public bool Matches(ActivityEvent activityEvent, int eventAgeMinutes)
    {
        if (activityEvent.EventType != EventType)
            return false;

        if (RecordKind.HasValue && activityEvent.RecordKind != RecordKind)
            return false;

        if (!string.IsNullOrEmpty(WorkspaceKind) &&
            activityEvent.WorkspaceKind != WorkspaceKind)
            return false;

        if (MaxAgeMinutes.HasValue && eventAgeMinutes > MaxAgeMinutes)
            return false;

        return true;
    }
}
