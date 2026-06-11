using Aktavara.WorkflowIntelligence.Core.Interfaces;
using Aktavara.WorkflowIntelligence.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aktavara.WorkflowIntelligence.Core.Services;

/// <summary>
/// Matches user activity against workflow definitions with comprehensive confidence scoring.
/// Uses multiple scoring factors: rule matching, sequencing, entity correlation, and staleness.
/// </summary>
public class WorkflowMatcher : IWorkflowMatcher
{
    private readonly ILogger<WorkflowMatcher> _logger;
    private const double SequenceBonusMax = 0.15;
    private const double EntityCorrelationBonusMax = 0.15;
    private const double StalenesssPenaltyMax = 0.10;
    private const int PreferredEventWindowMinutes = 30;

    public WorkflowMatcher(ILogger<WorkflowMatcher> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Finds matching workflows ranked by confidence score.
    /// </summary>
    public IReadOnlyList<WorkflowMatchResult> FindMatches(
        ActivityContext activityContext,
        IReadOnlyList<WorkflowDefinition> workflows)
    {
        if (workflows.Count == 0)
        {
            _logger.LogInformation("No workflows to match against");
            return new List<WorkflowMatchResult>();
        }

        var results = new List<WorkflowMatchResult>();

        foreach (var workflow in workflows)
        {
            var result = ScoreWorkflow(workflow, activityContext);
            results.Add(result);
        }

        // Return ranked by confidence (highest first)
        return results
            .OrderByDescending(r => r.ConfidenceScore)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Finds the best matching workflow above the minimum confidence threshold.
    /// </summary>
    public WorkflowMatchResult? FindBestMatch(
        ActivityContext activityContext,
        IReadOnlyList<WorkflowDefinition> workflows,
        double minimumConfidence = 0.55)
    {
        var matches = FindMatches(activityContext, workflows);
        var bestMatch = matches.FirstOrDefault();

        if (bestMatch != null && bestMatch.ConfidenceScore >= minimumConfidence)
        {
            _logger.LogInformation(
                "Found best match: {WorkflowId} with confidence {Confidence:P0}",
                bestMatch.WorkflowId,
                bestMatch.ConfidenceScore);
            return bestMatch;
        }

        _logger.LogInformation(
            "No workflows matched with confidence >= {MinConfidence:P0}",
            minimumConfidence);
        return null;
    }

    /// <summary>
    /// Scores a single workflow against activity context using comprehensive algorithm.
    /// </summary>
    public WorkflowMatchResult ScoreWorkflow(
        WorkflowDefinition workflow,
        ActivityContext activityContext)
    {
        var result = new WorkflowMatchResult
        {
            WorkflowId = workflow.WorkflowId,
            WorkflowName = workflow.Name,
            CalculatedAt = DateTime.UtcNow
        };

        var breakdown = new WorkflowScoreBreakdown();

        // Get recent events (prefer events within preferred window)
        var recentEvents = activityContext.RecentEvents.ToList();
        if (recentEvents.Count == 0)
        {
            result.ConfidenceScore = 0.0;
            result.ScoreBreakdown = breakdown;
            return result;
        }

        var now = DateTime.UtcNow;

        _logger.LogInformation("Available events for matching: {EventCount} total events", recentEvents.Count);
        foreach (var evt in recentEvents)
        {
            var age = (int)(now - evt.Timestamp).TotalMinutes;
            _logger.LogInformation(
                "  - EventType={EventType}, RecordKind={RecordKind}, WorkspaceKind={WorkspaceKind}, Timestamp={Timestamp:O} (age={AgeMinutes}min)",
                evt.EventType,
                evt.RecordKind,
                evt.WorkspaceKind,
                evt.Timestamp,
                age);
        }

        // 1. Score matched rules
        breakdown.MatchedRulesWeight = 0.0;
        var matchedRules = new HashSet<string>();

        _logger.LogInformation("=== Rule Matching for {WorkflowName} ===", workflow.Name);
        foreach (var rule in workflow.ActivitySignature)
        {
            var matched = false;
            ActivityEvent? matchedEvent = null;

            foreach (var evt in recentEvents)
            {
                var eventAge = (int)(now - evt.Timestamp).TotalMinutes;
                if (rule.Matches(evt, eventAge))
                {
                    matched = true;
                    matchedEvent = evt;
                    break;
                }
            }

            if (matched)
            {
                breakdown.MatchedRulesWeight += rule.Weight;
                result.RuleScores[rule.Description] = rule.Weight;
                matchedRules.Add(rule.Description);
                _logger.LogInformation(
                    "✓ MATCHED: {Description} (weight={Weight:F2}) | EventType={EventType}, RecordKind={RecordKind}, Timestamp={Timestamp:O}",
                    rule.Description,
                    rule.Weight,
                    matchedEvent?.EventType,
                    matchedEvent?.RecordKind,
                    matchedEvent?.Timestamp);
            }
            else
            {
                var reasons = new List<string>();
                foreach (var evt in recentEvents)
                {
                    var eventAge = (int)(now - evt.Timestamp).TotalMinutes;
                    if (evt.EventType != rule.EventType)
                        reasons.Add($"EventType mismatch: expected {rule.EventType}, got {evt.EventType}");
                    if (rule.RecordKind.HasValue && evt.RecordKind != rule.RecordKind)
                        reasons.Add($"RecordKind mismatch: expected {rule.RecordKind}, got {evt.RecordKind}");
                    if (!string.IsNullOrEmpty(rule.WorkspaceKind) && evt.WorkspaceKind != rule.WorkspaceKind)
                        reasons.Add($"WorkspaceKind mismatch: expected {rule.WorkspaceKind}, got {evt.WorkspaceKind}");
                    if (rule.MaxAgeMinutes.HasValue && eventAge > rule.MaxAgeMinutes)
                        reasons.Add($"Age check: event is {eventAge}min old, max allowed is {rule.MaxAgeMinutes}min");
                }

                if (rule.Required)
                {
                    // 2. Penalize missing required rules
                    breakdown.MissingRulesPenalty += rule.MissingPenalty;
                    result.MissingEvidence.Add(rule.Description);
                    _logger.LogWarning(
                        "✗ MISSING (REQUIRED): {Description} (penalty={Penalty:F2}) | No matching events found",
                        rule.Description,
                        rule.MissingPenalty);
                }
                else
                {
                    _logger.LogInformation(
                        "○ OPTIONAL NOT MATCHED: {Description} (weight={Weight:F2})",
                        rule.Description,
                        rule.Weight);
                }
            }
        }

        // 3. Sequence bonus: events in expected order
        var sequenceBonus = CalculateSequenceBonus(workflow.ActivitySignature, recentEvents);
        breakdown.SequenceBonus = sequenceBonus;

        // 4. Entity correlation bonus
        var correlationBonus = CalculateEntityCorrelationBonus(workflow, recentEvents);
        breakdown.EntityCorrelationBonus = correlationBonus;

        // 5. Staleness penalty for old events
        var stalenesspPenalty = CalculateStalenesssPenalty(recentEvents, now);
        breakdown.StalenesssPenalty = stalenesspPenalty;

        // Calculate raw score
        breakdown.RawScore = breakdown.MatchedRulesWeight
            - breakdown.MissingRulesPenalty
            + breakdown.SequenceBonus
            + breakdown.EntityCorrelationBonus
            - breakdown.StalenesssPenalty;

        // Clamp to [0, 1]
        breakdown.FinalScore = Math.Max(0.0, Math.Min(1.0, breakdown.RawScore));
        result.ConfidenceScore = breakdown.FinalScore;

        // 9. Determine current state
        DetermineCurrentState(workflow, recentEvents, result);

        // Collect matched evidence
        foreach (var evt in recentEvents.Where(e => matchedRules.Any(
            r => r.Contains(e.EventType.ToString()))))
        {
            result.MatchedEvidence.Add(evt);
        }

        // Build explanation
        result.ScoreBreakdown = breakdown;
        BuildExplanation(result);

        return result;
    }

    /// <summary>
    /// Calculates bonus for events appearing in expected order.
    /// </summary>
    private double CalculateSequenceBonus(
        IReadOnlyList<WorkflowSignatureRule> rules,
        IReadOnlyList<ActivityEvent> events)
    {
        if (rules.Count < 2 || events.Count < 2)
            return 0.0;

        // Check if events appear in rule order
        int ruleIndex = 0;
        var matchedSequentially = 0;

        foreach (var evt in events)
        {
            for (int i = ruleIndex; i < rules.Count; i++)
            {
                if (rules[i].Matches(evt, 0))
                {
                    if (i == ruleIndex)
                    {
                        matchedSequentially++;
                        ruleIndex = i + 1;
                    }
                    break;
                }
            }
        }

        // Bonus based on sequential matching
        var sequenceRatio = (double)matchedSequentially / rules.Count;
        return Math.Min(SequenceBonusMax, sequenceRatio * SequenceBonusMax * 2);
    }

    /// <summary>
    /// Calculates bonus for entity correlation (same workspace/record).
    /// </summary>
    private double CalculateEntityCorrelationBonus(
        WorkflowDefinition workflow,
        IReadOnlyList<ActivityEvent> events)
    {
        var bonus = 0.0;

        // Look for correlation patterns
        var openWorkspaceEvents = events.Where(e => e.EventType == EventType.OpenWorkspace).ToList();
        var saveEvents = events.Where(e => e.EventType == EventType.SaveRecords).ToList();

        if (openWorkspaceEvents.Count > 0)
        {
            // Bonus if same workspace was opened
            var openedWorkspaceKind = openWorkspaceEvents.First().WorkspaceKind;
            var savesSameWorkspace = saveEvents.Any(e =>
                e.WorkspaceKind == openedWorkspaceKind ||
                e.RecordKind.ToString() == openedWorkspaceKind);

            if (savesSameWorkspace)
            {
                bonus += EntityCorrelationBonusMax * 0.5;
            }

            // Bonus if saves match the opened workspace type
            var correlatedSaves = saveEvents.Where(s =>
                s.RecordKind?.ToString() == openedWorkspaceKind ||
                (s.RecordKind.HasValue && (int)s.RecordKind < 10)).Count();

            if (correlatedSaves > 0)
            {
                bonus += Math.Min(EntityCorrelationBonusMax * 0.5, correlatedSaves * 0.05);
            }
        }

        return Math.Min(EntityCorrelationBonusMax, bonus);
    }

    /// <summary>
    /// Calculates penalty for events outside preferred time window.
    /// </summary>
    private double CalculateStalenesssPenalty(
        IReadOnlyList<ActivityEvent> events,
        DateTime now)
    {
        if (events.Count == 0)
            return 0.0;

        var oldestEventAge = (int)(now - events.Min(e => e.Timestamp)).TotalMinutes;

        if (oldestEventAge <= PreferredEventWindowMinutes)
            return 0.0;

        // Linear penalty for age beyond preferred window
        var excessMinutes = oldestEventAge - PreferredEventWindowMinutes;
        var penaltyRatio = Math.Min(1.0, excessMinutes / (double)PreferredEventWindowMinutes);

        return penaltyRatio * StalenesssPenaltyMax;
    }

    /// <summary>
    /// Determines the current workflow state based on matched evidence.
    /// </summary>
    private void DetermineCurrentState(
        WorkflowDefinition workflow,
        IReadOnlyList<ActivityEvent> events,
        WorkflowMatchResult result)
    {
        if (workflow.States.Count == 0)
            return;

        // Check each state to see if its evidence requirements are met
        foreach (var state in workflow.States.OrderByDescending(s => s.Sequence))
        {
            var stateMatched = state.RequiredEvidence.All(evidence =>
                events.Any(e => e.EventType.ToString() == evidence ||
                               e.Evidence.Any(ev => ev.Contains(evidence))));

            if (stateMatched)
            {
                result.CurrentStateId = state.StateId;
                result.CurrentStateName = state.Name;

                // Set next state
                if (!state.IsTerminal && !string.IsNullOrEmpty(state.NextStateId))
                {
                    result.NextStateId = state.NextStateId;
                }

                break;
            }
        }

        // If no state matched, set to initial state
        if (string.IsNullOrEmpty(result.CurrentStateId))
        {
            var initialState = workflow.GetInitialState();
            if (initialState != null)
            {
                result.CurrentStateId = initialState.StateId;
                result.CurrentStateName = initialState.Name;
            }
        }
    }

    /// <summary>
    /// Builds human-readable explanation of the match result.
    /// </summary>
    private void BuildExplanation(WorkflowMatchResult result)
    {
        var explanation = new List<string>();

        explanation.Add($"Confidence: {result.ConfidenceScore:P0} ({result.ConfidenceLevel})");

        if (result.MatchedEvidence.Count > 0)
        {
            explanation.Add($"Matched {result.MatchedEvidence.Count} evidence rules");
        }

        if (result.MissingEvidence.Count > 0)
        {
            explanation.Add($"Missing {result.MissingEvidence.Count} required evidence");
        }

        if (result.CurrentStateName != null)
        {
            explanation.Add($"Current state: {result.CurrentStateName}");
        }

        var breakdown = result.ScoreBreakdown;
        explanation.Add($"Score components: +{breakdown.MatchedRulesWeight:F2} (rules) " +
                       $"-{breakdown.MissingRulesPenalty:F2} (missing) " +
                       $"+{breakdown.SequenceBonus:F2} (sequence) " +
                       $"+{breakdown.EntityCorrelationBonus:F2} (correlation) " +
                       $"-{breakdown.StalenesssPenalty:F2} (age)");

        // Populate Details dictionary for detailed breakdown display
        breakdown.Details["Matched Rules"] = $"{result.RuleScores.Count} rules matched: {string.Join(", ", result.RuleScores.Keys)}";
        breakdown.Details["Missing Rules"] = result.MissingEvidence.Count > 0
            ? $"{result.MissingEvidence.Count} rules missing: {string.Join(", ", result.MissingEvidence)}"
            : "No missing rules";
        breakdown.Details["Matched Evidence Count"] = $"{result.MatchedEvidence.Count} events";
        breakdown.Details["Sequence"] = $"Bonus: {breakdown.SequenceBonus:F3}";
        breakdown.Details["Entity Correlation"] = $"Bonus: {breakdown.EntityCorrelationBonus:F3}";
        if (breakdown.StalenesssPenalty > 0)
            breakdown.Details["Staleness"] = $"Penalty: {breakdown.StalenesssPenalty:F3}";
        breakdown.Details["Calculation"] = $"{breakdown.MatchedRulesWeight:F3} - {breakdown.MissingRulesPenalty:F3} + {breakdown.SequenceBonus:F3} + {breakdown.EntityCorrelationBonus:F3} - {breakdown.StalenesssPenalty:F3} = {breakdown.RawScore:F3} → {breakdown.FinalScore:F3}";

        result.Metadata["explanation"] = string.Join("; ", explanation);
    }
}
