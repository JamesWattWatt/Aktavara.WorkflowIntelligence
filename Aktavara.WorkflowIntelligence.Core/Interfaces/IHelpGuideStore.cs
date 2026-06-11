namespace Aktavara.WorkflowIntelligence.Core.Interfaces;

using Aktavara.WorkflowIntelligence.Core.Models;

/// <summary>
/// Provides access to help guides for workflows and steps.
/// </summary>
public interface IHelpGuideStore
{
    /// <summary>
    /// Gets a help guide by ID.
    /// </summary>
    HelpGuide? GetById(string helpGuideId);

    /// <summary>
    /// Gets all help guides for a specific workflow.
    /// </summary>
    IReadOnlyList<HelpGuide> GetByWorkflowId(string workflowId);

    /// <summary>
    /// Gets all help guides for a specific workflow step.
    /// </summary>
    IReadOnlyList<HelpGuide> GetByStepId(string workflowId, string stepId);

    /// <summary>
    /// Gets all loaded help guides.
    /// </summary>
    IReadOnlyList<HelpGuide> GetAll();
}
