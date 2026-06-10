using Aktavara.WorkflowIntelligence.Core.Interfaces;
using Aktavara.WorkflowIntelligence.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aktavara.WorkflowIntelligence.Core.Services;

/// <summary>
/// Normalizes raw activity log entries into structured activity events.
/// Handles action-specific extraction, XML/JSON parsing, and request/response correlation.
/// </summary>
public class ActivityEventNormalizer : IActivityEventNormalizer
{
    private readonly IAktaXmlExtractor _xmlExtractor;
    private readonly IAktaJsonExtractor _jsonExtractor;
    private readonly IRecordDiffService _diffService;
    private readonly ILogger<ActivityEventNormalizer> _logger;

    // Mapping from action names to event types
    private static readonly Dictionary<string, EventType> ActionEventTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Search records", EventType.SearchRecords },
        { "Open workspace Path", EventType.OpenWorkspace },
        { "Open workspace Diagram", EventType.OpenWorkspace },
        { "Save records", EventType.SaveRecords },
        { "Save workspace Diagram", EventType.SaveRecords },
        { "SavePathWsData", EventType.SaveRecords }
    };

    public ActivityEventNormalizer(
        IAktaXmlExtractor xmlExtractor,
        IAktaJsonExtractor jsonExtractor,
        ILogger<ActivityEventNormalizer> logger,
        IRecordDiffService? diffService = null)
    {
        _xmlExtractor = xmlExtractor;
        _jsonExtractor = jsonExtractor;
        _logger = logger;
        _diffService = diffService ?? new RecordDiffService(new Microsoft.Extensions.Logging.Abstractions.NullLogger<RecordDiffService>());
    }

    /// <summary>
    /// Normalizes raw activity log entries into activity events.
    /// </summary>
    public IReadOnlyList<ActivityEvent> Normalize(IReadOnlyList<RawActivityLogEntry> rawEntries)
    {
        var events = new List<ActivityEvent>();

        if (rawEntries.Count == 0)
            return events.AsReadOnly();

        _logger.LogInformation("Normalizing {Count} raw log entries", rawEntries.Count);

        // Build a map of OpenWorkspace snapshots by record ID
        var recordSnapshots = new Dictionary<string, AktaRecordSnapshot>(StringComparer.OrdinalIgnoreCase);

        // First pass: collect snapshots from OpenWorkspace events
        for (int i = 0; i < rawEntries.Count; i++)
        {
            var entry = rawEntries[i];

            if ((entry.ActionName.Equals("Open workspace Path", StringComparison.OrdinalIgnoreCase) ||
                 entry.ActionName.Equals("Open workspace Diagram", StringComparison.OrdinalIgnoreCase)) &&
                entry.Direction == EventType.RequestInitiated &&
                entry.RawXmlPayloads.Count > 0)
            {
                try
                {
                    var payload = entry.RawXmlPayloads[0];
                    var payloadType = PayloadTypeDetector.Detect(payload);

                    PathWorkspaceSnapshot? pathWorkspace = payloadType switch
                    {
                        PayloadType.Json => _jsonExtractor.ExtractPathWorkspace(payload),
                        PayloadType.Xml => _xmlExtractor.ExtractPathWorkspace(payload),
                        _ => null
                    };

                    if (pathWorkspace?.PathRecord != null)
                    {
                        // Store path and all nodes
                        recordSnapshots[pathWorkspace.PathRecord.RecordId] = pathWorkspace.PathRecord;
                        foreach (var node in pathWorkspace.Nodes)
                        {
                            recordSnapshots[node.RecordId] = node;
                        }
                        foreach (var connector in pathWorkspace.Connectors)
                        {
                            recordSnapshots[connector.RecordId] = connector;
                        }

                        _logger.LogDebug("Cached {Count} snapshots from OpenWorkspace event",
                            1 + pathWorkspace.Nodes.Count + pathWorkspace.Connectors.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error extracting snapshots from OpenWorkspace");
                }
            }
        }

        // Second pass: process all events
        for (int i = 0; i < rawEntries.Count; i++)
        {
            var entry = rawEntries[i];

            // Determine the event type from action name
            if (!ActionEventTypeMap.TryGetValue(entry.ActionName, out var eventType))
            {
                _logger.LogDebug("Unknown action: {ActionName}", entry.ActionName);
                continue;
            }

            // Skip responses unless they're correlated with a request
            if (entry.Direction == EventType.ResponseReceived && eventType == EventType.SearchRecords)
                continue;

            if (entry.Direction == EventType.ResponseReceived && eventType == EventType.SaveRecords)
                continue;

            // Process based on action type
            var normalizedEvents = eventType switch
            {
                EventType.SearchRecords => NormalizeSearchRecords(entry),
                EventType.OpenWorkspace => NormalizeOpenWorkspace(entry, rawEntries, i),
                EventType.SaveRecords => NormalizeSaveRecords(entry, rawEntries, i, recordSnapshots),
                _ => new List<ActivityEvent>()
            };

            events.AddRange(normalizedEvents);
        }

        _logger.LogInformation("Normalized to {Count} activity events", events.Count);
        return events.AsReadOnly();
    }

    /// <summary>
    /// Normalizes a Search records entry.
    /// </summary>
    private List<ActivityEvent> NormalizeSearchRecords(RawActivityLogEntry entry)
    {
        var events = new List<ActivityEvent>();

        if (entry.RawXmlPayloads.Count == 0)
            return events;

        try
        {
            var payload = entry.RawXmlPayloads[0];
            var payloadType = PayloadTypeDetector.Detect(payload);

            IReadOnlyList<AktaRecordSnapshot> records = payloadType switch
            {
                PayloadType.Json => _jsonExtractor.ExtractRecords(payload),
                PayloadType.Xml => _xmlExtractor.ExtractRecords(payload),
                _ => Array.Empty<AktaRecordSnapshot>()
            };

            // Look for search expression in records
            foreach (var record in records)
            {
                if (record.TypeId == "TypedSearchExpressionItem")
                {
                    // Extract search criteria
                    var kindProp = record.FindProperty("Kind");
                    var typeIdProp = record.FindProperty("TypeId");

                    var evt = new ActivityEvent
                    {
                        EventId = Guid.NewGuid().ToString(),
                        Timestamp = entry.Timestamp,
                        UserName = entry.UserName,
                        SessionId = entry.SessionId,
                        EventType = EventType.SearchRecords,
                        ActionName = entry.ActionName,
                        TypeId = typeIdProp?.Value?.ToString(),
                        IsSuccess = true
                    };

                    // Determine RecordKind from Kind property
                    if (kindProp?.Value is string kindStr)
                    {
                        evt.RecordKind = AktavaraTypeHelper.ToRecordKind(kindStr);
                    }

                    evt.Evidence.Add($"Search {evt.RecordKind ?? RecordKind.Other} of type {evt.TypeId ?? "unknown"}");
                    evt.Metadata["payload"] = payload;

                    events.Add(evt);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error normalizing search records");
        }

        return events;
    }

    /// <summary>
    /// Normalizes an Open workspace Path entry.
    /// </summary>
    private List<ActivityEvent> NormalizeOpenWorkspace(
        RawActivityLogEntry entry,
        IReadOnlyList<RawActivityLogEntry> allEntries,
        int entryIndex)
    {
        var events = new List<ActivityEvent>();

        if (entry.RawXmlPayloads.Count == 0)
            return events;

        try
        {
            var payload = entry.RawXmlPayloads[0];
            var payloadType = PayloadTypeDetector.Detect(payload);

            PathWorkspaceSnapshot? pathWorkspace = payloadType switch
            {
                PayloadType.Json => _jsonExtractor.ExtractPathWorkspace(payload),
                PayloadType.Xml => _xmlExtractor.ExtractPathWorkspace(payload),
                _ => null
            };

            if (pathWorkspace?.PathRecord == null)
            {
                _logger.LogDebug("No path workspace or path record found in open workspace entry");
                return events;
            }

            var evt = new ActivityEvent
            {
                EventId = Guid.NewGuid().ToString(),
                Timestamp = entry.Timestamp,
                UserName = entry.UserName,
                SessionId = entry.SessionId,
                EventType = EventType.OpenWorkspace,
                ActionName = entry.ActionName,
                RecordKind = RecordKind.Path,
                RecordId = pathWorkspace.PathRecord.RecordId,
                RecordName = pathWorkspace.PathRecord.FindProperty("Name")?.Value?.ToString(),
                TypeId = pathWorkspace.PathRecord.TypeId,
                IsSuccess = true
            };

            // Add related record IDs from nodes and connectors
            foreach (var node in pathWorkspace.Nodes)
            {
                evt.RelatedRecordIds.Add(node.RecordId);
            }
            foreach (var connector in pathWorkspace.Connectors)
            {
                evt.RelatedRecordIds.Add(connector.RecordId);
            }

            // Store evidence
            evt.Evidence.Add($"Opened Path record {evt.RecordId} {evt.RecordName ?? "unknown"}");
            evt.Evidence.Add($"Contains {pathWorkspace.Nodes.Count} nodes and {pathWorkspace.Connectors.Count} connectors");

            evt.Metadata["path_workspace"] = pathWorkspace;
            evt.Metadata["node_count"] = pathWorkspace.Nodes.Count;
            evt.Metadata["connector_count"] = pathWorkspace.Connectors.Count;

            events.Add(evt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error normalizing open workspace");
        }

        return events;
    }

    /// <summary>
    /// Normalizes a Save records entry with correlation and diffing.
    /// </summary>
    private List<ActivityEvent> NormalizeSaveRecords(
        RawActivityLogEntry entry,
        IReadOnlyList<RawActivityLogEntry> allEntries,
        int entryIndex,
        Dictionary<string, AktaRecordSnapshot> recordSnapshots)
    {
        var events = new List<ActivityEvent>();

        if (entry.RawXmlPayloads.Count == 0)
            return events;

        try
        {
            var payload = entry.RawXmlPayloads[0];
            var payloadType = PayloadTypeDetector.Detect(payload);

            IReadOnlyList<AktaRecordSnapshot> records = payloadType switch
            {
                PayloadType.Json => _jsonExtractor.ExtractRecords(payload),
                PayloadType.Xml => _xmlExtractor.ExtractRecords(payload),
                _ => Array.Empty<AktaRecordSnapshot>()
            };

            // Look for records being saved
            foreach (var record in records)
            {
                // Skip if no TypeKind (can't determine record type)
                if (string.IsNullOrEmpty(record.TypeKind))
                    continue;

                // Determine RecordKind from TypeKind
                var recordKind = AktavaraTypeHelper.ToRecordKind(record.TypeKind);

                var evt = new ActivityEvent
                {
                    EventId = Guid.NewGuid().ToString(),
                    Timestamp = entry.Timestamp,
                    UserName = entry.UserName,
                    SessionId = entry.SessionId,
                    EventType = EventType.SaveRecords,
                    ActionName = entry.ActionName,
                    RecordKind = recordKind,
                    TypeId = record.TypeId,
                    RecordId = record.RecordId,
                    RecordName = record.FindProperty("Name")?.Value?.ToString(),
                    RecordState = record.RecordState
                };

                // Try to diff with previously seen snapshots
                if (recordSnapshots.TryGetValue(record.RecordId, out var beforeSnapshot))
                {
                    var diffs = _diffService.Diff(beforeSnapshot, record);
                    evt.ChangedAttributes.AddRange(diffs);

                    if (diffs.Count > 0)
                    {
                        evt.Evidence.Add($"Detected {diffs.Count} attribute changes from previous snapshot");
                        evt.Metadata["diff_from_snapshot"] = true;
                    }
                }
                else
                {
                    // No previous snapshot, extract changed attributes from properties
                    foreach (var property in record.Properties)
                    {
                        var changedAttr = new ChangedAttribute
                        {
                            AttributeId = property.AttributeId,
                            ToValue = property.Value,
                            ValueType = property.ValueType
                        };
                        evt.ChangedAttributes.Add(changedAttr);
                    }
                }

                evt.Evidence.Add($"Saving {evt.RecordKind} record {evt.RecordId} (State: {evt.RecordState})");

                evt.Metadata["payload"] = payload;

                // Try to correlate with response
                CorrelateWithResponse(evt, entry, allEntries, entryIndex);

                events.Add(evt);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error normalizing save records");
        }

        return events;
    }

    /// <summary>
    /// Correlates a save request with its response and updates success status.
    /// </summary>
    private void CorrelateWithResponse(
        ActivityEvent saveEvent,
        RawActivityLogEntry requestEntry,
        IReadOnlyList<RawActivityLogEntry> allEntries,
        int requestIndex)
    {
        // Look for the matching response entry (should be next)
        for (int i = requestIndex + 1; i < allEntries.Count && i < requestIndex + 5; i++)
        {
            var potentialResponse = allEntries[i];

            // Check if this is the matching response
            if (potentialResponse.Direction == EventType.ResponseReceived &&
                potentialResponse.ActionName == requestEntry.ActionName &&
                potentialResponse.UserName == requestEntry.UserName &&
                potentialResponse.SessionId == requestEntry.SessionId)
            {
                // Extract success status from response
                if (potentialResponse.RawXmlPayloads.Count > 0)
                {
                    var responseXml = potentialResponse.RawXmlPayloads[0];
                    var successResult = _xmlExtractor.ExtractBooleanResult(responseXml);

                    if (successResult.HasValue)
                    {
                        saveEvent.IsSuccess = successResult.Value;

                        if (successResult.Value)
                        {
                            saveEvent.Evidence.Add("Response indicates success");
                        }
                        else
                        {
                            saveEvent.Evidence.Add("Response indicates failure");
                        }

                        saveEvent.Metadata["correlated_response_index"] = i;
                        saveEvent.Metadata["response_timestamp"] = potentialResponse.Timestamp;
                    }
                }

                break;
            }
        }
    }
}
