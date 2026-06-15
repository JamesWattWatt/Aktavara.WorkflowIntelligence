namespace Aktavara.WorkflowIntelligence.Core.Models;

/// <summary>
/// Represents a variation in a workflow pattern observed in activity data.
/// </summary>
public class WorkflowVariant
{
    public string VariantId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> DifferentSteps { get; set; } = new();
    public int OccurrenceCount { get; set; }
    public double Percentage { get; set; }
}
