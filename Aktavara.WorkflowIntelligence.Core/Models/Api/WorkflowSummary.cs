namespace Aktavara.WorkflowIntelligence.Core.Models.Api;

/// <summary>
/// Summary information about a workflow for list endpoints.
/// </summary>
public class WorkflowSummary
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Candidate";
    public string Version { get; set; } = "1.0";
    public string Description { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = "Medium";
    public List<string> Tags { get; set; } = new();
    public bool IsValid { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public int RuleCount { get; set; }
    public int StateCount { get; set; }
    public double ConfidenceThreshold { get; set; } = 0.5;
}
