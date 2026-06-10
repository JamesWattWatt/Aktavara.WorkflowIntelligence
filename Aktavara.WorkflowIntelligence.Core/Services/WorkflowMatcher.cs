using Aktavara.WorkflowIntelligence.Core.Interfaces;
using Aktavara.WorkflowIntelligence.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aktavara.WorkflowIntelligence.Core.Services;

public class WorkflowMatcher : IWorkflowMatcher
{
    private readonly IWorkflowProvider _workflowProvider;
    private readonly ILogger<WorkflowMatcher> _logger;

    public WorkflowMatcher(IWorkflowProvider workflowProvider, ILogger<WorkflowMatcher> logger)
    {
        _workflowProvider = workflowProvider;
        _logger = logger;
    }

    public async Task<List<WorkflowMatch>> MatchWorkflowsAsync(List<ActivityLogEntry> activities)
    {
        _logger.LogInformation("Matching workflows for {Count} activities", activities.Count);

        var workflows = await _workflowProvider.GetAllWorkflowsAsync();
        var matches = new List<WorkflowMatch>();

        foreach (var workflow in workflows)
        {
            var score = ScoreWorkflow(workflow, activities);
            matches.Add(new WorkflowMatch
            {
                Workflow = workflow,
                ConfidenceScore = score
            });
        }

        return matches.OrderByDescending(m => m.ConfidenceScore).ToList();
    }

    public async Task<WorkflowMatch?> FindBestMatchAsync(List<ActivityLogEntry> activities)
    {
        var matches = await MatchWorkflowsAsync(activities);
        return matches.FirstOrDefault();
    }

    private double ScoreWorkflow(ParsedWorkflow workflow, List<ActivityLogEntry> activities)
    {
        // TODO: Implement confidence scoring logic
        return 0.0;
    }
}
