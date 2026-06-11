using Aktavara.WorkflowIntelligence.Core.Models;

namespace Aktavara.WorkflowIntelligence.Core.Services;

/// <summary>
/// Detects ambiguity between activity-based and semantic workflow matches.
/// Recommends which match to use or whether to ask for clarification.
/// </summary>
public class AmbiguityDetector
{
    public AmbiguitySignal Detect(
        WorkflowMatchResult? activityMatch,
        SemanticWorkflowMatch? semanticMatch)
    {
        var signal = new AmbiguitySignal();

        // No matches at all
        if (activityMatch == null && semanticMatch == null)
        {
            signal.IsAmbiguous = false;
            signal.RecommendedAction = "NoMatch";
            signal.ClarificationQuestion = null;
            return signal;
        }

        // Only activity match
        if (activityMatch != null && semanticMatch == null)
        {
            signal.IsAmbiguous = false;
            signal.ActivityMatchId = activityMatch.WorkflowId;
            signal.ActivityConfidence = activityMatch.ConfidenceScore;
            signal.RecommendedAction = "UseActivity";
            signal.ClarificationQuestion = null;
            return signal;
        }

        // Only semantic match
        if (activityMatch == null && semanticMatch != null)
        {
            signal.IsAmbiguous = false;
            signal.SemanticMatchId = semanticMatch.WorkflowId;
            signal.SemanticScore = semanticMatch.Score;
            signal.RecommendedAction = "UseSemantic";
            signal.ClarificationQuestion = null;
            return signal;
        }

        // Both matches exist - analyze confidence levels
        signal.ActivityMatchId = activityMatch!.WorkflowId;
        signal.SemanticMatchId = semanticMatch!.WorkflowId;
        signal.ActivityConfidence = activityMatch.ConfidenceScore;
        signal.SemanticScore = semanticMatch.Score;

        // Same workflow matched by both
        if (activityMatch.WorkflowId == semanticMatch.WorkflowId)
        {
            signal.IsAmbiguous = false;
            signal.RecommendedAction = "UseActivity";
            signal.ClarificationQuestion = null;
            return signal;
        }

        // Different workflows matched - check confidence scores
        var activityIsStrong = activityMatch.ConfidenceScore >= 0.7;
        var semanticIsStrong = semanticMatch.Score >= 0.6;

        if (activityIsStrong && !semanticIsStrong)
        {
            signal.IsAmbiguous = false;
            signal.RecommendedAction = "UseActivity";
            signal.ClarificationQuestion = null;
            return signal;
        }

        if (semanticIsStrong && !activityIsStrong)
        {
            signal.IsAmbiguous = false;
            signal.RecommendedAction = "UseSemantic";
            signal.ClarificationQuestion = null;
            return signal;
        }

        // Both strong or both weak - ambiguous
        signal.IsAmbiguous = true;
        signal.RecommendedAction = "AskClarification";
        signal.ClarificationQuestion = GenerateClarificationQuestion(activityMatch, semanticMatch);

        return signal;
    }

    private string GenerateClarificationQuestion(WorkflowMatchResult activityMatch, SemanticWorkflowMatch semanticMatch)
    {
        return $"Did you mean to perform '{semanticMatch.WorkflowName}' based on your description, " +
               $"or continue with '{activityMatch.WorkflowName}' based on your current activity?";
    }
}
