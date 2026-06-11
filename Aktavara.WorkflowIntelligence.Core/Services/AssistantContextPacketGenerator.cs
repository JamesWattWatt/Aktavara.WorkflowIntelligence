using Aktavara.WorkflowIntelligence.Core.Interfaces;
using Aktavara.WorkflowIntelligence.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aktavara.WorkflowIntelligence.Core.Services;

/// <summary>
/// Generates AssistantContextPackets from activity context and workflow matching results.
/// Produces serializable packets suitable for sending to an LLM API.
/// </summary>
public class AssistantContextPacketGenerator : IAssistantContextPacketGenerator
{
    private readonly ILogger<AssistantContextPacketGenerator> _logger;
    private readonly IHelpGuideStore? _helpGuideStore;
    private readonly ISemanticWorkflowSearch? _semanticSearch;
    private readonly AmbiguityDetector _ambiguityDetector;

    public AssistantContextPacketGenerator(
        ILogger<AssistantContextPacketGenerator> logger,
        IHelpGuideStore? helpGuideStore = null,
        ISemanticWorkflowSearch? semanticSearch = null)
    {
        _logger = logger;
        _helpGuideStore = helpGuideStore;
        _semanticSearch = semanticSearch;
        _ambiguityDetector = new AmbiguityDetector();
    }

    /// <summary>
    /// Generates a context packet from activity and workflow matching results.
    /// Optionally performs semantic search and ambiguity detection if userText is provided.
    /// </summary>
    public AssistantContextPacket GeneratePacket(
        ActivityContext activityContext,
        IReadOnlyList<WorkflowMatchResult> allMatches,
        IWorkflowLibrary workflowLibrary,
        string? userText = null)
    {
        var packet = new AssistantContextPacket
        {
            GeneratedAt = DateTime.UtcNow,
            UserName = activityContext.UserName,
            SessionId = activityContext.RecentEvents.FirstOrDefault()?.SessionId,
            CurrentState = FormatCurrentState(activityContext.CurrentState),
            Summary = activityContext.Summary,
            WorkflowHints = activityContext.WorkflowHints,
            UserText = userText,
        };

        // Convert ActiveEntities to serializable form
        packet.ActiveEntities = activityContext.ActiveEntities
            .Select(e => new SerializableActiveEntity
            {
                RecordKind = e.RecordKind.ToString(),
                TypeId = e.TypeId,
                RecordId = e.RecordId,
                Name = e.Name,
                State = e.State,
                LastModified = e.LastModified
            })
            .ToList();

        // Populate matches - sorted by confidence score (highest first)
        packet.AllMatches = allMatches
            .OrderByDescending(m => m.ConfidenceScore)
            .Select(m => ConvertToSummary(m, workflowLibrary))
            .ToList();

        // Determine best match (highest confidence)
        packet.BestMatch = packet.AllMatches.FirstOrDefault();

        // Determine guidance level
        packet.GuidanceLevel = DetermineGuidanceLevel(packet.BestMatch);

        // Get recommended next step
        packet.RecommendedNextStep = packet.BestMatch != null
            ? packet.BestMatch.NextStepHint
            : null;

        // Load relevant help guide sections
        if (_helpGuideStore != null && packet.BestMatch != null)
        {
            var relevantSections = _helpGuideStore.GetByWorkflowAndStep(
                packet.BestMatch.WorkflowId,
                packet.BestMatch.CurrentStateName);

            packet.RelevantGuideSections = relevantSections
                .Take(2)
                .ToList();
        }

        // Perform semantic search if userText provided
        if (!string.IsNullOrWhiteSpace(userText) && _semanticSearch != null)
        {
            var semanticMatches = _semanticSearch.SearchAsync(userText, 5, CancellationToken.None).Result;
            packet.SemanticMatches = semanticMatches.ToList();

            // Detect ambiguity between activity and semantic matches
            var bestActivityMatch = packet.BestMatch != null
                ? allMatches.FirstOrDefault(m => m.WorkflowId == packet.BestMatch.WorkflowId)
                : null;
            var bestSemanticMatch = packet.SemanticMatches.FirstOrDefault();

            packet.Ambiguity = _ambiguityDetector.Detect(bestActivityMatch, bestSemanticMatch);
        }

        // Build context narrative
        packet.ContextNarrative = BuildContextNarrative(packet);

        _logger.LogInformation(
            "Generated context packet for {UserName}: {MatchCount} matches, guidance={GuidanceLevel}, state={CurrentState}",
            packet.UserName,
            packet.AllMatches.Count,
            packet.GuidanceLevel,
            packet.CurrentState);

        return packet;
    }

    /// <summary>
    /// Converts a WorkflowMatchResult to a WorkflowMatchSummary with full details.
    /// </summary>
    private WorkflowMatchSummary ConvertToSummary(
        WorkflowMatchResult matchResult,
        IWorkflowLibrary workflowLibrary)
    {
        var summary = new WorkflowMatchSummary
        {
            WorkflowId = matchResult.WorkflowId,
            WorkflowName = matchResult.WorkflowName,
            ConfidenceScore = matchResult.ConfidenceScore,
            ConfidenceLevel = matchResult.ConfidenceLevel.ToString(),
            CurrentStateName = matchResult.CurrentStateName,
            MatchedRules = matchResult.RuleScores.Keys.ToList(),
            MissingRules = matchResult.MissingEvidence.ToList(),
        };

        // Convert matched evidence to human-readable descriptions
        foreach (var evt in matchResult.MatchedEvidence)
        {
            summary.MatchedEvidence.Add(FormatEventEvidence(evt));
        }

        // Populate score breakdown
        var breakdown = matchResult.ScoreBreakdown;
        if (breakdown != null)
        {
            summary.ScoreBreakdown["Matched Rules"] = breakdown.MatchedRulesWeight;
            summary.ScoreBreakdown["Missing Penalty"] = breakdown.MissingRulesPenalty;
            summary.ScoreBreakdown["Sequence Bonus"] = breakdown.SequenceBonus;
            summary.ScoreBreakdown["Entity Correlation"] = breakdown.EntityCorrelationBonus;
            summary.ScoreBreakdown["Staleness Penalty"] = breakdown.StalenesssPenalty;
            summary.ScoreBreakdown["Final Score"] = breakdown.FinalScore;
        }

        // Look up next step from workflow definition
        summary.NextStepHint = LookupNextStep(matchResult, workflowLibrary);

        return summary;
    }

    /// <summary>
    /// Formats an ActivityEvent as human-readable evidence description.
    /// </summary>
    private string FormatEventEvidence(ActivityEvent evt)
    {
        var parts = new List<string>();

        // Event type and record kind
        parts.Add($"{evt.EventType}");
        if (evt.RecordKind.HasValue)
            parts.Add($"({evt.RecordKind})");

        // Record identifier
        if (!string.IsNullOrEmpty(evt.RecordName))
            parts.Add($"{evt.RecordName}");
        else if (!string.IsNullOrEmpty(evt.RecordId))
            parts.Add($"ID:{evt.RecordId}");

        // Timestamp
        parts.Add($"@ {evt.Timestamp:HH:mm:ss}");

        return string.Join(" ", parts);
    }

    /// <summary>
    /// Looks up the next step hint from the workflow definition's current state.
    /// </summary>
    private string? LookupNextStep(WorkflowMatchResult matchResult, IWorkflowLibrary workflowLibrary)
    {
        try
        {
            var workflows = workflowLibrary.GetAll();
            var workflow = workflows.FirstOrDefault(w => w.WorkflowId == matchResult.WorkflowId);

            if (workflow?.States != null && !string.IsNullOrEmpty(matchResult.CurrentStateId))
            {
                var state = workflow.States.FirstOrDefault(s => s.StateId == matchResult.CurrentStateId);
                if (state != null)
                {
                    return state.HelpGuideId ?? "Review the workflow documentation";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not look up next step for workflow {WorkflowId}", matchResult.WorkflowId);
        }

        return null;
    }

    /// <summary>
    /// Determines the guidance level based on best match confidence.
    /// </summary>
    private GuidanceLevel DetermineGuidanceLevel(WorkflowMatchSummary? bestMatch)
    {
        if (bestMatch == null)
            return GuidanceLevel.NoGuidance;

        return bestMatch.ConfidenceScore switch
        {
            >= 0.85 => GuidanceLevel.Instruct,
            >= 0.55 => GuidanceLevel.Confirm,
            > 0 => GuidanceLevel.Suggest,
            _ => GuidanceLevel.NoGuidance
        };
    }

    /// <summary>
    /// Builds a context narrative for the LLM system prompt.
    /// </summary>
    private string BuildContextNarrative(AssistantContextPacket packet)
    {
        var parts = new List<string>();

        // User and current state
        parts.Add($"User {packet.UserName} is currently {packet.CurrentState.ToLower()}.");

        // Active entities
        if (packet.ActiveEntities.Count > 0)
        {
            var entitySummary = string.Join(", ",
                packet.ActiveEntities
                    .Take(3)
                    .Select(e => $"{e.RecordKind} '{e.Name}'"));
            parts.Add($"They have been working with {entitySummary}");
            if (packet.ActiveEntities.Count > 3)
                parts.Add($"and {packet.ActiveEntities.Count - 3} other entities.");
        }

        // Workflow match
        if (packet.BestMatch != null)
        {
            parts.Add($"Recent activity suggests they are '{packet.BestMatch.WorkflowName}' " +
                     $"with {(packet.BestMatch.ConfidenceScore * 100):F0}% confidence.");

            // Evidence summary
            if (packet.BestMatch.MatchedEvidence.Count > 0)
            {
                parts.Add($"The matched evidence includes: {string.Join(", ", packet.BestMatch.MatchedEvidence.Take(3))}");
                if (packet.BestMatch.MatchedEvidence.Count > 3)
                    parts.Add($"and {packet.BestMatch.MatchedEvidence.Count - 3} other events.");
            }

            // Next step
            if (!string.IsNullOrEmpty(packet.RecommendedNextStep))
            {
                parts.Add($"Recommended next step: {packet.RecommendedNextStep}");
            }
        }
        else
        {
            parts.Add("The current activity does not clearly match any defined workflow.");
        }

        return string.Join(" ", parts);
    }

    /// <summary>
    /// Formats the CurrentState enum as a human-readable string.
    /// </summary>
    private string FormatCurrentState(CurrentState state) =>
        state switch
        {
            CurrentState.NoActivity => "inactive",
            CurrentState.PathOpened => "editing a path workspace",
            CurrentState.NodeModified => "modifying a node",
            CurrentState.NodeSaved => "saving a node",
            CurrentState.ConnectorCreated => "creating a connector",
            CurrentState.PathSaved => "saving a path",
            CurrentState.PathCreated => "creating a path",
            CurrentState.Unknown => "in an unclear state",
            _ => "in an unknown state"
        };
}
