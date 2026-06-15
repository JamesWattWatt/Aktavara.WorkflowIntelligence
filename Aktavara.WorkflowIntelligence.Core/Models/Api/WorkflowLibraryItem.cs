namespace Aktavara.WorkflowIntelligence.Core.Models.Api;

/// <summary>
/// Summary of a workflow in the library for list/library view.
/// </summary>
public class WorkflowLibraryItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string Description { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public int RuleCount { get; set; }
    public int StateCount { get; set; }
    public DateTime LastModified { get; set; }
    public string FileName { get; set; } = string.Empty;
}
