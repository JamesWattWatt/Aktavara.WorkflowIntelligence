namespace Aktavara.WorkflowIntelligence.Core.Models;

/// <summary>
/// Configuration options for record attribute diffing.
/// </summary>
public class DiffOptions
{
    /// <summary>
    /// Gets or sets the attribute IDs that should be ignored when diffing.
    /// Changes to these attributes will not be reported.
    /// </summary>
    public HashSet<string> IgnoredAttributeIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets whether to treat empty strings as equivalent to null/missing values.
    /// </summary>
    public bool TreatEmptyAsNull { get; set; } = false;

    /// <summary>
    /// Gets or sets whether value comparison is case-sensitive.
    /// </summary>
    public bool CaseSensitiveComparison { get; set; } = true;

    /// <summary>
    /// Creates a default instance with standard system attributes ignored.
    /// </summary>
    public static DiffOptions Default => new()
    {
        IgnoredAttributeIds = new(StringComparer.OrdinalIgnoreCase)
        {
            "LastChangedDate",
            "LastChangedUser",
            "CreatedDate",
            "CreatedUser",
            "RecordId",
            "TypeId",
            "TypeKind"
        }
    };

    /// <summary>
    /// Creates a permissive instance that doesn't ignore any attributes.
    /// </summary>
    public static DiffOptions IncludeAll => new()
    {
        IgnoredAttributeIds = new(StringComparer.OrdinalIgnoreCase)
    };
}
