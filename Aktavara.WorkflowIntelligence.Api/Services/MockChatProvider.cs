namespace Aktavara.WorkflowIntelligence.Api.Services;

using Aktavara.WorkflowIntelligence.Core.Interfaces;
using Aktavara.WorkflowIntelligence.Core.Models.Api;

/// <summary>
/// Mock chat LLM provider for testing without API keys.
/// </summary>
public class MockChatProvider : IChatLlmProvider
{
    private readonly ILogger<MockChatProvider> _logger;

    public string ProviderName => "Mock";

    public MockChatProvider(ILogger<MockChatProvider> logger)
    {
        _logger = logger;
    }

    public Task<string> CompleteAsync(
        List<ChatMessage> messages,
        string systemPrompt,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock provider responding to chat");

        var lastUserMessage = messages
            .Where(m => m.Role == "user")
            .LastOrDefault()?.Content ?? "Hello";

        // Return a canned response based on the user message
        var response = lastUserMessage.ToLower() switch
        {
            var s when s.Contains("next") => "Based on the workflow context, your next step should be to save the configuration and verify the changes.",
            var s when s.Contains("help") => "I can help you with workflow guidance. Ask me about the current step, what to do next, or how to troubleshoot issues.",
            var s when s.Contains("error") => "Common issues with this workflow include incomplete data and network timeouts. Try verifying your input and retrying.",
            _ => "I'm here to help with your Aktavara workflow. What would you like to know?"
        };

        return Task.FromResult(response);
    }
}
