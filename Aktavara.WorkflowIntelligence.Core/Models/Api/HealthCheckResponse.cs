namespace Aktavara.WorkflowIntelligence.Core.Models.Api;

/// <summary>
/// Response from GET /api/health endpoint.
/// </summary>
public class HealthCheckResponse
{
    public string Status { get; set; } = "healthy";
    public int WorkflowCount { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Version { get; set; } = "1.0.0";
}
