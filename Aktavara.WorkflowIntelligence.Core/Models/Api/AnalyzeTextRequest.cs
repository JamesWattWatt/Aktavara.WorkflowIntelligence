namespace Aktavara.WorkflowIntelligence.Core.Models.Api;

/// <summary>
/// Request body for POST /api/analyze/text endpoint.
/// </summary>
public class AnalyzeTextRequest
{
    public string LogContent { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int TimeWindowMinutes { get; set; } = 30;
    public string? UserQuestion { get; set; }
}
