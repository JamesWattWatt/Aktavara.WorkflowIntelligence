using Aktavara.WorkflowIntelligence.Core.Interfaces;
using Aktavara.WorkflowIntelligence.Core.Models;

namespace Aktavara.WorkflowIntelligence.Core.Services;

public class StaticWorkflowProvider : IWorkflowProvider
{
    private readonly List<ParsedWorkflow> _workflows;

    public StaticWorkflowProvider()
    {
        // TODO: Replace with database or external data source
        _workflows = new List<ParsedWorkflow>
        {
            new()
            {
                Id = "workflow-1",
                Name = "Standard Installation",
                Description = "Standard workflow for equipment installation"
            },
            new()
            {
                Id = "workflow-2",
                Name = "Emergency Repair",
                Description = "Emergency repair workflow"
            }
        };
    }

    public Task<List<ParsedWorkflow>> GetAllWorkflowsAsync()
    {
        return Task.FromResult(_workflows);
    }

    public Task<ParsedWorkflow?> GetWorkflowByIdAsync(string id)
    {
        return Task.FromResult(_workflows.FirstOrDefault(w => w.Id == id));
    }
}
