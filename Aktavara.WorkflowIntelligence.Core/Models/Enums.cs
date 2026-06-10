namespace Aktavara.WorkflowIntelligence.Core.Models;

/// <summary>
/// Represents the type of activity event that occurred.
/// </summary>
public enum EventType
{
    /// <summary>Records were searched or queried.</summary>
    SearchRecords,

    /// <summary>Workspace or path was opened.</summary>
    OpenWorkspace,

    /// <summary>Records were saved or persisted.</summary>
    SaveRecords,

    /// <summary>Record was created.</summary>
    RecordCreated,

    /// <summary>Record was updated.</summary>
    RecordUpdated,

    /// <summary>Record was deleted.</summary>
    RecordDeleted,

    /// <summary>Record state changed.</summary>
    StateChanged,

    /// <summary>Relationship or connection established.</summary>
    RelationshipEstablished,

    /// <summary>Relationship or connection removed.</summary>
    RelationshipRemoved,

    /// <summary>Validation occurred.</summary>
    ValidationPerformed,

    /// <summary>User interaction like selection or filtering.</summary>
    UserInteraction,

    /// <summary>System or workflow action executed.</summary>
    ActionExecuted,

    /// <summary>Request initiated by user or system.</summary>
    RequestInitiated,

    /// <summary>Response received from service or system.</summary>
    ResponseReceived,

    /// <summary>Error or exception occurred.</summary>
    ErrorOccurred,

    /// <summary>Unknown or unclassified event.</summary>
    Unknown
}

/// <summary>
/// Represents the kind/type of record being acted upon.
/// </summary>
public enum RecordKind
{
    /// <summary>A file path or location.</summary>
    Path,

    /// <summary>A node in a tree or hierarchy.</summary>
    Node,

    /// <summary>A connection or relationship between records.</summary>
    Connector,

    /// <summary>Other or unclassified record type.</summary>
    Other
}

/// <summary>
/// Represents the current status of a workflow definition.
/// </summary>
public enum WorkflowStatus
{
    /// <summary>Workflow is active and available.</summary>
    Active,

    /// <summary>Workflow is inactive or disabled.</summary>
    Inactive,

    /// <summary>Workflow is under review or testing.</summary>
    Draft,

    /// <summary>Workflow is deprecated and should not be used.</summary>
    Deprecated,

    /// <summary>Workflow is archived.</summary>
    Archived
}

/// <summary>
/// Represents the mode in which a workflow action should be executed.
/// </summary>
public enum WorkflowActionExecutionMode
{
    /// <summary>Action should be executed automatically.</summary>
    Automatic,

    /// <summary>User approval required before execution.</summary>
    RequiresApproval,

    /// <summary>Action is informational only.</summary>
    Informational,

    /// <summary>User should be prompted to execute the action.</summary>
    Prompt
}
