namespace Aktavara.WorkflowIntelligence.Core.Models.Api;

public class ReloadResponse
{
    public int ReloadedCount { get; set; }
    public DateTime Timestamp { get; set; }
    public List<string> LoadedWorkflowIds { get; set; } = new();
}
