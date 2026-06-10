namespace Aktavara.WorkflowIntelligence.Core.Models;

/// <summary>
/// Represents a complete workflow definition including its structure, signature, and guidance.
/// A workflow definition describes a repeatable business process that can be detected
/// from activity logs and assisted by the AI system.
/// </summary>
public class WorkflowDefinition
{
    /// <summary>
    /// Gets or sets the unique identifier for this workflow.
    /// </summary>
    public string WorkflowId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the workflow.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a detailed description of what this workflow accomplishes.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version of this workflow definition.
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Gets or sets the current status of this workflow definition.
    /// </summary>
    public WorkflowStatus Status { get; set; } = WorkflowStatus.Active;

    /// <summary>
    /// Gets or sets the activity signature rules that identify this workflow in action.
    /// These rules are used to detect when a user is performing this workflow.
    /// </summary>
    public List<WorkflowSignatureRule> ActivitySignature { get; set; } = new();

    /// <summary>
    /// Gets or sets the states that comprise this workflow.
    /// States define the progression through the workflow.
    /// </summary>
    public List<WorkflowStateDefinition> States { get; set; } = new();

    /// <summary>
    /// Gets or sets the help guide identifiers associated with this workflow.
    /// Help guides provide step-by-step assistance for completing the workflow.
    /// </summary>
    public List<string> HelpGuideIds { get; set; } = new();

    /// <summary>
    /// Gets or sets the actions that can be performed within this workflow.
    /// </summary>
    public List<WorkflowAction> Actions { get; set; } = new();

    /// <summary>
    /// Gets or sets tags that categorize or label this workflow.
    /// Examples: "design", "documentation", "validation", "deployment".
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Gets or sets the minimum confidence score (0-1) required to match this workflow.
    /// </summary>
    public double MinimumConfidenceThreshold { get; set; } = 0.6;

    /// <summary>
    /// Gets or sets the user or system that created this workflow definition.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when this workflow was created.
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when this workflow was last modified.
    /// </summary>
    public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets additional metadata about this workflow.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets the initial/starting state of this workflow.
    /// </summary>
    public WorkflowStateDefinition? GetInitialState() =>
        States.OrderBy(s => s.Sequence).FirstOrDefault();

    /// <summary>
    /// Gets a specific state by its identifier.
    /// </summary>
    public WorkflowStateDefinition? GetStateById(string stateId) =>
        States.FirstOrDefault(s => s.StateId == stateId);

    /// <summary>
    /// Gets all required signature rules (events that must occur).
    /// </summary>
    public List<WorkflowSignatureRule> GetRequiredSignatures() =>
        ActivitySignature.Where(r => r.Required).ToList();

    /// <summary>
    /// Determines if this workflow matches a set of activity events.
    /// </summary>
    /// <param name="events">The activity events to check.</param>
    /// <returns>True if the events match this workflow's signature; otherwise false.</returns>
    public bool MatchesSignature(List<ActivityEvent> events)
    {
        var requiredRules = GetRequiredSignatures();
        var now = DateTime.UtcNow;

        foreach (var rule in requiredRules)
        {
            var found = false;
            foreach (var evt in events)
            {
                var ageMinutes = (int)(now - evt.Timestamp).TotalMinutes;
                if (rule.Matches(evt, ageMinutes))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Determines if this workflow is active and available for use.
    /// </summary>
    public bool IsActive => Status == WorkflowStatus.Active;
}

/// <summary>
/// Represents an action that can be performed within a workflow.
/// </summary>
public class WorkflowAction
{
    /// <summary>
    /// Gets or sets the unique identifier for this action.
    /// </summary>
    public string ActionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the action.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of what this action does.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets how this action should be executed.
    /// </summary>
    public WorkflowActionExecutionMode ExecutionMode { get; set; } = WorkflowActionExecutionMode.Prompt;

    /// <summary>
    /// Gets or sets the state in which this action is available.
    /// </summary>
    public string? AvailableInStateId { get; set; }

    /// <summary>
    /// Gets or sets metadata about this action.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
