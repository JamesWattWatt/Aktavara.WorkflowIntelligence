namespace Aktavara.WorkflowIntelligence.Core.Models.Api;

/// <summary>
/// Request body for PATCH /api/workflows/{id}/status endpoint.
/// </summary>
public class UpdateWorkflowStatusRequest
{
    public string Status { get; set; } = "Candidate";
}
