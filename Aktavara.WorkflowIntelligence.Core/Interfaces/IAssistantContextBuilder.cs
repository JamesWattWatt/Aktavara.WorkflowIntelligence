using Aktavara.WorkflowIntelligence.Core.Models;

namespace Aktavara.WorkflowIntelligence.Core.Interfaces;

public interface IAssistantContextBuilder
{
    Task<AssistantContext> BuildContextAsync(string workOrderId, List<ActivityLogEntry> activities);
    Task<AssistantContext> BuildContextWithMatchesAsync(
        string workOrderId,
        List<ActivityLogEntry> activities,
        List<WorkflowMatch> matches);
}
