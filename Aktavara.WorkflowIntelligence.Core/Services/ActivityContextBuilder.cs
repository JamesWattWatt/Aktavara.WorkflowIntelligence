using Aktavara.WorkflowIntelligence.Core.Interfaces;
using Aktavara.WorkflowIntelligence.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aktavara.WorkflowIntelligence.Core.Services;

/// <summary>
/// Builds comprehensive activity contexts from normalized activity events.
/// Determines current state, identifies active entities, generates workflow hints,
/// and creates human-readable summaries of user activity.
/// </summary>
public class ActivityContextBuilder : IActivityContextBuilder
{
    private readonly ILogger<ActivityContextBuilder> _logger;
    private const int RapidSequenceThresholdSeconds = 60;

    public ActivityContextBuilder(ILogger<ActivityContextBuilder> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Builds an activity context from events for a specific user and time window.
    /// </summary>
    public ActivityContext BuildContext(
        IReadOnlyList<ActivityEvent> allEvents,
        string userName,
        DateTime timeWindowStart,
        DateTime timeWindowEnd)
    {
        var context = new ActivityContext
        {
            UserName = userName,
            TimeWindowStart = timeWindowStart,
            TimeWindowEnd = timeWindowEnd
        };

        // Filter events by user and time window
        var userEvents = allEvents
            .Where(e => e.UserName == userName)
            .Where(e => e.Timestamp >= timeWindowStart && e.Timestamp <= timeWindowEnd)
            .OrderBy(e => e.Timestamp)
            .ToList();

        context.RecentEvents = userEvents;

        if (userEvents.Count == 0)
        {
            context.CurrentState = CurrentState.NoActivity;
            context.Summary = $"No activity for {userName} in this time window";
            _logger.LogInformation("Built context for {UserName}: no activity", userName);
            return context;
        }

        // Set session ID from most recent event
        var mostRecentEvent = userEvents.Last();
        context.SessionId = mostRecentEvent.SessionId;

        // Determine current state from most recent meaningful event
        context.CurrentState = DetermineCurrentState(userEvents);

        // Identify active entities
        context.ActiveEntities = IdentifyActiveEntities(userEvents);

        // Generate workflow hints
        context.WorkflowHints = GenerateWorkflowHints(userEvents);

        // Build summary
        context.Summary = BuildSummary(context, userEvents);

        _logger.LogInformation(
            "Built context for {UserName}: {EventCount} events, state={State}, {EntityCount} active entities",
            userName,
            userEvents.Count,
            context.CurrentState,
            context.ActiveEntities.Count);

        return context;
    }

    /// <summary>
    /// Determines the current state from the last meaningful event in the window.
    /// </summary>
    private CurrentState DetermineCurrentState(IReadOnlyList<ActivityEvent> userEvents)
    {
        if (userEvents.Count == 0)
            return CurrentState.NoActivity;

        // Examine events in reverse order (most recent first)
        for (int i = userEvents.Count - 1; i >= 0; i--)
        {
            var evt = userEvents[i];

            if (evt.EventType == EventType.SaveRecords)
            {
                // SaveRecords handling based on RecordKind
                if (evt.RecordKind == RecordKind.Node)
                {
                    return CurrentState.NodeSaved;
                }
                else if (evt.RecordKind == RecordKind.Path)
                {
                    if (evt.RecordState == "Added" || evt.RecordState == "New")
                        return CurrentState.PathCreated;
                    else
                        return CurrentState.PathSaved;
                }
                else if (evt.RecordKind == RecordKind.Connector)
                {
                    return CurrentState.ConnectorCreated;
                }
            }
            else if (evt.EventType == EventType.OpenWorkspace && evt.RecordKind == RecordKind.Path)
            {
                return CurrentState.PathOpened;
            }
        }

        return CurrentState.Unknown;
    }

    /// <summary>
    /// Identifies active entities (Path, Node, Connector) from recent events.
    /// </summary>
    private List<ActiveEntity> IdentifyActiveEntities(IReadOnlyList<ActivityEvent> userEvents)
    {
        var entities = new Dictionary<string, ActiveEntity>();

        // Track all Path, Node, and Connector records from events
        foreach (var evt in userEvents)
        {
            if (evt.EventType == EventType.OpenWorkspace && evt.RecordKind == RecordKind.Path)
            {
                var key = $"Path:{evt.RecordId}";
                entities[key] = new ActiveEntity
                {
                    RecordKind = RecordKind.Path,
                    TypeId = evt.TypeId ?? "unknown",
                    RecordId = evt.RecordId ?? string.Empty,
                    Name = evt.RecordName ?? $"Path {evt.RecordId}",
                    State = evt.RecordState,
                    LastModified = evt.Timestamp,
                    RelatedEntityIds = evt.RelatedRecordIds.ToList()
                };
            }

            if (evt.EventType == EventType.SaveRecords)
            {
                if (evt.RecordKind == RecordKind.Path)
                {
                    var key = $"Path:{evt.RecordId}";
                    if (!entities.ContainsKey(key) || entities[key].LastModified < evt.Timestamp)
                    {
                        entities[key] = new ActiveEntity
                        {
                            RecordKind = RecordKind.Path,
                            TypeId = evt.TypeId ?? "unknown",
                            RecordId = evt.RecordId ?? string.Empty,
                            Name = evt.RecordName ?? $"Path {evt.RecordId}",
                            State = evt.RecordState,
                            LastModified = evt.Timestamp,
                            RelatedEntityIds = evt.RelatedRecordIds.ToList()
                        };
                    }
                }
                else if (evt.RecordKind == RecordKind.Node)
                {
                    var key = $"Node:{evt.RecordId}";
                    if (!entities.ContainsKey(key) || entities[key].LastModified < evt.Timestamp)
                    {
                        entities[key] = new ActiveEntity
                        {
                            RecordKind = RecordKind.Node,
                            TypeId = evt.TypeId ?? "unknown",
                            RecordId = evt.RecordId ?? string.Empty,
                            Name = evt.RecordName ?? $"Node {evt.RecordId}",
                            State = evt.RecordState,
                            LastModified = evt.Timestamp,
                            RelatedEntityIds = evt.RelatedRecordIds.ToList()
                        };
                    }
                }
                else if (evt.RecordKind == RecordKind.Connector)
                {
                    var key = $"Connector:{evt.RecordId}";
                    if (!entities.ContainsKey(key) || entities[key].LastModified < evt.Timestamp)
                    {
                        entities[key] = new ActiveEntity
                        {
                            RecordKind = RecordKind.Connector,
                            TypeId = evt.TypeId ?? "unknown",
                            RecordId = evt.RecordId ?? string.Empty,
                            Name = evt.RecordName ?? $"Connector {evt.RecordId}",
                            State = evt.RecordState,
                            LastModified = evt.Timestamp,
                            RelatedEntityIds = evt.RelatedRecordIds.ToList()
                        };
                    }
                }
            }
        }

        // Return the most recently modified entities
        return entities.Values
            .OrderByDescending(e => e.LastModified)
            .ToList();
    }

    /// <summary>
    /// Generates workflow hints from observable activity patterns.
    /// Detects rapid sequences, open-then-save patterns, and batch operations.
    /// </summary>
    private List<string> GenerateWorkflowHints(IReadOnlyList<ActivityEvent> userEvents)
    {
        var hints = new List<string>();

        if (userEvents.Count < 2)
            return hints;

        // Detect rapid sequences (events within 60 seconds)
        for (int i = 1; i < userEvents.Count; i++)
        {
            var prev = userEvents[i - 1];
            var curr = userEvents[i];
            var timeDiff = (curr.Timestamp - prev.Timestamp).TotalSeconds;

            if (timeDiff <= RapidSequenceThresholdSeconds && timeDiff > 0)
            {
                // Describe rapid sequence patterns
                if (prev.EventType == EventType.OpenWorkspace && curr.EventType == EventType.SaveRecords)
                {
                    if (prev.RecordKind == RecordKind.Path)
                    {
                        var pathName = prev.RecordName ?? $"Path {prev.RecordId}";
                        var recordKind = curr.RecordKind?.ToString() ?? "record";
                        hints.Add($"User opened {pathName} then saved {recordKind} within {(int)timeDiff}s");
                    }
                }

                // Detect when same record is accessed multiple times quickly
                if (prev.RecordId == curr.RecordId && prev.EventType == EventType.OpenWorkspace && curr.EventType == EventType.SaveRecords)
                {
                    var recordName = curr.RecordName ?? prev.RecordName ?? $"{curr.RecordKind} {curr.RecordId}";
                    hints.Add($"Rapid edit-save cycle on {recordName}");
                }
            }
        }

        // Detect batch operations (multiple record types saved together)
        var saveEvents = userEvents
            .Where(e => e.EventType == EventType.SaveRecords)
            .OrderBy(e => e.Timestamp)
            .ToList();

        if (saveEvents.Count >= 2)
        {
            for (int i = 1; i < saveEvents.Count; i++)
            {
                var prev = saveEvents[i - 1];
                var curr = saveEvents[i];
                var timeDiff = (curr.Timestamp - prev.Timestamp).TotalSeconds;

                if (timeDiff <= RapidSequenceThresholdSeconds && timeDiff > 0)
                {
                    if (prev.RecordKind != curr.RecordKind)
                    {
                        var records = new HashSet<string> { prev.RecordKind?.ToString() ?? "unknown", curr.RecordKind?.ToString() ?? "unknown" };

                        // Check if there's also a Path save
                        var allBatchSaves = saveEvents
                            .Where(e => Math.Abs((e.Timestamp - prev.Timestamp).TotalSeconds) <= RapidSequenceThresholdSeconds)
                            .Select(e => e.RecordKind?.ToString() ?? "unknown")
                            .Distinct()
                            .ToList();

                        if (allBatchSaves.Count >= 3)
                        {
                            hints.Add($"Batch save detected: {string.Join(", ", allBatchSaves.OrderBy(x => x))} records modified together");
                        }
                        else if (allBatchSaves.Count >= 2)
                        {
                            hints.Add($"Multiple record types ({string.Join(" + ", allBatchSaves.OrderBy(x => x))}) saved in close succession");
                        }
                    }
                }
            }
        }

        // Remove duplicates
        return hints.Distinct().ToList();
    }

    /// <summary>
    /// Builds a human-readable summary of the activity context.
    /// </summary>
    private string BuildSummary(ActivityContext context, IReadOnlyList<ActivityEvent> userEvents)
    {
        var parts = new List<string>();

        // Add user and time window info
        parts.Add($"{context.UserName} activity from {context.TimeWindowStart:HH:mm:ss} to {context.TimeWindowEnd:HH:mm:ss}");

        // Add current state
        parts.Add($"Current state: {FormatCurrentState(context.CurrentState)}");

        // Add event count and type breakdown
        var eventTypeBreakdown = userEvents
            .GroupBy(e => e.EventType)
            .Select(g => $"{g.Key}({g.Count()})")
            .ToList();

        if (eventTypeBreakdown.Count > 0)
        {
            parts.Add($"Activity: {string.Join(", ", eventTypeBreakdown)}");
        }

        // Add active entities
        if (context.ActiveEntities.Count > 0)
        {
            var entitySummary = context.ActiveEntities
                .Take(3)
                .Select(e => $"{e.RecordKind}({e.Name})")
                .ToList();

            parts.Add($"Active: {string.Join(", ", entitySummary)}");
            if (context.ActiveEntities.Count > 3)
                parts.Add($"... and {context.ActiveEntities.Count - 3} more entities");
        }

        return string.Join(" | ", parts);
    }

    /// <summary>
    /// Formats the CurrentState enum as a human-readable string.
    /// </summary>
    private string FormatCurrentState(CurrentState state) =>
        state switch
        {
            CurrentState.NoActivity => "No activity",
            CurrentState.PathOpened => "Path workspace opened",
            CurrentState.NodeModified => "Node modified (unsaved)",
            CurrentState.NodeSaved => "Node saved",
            CurrentState.ConnectorCreated => "Connector created",
            CurrentState.PathSaved => "Path saved",
            CurrentState.PathCreated => "Path created",
            CurrentState.Unknown => "Unknown state",
            _ => "Unknown"
        };
}
