namespace Aktavara.WorkflowIntelligence.Core.Models;

/// <summary>
/// Result from semantic (keyword-based) workflow search.
/// </summary>
public class SemanticWorkflowMatch
{
    public string WorkflowId { get; set; } = string.Empty;
    public string WorkflowName { get; set; } = string.Empty;
    public double Score { get; set; }
    public List<string> MatchedTerms { get; set; } = new();
    public List<string> MatchedFields { get; set; } = new();
    public string Reason { get; set; } = string.Empty;
}
