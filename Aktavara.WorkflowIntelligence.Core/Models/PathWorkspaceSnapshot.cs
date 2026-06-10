namespace Aktavara.WorkflowIntelligence.Core.Models;

/// <summary>
/// Represents a snapshot of a Path workspace extracted from XML.
/// A Path is a hierarchical container that holds nodes and connectors with relationships.
/// </summary>
public class PathWorkspaceSnapshot
{
    /// <summary>
    /// Gets or sets the main Path record itself.
    /// </summary>
    public AktaRecordSnapshot PathRecord { get; set; } = null!;

    /// <summary>
    /// Gets or sets the StartVertex (node) records within this path.
    /// These are nodes that serve as starting points for connections.
    /// </summary>
    public List<AktaRecordSnapshot> Nodes { get; set; } = new();

    /// <summary>
    /// Gets or sets the connector records within this path.
    /// Connectors represent relationships between nodes.
    /// </summary>
    public List<AktaRecordSnapshot> Connectors { get; set; } = new();

    /// <summary>
    /// Gets or sets the edges that connect nodes via connectors.
    /// </summary>
    public List<AktaEdgeSnapshot> Edges { get; set; } = new();

    /// <summary>
    /// Gets or sets optional historical information or audit trail.
    /// </summary>
    public string? History { get; set; }

    /// <summary>
    /// Gets or sets additional metadata about this path.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets the total number of entities in this path (nodes + connectors).
    /// </summary>
    public int TotalEntityCount => Nodes.Count + Connectors.Count;

    /// <summary>
    /// Gets the total number of relationships (edges) in this path.
    /// </summary>
    public int TotalRelationshipCount => Edges.Count;

    /// <summary>
    /// Finds a node by its record ID.
    /// </summary>
    public AktaRecordSnapshot? FindNode(string recordId) =>
        Nodes.FirstOrDefault(n => n.RecordId == recordId);

    /// <summary>
    /// Finds a connector by its record ID.
    /// </summary>
    public AktaRecordSnapshot? FindConnector(string recordId) =>
        Connectors.FirstOrDefault(c => c.RecordId == recordId);

    /// <summary>
    /// Finds edges connected to a specific node.
    /// </summary>
    public List<AktaEdgeSnapshot> FindEdgesForNode(string nodeRecordId) =>
        Edges.Where(e => e.StartNodeRecordId == nodeRecordId || e.EndNodeRecordId == nodeRecordId).ToList();

    /// <summary>
    /// Gets a summary of this path.
    /// </summary>
    public string GetSummary() =>
        $"Path: {PathRecord.RecordId}, Nodes: {Nodes.Count}, Connectors: {Connectors.Count}, Edges: {Edges.Count}";
}

/// <summary>
/// Represents a relationship edge between two nodes via a connector.
/// </summary>
public class AktaEdgeSnapshot
{
    /// <summary>
    /// Gets or sets the record ID of the starting node.
    /// </summary>
    public string StartNodeRecordId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the record ID of the ending node.
    /// </summary>
    public string EndNodeRecordId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the record ID of the connector that represents this edge.
    /// </summary>
    public string ConnectorRecordId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets metadata about this edge relationship.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets a summary of this edge.
    /// </summary>
    public string GetSummary() =>
        $"{StartNodeRecordId} -[{ConnectorRecordId}]-> {EndNodeRecordId}";
}

/// <summary>
/// Represents the information about a page or view within Aktavara workspace.
/// </summary>
public class PageInfoSnapshot
{
    /// <summary>
    /// Gets or sets the page number.
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Gets or sets the total number of records in the collection.
    /// </summary>
    public int TotalRecords { get; set; }

    /// <summary>
    /// Gets or sets the page size (number of records per page).
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the starting index of records on this page.
    /// </summary>
    public int StartIndex { get; set; }

    /// <summary>
    /// Gets or sets whether there are more pages available.
    /// </summary>
    public bool HasMorePages { get; set; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (TotalRecords + PageSize - 1) / PageSize : 0;

    /// <summary>
    /// Gets the number of records on this page.
    /// </summary>
    public int RecordCountOnPage => Math.Min(PageSize, TotalRecords - StartIndex);
}
