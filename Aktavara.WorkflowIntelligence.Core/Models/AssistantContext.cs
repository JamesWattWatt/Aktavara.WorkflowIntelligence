namespace Aktavara.WorkflowIntelligence.Core.Models;

public class AssistantContext
{
    public string WorkOrderId { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = string.Empty;
    public List<ActivityLogEntry> RecentActivity { get; set; } = new();
    public List<WorkflowMatch> PossibleWorkflows { get; set; } = new();
    public Dictionary<string, object> ContextData { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
}
