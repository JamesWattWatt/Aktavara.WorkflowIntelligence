namespace Aktavara.WorkflowIntelligence.Core.Models.Api;

/// <summary>
/// Response from the chat endpoint.
/// </summary>
public class ChatResponse
{
    /// <summary>
    /// Session ID for future messages
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// The assistant's reply
    /// </summary>
    public string Reply { get; set; } = string.Empty;

    /// <summary>
    /// Which MCP tools (if any) were used
    /// </summary>
    public List<string> ToolsUsed { get; set; } = new();

    /// <summary>
    /// The detected workflow context (if any)
    /// </summary>
    public WorkflowCandidateResult? WorkflowContext { get; set; }

    /// <summary>
    /// Guide sections or resources referenced
    /// </summary>
    public List<string> Sources { get; set; } = new();

    /// <summary>
    /// System prompt sent to LLM
    /// </summary>
    public string? SystemPrompt { get; set; }

    /// <summary>
    /// Input tokens used by the LLM
    /// </summary>
    public int InputTokens { get; set; }

    /// <summary>
    /// Output tokens used by the LLM
    /// </summary>
    public int OutputTokens { get; set; }

    /// <summary>
    /// Response time in milliseconds
    /// </summary>
    public long ResponseTimeMs { get; set; }

    /// <summary>
    /// Which help guide sections were included
    /// </summary>
    public List<string> GuideReferences { get; set; } = new();

    /// <summary>
    /// The detected workflow from analysis
    /// </summary>
    public WorkflowCandidateResult? DetectedWorkflow { get; set; }
}
