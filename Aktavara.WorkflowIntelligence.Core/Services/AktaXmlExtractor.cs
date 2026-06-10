using System.Xml.Linq;
using Aktavara.WorkflowIntelligence.Core.Interfaces;
using Aktavara.WorkflowIntelligence.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aktavara.WorkflowIntelligence.Core.Services;

/// <summary>
/// Extracts structured data from Aktavara XML payloads.
/// Handles various XML formats and namespace variations robustly.
/// </summary>
public class AktaXmlExtractor : IAktaXmlExtractor
{
    private readonly ILogger<AktaXmlExtractor> _logger;

    // Common namespace URIs
    private static readonly XNamespace XsiNamespace = "http://www.w3.org/2001/XMLSchema-instance";

    public AktaXmlExtractor(ILogger<AktaXmlExtractor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Extracts record information from XML content.
    /// </summary>
    public IReadOnlyList<AktaRecordSnapshot> ExtractRecords(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return Array.Empty<AktaRecordSnapshot>();

        try
        {
            var doc = XDocument.Parse(xml);
            var records = new List<AktaRecordSnapshot>();

            // Look for Record elements (namespace-agnostic)
            var recordElements = doc.Descendants()
                .Where(e => e.Name.LocalName == "Record")
                .ToList();

            foreach (var element in recordElements)
            {
                var record = ExtractRecordFromElement(element);
                if (record != null)
                {
                    records.Add(record);
                }
            }

            _logger.LogInformation("Extracted {Count} records from XML", records.Count);
            return records.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting records from XML");
            return Array.Empty<AktaRecordSnapshot>();
        }
    }

    /// <summary>
    /// Extracts pagination information from XML response.
    /// </summary>
    public PageInfoSnapshot? ExtractPageInfo(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return null;

        try
        {
            var doc = XDocument.Parse(xml);

            // Look for PageInfo element
            var pageInfoElement = doc.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "PageInfo");

            if (pageInfoElement == null)
                return null;

            var pageInfo = new PageInfoSnapshot();

            // Extract page information
            if (int.TryParse(pageInfoElement.Element(pageInfoElement.Name.NamespaceName + "PageNumber")?.Value
                    ?? pageInfoElement.Element("PageNumber")?.Value, out var pageNum))
                pageInfo.PageNumber = pageNum;

            if (int.TryParse(pageInfoElement.Element(pageInfoElement.Name.NamespaceName + "TotalRecords")?.Value
                    ?? pageInfoElement.Element("TotalRecords")?.Value, out var totalRecs))
                pageInfo.TotalRecords = totalRecs;

            if (int.TryParse(pageInfoElement.Element(pageInfoElement.Name.NamespaceName + "PageSize")?.Value
                    ?? pageInfoElement.Element("PageSize")?.Value, out var pageSize))
                pageInfo.PageSize = pageSize;

            if (int.TryParse(pageInfoElement.Element(pageInfoElement.Name.NamespaceName + "StartIndex")?.Value
                    ?? pageInfoElement.Element("StartIndex")?.Value, out var startIdx))
                pageInfo.StartIndex = startIdx;

            var hasMoreStr = pageInfoElement.Element(pageInfoElement.Name.NamespaceName + "HasMorePages")?.Value
                    ?? pageInfoElement.Element("HasMorePages")?.Value;
            if (!string.IsNullOrEmpty(hasMoreStr) && bool.TryParse(hasMoreStr, out var hasMore))
                pageInfo.HasMorePages = hasMore;

            _logger.LogInformation("Extracted page info: {PageNumber}/{TotalPages}",
                pageInfo.PageNumber, pageInfo.TotalPages);

            return pageInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting page info from XML");
            return null;
        }
    }

    /// <summary>
    /// Extracts a complete Path workspace structure from XML.
    /// </summary>
    public PathWorkspaceSnapshot? ExtractPathWorkspace(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return null;

        try
        {
            var doc = XDocument.Parse(xml);

            // Look for PathWkData element
            var pathWkElement = doc.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "PathWkData");

            if (pathWkElement == null)
            {
                _logger.LogWarning("No PathWkData element found in XML");
                return null;
            }

            var pathWorkspace = new PathWorkspaceSnapshot();

            // Extract the main Path record
            var pathElement = pathWkElement.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "Path");

            if (pathElement != null)
            {
                var pathRecord = ExtractRecordFromElement(pathElement);
                if (pathRecord != null)
                {
                    pathRecord.TypeKind = "Path";
                    pathWorkspace.PathRecord = pathRecord;
                }
            }

            // Extract StartVertex (node) records
            var startVertices = pathWkElement.Descendants()
                .Where(e => e.Name.LocalName == "StartVertex")
                .ToList();

            foreach (var vertex in startVertices)
            {
                var recordElement = vertex.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "Record");
                if (recordElement != null)
                {
                    var node = ExtractRecordFromElement(recordElement);
                    if (node != null)
                    {
                        node.TypeKind = "Node";
                        pathWorkspace.Nodes.Add(node);
                    }
                }
            }

            // Extract EndVertex (node) records
            var endVertices = pathWkElement.Descendants()
                .Where(e => e.Name.LocalName == "EndVertex")
                .ToList();

            foreach (var vertex in endVertices)
            {
                var recordElement = vertex.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "Record");
                if (recordElement != null)
                {
                    var node = ExtractRecordFromElement(recordElement);
                    if (node != null && !pathWorkspace.Nodes.Any(n => n.RecordId == node.RecordId))
                    {
                        node.TypeKind = "Node";
                        pathWorkspace.Nodes.Add(node);
                    }
                }
            }

            // Extract Edge connector records
            var edges = pathWkElement.Descendants()
                .Where(e => e.Name.LocalName == "Edge")
                .ToList();

            foreach (var edgeElement in edges)
            {
                var edge = ExtractEdgeFromElement(edgeElement);
                if (edge != null)
                {
                    pathWorkspace.Edges.Add(edge);

                    // Also extract the connector record if present
                    var connectorElement = edgeElement.Descendants()
                        .FirstOrDefault(e => e.Name.LocalName == "Connector");

                    if (connectorElement != null)
                    {
                        var recordElement = connectorElement.Descendants()
                            .FirstOrDefault(e => e.Name.LocalName == "Record");

                        if (recordElement != null)
                        {
                            var connector = ExtractRecordFromElement(recordElement);
                            if (connector != null)
                            {
                                connector.TypeKind = "Connector";
                                pathWorkspace.Connectors.Add(connector);
                            }
                        }
                    }
                }
            }

            _logger.LogInformation(
                "Extracted path workspace: {NodeCount} nodes, {ConnectorCount} connectors, {EdgeCount} edges",
                pathWorkspace.Nodes.Count, pathWorkspace.Connectors.Count, pathWorkspace.Edges.Count);

            return pathWorkspace;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting path workspace from XML");
            return null;
        }
    }

    /// <summary>
    /// Extracts a simple boolean result from XML response.
    /// </summary>
    public bool? ExtractBooleanResult(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return null;

        try
        {
            var doc = XDocument.Parse(xml);
            var root = doc.Root;

            if (root == null)
                return null;

            // Look for common boolean element names
            var resultElement = root.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "Result" ||
                                     e.Name.LocalName == "IsSuccess" ||
                                     e.Name.LocalName == "Success" ||
                                     e.Name.LocalName == "IsValid" ||
                                     e.Name.LocalName == "Valid");

            if (resultElement != null && bool.TryParse(resultElement.Value, out var result))
            {
                return result;
            }

            // If root element itself is a simple boolean value
            if (bool.TryParse(root.Value, out var rootValue))
            {
                return rootValue;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting boolean result from XML");
            return null;
        }
    }

    /// <summary>
    /// Extracts a record from an XElement.
    /// </summary>
    private AktaRecordSnapshot? ExtractRecordFromElement(XElement element)
    {
        try
        {
            var record = new AktaRecordSnapshot();

            // Extract basic record attributes
            record.TypeKind = element.Attribute("TypeKind")?.Value ?? string.Empty;
            record.TypeId = element.Attribute("TypeId")?.Value ?? string.Empty;
            record.RecordId = element.Attribute("RecordId")?.Value ?? string.Empty;
            record.RecordState = element.Attribute("State")?.Value ?? string.Empty;
            record.StageId = element.Attribute("StageId")?.Value;

            // Extract LastChangedDate
            if (DateTime.TryParse(element.Attribute("LastChangedDate")?.Value, out var lastChanged))
            {
                record.LastChangedDate = lastChanged;
            }

            // Extract properties
            var attributeElements = element.Descendants()
                .Where(e => e.Name.LocalName == "Attribute")
                .ToList();

            foreach (var attrElement in attributeElements)
            {
                var property = ExtractPropertyFromElement(attrElement);
                if (property != null)
                {
                    record.Properties.Add(property);
                }
            }

            return record;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting record from XML element");
            return null;
        }
    }

    /// <summary>
    /// Extracts a record property from an XElement.
    /// </summary>
    private AktaRecordPropertySnapshot? ExtractPropertyFromElement(XElement element)
    {
        try
        {
            var property = new AktaRecordPropertySnapshot
            {
                AttributeId = element.Attribute("AttributeId")?.Value ?? string.Empty,
                XsiType = element.Attribute(XsiNamespace + "type")?.Value
            };

            // Extract the attribute value
            var valueElement = element.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "AttributeValue");

            if (valueElement != null)
            {
                property.Value = valueElement.Value;
                property.ValueType = valueElement.Attribute("ValueType")?.Value;
            }
            else
            {
                // Try direct value if no AttributeValue wrapper
                property.Value = element.Value;
            }

            return property;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting property from XML element");
            return null;
        }
    }

    /// <summary>
    /// Extracts edge information from an XElement.
    /// </summary>
    private AktaEdgeSnapshot? ExtractEdgeFromElement(XElement element)
    {
        try
        {
            var edge = new AktaEdgeSnapshot
            {
                StartNodeRecordId = element.Attribute("StartNodeRecordId")?.Value ?? string.Empty,
                EndNodeRecordId = element.Attribute("EndNodeRecordId")?.Value ?? string.Empty,
                ConnectorRecordId = element.Attribute("ConnectorRecordId")?.Value ?? string.Empty
            };

            // Edge is valid only if it has both nodes
            if (!string.IsNullOrEmpty(edge.StartNodeRecordId) && !string.IsNullOrEmpty(edge.EndNodeRecordId))
            {
                return edge;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting edge from XML element");
            return null;
        }
    }
}
