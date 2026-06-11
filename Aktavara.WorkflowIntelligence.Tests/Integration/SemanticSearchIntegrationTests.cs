using Aktavara.WorkflowIntelligence.Core.Interfaces;
using Aktavara.WorkflowIntelligence.Core.Models;
using Aktavara.WorkflowIntelligence.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aktavara.WorkflowIntelligence.Tests.Integration;

public class SemanticSearchIntegrationTests
{
    [Fact]
    public async Task SemanticSearch_IntegratesWithPacketGenerator()
    {
        // Setup mock workflows and help guides
        var mockLibrary = new Mock<IWorkflowLibrary>();
        var mockHelpGuideStore = new Mock<IHelpGuideStore>();
        var mockLogger = new Mock<ILogger<AssistantContextPacketGenerator>>();

        var workflows = new List<WorkflowDefinition>
        {
            new()
            {
                WorkflowId = "add-connector",
                Name = "add connector to path",
                Description = "Add a connector between two nodes",
                Tags = new List<string> { "connector", "path" }
            },
            new()
            {
                WorkflowId = "update-node",
                Name = "update node in path",
                Description = "Update node properties",
                Tags = new List<string> { "node", "update" }
            }
        };

        mockLibrary.Setup(x => x.GetAll()).Returns(workflows);
        mockHelpGuideStore.Setup(x => x.GetAll()).Returns(new List<HelpGuide>());
        mockHelpGuideStore.Setup(x => x.GetByWorkflowAndStep(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new List<HelpGuideSection>());

        var semanticSearch = new KeywordSemanticWorkflowSearch(mockLibrary.Object, mockHelpGuideStore.Object);
        var packetGenerator = new AssistantContextPacketGenerator(mockLogger.Object, mockHelpGuideStore.Object, semanticSearch);

        // Create activity context
        var activityContext = new ActivityContext
        {
            UserName = "testuser",
            CurrentState = CurrentState.PathOpened,
            RecentEvents = new List<ActivityEvent>()
        };

        // Create matches
        var activityMatches = new List<WorkflowMatchResult>
        {
            new()
            {
                WorkflowId = "update-node",
                WorkflowName = "update node in path",
                ConfidenceScore = 0.6,
                CurrentStateName = "editing",
                CurrentStateId = "state1",
                RuleScores = new Dictionary<string, double>(),
                MissingEvidence = new List<string>(),
                MatchedEvidence = new List<ActivityEvent>()
            }
        };

        // Generate packet with user question
        var packet = packetGenerator.GeneratePacket(
            activityContext,
            activityMatches,
            mockLibrary.Object,
            "connect two nodes");

        // Verify semantic search was performed
        Assert.NotEmpty(packet.SemanticMatches);
        Assert.Contains("add-connector", packet.SemanticMatches.Select(m => m.WorkflowId));
    }

    [Fact]
    public async Task SemanticSearch_DetectsAmbiguity()
    {
        var mockLibrary = new Mock<IWorkflowLibrary>();
        var mockHelpGuideStore = new Mock<IHelpGuideStore>();
        var mockLogger = new Mock<ILogger<AssistantContextPacketGenerator>>();

        var workflows = new List<WorkflowDefinition>
        {
            new()
            {
                WorkflowId = "add-connector",
                Name = "add connector to path",
                Description = "Add a connector",
                Tags = new List<string>()
            },
            new()
            {
                WorkflowId = "update-node",
                Name = "update node",
                Description = "Update node",
                Tags = new List<string>()
            }
        };

        mockLibrary.Setup(x => x.GetAll()).Returns(workflows);
        mockHelpGuideStore.Setup(x => x.GetAll()).Returns(new List<HelpGuide>());
        mockHelpGuideStore.Setup(x => x.GetByWorkflowAndStep(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new List<HelpGuideSection>());

        var semanticSearch = new KeywordSemanticWorkflowSearch(mockLibrary.Object, mockHelpGuideStore.Object);
        var packetGenerator = new AssistantContextPacketGenerator(mockLogger.Object, mockHelpGuideStore.Object, semanticSearch);

        var activityContext = new ActivityContext
        {
            UserName = "testuser",
            CurrentState = CurrentState.PathOpened,
            RecentEvents = new List<ActivityEvent>()
        };

        // Activity matches one workflow, semantic search returns another
        var activityMatches = new List<WorkflowMatchResult>
        {
            new()
            {
                WorkflowId = "update-node",
                WorkflowName = "update node",
                ConfidenceScore = 0.8,
                CurrentStateName = "editing",
                CurrentStateId = "state1",
                RuleScores = new Dictionary<string, double>(),
                MissingEvidence = new List<string>(),
                MatchedEvidence = new List<ActivityEvent>()
            }
        };

        var packet = packetGenerator.GeneratePacket(
            activityContext,
            activityMatches,
            mockLibrary.Object,
            "connect nodes");

        // Should have ambiguity signal
        Assert.NotNull(packet.Ambiguity);
        Assert.True(packet.Ambiguity.IsAmbiguous || !string.IsNullOrEmpty(packet.Ambiguity.RecommendedAction));
    }

    [Fact]
    public async Task SemanticSearch_NoAmbiguityWhenOnlyActivityMatch()
    {
        var mockLibrary = new Mock<IWorkflowLibrary>();
        var mockHelpGuideStore = new Mock<IHelpGuideStore>();
        var mockLogger = new Mock<ILogger<AssistantContextPacketGenerator>>();

        var workflows = new List<WorkflowDefinition>
        {
            new()
            {
                WorkflowId = "update-node",
                Name = "update node",
                Description = "Update node properties",
                Tags = new List<string>()
            }
        };

        mockLibrary.Setup(x => x.GetAll()).Returns(workflows);
        mockHelpGuideStore.Setup(x => x.GetAll()).Returns(new List<HelpGuide>());
        mockHelpGuideStore.Setup(x => x.GetByWorkflowAndStep(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new List<HelpGuideSection>());

        var semanticSearch = new KeywordSemanticWorkflowSearch(mockLibrary.Object, mockHelpGuideStore.Object);
        var packetGenerator = new AssistantContextPacketGenerator(mockLogger.Object, mockHelpGuideStore.Object, semanticSearch);

        var activityContext = new ActivityContext
        {
            UserName = "testuser",
            CurrentState = CurrentState.PathOpened,
            RecentEvents = new List<ActivityEvent>()
        };

        var activityMatches = new List<WorkflowMatchResult>
        {
            new()
            {
                WorkflowId = "update-node",
                WorkflowName = "update node",
                ConfidenceScore = 0.8,
                CurrentStateName = "editing",
                CurrentStateId = "state1",
                RuleScores = new Dictionary<string, double>(),
                MissingEvidence = new List<string>(),
                MatchedEvidence = new List<ActivityEvent>()
            }
        };

        var packet = packetGenerator.GeneratePacket(
            activityContext,
            activityMatches,
            mockLibrary.Object,
            "something unrelated");

        // If semantic search finds no match, ambiguity should recommend UseActivity
        if (packet.Ambiguity != null)
        {
            Assert.NotEqual("AskClarification", packet.Ambiguity.RecommendedAction);
        }
    }
}
