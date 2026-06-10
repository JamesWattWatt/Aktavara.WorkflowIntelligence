namespace Aktavara.WorkflowIntelligence.Core.Models;

public class ParsedWorkflow
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<WorkflowStep> Steps { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class WorkflowStep
{
    public int Order { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> Configuration { get; set; } = new();
}
