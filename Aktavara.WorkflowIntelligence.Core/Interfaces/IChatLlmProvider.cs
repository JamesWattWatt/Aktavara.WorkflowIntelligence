namespace Aktavara.WorkflowIntelligence.Core.Interfaces;

using Aktavara.WorkflowIntelligence.Core.Models.Api;

/// <summary>
/// Debug information from LLM provider
/// </summary>
public class LlmDebugInfo
{
    public string SystemPrompt { get; set; } = string.Empty;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public long ResponseTimeMs { get; set; }
}

/// <summary>
/// Strategy interface for LLM chat completion providers.
/// </summary>
public interface IChatLlmProvider
{
    /// <summary>
    /// Get the name of this provider (e.g., "Anthropic", "OpenAI", "Mock")
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Complete a chat conversation.
    /// </summary>
    /// <param name="messages">Full message history</param>
    /// <param name="systemPrompt">System prompt with context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Assistant's reply text</returns>
    Task<string> CompleteAsync(
        List<ChatMessage> messages,
        string systemPrompt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Complete with debug info capture.
    /// </summary>
    /// <param name="messages">Full message history</param>
    /// <param name="systemPrompt">System prompt with context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple of response text and debug info</returns>
    Task<(string Response, LlmDebugInfo DebugInfo)> CompleteWithDebugAsync(
        List<ChatMessage> messages,
        string systemPrompt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stream a chat response token by token.
    /// </summary>
    /// <param name="messages">Full message history</param>
    /// <param name="systemPrompt">System prompt with context</param>
    /// <param name="onDelta">Callback for each token/chunk received</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task StreamAsync(
        List<ChatMessage> messages,
        string systemPrompt,
        Func<string, Task> onDelta,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stream with debug info capture.
    /// </summary>
    /// <param name="messages">Full message history</param>
    /// <param name="systemPrompt">System prompt with context</param>
    /// <param name="onDelta">Callback for each token/chunk received</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Debug info including token counts</returns>
    Task<LlmDebugInfo> StreamWithDebugAsync(
        List<ChatMessage> messages,
        string systemPrompt,
        Func<string, Task> onDelta,
        CancellationToken cancellationToken = default);
}
