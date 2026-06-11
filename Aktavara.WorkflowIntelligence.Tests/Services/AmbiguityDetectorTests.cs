using Aktavara.WorkflowIntelligence.Core.Models;
using Aktavara.WorkflowIntelligence.Core.Services;
using Xunit;

namespace Aktavara.WorkflowIntelligence.Tests.Services;

public class AmbiguityDetectorTests
{
    private readonly AmbiguityDetector _detector = new();

    [Fact]
    public void Detect_WithNoMatches_ReturnsNoMatch()
    {
        var result = _detector.Detect(null, null);

        Assert.False(result.IsAmbiguous);
        Assert.Equal("NoMatch", result.RecommendedAction);
    }

    [Fact]
    public void Detect_WithOnlyActivityMatch_ReturnsUseActivity()
    {
        var activityMatch = new WorkflowMatchResult { WorkflowId = "wf1", ConfidenceScore = 0.8 };

        var result = _detector.Detect(activityMatch, null);

        Assert.False(result.IsAmbiguous);
        Assert.Equal("UseActivity", result.RecommendedAction);
        Assert.Equal("wf1", result.ActivityMatchId);
    }

    [Fact]
    public void Detect_WithOnlySemanticMatch_ReturnsUseSemantic()
    {
        var semanticMatch = new SemanticWorkflowMatch { WorkflowId = "wf1", Score = 0.7 };

        var result = _detector.Detect(null, semanticMatch);

        Assert.False(result.IsAmbiguous);
        Assert.Equal("UseSemantic", result.RecommendedAction);
        Assert.Equal("wf1", result.SemanticMatchId);
    }

    [Fact]
    public void Detect_WithSameWorkflowMatches_ReturnsUseActivity()
    {
        var activityMatch = new WorkflowMatchResult { WorkflowId = "wf1", ConfidenceScore = 0.8 };
        var semanticMatch = new SemanticWorkflowMatch { WorkflowId = "wf1", Score = 0.7 };

        var result = _detector.Detect(activityMatch, semanticMatch);

        Assert.False(result.IsAmbiguous);
        Assert.Equal("UseActivity", result.RecommendedAction);
    }

    [Fact]
    public void Detect_WithHighActivityLowSemantic_ReturnsUseActivity()
    {
        var activityMatch = new WorkflowMatchResult { WorkflowId = "wf1", ConfidenceScore = 0.85 };
        var semanticMatch = new SemanticWorkflowMatch { WorkflowId = "wf2", Score = 0.3 };

        var result = _detector.Detect(activityMatch, semanticMatch);

        Assert.False(result.IsAmbiguous);
        Assert.Equal("UseActivity", result.RecommendedAction);
    }

    [Fact]
    public void Detect_WithLowActivityHighSemantic_ReturnsUseSemantic()
    {
        var activityMatch = new WorkflowMatchResult { WorkflowId = "wf1", ConfidenceScore = 0.3 };
        var semanticMatch = new SemanticWorkflowMatch { WorkflowId = "wf2", Score = 0.75 };

        var result = _detector.Detect(activityMatch, semanticMatch);

        Assert.False(result.IsAmbiguous);
        Assert.Equal("UseSemantic", result.RecommendedAction);
    }

    [Fact]
    public void Detect_WithBothStrong_ReturnsAskClarification()
    {
        var activityMatch = new WorkflowMatchResult { WorkflowId = "wf1", ConfidenceScore = 0.8 };
        var semanticMatch = new SemanticWorkflowMatch { WorkflowId = "wf2", Score = 0.75 };

        var result = _detector.Detect(activityMatch, semanticMatch);

        Assert.True(result.IsAmbiguous);
        Assert.Equal("AskClarification", result.RecommendedAction);
        Assert.NotNull(result.ClarificationQuestion);
    }

    [Fact]
    public void Detect_WithBothWeak_ReturnsAskClarification()
    {
        var activityMatch = new WorkflowMatchResult { WorkflowId = "wf1", ConfidenceScore = 0.4 };
        var semanticMatch = new SemanticWorkflowMatch { WorkflowId = "wf2", Score = 0.4 };

        var result = _detector.Detect(activityMatch, semanticMatch);

        Assert.True(result.IsAmbiguous);
        Assert.Equal("AskClarification", result.RecommendedAction);
        Assert.NotNull(result.ClarificationQuestion);
    }

    [Fact]
    public void Detect_PopulatesConfidenceMetrics()
    {
        var activityMatch = new WorkflowMatchResult { WorkflowId = "wf1", ConfidenceScore = 0.75 };
        var semanticMatch = new SemanticWorkflowMatch { WorkflowId = "wf2", Score = 0.65 };

        var result = _detector.Detect(activityMatch, semanticMatch);

        Assert.Equal(0.75, result.ActivityConfidence);
        Assert.Equal(0.65, result.SemanticScore);
    }

    [Fact]
    public void Detect_ClarificationQuestionMentionsBothWorkflows()
    {
        var activityMatch = new WorkflowMatchResult { WorkflowId = "wf1", ConfidenceScore = 0.8 };
        var semanticMatch = new SemanticWorkflowMatch { WorkflowId = "wf2", WorkflowName = "Test Workflow", Score = 0.75 };

        var result = _detector.Detect(activityMatch, semanticMatch);

        Assert.NotNull(result.ClarificationQuestion);
        Assert.Contains("Test Workflow", result.ClarificationQuestion);
    }
}
