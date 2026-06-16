namespace Aktavara.WorkflowIntelligence.Core.Models.Api;

/// <summary>
/// A chat session containing conversation history and context.
/// </summary>
public class ChatSession
{
    /// <summary>
    /// Unique session identifier (GUID)
    /// </summary>
    public string SessionId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// When the session was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last activity timestamp (for expiration tracking)
    /// </summary>
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// All messages in this conversation
    /// </summary>
    public List<ChatMessage> Messages { get; set; } = new();

    /// <summary>
    /// Name of the log file that was analyzed (if any)
    /// </summary>
    public string? LogFileName { get; set; }

    /// <summary>
    /// The analysis context from the dropped log (if any)
    /// </summary>
    public AnalyzeResponse? AnalyzeResponse { get; set; }
}
