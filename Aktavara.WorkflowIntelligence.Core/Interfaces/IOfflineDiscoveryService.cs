namespace Aktavara.WorkflowIntelligence.Core.Interfaces;

using Aktavara.WorkflowIntelligence.Core.Models;

/// <summary>
/// Service for inferring workflow definitions from activity event logs.
/// </summary>
public interface IOfflineDiscoveryService
{
    /// <summary>
    /// Infers a workflow suggestion from activity events.
    /// </summary>
    /// <param name="events">Activity events to analyze</param>
    /// <param name="candidateWorkflowId">Optional: workflow ID to check for ambiguity</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Inferred workflow suggestion with rules, states, and metadata</returns>
    Task<InferredWorkflowSuggestion> InferWorkflowSuggestionAsync(
        IEnumerable<ActivityEvent> events,
        string? candidateWorkflowId = null,
        CancellationToken cancellationToken = default);
}
