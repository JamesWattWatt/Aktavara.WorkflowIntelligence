using Aktavara.WorkflowIntelligence.Core.Interfaces;
using Aktavara.WorkflowIntelligence.Core.Models;

namespace Aktavara.WorkflowIntelligence.Core.Services;

/// <summary>
/// Keyword-based semantic search for workflows.
/// Scores workflows based on keyword matches in name, description, tags, and guide titles.
/// </summary>
public class KeywordSemanticWorkflowSearch : ISemanticWorkflowSearch
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "i", "am", "trying", "to", "a", "the", "is", "in",
        "and", "or", "my", "for", "of", "with", "this", "that",
        "want", "need", "how", "do", "can", "please", "help"
    };

    private readonly IWorkflowLibrary _workflowLibrary;
    private readonly IHelpGuideStore _helpGuideStore;

    public bool IsAvailable => false;

    public KeywordSemanticWorkflowSearch(
        IWorkflowLibrary workflowLibrary,
        IHelpGuideStore helpGuideStore)
    {
        _workflowLibrary = workflowLibrary;
        _helpGuideStore = helpGuideStore;
    }

    public Task<IReadOnlyList<SemanticWorkflowMatch>> SearchAsync(
        string userText,
        int topK,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userText))
            return Task.FromResult<IReadOnlyList<SemanticWorkflowMatch>>(new List<SemanticWorkflowMatch>());

        var keywords = ExtractKeywords(userText);
        var userTextLower = userText.ToLowerInvariant();

        if (keywords.Count == 0)
            return Task.FromResult<IReadOnlyList<SemanticWorkflowMatch>>(new List<SemanticWorkflowMatch>());

        var workflows = _workflowLibrary.GetAll();
        var matches = new List<SemanticWorkflowMatch>();

        foreach (var workflow in workflows)
        {
            var match = ScoreWorkflow(workflow, keywords, userTextLower);
            if (match.Score > 0.1)
            {
                matches.Add(match);
            }
        }

        var results = matches
            .OrderByDescending(m => m.Score)
            .Take(topK)
            .ToList();

        return Task.FromResult<IReadOnlyList<SemanticWorkflowMatch>>(results);
    }

    private List<string> ExtractKeywords(string userText)
    {
        return userText
            .Split(new[] { ' ', ',', '.', ':', ';', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => !StopWords.Contains(w) && w.Length > 1)
            .Select(w => w.ToLowerInvariant())
            .Distinct()
            .ToList();
    }

    private SemanticWorkflowMatch ScoreWorkflow(WorkflowDefinition workflow, List<string> keywords, string userTextLower)
    {
        var match = new SemanticWorkflowMatch
        {
            WorkflowId = workflow.WorkflowId,
            WorkflowName = workflow.Name
        };

        double score = 0.0;

        // Name matching
        var nameScore = ScoreName(workflow.Name, keywords, userTextLower, match);
        score += nameScore;

        // Description matching
        var descriptionScore = ScoreDescription(workflow.Description ?? string.Empty, keywords, match);
        score += descriptionScore;

        // Tag matching
        var tagScore = ScoreTags(workflow.Tags, keywords, match);
        score += tagScore;

        // Help guide title matching
        var guideScore = ScoreGuides(keywords, match);
        score += guideScore;

        match.Score = Math.Min(1.0, score);
        match.Reason = GenerateReason(match);

        return match;
    }

    private double ScoreName(string name, List<string> keywords, string userTextLower, SemanticWorkflowMatch match)
    {
        var nameLower = name.ToLowerInvariant();
        var score = 0.0;

        // Check for exact phrase match
        if (nameLower.Contains(userTextLower.Replace(" ", "-")))
        {
            score = 0.5;
            foreach (var keyword in keywords)
            {
                if (!match.MatchedTerms.Contains(keyword))
                    match.MatchedTerms.Add(keyword);
            }
        }
        else
        {
            // Check for all keywords individually
            var matchedKeywords = 0;
            foreach (var keyword in keywords)
            {
                if (nameLower.Contains(keyword))
                {
                    if (!match.MatchedTerms.Contains(keyword))
                        match.MatchedTerms.Add(keyword);
                    matchedKeywords++;
                }
            }

            if (matchedKeywords == keywords.Count)
            {
                score = 0.4;
            }
            else
            {
                score = Math.Min(0.3, matchedKeywords * 0.1);
            }
        }

        if (score > 0)
        {
            if (!match.MatchedFields.Contains("name"))
                match.MatchedFields.Add("name");
        }

        return score;
    }

    private double ScoreDescription(string description, List<string> keywords, SemanticWorkflowMatch match)
    {
        var descLower = description.ToLowerInvariant();
        var matchedInDesc = 0;

        foreach (var keyword in keywords)
        {
            if (descLower.Contains(keyword) && !match.MatchedTerms.Contains(keyword))
            {
                match.MatchedTerms.Add(keyword);
                matchedInDesc++;
            }
        }

        var score = Math.Min(0.2, matchedInDesc * 0.05);

        if (score > 0)
        {
            if (!match.MatchedFields.Contains("description"))
                match.MatchedFields.Add("description");
        }

        return score;
    }

    private double ScoreTags(List<string> tags, List<string> keywords, SemanticWorkflowMatch match)
    {
        var score = 0.0;

        foreach (var tag in tags)
        {
            var tagLower = tag.ToLowerInvariant();
            foreach (var keyword in keywords)
            {
                if (tagLower == keyword)
                {
                    if (!match.MatchedTerms.Contains(keyword))
                        match.MatchedTerms.Add(keyword);
                    score += 0.15;
                }
            }
        }

        score = Math.Min(0.3, score);

        if (score > 0)
        {
            if (!match.MatchedFields.Contains("tags"))
                match.MatchedFields.Add("tags");
        }

        return score;
    }

    private double ScoreGuides(List<string> keywords, SemanticWorkflowMatch match)
    {
        var guides = _helpGuideStore.GetAll();
        var score = 0.0;
        var matchedInGuides = 0;

        foreach (var guide in guides)
        {
            var guideTitleLower = guide.Title.ToLowerInvariant();
            foreach (var keyword in keywords)
            {
                if (guideTitleLower.Contains(keyword) && !match.MatchedTerms.Contains(keyword))
                {
                    match.MatchedTerms.Add(keyword);
                    matchedInGuides++;
                }
            }
        }

        score = Math.Min(0.2, matchedInGuides * 0.05);

        if (score > 0)
        {
            if (!match.MatchedFields.Contains("guide"))
                match.MatchedFields.Add("guide");
        }

        return score;
    }

    private string GenerateReason(SemanticWorkflowMatch match)
    {
        if (match.MatchedTerms.Count == 0)
            return "No keyword matches found";

        var fieldsSummary = string.Join(", ", match.MatchedFields);
        var termsSummary = string.Join("', '", match.MatchedTerms.Take(3));

        return $"Matched '{termsSummary}' in {fieldsSummary}";
    }
}
