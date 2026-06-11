using Aktavara.WorkflowIntelligence.Core.Models;

namespace Aktavara.WorkflowIntelligence.Core.Interfaces;

/// <summary>
/// Generates an AssistantContextPacket from activity and workflow matching results.
/// The packet contains everything needed to generate guidance via an LLM.
/// </summary>
public interface IAssistantContextPacketGenerator
{
    /// <summary>
    /// Generates a context packet from activity context and workflow matching results.
    /// Optionally performs semantic search and ambiguity detection if userText is provided.
    /// </summary>
    /// <param name="activityContext">The user's activity context.</param>
    /// <param name="allMatches">All workflow matches, ranked by confidence.</param>
    /// <param name="workflowLibrary">The workflow library for looking up definitions.</param>
    /// <param name="userText">Optional user text for semantic search and ambiguity detection.</param>
    /// <returns>A complete AssistantContextPacket ready for sending to an LLM.</returns>
    AssistantContextPacket GeneratePacket(
        ActivityContext activityContext,
        IReadOnlyList<WorkflowMatchResult> allMatches,
        IWorkflowLibrary workflowLibrary,
        string? userText = null);
}
