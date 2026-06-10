using Aktavara.WorkflowIntelligence.Core.Models;

namespace Aktavara.WorkflowIntelligence.Core.Interfaces;

/// <summary>
/// Normalizes raw activity log entries into structured activity events.
/// Handles action-specific extraction, XML parsing, and request/response correlation.
/// </summary>
public interface IActivityEventNormalizer
{
    /// <summary>
    /// Normalizes a list of raw activity log entries into activity events.
    /// Performs XML extraction, action-specific processing, and correlation.
    /// </summary>
    /// <param name="rawEntries">Raw log entries to normalize.</param>
    /// <returns>A read-only list of normalized activity events.</returns>
    IReadOnlyList<ActivityEvent> Normalize(IReadOnlyList<RawActivityLogEntry> rawEntries);
}
