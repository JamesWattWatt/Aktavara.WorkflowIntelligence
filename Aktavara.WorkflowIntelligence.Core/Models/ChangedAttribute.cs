namespace Aktavara.WorkflowIntelligence.Core.Models;

/// <summary>
/// Represents a single attribute that changed during an activity event.
/// Captures the before and after values of an attribute change.
/// </summary>
public class ChangedAttribute
{
    /// <summary>
    /// Gets or sets the identifier of the attribute that changed.
    /// </summary>
    public string AttributeId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value of the attribute before the change.
    /// </summary>
    public object? FromValue { get; set; }

    /// <summary>
    /// Gets or sets the value of the attribute after the change.
    /// </summary>
    public object? ToValue { get; set; }

    /// <summary>
    /// Gets or sets the data type of the attribute values.
    /// May be null if the type is unknown or mixed.
    /// </summary>
    public string? ValueType { get; set; }

    /// <summary>
    /// Determines whether the attribute actually changed.
    /// </summary>
    /// <returns>True if FromValue and ToValue are different; otherwise false.</returns>
    public bool HasActualChange() =>
        !Equals(FromValue, ToValue);
}

/// <summary>
/// Represents a record as a C# record for immutable use cases with value semantics.
/// </summary>
public record ChangedAttributeRecord(
    string AttributeId,
    object? FromValue,
    object? ToValue,
    string? ValueType)
{
    /// <summary>
    /// Determines whether the attribute actually changed.
    /// </summary>
    public bool HasActualChange() => !Equals(FromValue, ToValue);
}
