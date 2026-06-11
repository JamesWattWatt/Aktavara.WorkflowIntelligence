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
    // These are the user-facing action names that appear in activity logs
    private static readonly Dictionary<string, EventType> ActionEventTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Search/Query actions
        { "Search records", EventType.SearchRecords },

        // Workspace open actions - all workspace types
        { "Open workspace Path", EventType.OpenWorkspace },
        { "Open workspace Topology", EventType.OpenWorkspace },
        { "Open workspace Diagram", EventType.OpenWorkspace },
        { "Open workspace Carrier", EventType.OpenWorkspace },
        { "Open workspace Schema", EventType.OpenWorkspace },

        // Record save actions
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
            EventType eventType;
            bool isRecognizedAction = ActionEventTypeMap.TryGetValue(entry.ActionName, out var mappedEventType);

            if (!isRecognizedAction)
            {
                _logger.LogWarning("Unrecognized action type: {ActionName}. Creating Unknown event.", entry.ActionName);
                eventType = EventType.Unknown;
            }
            else
            {
                eventType = mappedEventType;
            }

            // Handle Response-only entries specially to capture them as events
            if (entry.Direction == EventType.ResponseReceived && (eventType == EventType.SearchRecords || eventType == EventType.SaveRecords))
            {
                // Create a Response event instead of silently dropping
                _logger.LogDebug("Response-only entry for {ActionName}. Creating Response event.", entry.ActionName);
                var responseEvent = new ActivityEvent
                {
                    EventId = Guid.NewGuid().ToString(),
                    Timestamp = entry.Timestamp,
                    UserName = entry.UserName,
                    SessionId = entry.SessionId,
                    EventType = eventType,
                    ActionName = entry.ActionName,
                    IsSuccess = true,
                    RecordKind = RecordKind.Other
                };
                responseEvent.Evidence.Add($"Response entry for {entry.ActionName}");
                responseEvent.Metadata["direction"] = "Response";
                events.Add(responseEvent);
                continue;
            }

            // Process based on action type
            var normalizedEvents = eventType switch
            {
                EventType.SearchRecords => NormalizeSearchRecords(entry),
                EventType.OpenWorkspace => NormalizeOpenWorkspace(entry, rawEntries, i),
                EventType.SaveRecords => NormalizeSaveRecords(entry, rawEntries, i, recordSnapshots),
                EventType.Unknown => NormalizeUnknownActivity(entry),
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
                // Handle records with no TypeKind - use Other as fallback instead of skipping
                if (string.IsNullOrEmpty(record.TypeKind))
                {
                    _logger.LogWarning("Save records entry has record without TypeKind. RecordId: {RecordId}", record.RecordId);
                    // Don't skip - process with Other type
                }

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

    /// <summary>
    /// Normalizes an unrecognized action into an Unknown event.
    /// This fallback handler ensures no events are silently dropped.
    /// </summary>
    private List<ActivityEvent> NormalizeUnknownActivity(RawActivityLogEntry entry)
    {
        var events = new List<ActivityEvent>();

        try
        {
            var evt = new ActivityEvent
            {
                EventId = Guid.NewGuid().ToString(),
                Timestamp = entry.Timestamp,
                UserName = entry.UserName,
                SessionId = entry.SessionId,
                EventType = EventType.Unknown,
                ActionName = entry.ActionName,
                IsSuccess = entry.Direction != EventType.ResponseReceived,
                RecordKind = RecordKind.Other
            };

            evt.Evidence.Add($"Unrecognized action: {entry.ActionName}");

            if (entry.RawXmlPayloads.Count > 0)
            {
                evt.Metadata["payload_count"] = entry.RawXmlPayloads.Count;
            }

            evt.Metadata["direction"] = entry.Direction.ToString();

            events.Add(evt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error normalizing unknown activity");
        }

        return events;
    }

    /// <summary>
    /// Correlates record names in an ActivityContext by tracking opened records
    /// and using attribute saves to resolve names for records modified in the session.
    /// </summary>
    public void CorrelateRecordNames(ActivityContext context, IReadOnlyList<RawActivityLogEntry> rawEntries)
    {
        if (context?.RecentEvents.Count == 0)
            return;

        // First pass: Track all opened records
        foreach (var evt in context.RecentEvents)
        {
            if (evt.EventType == EventType.OpenWorkspace && !string.IsNullOrEmpty(evt.RecordId))
            {
                context.TrackOpenedRecord(
                    evt.RecordId,
                    evt.RecordName,
                    evt.WorkspaceKind,
                    evt.RecordKind ?? RecordKind.Other);
            }
        }

        // Second pass: Use SaveRecords events to resolve names for records we don't have names for
        var saveEvents = context.RecentEvents.Where(e => e.EventType == EventType.SaveRecords).ToList();

        foreach (var saveEvent in saveEvents)
        {
            if (string.IsNullOrEmpty(saveEvent.RecordName) && !string.IsNullOrEmpty(saveEvent.RecordId))
            {
                // Try to resolve name from opened records
                var resolvedName = context.ResolveRecordName(saveEvent.RecordId);
                if (!string.IsNullOrEmpty(resolvedName))
                {
                    saveEvent.RecordName = resolvedName;
                }
                else
                {
                    // Try to extract name from the raw entry's payload attributes
                    var nameFromPayload = ExtractNameFromSaveRecordsPayload(
                        rawEntries,
                        saveEvent.Timestamp,
                        saveEvent.UserName,
                        saveEvent.SessionId);

                    if (!string.IsNullOrEmpty(nameFromPayload))
                    {
                        saveEvent.RecordName = nameFromPayload;
                        // Also update the context for future correlation
                        if (context.OpenedRecords.TryGetValue(saveEvent.RecordId, out var recordInfo))
                        {
                            recordInfo.Name = nameFromPayload;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Extracts a record name from a SaveRecords payload by looking for the Name attribute (typically AttributeId 1 or similar).
    /// </summary>
    private string? ExtractNameFromSaveRecordsPayload(
        IReadOnlyList<RawActivityLogEntry> rawEntries,
        DateTime eventTimestamp,
        string userName,
        string sessionId)
    {
        // Find the matching raw entry
        var matchingEntry = rawEntries.FirstOrDefault(e =>
            e.Timestamp == eventTimestamp &&
            e.UserName == userName &&
            e.SessionId == sessionId &&
            (string.Equals(e.ActionName, "Save records", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(e.ActionName, "Save workspace Diagram", StringComparison.OrdinalIgnoreCase)));

        if (matchingEntry?.RawXmlPayloads.Count == 0)
            return null;

        var payload = matchingEntry.RawXmlPayloads[0];
        var payloadType = PayloadTypeDetector.Detect(payload);

        try
        {
            var records = payloadType switch
            {
                PayloadType.Json => _jsonExtractor.ExtractRecords(payload),
                PayloadType.Xml => _xmlExtractor.ExtractRecords(payload),
                _ => new AktaRecordSnapshot[] { }
            };

            // Extract name from first record's properties (usually AttributeId 1 or similar)
            if (records.Count > 0)
            {
                var record = records[0];
                var nameProperty = record.FindProperty("Name") ??
                                  record.Properties.FirstOrDefault(p =>
                                      p.AttributeId == "1" || p.AttributeId == "6"); // Common name attribute IDs

                if (nameProperty?.Value != null)
                {
                    return nameProperty.Value.ToString();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error extracting name from save records payload");
        }

        return null;
    }
}
