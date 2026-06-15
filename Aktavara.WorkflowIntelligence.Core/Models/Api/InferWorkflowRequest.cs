namespace Aktavara.WorkflowIntelligence.Core.Models.Api;

/// <summary>
/// Request body for POST /api/workflows/infer endpoint.
/// </summary>
public class InferWorkflowRequest
{
    public string? RawLogContent { get; set; }
    public string? CandidateWorkflowId { get; set; }
}
