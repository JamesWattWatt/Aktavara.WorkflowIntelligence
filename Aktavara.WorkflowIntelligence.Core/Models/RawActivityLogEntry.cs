namespace Aktavara.WorkflowIntelligence.Core.Models;

/// <summary>
/// Represents a raw activity log entry as parsed from a log file or source.
/// Contains minimal processing - primarily extracted fields and raw content.
/// </summary>
public class RawActivityLogEntry
{
    /// <summary>
    /// Gets or sets the timestamp of when the activity occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the name of the user who initiated the activity.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the session identifier if available.
    /// May be null for stateless activities.
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Gets or sets the direction of the activity (Request or Response).
    /// </summary>
    public EventType Direction { get; set; }

    /// <summary>
    /// Gets or sets the name of the action that was performed.
    /// </summary>
    public string ActionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the raw, unparsed text content of the log entry.
    /// </summary>
    public string RawText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the collection of raw XML payloads extracted from the log entry.
    /// </summary>
    public List<string> RawXmlPayloads { get; set; } = new();

    /// <summary>
    /// Gets or sets any additional metadata or context associated with this entry.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
