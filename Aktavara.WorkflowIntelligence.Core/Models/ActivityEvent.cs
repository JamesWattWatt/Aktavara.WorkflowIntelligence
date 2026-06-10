namespace Aktavara.WorkflowIntelligence.Core.Models;

/// <summary>
/// Represents a processed activity event extracted and enriched from raw log data.
/// An activity event is a meaningful unit of work that describes what happened in the system.
/// </summary>
public class ActivityEvent
{
    /// <summary>
    /// Gets or sets the unique identifier for this event.
    /// </summary>
    public string EventId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the timestamp when the event occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the name of the user who triggered the event.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the session identifier associated with this event.
    /// May be null for stateless operations.
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Gets or sets the type of event (Create, Update, Delete, etc.).
    /// </summary>
    public EventType EventType { get; set; }

    /// <summary>
    /// Gets or sets the name of the action that was performed.
    /// </summary>
    public string ActionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the kind of record being acted upon (Path, Node, Connector, Other).
    /// May be null if not applicable or unknown.
    /// </summary>
    public RecordKind? RecordKind { get; set; }

    /// <summary>
    /// Gets or sets the type identifier of the affected record.
    /// May be null if not applicable.
    /// </summary>
    public string? TypeId { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the affected record.
    /// May be null if not applicable or multiple records affected.
    /// </summary>
    public string? RecordId { get; set; }

    /// <summary>
    /// Gets or sets the name or display text of the affected record.
    /// May be null if not available.
    /// </summary>
    public string? RecordName { get; set; }

    /// <summary>
    /// Gets or sets the kind of workspace where the event occurred.
    /// Examples: "Design", "Management", "Administration".
    /// May be null if not applicable.
    /// </summary>
    public string? WorkspaceKind { get; set; }

    /// <summary>
    /// Gets or sets the state of the record after the event.
    /// Examples: "Active", "Draft", "Archived".
    /// May be null if not applicable.
    /// </summary>
    public string? RecordState { get; set; }

    /// <summary>
    /// Gets or sets whether the action completed successfully.
    /// May be null if success status is unknown.
    /// </summary>
    public bool? IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the collection of record identifiers related to this event.
    /// </summary>
    public List<string> RelatedRecordIds { get; set; } = new();

    /// <summary>
    /// Gets or sets the collection of attributes that changed during this event.
    /// </summary>
    public List<ChangedAttribute> ChangedAttributes { get; set; } = new();

    /// <summary>
    /// Gets or sets evidence or supporting data for this event.
    /// Could include error messages, validation results, or extracted payloads.
    /// </summary>
    public List<string> Evidence { get; set; } = new();

    /// <summary>
    /// Gets or sets additional contextual metadata about this event.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Determines whether this event represents a successful operation.
    /// </summary>
    public bool IsSuccessful => IsSuccess ?? true;

    /// <summary>
    /// Gets a summary description of this event.
    /// </summary>
    public string GetSummary() =>
        $"{EventType} - {ActionName} on {RecordName ?? RecordId ?? "unknown record"} by {UserName}";
}
