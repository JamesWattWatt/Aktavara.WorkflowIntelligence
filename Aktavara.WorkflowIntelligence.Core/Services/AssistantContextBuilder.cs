using Aktavara.WorkflowIntelligence.Core.Interfaces;
using Aktavara.WorkflowIntelligence.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aktavara.WorkflowIntelligence.Core.Services;

public class AssistantContextBuilder : IAssistantContextBuilder
{
    private readonly ILogger<AssistantContextBuilder> _logger;

    public AssistantContextBuilder(ILogger<AssistantContextBuilder> logger)
    {
        _logger = logger;
    }

    public async Task<AssistantContext> BuildContextAsync(string workOrderId, List<ActivityLogEntry> activities)
    {
        _logger.LogInformation("Building assistant context for work order {WorkOrderId}", workOrderId);
        await Task.Delay(0);

        var context = new AssistantContext
        {
            WorkOrderId = workOrderId,
            GeneratedAt = DateTime.UtcNow,
            RecentActivity = activities.OrderByDescending(a => a.Timestamp).Take(50).ToList()
        };

        return context;
    }

    public async Task<AssistantContext> BuildContextWithMatchesAsync(
        string workOrderId,
        List<ActivityLogEntry> activities,
        List<WorkflowMatch> matches)
    {
        var context = await BuildContextAsync(workOrderId, activities);
        context.PossibleWorkflows = matches;

        return context;
    }
}
