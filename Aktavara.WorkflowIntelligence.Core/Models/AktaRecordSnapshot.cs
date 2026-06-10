namespace Aktavara.WorkflowIntelligence.Core.Models;

/// <summary>
/// Represents a snapshot of an Aktavara record extracted from XML.
/// A record is a persistent entity like a design object, configuration, or data element.
/// </summary>
public class AktaRecordSnapshot
{
    /// <summary>
    /// Gets or sets the kind/type of record (e.g., "Path", "Node", "Connector").
    /// </summary>
    public string TypeKind { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type identifier of the record.
    /// Identifies the class or category of the record.
    /// </summary>
    public string TypeId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier of the record.
    /// </summary>
    public string RecordId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the record was last changed.
    /// May be null if not available in the source XML.
    /// </summary>
    public DateTime? LastChangedDate { get; set; }

    /// <summary>
    /// Gets or sets the current state of the record.
    /// Examples: "Active", "Draft", "Released", "Archived".
    /// </summary>
    public string RecordState { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the stage identifier if the record supports stages.
    /// May be null if not applicable.
    /// </summary>
    public string? StageId { get; set; }

    /// <summary>
    /// Gets or sets the collection of properties/attributes of this record.
    /// </summary>
    public List<AktaRecordPropertySnapshot> Properties { get; set; } = new();

    /// <summary>
    /// Gets or sets additional metadata about this record.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Finds a property by its attribute identifier.
    /// </summary>
    public AktaRecordPropertySnapshot? FindProperty(string attributeId) =>
        Properties.FirstOrDefault(p => p.AttributeId == attributeId);

    /// <summary>
    /// Gets the value of a property by attribute identifier.
    /// </summary>
    public object? GetPropertyValue(string attributeId) =>
        FindProperty(attributeId)?.Value;

    /// <summary>
    /// Gets a summary string describing this record.
    /// </summary>
    public string GetSummary() =>
        $"{TypeKind}:{TypeId}[{RecordId}] ({RecordState})";
}

/// <summary>
/// Represents a single property/attribute of an Aktavara record.
/// </summary>
public class AktaRecordPropertySnapshot
{
    /// <summary>
    /// Gets or sets the identifier of the attribute.
    /// </summary>
    public string AttributeId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value of the attribute.
    /// May be null if the attribute has no value.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Gets or sets the data type of the value.
    /// Examples: "String", "Integer", "DateTime", "Boolean".
    /// May be null if type is unknown.
    /// </summary>
    public string? ValueType { get; set; }

    /// <summary>
    /// Gets or sets the raw XML type information (xsi:type).
    /// Useful for debugging and understanding complex types.
    /// </summary>
    public string? XsiType { get; set; }

    /// <summary>
    /// Gets a string representation of the property.
    /// </summary>
    public override string ToString() =>
        $"{AttributeId}={Value} ({ValueType})";
}
