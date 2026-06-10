using Aktavara.WorkflowIntelligence.Core.Interfaces;
using Aktavara.WorkflowIntelligence.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aktavara.WorkflowIntelligence.Core.Services;

/// <summary>
/// Service for computing attribute-level differences between record snapshots.
/// Uses property-based comparison to identify changes in values.
/// </summary>
public class RecordDiffService : IRecordDiffService
{
    private readonly ILogger<RecordDiffService> _logger;

    public RecordDiffService(ILogger<RecordDiffService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Computes differences using default options.
    /// </summary>
    public IReadOnlyList<ChangedAttribute> Diff(AktaRecordSnapshot before, AktaRecordSnapshot after)
    {
        return Diff(before, after, DiffOptions.Default);
    }

    /// <summary>
    /// Computes differences using custom options.
    /// </summary>
    public IReadOnlyList<ChangedAttribute> Diff(
        AktaRecordSnapshot before,
        AktaRecordSnapshot after,
        DiffOptions options)
    {
        if (before == null)
            throw new ArgumentNullException(nameof(before));
        if (after == null)
            throw new ArgumentNullException(nameof(after));
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        var changes = new List<ChangedAttribute>();

        // Verify records are the same type
        if (before.RecordId != after.RecordId)
        {
            _logger.LogWarning(
                "Diff called on different records: {BeforeId} vs {AfterId}",
                before.RecordId, after.RecordId);
            return changes.AsReadOnly();
        }

        // Build maps of properties by attribute ID
        var beforeProps = before.Properties.ToDictionary(p => p.AttributeId, StringComparer.OrdinalIgnoreCase);
        var afterProps = after.Properties.ToDictionary(p => p.AttributeId, StringComparer.OrdinalIgnoreCase);

        // Get all unique attribute IDs
        var allAttributeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        allAttributeIds.UnionWith(beforeProps.Keys);
        allAttributeIds.UnionWith(afterProps.Keys);

        // Compare each attribute
        foreach (var attributeId in allAttributeIds)
        {
            // Skip ignored attributes
            if (options.IgnoredAttributeIds.Contains(attributeId))
                continue;

            var hasBefore = beforeProps.TryGetValue(attributeId, out var beforeProp);
            var hasAfter = afterProps.TryGetValue(attributeId, out var afterProp);

            var beforeValue = hasBefore ? beforeProp!.Value : null;
            var afterValue = hasAfter ? afterProp!.Value : null;
            var afterValueType = hasAfter ? afterProp!.ValueType : null;

            // Check if values are different
            if (!ValuesAreEqual(beforeValue, afterValue, options))
            {
                var change = new ChangedAttribute
                {
                    AttributeId = attributeId,
                    FromValue = beforeValue,
                    ToValue = afterValue,
                    ValueType = afterValueType ?? (hasBefore ? beforeProp!.ValueType : null)
                };

                changes.Add(change);

                _logger.LogDebug(
                    "Attribute {AttributeId} changed from {FromValue} to {ToValue}",
                    attributeId, FormatValue(beforeValue), FormatValue(afterValue));
            }
        }

        _logger.LogInformation(
            "Diff for {RecordId}: {ChangeCount} changes detected",
            before.RecordId, changes.Count);

        return changes.AsReadOnly();
    }

    /// <summary>
    /// Determines if two values are equal according to diff options.
    /// </summary>
    private bool ValuesAreEqual(object? value1, object? value2, DiffOptions options)
    {
        // Both null/missing = equal
        if (value1 == null && value2 == null)
            return true;

        // One null, one not = not equal
        if ((value1 == null) != (value2 == null))
            return false;

        // Convert to strings for comparison
        var str1 = value1?.ToString() ?? string.Empty;
        var str2 = value2?.ToString() ?? string.Empty;

        // Handle empty as null if configured
        if (options.TreatEmptyAsNull)
        {
            str1 = string.IsNullOrEmpty(str1) ? null : str1;
            str2 = string.IsNullOrEmpty(str2) ? null : str2;

            if ((str1 == null) && (str2 == null))
                return true;
            if ((str1 == null) != (str2 == null))
                return false;
        }

        // String comparison with case sensitivity option
        if (options.CaseSensitiveComparison)
        {
            return str1 == str2;
        }
        else
        {
            return string.Equals(str1, str2, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Formats a value for logging.
    /// </summary>
    private string FormatValue(object? value) =>
        value switch
        {
            null => "<null>",
            string s when string.IsNullOrEmpty(s) => "<empty>",
            _ => value.ToString() ?? "<empty>"
        };
}
