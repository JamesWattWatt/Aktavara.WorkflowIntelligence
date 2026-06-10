namespace Aktavara.WorkflowIntelligence.Core.Models;

public class ActivityLogEntry
{
    public string Id { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public Dictionary<string, string> Details { get; set; } = new();
    public string? RawContent { get; set; }
}
