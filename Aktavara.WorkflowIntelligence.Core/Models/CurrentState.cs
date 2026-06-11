namespace Aktavara.WorkflowIntelligence.Core.Models;

/// <summary>
/// Represents the current state of user activity within a time window.
/// Determined from the most recent meaningful event in the context window.
/// </summary>
public enum CurrentState
{
    /// <summary>
    /// No activity has been recorded in the time window.
    /// </summary>
    NoActivity = 0,

    /// <summary>
    /// A path workspace has been opened but not yet modified.
    /// </summary>
    PathOpened = 1,

    /// <summary>
    /// A node has been opened and modified but not yet saved.
    /// </summary>
    NodeModified = 2,

    /// <summary>
    /// A node has been saved (SaveRecords with RecordKind=Node).
    /// </summary>
    NodeSaved = 3,

    /// <summary>
    /// A connector has been created and saved (SaveRecords with RecordKind=Connector).
    /// </summary>
    ConnectorCreated = 4,

    /// <summary>
    /// A path has been modified and saved (SaveRecords with RecordKind=Path, State=Modified).
    /// </summary>
    PathSaved = 5,

    /// <summary>
    /// A new path has been created and saved (SaveRecords with RecordKind=Path, State=Added).
    /// </summary>
    PathCreated = 6,

    /// <summary>
    /// The current state could not be determined from available events.
    /// </summary>
    Unknown = 7
}
