namespace Aktavara.WorkflowIntelligence.Core.Interfaces;

/// <summary>
/// Service for persisting help guide mappings to the mapping file.
/// </summary>
public interface IHelpGuideMappingWriter
{
    /// <summary>
    /// Saves a help guide mapping for a workflow step (adds or updates existing).
    /// </summary>
    Task SaveMappingAsync(
        string workflowId,
        string stepId,
        string guideFile,
        string? sectionId,
        CancellationToken cancellationToken = default);
}
