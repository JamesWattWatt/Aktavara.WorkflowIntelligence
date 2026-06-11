using System.Text.Json;
using Aktavara.WorkflowIntelligence.Core.Interfaces;
using Aktavara.WorkflowIntelligence.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aktavara.WorkflowIntelligence.Core.Services;

/// <summary>
/// Extracts structured data from Aktavara JSON payloads.
/// Handles the $type/$id reference system common in .NET JSON serialization.
/// </summary>
public class AktaJsonExtractor : IAktaJsonExtractor
{
    private readonly ILogger<AktaJsonExtractor> _logger;

    // Reference map for resolving $ref pointers
    private Dictionary<string, JsonElement> _referenceMap = new();

    public AktaJsonExtractor(ILogger<AktaJsonExtractor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Extracts record information from JSON content.
    /// Handles $ref resolution to get complete records.
    /// </summary>
    public IReadOnlyList<AktaRecordSnapshot> ExtractRecords(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Array.Empty<AktaRecordSnapshot>();

        try
        {
            using var doc = JsonDocument.Parse(json);
            _referenceMap = new();

            // First pass: build reference map of all $id objects
            BuildReferenceMap(doc.RootElement);

            var records = new List<AktaRecordSnapshot>();

            // Second pass: extract records with $ref resolution
            ExtractRecordsFromElement(doc.RootElement, records);

            _logger.LogInformation("Extracted {Count} records from JSON", records.Count);
            return records.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ERROR extracting records from JSON payload: {Message}. Payload length: {Length}",
                ex.Message, json?.Length ?? 0);
            return Array.Empty<AktaRecordSnapshot>();
        }
    }

    /// <summary>
    /// Extracts a Path workspace structure from JSON.
    /// Handles $ref resolution to get complete workspace data.
    /// </summary>
    public PathWorkspaceSnapshot? ExtractPathWorkspace(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            _referenceMap = new();

            // First pass: build reference map of all $id objects
            BuildReferenceMap(doc.RootElement);

            var workspace = new PathWorkspaceSnapshot();

            // Navigate to Result.Path
            if (doc.RootElement.TryGetProperty("Result", out var result) &&
                result.ValueKind == JsonValueKind.Object &&
                result.TryGetProperty("Path", out var pathElement) &&
                pathElement.ValueKind == JsonValueKind.Object)
            {
                // Extract path record from Result.Path
                if (result.TryGetProperty("Path", out var pathRecord))
                {
                    var record = ExtractRecordFromElement(pathRecord);
                    if (record != null)
                    {
                        record.TypeKind = AktavaraTypeKind.Path.ToString();
                        workspace.PathRecord = record;
                    }
                }

                // Extract edges and nodes from Elements array
                if (pathElement.TryGetProperty("Elements", out var elementsWrapper) &&
                    elementsWrapper.ValueKind == JsonValueKind.Object &&
                    elementsWrapper.TryGetProperty("$data", out var elementsArray) &&
                    elementsArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in elementsArray.EnumerateArray())
                    {
                        ExtractEdgeElement(element, workspace);
                    }
                }
            }

            _logger.LogInformation(
                "Extracted path workspace: {NodeCount} nodes, {ConnectorCount} connectors, {EdgeCount} edges",
                workspace.Nodes.Count, workspace.Connectors.Count, workspace.Edges.Count);

            return workspace;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not extract path workspace from JSON payload");
            return null;
        }
    }

    /// <summary>
    /// Extracts a boolean result from JSON response.
    /// </summary>
    public bool? ExtractBooleanResult(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Look for Result property with boolean value
            if (root.TryGetProperty("Result", out var result))
            {
                if (result.ValueKind == JsonValueKind.True)
                    return true;
                if (result.ValueKind == JsonValueKind.False)
                    return false;
            }

            // Try parsing root element if it's a boolean
            if (root.ValueKind == JsonValueKind.True)
                return true;
            if (root.ValueKind == JsonValueKind.False)
                return false;

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not extract boolean result from JSON");
            return null;
        }
    }

    /// <summary>
    /// Builds a map of all objects by $id for reference resolution.
    /// </summary>
    private void BuildReferenceMap(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            // Register this object if it has an $id
            if (element.TryGetProperty("$id", out var idProp) &&
                idProp.ValueKind == JsonValueKind.String)
            {
                var id = idProp.GetString();
                if (id != null && !_referenceMap.ContainsKey(id))
                {
                    _referenceMap[id] = element;
                }
            }

            // Recurse into all properties
            foreach (var prop in element.EnumerateObject())
            {
                BuildReferenceMap(prop.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                BuildReferenceMap(item);
            }
        }
    }

    /// <summary>
    /// Resolves a $ref pointer to get the actual object.
    /// </summary>
    private JsonElement? ResolveReference(string refId)
    {
        if (_referenceMap.TryGetValue(refId, out var element))
        {
            return element;
        }
        return null;
    }

    /// <summary>
    /// Gets a value from a JsonElement, resolving $ref if needed.
    /// </summary>
    private JsonElement ResolveElement(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty("$ref", out var refProp) &&
            refProp.ValueKind == JsonValueKind.String)
        {
            var refId = refProp.GetString();
            if (refId != null)
            {
                var resolved = ResolveReference(refId);
                if (resolved.HasValue)
                    return resolved.Value;
            }
        }
        return element;
    }

    /// <summary>
    /// Recursively extracts records from JSON elements.
    /// </summary>
    private void ExtractRecordsFromElement(JsonElement element, List<AktaRecordSnapshot> records)
    {
        // Handle arrays first (before trying property access which fails on arrays)
        if (element.ValueKind == JsonValueKind.Array)
        {
            // Direct array - enumerate items
            foreach (var item in element.EnumerateArray())
            {
                ExtractRecordsFromElement(item, records);
            }
        }
        else if (element.ValueKind == JsonValueKind.Object)
        {
            // Check if this element is a record
            if (element.TryGetProperty("TypeKind", out var typeKind) &&
                element.TryGetProperty("RecordId", out var recordId))
            {
                var record = ExtractRecordFromElement(element);
                if (record != null)
                    records.Add(record);
            }

            // Check for wrapped array with $data property
            if (element.TryGetProperty("$data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in dataArray.EnumerateArray())
                {
                    ExtractRecordsFromElement(item, records);
                }
            }

            // Recurse into other object properties
            foreach (var prop in element.EnumerateObject())
            {
                // Skip $data and $list and $type as they're metadata
                if (!prop.Name.StartsWith("$"))
                {
                    ExtractRecordsFromElement(prop.Value, records);
                }
            }
        }
    }

    /// <summary>
    /// Extracts a record from a JSON object element.
    /// Resolves $ref pointers to get complete object data.
    /// </summary>
    private AktaRecordSnapshot? ExtractRecordFromElement(JsonElement element)
    {
        try
        {
            // Resolve $ref if this element is a reference
            element = ResolveElement(element);

            var record = new AktaRecordSnapshot();

            // Extract basic attributes
            if (element.TryGetProperty("TypeKind", out var typeKind))
                record.TypeKind = typeKind.GetString() ?? string.Empty;

            if (element.TryGetProperty("TypeId", out var typeId) && typeId.ValueKind == JsonValueKind.Number)
                record.TypeId = typeId.GetInt32().ToString();

            if (element.TryGetProperty("RecordId", out var recordId) && recordId.ValueKind == JsonValueKind.Number)
                record.RecordId = recordId.GetInt32().ToString();

            if (element.TryGetProperty("RecordState", out var state))
                record.RecordState = state.GetString() ?? string.Empty;

            if (element.TryGetProperty("StageId", out var stageId))
            {
                record.StageId = stageId.ValueKind switch
                {
                    JsonValueKind.Number => stageId.GetInt32().ToString(),
                    JsonValueKind.String => stageId.GetString(),
                    _ => null
                };
            }

            if (element.TryGetProperty("LastChangedDate", out var lastChanged) &&
                DateTime.TryParse(lastChanged.GetString(), out var changedDate))
                record.LastChangedDate = changedDate;

            // Extract properties
            if (element.TryGetProperty("Properties", out var propsWrapper))
            {
                // Resolve $ref if properties is a reference
                var resolvedProps = ResolveElement(propsWrapper);

                if (resolvedProps.ValueKind == JsonValueKind.Object &&
                    resolvedProps.TryGetProperty("$data", out var propsArray) &&
                    propsArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var propElement in propsArray.EnumerateArray())
                    {
                        var prop = ExtractPropertyFromElement(propElement);
                        if (prop != null)
                            record.Properties.Add(prop);
                    }
                }
            }

            return record;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error extracting record from JSON element");
            return null;
        }
    }

    /// <summary>
    /// Extracts a property from a JSON object element.
    /// Resolves $ref pointers to get complete property data.
    /// </summary>
    private AktaRecordPropertySnapshot? ExtractPropertyFromElement(JsonElement element)
    {
        try
        {
            // Resolve $ref if this element is a reference
            element = ResolveElement(element);

            var property = new AktaRecordPropertySnapshot();

            if (element.TryGetProperty("AttributeId", out var attrId) && attrId.ValueKind == JsonValueKind.Number)
                property.AttributeId = attrId.GetInt32().ToString();

            // Handle AttributeValue which can be null or an object with $type and Value
            if (element.TryGetProperty("AttributeValue", out var attrValue))
            {
                // Resolve $ref if AttributeValue is a reference
                var resolvedAttrValue = ResolveElement(attrValue);

                if (resolvedAttrValue.ValueKind == JsonValueKind.Object)
                {
                    if (resolvedAttrValue.TryGetProperty("$type", out var typeInfo))
                        property.XsiType = typeInfo.GetString();

                    if (resolvedAttrValue.TryGetProperty("Value", out var value))
                    {
                        property.Value = value.ValueKind switch
                        {
                            JsonValueKind.String => value.GetString(),
                            JsonValueKind.Number => value.GetDouble(),
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            _ => value.ToString()
                        };
                        property.ValueType = resolvedAttrValue.TryGetProperty("$type", out var vt)
                            ? vt.GetString()
                            : null;
                    }
                }
                else if (resolvedAttrValue.ValueKind == JsonValueKind.String)
                {
                    property.Value = resolvedAttrValue.GetString();
                }
                else if (resolvedAttrValue.ValueKind == JsonValueKind.Number)
                {
                    property.Value = resolvedAttrValue.GetDouble();
                }
                // null values are skipped
            }

            return property;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error extracting property from JSON element");
            return null;
        }
    }

    /// <summary>
    /// Extracts edge and node information from an edge element.
    /// </summary>
    private void ExtractEdgeElement(JsonElement element, PathWorkspaceSnapshot workspace)
    {
        try
        {
            // Extract StartVertex node
            if (element.TryGetProperty("StartVertex", out var startVertex) &&
                startVertex.ValueKind == JsonValueKind.Object &&
                startVertex.TryGetProperty("Record", out var startRecord))
            {
                var node = ExtractRecordFromElement(startRecord);
                if (node != null && !workspace.Nodes.Any(n => n.RecordId == node.RecordId))
                {
                    node.TypeKind = AktavaraTypeKind.Node.ToString();
                    workspace.Nodes.Add(node);
                }
            }

            // Extract EndVertex node
            if (element.TryGetProperty("EndVertex", out var endVertex) &&
                endVertex.ValueKind == JsonValueKind.Object &&
                endVertex.TryGetProperty("Record", out var endRecord))
            {
                var node = ExtractRecordFromElement(endRecord);
                if (node != null && !workspace.Nodes.Any(n => n.RecordId == node.RecordId))
                {
                    node.TypeKind = AktavaraTypeKind.Node.ToString();
                    workspace.Nodes.Add(node);
                }
            }

            // Extract Edge connector
            if (element.TryGetProperty("Edge", out var edgeWrapper) &&
                edgeWrapper.ValueKind == JsonValueKind.Object &&
                edgeWrapper.TryGetProperty("Record", out var connectorRecord))
            {
                var connector = ExtractRecordFromElement(connectorRecord);
                if (connector != null && !workspace.Connectors.Any(c => c.RecordId == connector.RecordId))
                {
                    connector.TypeKind = AktavaraTypeKind.Connector.ToString();
                    workspace.Connectors.Add(connector);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error extracting edge element from JSON");
        }
    }
}
