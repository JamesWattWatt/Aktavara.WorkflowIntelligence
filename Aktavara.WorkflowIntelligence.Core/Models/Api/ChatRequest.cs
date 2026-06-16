namespace Aktavara.WorkflowIntelligence.Core.Models.Api;

/// <summary>
/// Request to send a chat message.
/// </summary>
public class ChatRequest
{
    /// <summary>
    /// Session ID (null = create new session)
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// The user's message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional log content to analyze (re-analyzes if provided)
    /// </summary>
    public string? LogContent { get; set; }

    /// <summary>
    /// Optional user name for context
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Optional user question for semantic search
    /// </summary>
    public string? UserQuestion { get; set; }
}
