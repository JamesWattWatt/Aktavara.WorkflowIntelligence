namespace Aktavara.WorkflowIntelligence.Core.Interfaces;

using Aktavara.WorkflowIntelligence.Core.Models;

/// <summary>
/// Provides access to help guides and sections from markdown documentation.
/// </summary>
public interface IHelpGuideStore
{
    /// <summary>
    /// Gets a help guide by filename (without .md extension).
    /// </summary>
    HelpGuide? GetByFileName(string fileName);

    /// <summary>
    /// Gets a help guide by its ID (filename without extension).
    /// </summary>
    HelpGuide? GetById(string helpGuideId);

    /// <summary>
    /// Gets a specific section within a guide.
    /// </summary>
    HelpGuideSection? GetSection(string fileName, string sectionId);

    /// <summary>
    /// Gets sections relevant to a workflow step.
    /// Returns empty list if no mapping exists (not an error).
    /// </summary>
    IReadOnlyList<HelpGuideSection> GetByWorkflowAndStep(string workflowId, string stepId);

    /// <summary>
    /// Gets all loaded help guides.
    /// </summary>
    IReadOnlyList<HelpGuide> GetAll();

    /// <summary>
    /// Gets all distinct workspace types represented in loaded guides.
    /// </summary>
    IReadOnlyList<string> GetWorkspaceTypes();

    /// <summary>
    /// Reloads all help guides and mappings from disk.
    /// Used when mappings are updated via API.
    /// </summary>
    void Reload();
}
