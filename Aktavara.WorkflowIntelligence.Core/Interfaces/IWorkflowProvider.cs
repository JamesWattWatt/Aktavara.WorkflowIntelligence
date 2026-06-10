using Aktavara.WorkflowIntelligence.Core.Models;

namespace Aktavara.WorkflowIntelligence.Core.Interfaces;

public interface IWorkflowProvider
{
    Task<List<ParsedWorkflow>> GetAllWorkflowsAsync();
    Task<ParsedWorkflow?> GetWorkflowByIdAsync(string id);
}
