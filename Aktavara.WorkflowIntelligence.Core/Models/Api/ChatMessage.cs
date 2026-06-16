namespace Aktavara.WorkflowIntelligence.Core.Models.Api;

/// <summary>
/// A single message in a chat conversation.
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// Role: "user", "assistant", or "system"
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Message content text
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// When the message was created
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Which MCP tools (if any) were used to generate this message
    /// </summary>
    public List<string> ToolsUsed { get; set; } = new();
}
