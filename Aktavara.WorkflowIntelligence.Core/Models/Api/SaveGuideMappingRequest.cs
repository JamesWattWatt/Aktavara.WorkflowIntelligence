namespace Aktavara.WorkflowIntelligence.Core.Models.Api;

/// <summary>
/// Request body for POST /api/help-guides/mapping endpoint.
/// </summary>
public class SaveGuideMappingRequest
{
    public string WorkflowId { get; set; } = string.Empty;
    public string StepId { get; set; } = string.Empty;
    public string GuideFile { get; set; } = string.Empty;
    public string? SectionId { get; set; }
}
