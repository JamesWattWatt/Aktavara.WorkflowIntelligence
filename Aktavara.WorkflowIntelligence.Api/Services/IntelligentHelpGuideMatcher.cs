namespace Aktavara.WorkflowIntelligence.Api.Services;

using Aktavara.WorkflowIntelligence.Core.Interfaces;
using Aktavara.WorkflowIntelligence.Core.Models;
using System.Text.Json;

public class IntelligentHelpGuideMatcher : IIntelligentHelpGuideMatcher
{
    private readonly IHelpGuideStore _helpGuideStore;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IntelligentHelpGuideMatcher> _logger;
    private readonly HttpClient _httpClient;

    public IntelligentHelpGuideMatcher(
        IHelpGuideStore helpGuideStore,
        IConfiguration configuration,
        ILogger<IntelligentHelpGuideMatcher> logger,
        HttpClient httpClient)
    {
        _helpGuideStore = helpGuideStore;
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<GuideSuggestion?> SuggestAsync(
        string workflowId,
        string workflowName,
        string stepId,
        string currentStateName,
        IReadOnlyList<string> matchedRules,
        IReadOnlyList<string> matchedEvidence,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = _configuration.GetValue<string>("Anthropic:ApiKey");
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("Anthropic API key not configured");
                return null;
            }

            // Build available sections list
            var availableSections = BuildAvailableSectionsList();
            if (availableSections.Count == 0)
            {
                _logger.LogWarning("No help guide sections available");
                return null;
            }

            // Build LLM prompt
            var prompt = BuildPrompt(workflowName, currentStateName, matchedRules, matchedEvidence, availableSections);

            // Call Anthropic API
            var suggestion = await CallAnthropicAsync(apiKey, prompt, cancellationToken);

            if (suggestion != null)
            {
                suggestion.WorkflowId = workflowId;
                suggestion.StepId = stepId;
                _logger.LogInformation("Guide suggestion generated for {WorkflowId}:{StepId}", workflowId, stepId);
            }

            return suggestion;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suggesting help guide for {WorkflowId}:{StepId}", workflowId, stepId);
            return null;
        }
    }

    private List<string> BuildAvailableSectionsList()
    {
        var sections = new List<string>();

        foreach (var guide in _helpGuideStore.GetAll())
        {
            foreach (var section in guide.Sections)
            {
                sections.Add($"{guide.FileName} | {section.SectionId} | {section.Heading}");
            }
        }

        // Limit to 100 sections to keep prompt reasonable
        return sections.Take(100).ToList();
    }

    private string BuildPrompt(
        string workflowName,
        string currentStateName,
        IReadOnlyList<string> matchedRules,
        IReadOnlyList<string> matchedEvidence,
        List<string> availableSections)
    {
        var evidenceText = string.Join(" | ", matchedEvidence.Take(5));
        var rulesText = string.Join(" | ", matchedRules);
        var sectionsText = string.Join("\n", availableSections);

        var prompt = $@"Workflow: {workflowName}
Current step/state: {currentStateName}
Matched rules: {rulesText}
Evidence: {evidenceText}

Available documentation sections:
{sectionsText}

Which section best explains what the user should do at this step?
Respond with JSON: {{""guideFile"": ""..."", ""sectionId"": null or ""..."", ""sectionHeading"": ""..."", ""reason"": ""...""}}";

        return prompt;
    }

    private async Task<GuideSuggestion?> CallAnthropicAsync(
        string apiKey,
        string prompt,
        CancellationToken cancellationToken)
    {
        var request = new
        {
            model = "claude-sonnet-4-6",
            max_tokens = 500,
            messages = new object[]
            {
                new
                {
                    role = "user",
                    content = prompt
                }
            },
            system = "You are helping map Aktavara workflow steps to documentation sections. Given a workflow step and its observed evidence, identify the single most relevant documentation section from the provided list. Respond only with valid JSON."
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
            return null;

        var firstContent = contentArray[0];
        if (!firstContent.TryGetProperty("text", out var textElement))
            return null;

        var text = textElement.GetString() ?? "";

        // Parse JSON from response
        var jsonStart = text.IndexOf('{');
        var jsonEnd = text.LastIndexOf('}');
        if (jsonStart < 0 || jsonEnd < 0)
            return null;

        var jsonStr = text[jsonStart..(jsonEnd + 1)];
        using var jsonDoc = JsonDocument.Parse(jsonStr);
        var suggestionData = jsonDoc.RootElement;

        var suggestion = new GuideSuggestion
        {
            SuggestedGuideFile = suggestionData.TryGetProperty("guideFile", out var gf) ? gf.GetString() ?? "" : "",
            SuggestedSectionId = suggestionData.TryGetProperty("sectionId", out var si) && si.ValueKind != JsonValueKind.Null ? si.GetString() : null,
            SuggestedSectionHeading = suggestionData.TryGetProperty("sectionHeading", out var sh) ? sh.GetString() ?? "" : "",
            ConfidenceReason = suggestionData.TryGetProperty("reason", out var r) ? r.GetString() ?? "" : ""
        };

        return !string.IsNullOrEmpty(suggestion.SuggestedGuideFile) ? suggestion : null;
    }
}
