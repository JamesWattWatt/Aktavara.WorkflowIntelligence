namespace Aktavara.WorkflowIntelligence.Api.Services;

using Aktavara.WorkflowIntelligence.Core.Interfaces;
using Aktavara.WorkflowIntelligence.Core.Models.Api;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Chat LLM provider using Anthropic API.
/// </summary>
public class AnthropicChatProvider : IChatLlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<AnthropicChatProvider> _logger;

    public string ProviderName => "Anthropic";

    public AnthropicChatProvider(
        HttpClient httpClient,
        IConfiguration config,
        ILogger<AnthropicChatProvider> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public async Task<string> CompleteAsync(
        List<ChatMessage> messages,
        string systemPrompt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = _config["Anthropic:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("Anthropic API key not configured");
                throw new InvalidOperationException("Anthropic API key not configured");
            }

            var model = _config.GetValue<string>("ChatLlm:Model") ?? "claude-sonnet-4-6";
            var maxTokens = _config.GetValue<int?>("ChatLlm:MaxTokens") ?? 1000;

            // Convert ChatMessage to Anthropic format
            var anthropicMessages = messages
                .Where(m => m.Role != "system")
                .Select(m => new { role = m.Role, content = m.Content })
                .ToList();

            var request = new
            {
                model,
                max_tokens = maxTokens,
                system = systemPrompt,
                messages = anthropicMessages
            };

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
            {
                Content = JsonContent.Create(request),
                Headers = { { "anthropic-version", "2023-06-01" } }
            };
            requestMessage.Headers.Add("x-api-key", apiKey);

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = System.Text.Json.JsonSerializer.Deserialize<AnthropicResponse>(json);
            var textContent = result?.Content?.FirstOrDefault(c => c.Type == "text");

            if (textContent == null)
            {
                _logger.LogError("No text content in Anthropic response");
                throw new InvalidOperationException("No text content in Anthropic response");
            }

            return textContent.Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Anthropic API");
            throw;
        }
    }

    private class AnthropicResponse
    {
        [JsonPropertyName("content")]
        public List<ContentBlock>? Content { get; set; }
    }

    private class ContentBlock
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
