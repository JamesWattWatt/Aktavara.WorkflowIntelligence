namespace Aktavara.WorkflowIntelligence.Api.Services;

using Aktavara.WorkflowIntelligence.Core.Interfaces;
using Aktavara.WorkflowIntelligence.Core.Models.Api;
using System.Net.Http.Json;
using System.Text.Json;
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

    public async Task StreamAsync(
        List<ChatMessage> messages,
        string systemPrompt,
        Func<string, Task> onDelta,
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

            var anthropicMessages = messages
                .Where(m => m.Role != "system")
                .Select(m => new { role = m.Role, content = m.Content })
                .ToList();

            var request = new
            {
                model,
                max_tokens = maxTokens,
                system = systemPrompt,
                stream = true,
                messages = anthropicMessages
            };

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
            {
                Content = JsonContent.Create(request),
                Headers = { { "anthropic-version", "2023-06-01" } }
            };
            requestMessage.Headers.Add("x-api-key", apiKey);

            var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            using (var stream = await response.Content.ReadAsStreamAsync(cancellationToken))
            using (var reader = new StreamReader(stream))
            {
                string? line;
                while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    if (!line.StartsWith("data: "))
                        continue;

                    var jsonStr = line.Substring("data: ".Length);
                    if (jsonStr == "[DONE]")
                        break;

                    try
                    {
                        var streamEvent = JsonSerializer.Deserialize<StreamEvent>(jsonStr);
                        if (streamEvent?.Type == "content_block_delta")
                        {
                            var delta = streamEvent.Delta as JsonElement?;
                            if (delta?.GetProperty("type").GetString() == "text_delta")
                            {
                                var text = delta?.GetProperty("text").GetString() ?? "";
                                await onDelta(text);
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogDebug(ex, "Failed to parse SSE event");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming from Anthropic API");
            throw;
        }
    }

    private class AnthropicResponse
    {
        [JsonPropertyName("content")]
        public List<ContentBlock>? Content { get; set; }

        [JsonPropertyName("usage")]
        public UsageInfo? Usage { get; set; }
    }

    private class UsageInfo
    {
        [JsonPropertyName("input_tokens")]
        public int InputTokens { get; set; }

        [JsonPropertyName("output_tokens")]
        public int OutputTokens { get; set; }
    }

    private class ContentBlock
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    public async Task<(string Response, LlmDebugInfo DebugInfo)> CompleteWithDebugAsync(
        List<ChatMessage> messages,
        string systemPrompt,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
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

            var elapsed = DateTime.UtcNow - startTime;
            var debugInfo = new LlmDebugInfo
            {
                SystemPrompt = systemPrompt,
                InputTokens = result?.Usage?.InputTokens ?? 0,
                OutputTokens = result?.Usage?.OutputTokens ?? 0,
                ResponseTimeMs = (long)elapsed.TotalMilliseconds
            };

            return (textContent.Text ?? "", debugInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Anthropic API");
            throw;
        }
    }

    public async Task<LlmDebugInfo> StreamWithDebugAsync(
        List<ChatMessage> messages,
        string systemPrompt,
        Func<string, Task> onDelta,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var debugInfo = new LlmDebugInfo
        {
            SystemPrompt = systemPrompt,
            InputTokens = 0,
            OutputTokens = 0,
            ResponseTimeMs = 0
        };

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

            var anthropicMessages = messages
                .Where(m => m.Role != "system")
                .Select(m => new { role = m.Role, content = m.Content })
                .ToList();

            var request = new
            {
                model,
                max_tokens = maxTokens,
                system = systemPrompt,
                stream = true,
                messages = anthropicMessages
            };

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
            {
                Content = JsonContent.Create(request),
                Headers = { { "anthropic-version", "2023-06-01" } }
            };
            requestMessage.Headers.Add("x-api-key", apiKey);

            var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            using (var stream = await response.Content.ReadAsStreamAsync(cancellationToken))
            using (var reader = new StreamReader(stream))
            {
                string? line;
                while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    if (!line.StartsWith("data: "))
                        continue;

                    var jsonStr = line.Substring("data: ".Length);
                    if (jsonStr == "[DONE]")
                        break;

                    try
                    {
                        var streamEvent = JsonSerializer.Deserialize<StreamEvent>(jsonStr);
                        if (streamEvent?.Type == "content_block_delta")
                        {
                            var delta = streamEvent.Delta as JsonElement?;
                            if (delta?.GetProperty("type").GetString() == "text_delta")
                            {
                                var text = delta?.GetProperty("text").GetString() ?? "";
                                await onDelta(text);
                            }
                        }
                        else if (streamEvent?.Type == "message_delta")
                        {
                            var delta = streamEvent.Delta as JsonElement?;
                            if (delta?.TryGetProperty("usage", out var usage) == true)
                            {
                                if (usage.TryGetProperty("output_tokens", out var outputTokens))
                                {
                                    debugInfo.OutputTokens = outputTokens.GetInt32();
                                }
                            }
                        }
                        else if (streamEvent?.Type == "message_start")
                        {
                            var messageData = streamEvent.Message as JsonElement?;
                            if (messageData?.TryGetProperty("usage", out var usage) == true)
                            {
                                if (usage.TryGetProperty("input_tokens", out var inputTokens))
                                {
                                    debugInfo.InputTokens = inputTokens.GetInt32();
                                }
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogDebug(ex, "Failed to parse SSE event");
                    }
                }
            }

            var elapsed = DateTime.UtcNow - startTime;
            debugInfo.ResponseTimeMs = (long)elapsed.TotalMilliseconds;
            return debugInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming from Anthropic API");
            throw;
        }
    }

    private class StreamEvent
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("delta")]
        public JsonElement? Delta { get; set; }

        [JsonPropertyName("message")]
        public JsonElement? Message { get; set; }
    }
}
