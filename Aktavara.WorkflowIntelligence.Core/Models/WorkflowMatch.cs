namespace Aktavara.WorkflowIntelligence.Core.Models;

public class WorkflowMatch
{
    public ParsedWorkflow Workflow { get; set; } = null!;
    public double ConfidenceScore { get; set; }
    public List<MatchingCriteria> MatchedCriteria { get; set; } = new();
    public List<string> MissingCriteria { get; set; } = new();
}

public class MatchingCriteria
{
    public string Name { get; set; } = string.Empty;
    public bool IsMet { get; set; }
    public double Weight { get; set; }
}
