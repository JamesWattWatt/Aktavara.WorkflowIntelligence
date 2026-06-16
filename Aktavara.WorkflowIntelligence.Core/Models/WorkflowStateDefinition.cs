namespace Aktavara.WorkflowIntelligence.Core.Models;

/// <summary>
/// Represents a state within a workflow definition.
/// States define stages of a workflow and what evidence/activities
/// are needed to determine the current state.
/// </summary>
public class WorkflowStateDefinition
{
    /// <summary>
    /// Gets or sets the unique identifier for this state.
    /// </summary>
    public string StateId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of this state.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a detailed description of this state.
    /// Describes what this state means in the context of the workflow.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of event type identifiers that constitute required evidence
    /// to determine that the workflow is in this state.
    /// </summary>
    public List<string> RequiredEvidence { get; set; } = new();

    /// <summary>
    /// Gets or sets the state identifier for the next step in the workflow.
    /// May be null if this is a terminal state.
    /// </summary>
    public string? NextStateId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of a help guide that provides assistance for this state.
    /// May be null if no specific help is available.
    /// </summary>
    public string? HelpGuideId { get; set; }

    /// <summary>
    /// Gets or sets the order/sequence of this state in the workflow.
    /// Lower numbers represent earlier stages.
    /// </summary>
    public int Sequence { get; set; }

    /// <summary>
    /// Gets or sets whether this is a terminal (final) state of the workflow.
    /// Terminal states have no next state.
    /// </summary>
    public bool IsTerminal { get; set; }

    /// <summary>
    /// Gets or sets metadata associated with this state.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of workshop questions to ask when this state is reached.
    /// Used to guide users through workflow qualification and refinement.
    /// </summary>
    public List<string> WorkshopQuestions { get; set; } = new();

    /// <summary>
    /// Determines whether this state has all required evidence present.
    /// </summary>
    /// <param name="evidenceAvailable">The set of evidence event types available.</param>
    /// <returns>True if all required evidence is present; otherwise false.</returns>
    public bool HasAllRequiredEvidence(IEnumerable<string> evidenceAvailable)
    {
        var available = new HashSet<string>(evidenceAvailable);
        return RequiredEvidence.All(evidence => available.Contains(evidence));
    }
}
