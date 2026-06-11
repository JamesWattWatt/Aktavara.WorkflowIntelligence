using Aktavara.WorkflowIntelligence.Core.Interfaces;
using Aktavara.WorkflowIntelligence.Core.Models;
using Aktavara.WorkflowIntelligence.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aktavara.WorkflowIntelligence.Tests;

/// <summary>
/// Tests for AssistantContextPacketGenerator - verifies correct packet generation.
/// </summary>
public class AssistantContextPacketGeneratorTests
{
    private readonly IAssistantContextPacketGenerator _generator;
    private readonly Mock<IWorkflowLibrary> _mockWorkflowLibrary;

    public AssistantContextPacketGeneratorTests()
    {
        var mockLogger = new Mock<ILogger<AssistantContextPacketGenerator>>();
        _generator = new AssistantContextPacketGenerator(mockLogger.Object);
        _mockWorkflowLibrary = new Mock<IWorkflowLibrary>();
        _mockWorkflowLibrary.Setup(wl => wl.GetAll()).Returns(new List<WorkflowDefinition>());
    }

    /// <summary>
    /// Test 1: High confidence match → GuidanceLevel = Instruct
    /// </summary>
    [Fact]
    public void GeneratePacket_HighConfidenceMatch_GuidanceLevelIsInstruct()
    {
        // Arrange
        var activityContext = CreateActivityContext("User1");
        var matchResult = CreateWorkflowMatch("workflow1", "Workflow 1", 0.90);
        var matches = new List<WorkflowMatchResult> { matchResult };

        // Act
        var packet = _generator.GeneratePacket(activityContext, matches, _mockWorkflowLibrary.Object);

        // Assert
        Assert.Equal(GuidanceLevel.Instruct, packet.GuidanceLevel);
        Assert.NotNull(packet.BestMatch);
        Assert.Equal(0.90, packet.BestMatch.ConfidenceScore);
    }

    /// <summary>
    /// Test 2: Medium confidence match → GuidanceLevel = Confirm
    /// </summary>
    [Fact]
    public void GeneratePacket_MediumConfidenceMatch_GuidanceLevelIsConfirm()
    {
        var activityContext = CreateActivityContext("User1");
        var matchResult = CreateWorkflowMatch("workflow1", "Workflow 1", 0.70);
        var matches = new List<WorkflowMatchResult> { matchResult };

        var packet = _generator.GeneratePacket(activityContext, matches, _mockWorkflowLibrary.Object);

        Assert.Equal(GuidanceLevel.Confirm, packet.GuidanceLevel);
    }

    /// <summary>
    /// Test 3: Low confidence match → GuidanceLevel = Suggest
    /// </summary>
    [Fact]
    public void GeneratePacket_LowConfidenceMatch_GuidanceLevelIsSuggest()
    {
        var activityContext = CreateActivityContext("User1");
        var matchResult = CreateWorkflowMatch("workflow1", "Workflow 1", 0.40);
        var matches = new List<WorkflowMatchResult> { matchResult };

        var packet = _generator.GeneratePacket(activityContext, matches, _mockWorkflowLibrary.Object);

        Assert.Equal(GuidanceLevel.Suggest, packet.GuidanceLevel);
    }

    /// <summary>
    /// Test 4: No match → GuidanceLevel = NoGuidance
    /// </summary>
    [Fact]
    public void GeneratePacket_NoMatch_GuidanceLevelIsNoGuidance()
    {
        var activityContext = CreateActivityContext("User1");
        var matches = new List<WorkflowMatchResult>();

        var packet = _generator.GeneratePacket(activityContext, matches, _mockWorkflowLibrary.Object);

        Assert.Equal(GuidanceLevel.NoGuidance, packet.GuidanceLevel);
        Assert.Null(packet.BestMatch);
    }

    /// <summary>
    /// Test 5: RecommendedNextStep from workflow state definition
    /// </summary>
    [Fact]
    public void GeneratePacket_WithStateDefinition_PopulatesNextStep()
    {
        var activityContext = CreateActivityContext("User1");
        var matchResult = CreateWorkflowMatch("workflow1", "Workflow 1", 0.85);
        matchResult.CurrentStateId = "state1";
        var matches = new List<WorkflowMatchResult> { matchResult };

        var workflowDef = new WorkflowDefinition
        {
            WorkflowId = "workflow1",
            States = new List<WorkflowStateDefinition>
            {
                new()
                {
                    StateId = "state1",
                    Name = "State 1",
                    HelpGuideId = "guide-next-step"
                }
            }
        };
        _mockWorkflowLibrary.Setup(wl => wl.GetAll()).Returns(new List<WorkflowDefinition> { workflowDef });

        var packet = _generator.GeneratePacket(activityContext, matches, _mockWorkflowLibrary.Object);

        Assert.NotNull(packet.RecommendedNextStep);
        Assert.Equal("guide-next-step", packet.RecommendedNextStep);
    }

    /// <summary>
    /// Test 6: RecommendedNextStep null when no match
    /// </summary>
    [Fact]
    public void GeneratePacket_NoMatch_NextStepIsNull()
    {
        var activityContext = CreateActivityContext("User1");
        var matches = new List<WorkflowMatchResult>();

        var packet = _generator.GeneratePacket(activityContext, matches, _mockWorkflowLibrary.Object);

        Assert.Null(packet.RecommendedNextStep);
    }

    /// <summary>
    /// Test 7: ContextNarrative contains user ID and workflow name
    /// </summary>
    [Fact]
    public void GeneratePacket_ContextNarrative_ContainsUserAndWorkflowName()
    {
        var activityContext = CreateActivityContext("TestUser");
        var matchResult = CreateWorkflowMatch("wf1", "Update Node Workflow", 0.80);
        var matches = new List<WorkflowMatchResult> { matchResult };

        var packet = _generator.GeneratePacket(activityContext, matches, _mockWorkflowLibrary.Object);

        Assert.Contains("TestUser", packet.ContextNarrative);
        Assert.Contains("Update Node Workflow", packet.ContextNarrative);
        Assert.Equal("TestUser", packet.UserName);
    }

    /// <summary>
    /// Test 8: ContextNarrative contains evidence summary
    /// </summary>
    [Fact]
    public void GeneratePacket_ContextNarrative_ContainsEvidenceSummary()
    {
        var activityContext = CreateActivityContext("User1");
        var matchResult = CreateWorkflowMatch("wf1", "Workflow 1", 0.80);
        matchResult.MatchedEvidence.Add(new ActivityEvent
        {
            EventType = EventType.SearchRecords,
            RecordKind = RecordKind.Path,
            RecordName = "TestPath",
            Timestamp = DateTime.UtcNow
        });
        var matches = new List<WorkflowMatchResult> { matchResult };

        var packet = _generator.GeneratePacket(activityContext, matches, _mockWorkflowLibrary.Object);

        Assert.Contains("evidence", packet.ContextNarrative.ToLower());
    }

    /// <summary>
    /// Test 9: AllMatches includes both workflows when both scored
    /// </summary>
    [Fact]
    public void GeneratePacket_MultipleMatches_AllMatchesPopulated()
    {
        var activityContext = CreateActivityContext("User1");
        var match1 = CreateWorkflowMatch("wf1", "Workflow 1", 0.85);
        var match2 = CreateWorkflowMatch("wf2", "Workflow 2", 0.60);
        var matches = new List<WorkflowMatchResult> { match1, match2 };

        var packet = _generator.GeneratePacket(activityContext, matches, _mockWorkflowLibrary.Object);

        Assert.Equal(2, packet.AllMatches.Count);
        Assert.Contains(packet.AllMatches, m => m.WorkflowId == "wf1");
        Assert.Contains(packet.AllMatches, m => m.WorkflowId == "wf2");
    }

    /// <summary>
    /// Test 10: BestMatch is highest scoring workflow
    /// </summary>
    [Fact]
    public void GeneratePacket_MultipleMatches_BestMatchIsHighestScore()
    {
        var activityContext = CreateActivityContext("User1");
        var match1 = CreateWorkflowMatch("wf1", "Workflow 1", 0.60);
        var match2 = CreateWorkflowMatch("wf2", "Workflow 2", 0.85);
        var matches = new List<WorkflowMatchResult> { match1, match2 };

        var packet = _generator.GeneratePacket(activityContext, matches, _mockWorkflowLibrary.Object);

        Assert.NotNull(packet.BestMatch);
        Assert.Equal("wf2", packet.BestMatch.WorkflowId);
        Assert.Equal(0.85, packet.BestMatch.ConfidenceScore);
    }

    /// <summary>
    /// Additional test: Packet serializes to valid JSON
    /// </summary>
    [Fact]
    public void AssistantContextPacket_ToJson_ProducesValidJson()
    {
        var packet = new AssistantContextPacket
        {
            UserName = "TestUser",
            CurrentState = "idle",
            Summary = "Test summary",
            GuidanceLevel = GuidanceLevel.Suggest
        };

        var json = packet.ToJson();

        Assert.Contains("TestUser", json);
        Assert.Contains("idle", json);
        Assert.NotNull(json);
        Assert.True(json.Length > 0);
    }

    /// <summary>
    /// Helper: Create an ActivityContext for testing
    /// </summary>
    private ActivityContext CreateActivityContext(string userName)
    {
        return new ActivityContext
        {
            UserName = userName,
            TimeWindowStart = DateTime.UtcNow.AddMinutes(-30),
            TimeWindowEnd = DateTime.UtcNow,
            Summary = $"Activity for {userName}",
            CurrentState = CurrentState.NodeSaved,
            RecentEvents = new List<ActivityEvent>
            {
                new()
                {
                    UserName = userName,
                    SessionId = "session123",
                    EventType = EventType.OpenWorkspace,
                    RecordKind = RecordKind.Path,
                    Timestamp = DateTime.UtcNow.AddMinutes(-10)
                },
                new()
                {
                    UserName = userName,
                    SessionId = "session123",
                    EventType = EventType.SaveRecords,
                    RecordKind = RecordKind.Node,
                    Timestamp = DateTime.UtcNow
                }
            }
        };
    }

    /// <summary>
    /// Helper: Create a WorkflowMatchResult for testing
    /// </summary>
    private WorkflowMatchResult CreateWorkflowMatch(
        string workflowId,
        string workflowName,
        double confidenceScore)
    {
        return new WorkflowMatchResult
        {
            WorkflowId = workflowId,
            WorkflowName = workflowName,
            ConfidenceScore = confidenceScore,
            CurrentStateId = "state1",
            CurrentStateName = "State 1",
            RuleScores = new Dictionary<string, double>
            {
                { "Rule 1", 0.3 },
                { "Rule 2", 0.35 }
            },
            MissingEvidence = new List<string>(),
            MatchedEvidence = new List<ActivityEvent>()
        };
    }
}
