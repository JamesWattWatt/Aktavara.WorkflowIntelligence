namespace Aktavara.WorkflowIntelligence.Core.Models;

/// <summary>
/// Indicates whether there is ambiguity between activity and semantic matches,
/// and recommends an action.
/// </summary>
public class AmbiguitySignal
{
    public bool IsAmbiguous { get; set; }
    public string? ActivityMatchId { get; set; }
    public string? SemanticMatchId { get; set; }
    public double ActivityConfidence { get; set; }
    public double SemanticScore { get; set; }

    /// <summary>
    /// Recommended action: UseActivity, UseSemantic, AskClarification, NoMatch
    /// </summary>
    public string RecommendedAction { get; set; } = "NoMatch";

    public string? ClarificationQuestion { get; set; }
}
