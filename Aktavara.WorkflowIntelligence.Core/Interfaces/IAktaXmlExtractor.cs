using Aktavara.WorkflowIntelligence.Core.Models;

namespace Aktavara.WorkflowIntelligence.Core.Interfaces;

/// <summary>
/// Extracts structured data from Aktavara XML payloads.
/// Converts raw XML into typed snapshot objects that represent records and their relationships.
/// </summary>
public interface IAktaXmlExtractor
{
    /// <summary>
    /// Extracts record information from XML content.
    /// Handles both single records and collections of records.
    /// </summary>
    /// <param name="xml">The XML content to parse.</param>
    /// <returns>A read-only list of extracted record snapshots. Empty list if no records found.</returns>
    IReadOnlyList<AktaRecordSnapshot> ExtractRecords(string xml);

    /// <summary>
    /// Extracts pagination information from XML response.
    /// Used to understand pagination in search results and list responses.
    /// </summary>
    /// <param name="xml">The XML content to parse.</param>
    /// <returns>Page information if found; null otherwise.</returns>
    PageInfoSnapshot? ExtractPageInfo(string xml);

    /// <summary>
    /// Extracts a complete Path workspace structure from XML.
    /// Includes the path record, nodes, connectors, and their relationships.
    /// </summary>
    /// <param name="xml">The XML content containing Path workspace data (PathWkData).</param>
    /// <returns>Path workspace snapshot if found; null otherwise.</returns>
    PathWorkspaceSnapshot? ExtractPathWorkspace(string xml);

    /// <summary>
    /// Extracts a simple boolean result from XML response.
    /// Used for operation results like success/failure indicators.
    /// </summary>
    /// <param name="xml">The XML content to parse.</param>
    /// <returns>Boolean result if found; null otherwise.</returns>
    bool? ExtractBooleanResult(string xml);
}
