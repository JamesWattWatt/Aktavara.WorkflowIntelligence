namespace Aktavara.WorkflowIntelligence.Core.Interfaces;

using Aktavara.WorkflowIntelligence.Core.Models.Api;

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
}
