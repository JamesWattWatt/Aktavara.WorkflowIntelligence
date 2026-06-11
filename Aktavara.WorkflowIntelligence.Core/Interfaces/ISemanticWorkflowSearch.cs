namespace Aktavara.WorkflowIntelligence.Core.Interfaces;

using Aktavara.WorkflowIntelligence.Core.Models;

/// <summary>
/// Provides semantic (keyword or embedding-based) search for workflows
/// based on free-text user descriptions.
/// </summary>
public interface ISemanticWorkflowSearch
{
    /// <summary>
    /// Whether this implementation uses real embeddings (true) or keyword matching (false).
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Searches for workflows matching the user's text description.
    /// </summary>
    Task<IReadOnlyList<SemanticWorkflowMatch>> SearchAsync(
        string userText,
        int topK,
        CancellationToken cancellationToken);
}
