using Aktavara.WorkflowIntelligence.Core.Models;

namespace Aktavara.WorkflowIntelligence.Core.Interfaces;

public interface IWorkflowMatcher
{
    Task<List<WorkflowMatch>> MatchWorkflowsAsync(List<ActivityLogEntry> activities);
    Task<WorkflowMatch?> FindBestMatchAsync(List<ActivityLogEntry> activities);
}
