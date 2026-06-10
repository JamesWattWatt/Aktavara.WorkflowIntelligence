namespace Aktavara.WorkflowIntelligence.Core.Interfaces;

using Aktavara.WorkflowIntelligence.Core.Models;

/// <summary>
/// Provides access to workflow definitions from a library.
/// Workflows can be queried by ID or enumerated in full.
/// </summary>
public interface IWorkflowLibrary
{
    /// <summary>
    /// Gets all workflow definitions in the library.
    /// </summary>
    /// <returns>Read-only list of all available workflows.</returns>
    IReadOnlyList<WorkflowDefinition> GetAll();

    /// <summary>
    /// Gets a specific workflow definition by its unique identifier.
    /// </summary>
    /// <param name="workflowId">The ID of the workflow to retrieve.</param>
    /// <returns>The workflow definition if found; otherwise null.</returns>
    WorkflowDefinition? GetById(string workflowId);

    /// <summary>
    /// Gets all workflows that match a given tag.
    /// </summary>
    /// <param name="tag">The tag to search for.</param>
    /// <returns>Read-only list of workflows with the matching tag.</returns>
    IReadOnlyList<WorkflowDefinition> GetByTag(string tag);

    /// <summary>
    /// Gets all active/enabled workflows.
    /// </summary>
    /// <returns>Read-only list of active workflows.</returns>
    IReadOnlyList<WorkflowDefinition> GetActive();

    /// <summary>
    /// Gets validation errors for a workflow (if any).
    /// Returns empty list if the workflow is valid.
    /// </summary>
    /// <param name="workflowId">The ID of the workflow to validate.</param>
    /// <returns>List of validation errors; empty if valid.</returns>
    IReadOnlyList<string> GetValidationErrors(string workflowId);
}
