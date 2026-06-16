namespace Aktavara.WorkflowIntelligence.Core.Interfaces;

public interface IWorkshopQuestionGenerator
{
    Task<List<string>> GenerateQuestionsAsync(
        string workflowName,
        string stateId,
        string stateName,
        string stateDescription,
        IReadOnlyList<string> matchedRules,
        IReadOnlyList<string> suggestedTags,
        string riskLevel,
        CancellationToken cancellationToken = default);
}
