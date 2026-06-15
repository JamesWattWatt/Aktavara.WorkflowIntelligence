namespace Aktavara.WorkflowIntelligence.Core.Interfaces;

using Aktavara.WorkflowIntelligence.Core.Models;

/// <summary>
/// Service for suggesting relevant help guide sections for workflow steps using LLM.
/// </summary>
public interface IIntelligentHelpGuideMatcher
{
    /// <summary>
    /// Suggests the best matching help guide section for a workflow step based on activity evidence.
    /// </summary>
    Task<GuideSuggestion?> SuggestAsync(
        string workflowId,
        string workflowName,
        string stepId,
        string currentStateName,
        IReadOnlyList<string> matchedRules,
        IReadOnlyList<string> matchedEvidence,
        CancellationToken cancellationToken = default);
}
