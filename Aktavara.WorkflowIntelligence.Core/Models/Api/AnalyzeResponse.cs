namespace Aktavara.WorkflowIntelligence.Core.Models.Api;

/// <summary>
/// Response from the analyze endpoints containing full pipeline results.
/// </summary>
public class AnalyzeResponse
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
    public string FileName { get; set; } = string.Empty;
    public DateTime ParsedAt { get; set; } = DateTime.UtcNow;
    public int TotalEntries { get; set; }
    public int TotalEvents { get; set; }
    public long DurationMs { get; set; }
    public string CurrentState { get; set; } = string.Empty;
    public string GuidanceLevel { get; set; } = string.Empty;
    public string? RecommendedNextStep { get; set; }
    public string ContextNarrative { get; set; } = string.Empty;
    public List<WorkflowCandidateResult> WorkflowCandidates { get; set; } = new();
    public List<SerializableActiveEntity> ActiveEntities { get; set; } = new();
    public List<string> WorkflowHints { get; set; } = new();
}
