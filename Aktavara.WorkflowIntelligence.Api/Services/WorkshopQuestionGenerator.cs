namespace Aktavara.WorkflowIntelligence.Api.Services;

using Aktavara.WorkflowIntelligence.Core.Interfaces;
using System.Text.Json;

public class WorkshopQuestionGenerator : IWorkshopQuestionGenerator
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<WorkshopQuestionGenerator> _logger;
    private readonly HttpClient _httpClient;

    private static readonly List<string> FallbackQuestions = new()
    {
        "What is the business purpose of this step?",
        "Are there any edge cases or exceptions to be aware of?",
        "How often does this step occur in a typical workflow?"
    };

    public WorkshopQuestionGenerator(
        IConfiguration configuration,
        ILogger<WorkshopQuestionGenerator> logger,
        HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<List<string>> GenerateQuestionsAsync(
        string workflowName,
        string stateId,
        string stateName,
        string stateDescription,
        IReadOnlyList<string> matchedRules,
        IReadOnlyList<string> suggestedTags,
        string riskLevel,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = _configuration.GetValue<string>("Anthropic:ApiKey");
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("Anthropic API key not configured, using fallback questions");
                return FallbackQuestions;
            }

            var prompt = BuildPrompt(workflowName, stateName, stateDescription, matchedRules, suggestedTags, riskLevel);
            var questions = await CallAnthropicAsync(apiKey, prompt, cancellationToken);

            return questions?.Count > 0 ? questions : FallbackQuestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating workshop questions for {StateId}, using fallback", stateId);
            return FallbackQuestions;
        }
    }

    private string BuildPrompt(
        string workflowName,
        string stateName,
        string stateDescription,
        IReadOnlyList<string> matchedRules,
        IReadOnlyList<string> suggestedTags,
        string riskLevel)
    {
        var rulesText = matchedRules.Count > 0 ? string.Join(", ", matchedRules) : "None detected";
        var tagsText = suggestedTags.Count > 0 ? string.Join(", ", suggestedTags) : "None detected";

        var prompt = $@"Workflow: {workflowName}
Current step: {stateName}
Step description: {stateDescription}
Activity at this step: {rulesText}
Record types involved: {tagsText}
Risk level: {riskLevel}

Generate 3 workshop questions for this step.";

        return prompt;
    }

    private async Task<List<string>> CallAnthropicAsync(
        string apiKey,
        string prompt,
        CancellationToken cancellationToken)
    {
        var request = new
        {
            model = "claude-sonnet-4-6",
            max_tokens = 300,
            messages = new object[]
            {
                new
                {
                    role = "user",
                    content = prompt
                }
            },
            system = @"You are helping build a workflow guidance system for Aktavara, a network management platform. Generate exactly 3 workshop questions that a consultant would ask a customer to understand how they perform this workflow step. Questions should:
- Be specific to the workflow step, not generic
- Help capture edge cases and variations
- Use plain business language, not technical jargon
- Be answerable in a workshop session
Respond only with a JSON array of 3 strings: [""question1"", ""question2"", ""question3""]"
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
        {
            Content = JsonContent.Create(request)
        };

        httpRequest.Headers.Add("x-api-key", apiKey);
        httpRequest.Headers.Add("anthropic-version", "2023-06-01");

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseContent);
        var root = doc.RootElement;

        if (!root.TryGetProperty("content", out var contentArray) || contentArray.GetArrayLength() == 0)
            return new();

        var firstContent = contentArray[0];
        if (!firstContent.TryGetProperty("text", out var textElement))
            return new();

        var text = textElement.GetString() ?? "";

        // Parse JSON array from response
        var jsonStart = text.IndexOf('[');
        var jsonEnd = text.LastIndexOf(']');
        if (jsonStart < 0 || jsonEnd < 0)
            return new();

        var jsonStr = text[jsonStart..(jsonEnd + 1)];
        using var jsonDoc = JsonDocument.Parse(jsonStr);
        var questionsArray = jsonDoc.RootElement;

        var questions = new List<string>();
        if (questionsArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var question in questionsArray.EnumerateArray())
            {
                if (question.ValueKind == JsonValueKind.String)
                {
                    var q = question.GetString();
                    if (!string.IsNullOrEmpty(q))
                    {
                        questions.Add(q);
                    }
                }
            }
        }

        return questions.Count == 3 ? questions : new();
    }
}
