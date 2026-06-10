using Aktavara.WorkflowIntelligence.Core.Models;

namespace Aktavara.WorkflowIntelligence.Core.Interfaces;

/// <summary>
/// Extracts structured data from Aktavara JSON payloads.
/// Handles the $type/$id reference system and converts JSON to record snapshots.
/// </summary>
public interface IAktaJsonExtractor
{
    /// <summary>
    /// Extracts record information from JSON content.
    /// </summary>
    /// <param name="json">The JSON content to parse.</param>
    /// <returns>A read-only list of extracted record snapshots. Empty list if no records found.</returns>
    IReadOnlyList<AktaRecordSnapshot> ExtractRecords(string json);

    /// <summary>
    /// Extracts a Path workspace structure from JSON.
    /// </summary>
    /// <param name="json">The JSON content containing path workspace data.</param>
    /// <returns>Path workspace snapshot if found; null otherwise.</returns>
    PathWorkspaceSnapshot? ExtractPathWorkspace(string json);

    /// <summary>
    /// Extracts a boolean result from JSON response.
    /// </summary>
    /// <param name="json">The JSON content to parse.</param>
    /// <returns>Boolean result if found; null otherwise.</returns>
    bool? ExtractBooleanResult(string json);
}
