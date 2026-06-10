namespace Aktavara.WorkflowIntelligence.Core.Models;

/// <summary>
/// Represents an entity (record) that is currently active in the user's work context.
/// An active entity is a record that was recently accessed or modified and is likely
/// still relevant to the current workflow.
/// </summary>
public class ActiveEntity
{
    /// <summary>
    /// Gets or sets the kind of record (Path, Node, Connector, Other).
    /// </summary>
    public RecordKind RecordKind { get; set; }

    /// <summary>
    /// Gets or sets the type identifier of this entity.
    /// Identifies the category or class of the entity.
    /// </summary>
    public string TypeId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier of this entity.
    /// </summary>
    public string RecordId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of this entity.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current state of the entity.
    /// Examples: "Active", "Draft", "Archived".
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this entity was last accessed or modified.
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the attributes of this entity as key-value pairs.
    /// </summary>
    public Dictionary<string, object> Attributes { get; set; } = new();

    /// <summary>
    /// Gets or sets related entity identifiers that are associated with this entity.
    /// </summary>
    public List<string> RelatedEntityIds { get; set; } = new();

    /// <summary>
    /// Gets a unique key for this entity used in collections.
    /// </summary>
    public string GetKey() => $"{TypeId}:{RecordId}";
}

/// <summary>
/// Immutable record version of ActiveEntity for functional programming patterns.
/// </summary>
public record ActiveEntityRecord(
    RecordKind RecordKind,
    string TypeId,
    string RecordId,
    string Name,
    string? State,
    DateTime LastModified,
    Dictionary<string, object> Attributes,
    List<string> RelatedEntityIds)
{
    /// <summary>
    /// Gets a unique key for this entity.
    /// </summary>
    public string GetKey() => $"{TypeId}:{RecordId}";
}
