namespace Aktavara.WorkflowIntelligence.Core.Models;

/// <summary>
/// Represents the current activity context for a user during a specific time window.
/// This is the processed summary of a user's recent actions and the state of entities
/// they have been working with.
/// </summary>
public class ActivityContext
{
    /// <summary>
    /// Gets or sets the name of the user for whom this context applies.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the start of the time window for collecting activity data.
    /// </summary>
    public DateTime TimeWindowStart { get; set; }

    /// <summary>
    /// Gets or sets the end of the time window for collecting activity data.
    /// </summary>
    public DateTime TimeWindowEnd { get; set; }

    /// <summary>
    /// Gets or sets the list of recent activity events in chronological order.
    /// </summary>
    public List<ActivityEvent> RecentEvents { get; set; } = new();

    /// <summary>
    /// Gets or sets the collection of entities that are actively being worked on.
    /// </summary>
    public List<ActiveEntity> ActiveEntities { get; set; } = new();

    /// <summary>
    /// Gets or sets the last known state of significant records/entities.
    /// Captures snapshots of important state for context building.
    /// </summary>
    public Dictionary<string, object> LastKnownState { get; set; } = new();

    /// <summary>
    /// Tracks records that were opened in this session.
    /// Maps RecordId to record information (name, type, etc.) for correlation.
    /// </summary>
    public Dictionary<string, OpenedRecordInfo> OpenedRecords { get; set; } = new();

    /// <summary>
    /// Gets or sets the session identifier from the most recent event.
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Gets or sets the current state of user activity in this context.
    /// Determined from the most recent meaningful event.
    /// </summary>
    public CurrentState CurrentState { get; set; } = CurrentState.NoActivity;

    /// <summary>
    /// Gets or sets a human-readable summary of the activity context.
    /// Typically generated from the recent events and active entities.
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the workflow hints derived from observable activity patterns.
    /// These are signals useful for matching against workflow definitions.
    /// </summary>
    public List<string> WorkflowHints { get; set; } = new();

    /// <summary>
    /// Gets the duration of the time window.
    /// </summary>
    public TimeSpan Duration => TimeWindowEnd - TimeWindowStart;

    /// <summary>
    /// Gets the number of recent events captured in this context.
    /// </summary>
    public int EventCount => RecentEvents.Count;

    /// <summary>
    /// Gets the number of active entities in this context.
    /// </summary>
    public int ActiveEntityCount => ActiveEntities.Count;

    /// <summary>
    /// Gets the most recent activity event if available.
    /// </summary>
    public ActivityEvent? GetMostRecentEvent() =>
        RecentEvents.OrderByDescending(e => e.Timestamp).FirstOrDefault();

    /// <summary>
    /// Gets all events of a specific type from the recent events.
    /// </summary>
    public List<ActivityEvent> GetEventsByType(EventType eventType) =>
        RecentEvents.Where(e => e.EventType == eventType).ToList();

    /// <summary>
    /// Gets all active entities of a specific record kind.
    /// </summary>
    public List<ActiveEntity> GetEntitiesByKind(RecordKind recordKind) =>
        ActiveEntities.Where(e => e.RecordKind == recordKind).ToList();

    /// <summary>
    /// Finds an active entity by its record ID.
    /// </summary>
    public ActiveEntity? FindEntityById(string recordId) =>
        ActiveEntities.FirstOrDefault(e => e.RecordId == recordId);

    /// <summary>
    /// Determines if there have been any errors in recent activity.
    /// </summary>
    public bool HasErrors() =>
        RecentEvents.Any(e => e.EventType == EventType.ErrorOccurred || e.IsSuccess == false);

    /// <summary>
    /// Gets a filtered view of recent events within a specific time range.
    /// </summary>
    public List<ActivityEvent> GetEventsInRange(DateTime startTime, DateTime endTime) =>
        RecentEvents.Where(e => e.Timestamp >= startTime && e.Timestamp <= endTime).ToList();

    /// <summary>
    /// Resolves the name of a record from the opened records in this session.
    /// Useful for correlating attribute saves back to workspace records.
    /// </summary>
    public string? ResolveRecordName(string recordId)
    {
        if (OpenedRecords.TryGetValue(recordId, out var recordInfo))
        {
            return recordInfo.Name;
        }
        return null;
    }

    /// <summary>
    /// Tracks an opened record for correlation with later attribute saves.
    /// </summary>
    public void TrackOpenedRecord(string recordId, string? name, string? workspaceKind, RecordKind recordKind)
    {
        OpenedRecords[recordId] = new OpenedRecordInfo
        {
            RecordId = recordId,
            Name = name,
            WorkspaceKind = workspaceKind,
            RecordKind = recordKind,
            OpenedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Information about a record that was opened in a workspace.
/// Used to correlate attribute saves back to the original record.
/// </summary>
public class OpenedRecordInfo
{
    /// <summary>
    /// The record's ID.
    /// </summary>
    public string RecordId { get; set; } = string.Empty;

    /// <summary>
    /// The record's name (if available).
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The type of workspace the record was opened in.
    /// </summary>
    public string? WorkspaceKind { get; set; }

    /// <summary>
    /// The kind of record.
    /// </summary>
    public RecordKind RecordKind { get; set; }

    /// <summary>
    /// When the record was opened in this session.
    /// </summary>
    public DateTime OpenedAt { get; set; }
}
