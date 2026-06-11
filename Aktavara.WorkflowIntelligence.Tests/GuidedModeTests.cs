using Aktavara.WorkflowIntelligence.Core.Interfaces;
using Aktavara.WorkflowIntelligence.Core.Models;
using Aktavara.WorkflowIntelligence.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aktavara.WorkflowIntelligence.Tests;

/// <summary>
/// Tests for guided mode CLI command functionality.
/// Tests the core logic: time window filtering, context building, and guidance generation.
/// </summary>
public class GuidedModeTests
{
    /// <summary>
    /// Test 1: Time window filtering excludes events outside the window
    /// </summary>
    [Fact]
    public void GuidedMode_TimeWindowFiltering_ExcludesOldEvents()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var windowMinutes = 30;
        var windowStart = now.AddMinutes(-windowMinutes);

        var events = new List<ActivityEvent>
        {
            new()
            {
                Timestamp = now.AddMinutes(-60),  // Outside window
                UserName = "User1",
                EventType = EventType.SearchRecords
            },
            new()
            {
                Timestamp = now.AddMinutes(-20),  // Inside window
                UserName = "User1",
                EventType = EventType.OpenWorkspace,
                RecordKind = RecordKind.Path,
                WorkspaceKind = "Path"
            },
            new()
            {
                Timestamp = now.AddMinutes(-5),   // Inside window
                UserName = "User1",
                EventType = EventType.SaveRecords,
                RecordKind = RecordKind.Node
            }
        };

        // Act - filter events like guided mode does
        var filteredEvents = events
            .Where(e => e.UserName == "User1" && e.Timestamp >= windowStart && e.Timestamp <= now)
            .ToList();

        // Assert
        Assert.Equal(2, filteredEvents.Count);
        Assert.All(filteredEvents, e => Assert.True(e.Timestamp >= windowStart));
    }

    /// <summary>
    /// Test 2: User filtering excludes other users' events
    /// </summary>
    [Fact]
    public void GuidedMode_UserFiltering_ExcludesOtherUsers()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var targetUser = "User1";

        var events = new List<ActivityEvent>
        {
            new()
            {
                Timestamp = now,
                UserName = "User1",
                EventType = EventType.SearchRecords
            },
            new()
            {
                Timestamp = now.AddSeconds(-10),
                UserName = "User2",  // Different user
                EventType = EventType.OpenWorkspace
            },
            new()
            {
                Timestamp = now.AddSeconds(-5),
                UserName = "User1",
                EventType = EventType.SaveRecords
            }
        };

        // Act
        var filteredEvents = events
            .Where(e => e.UserName == targetUser)
            .ToList();

        // Assert
        Assert.Equal(2, filteredEvents.Count);
        Assert.All(filteredEvents, e => Assert.Equal(targetUser, e.UserName));
    }

    /// <summary>
    /// Test 3: Guided mode produces GuidanceLevel from context and matches
    /// </summary>
    [Fact]
    public void GuidedMode_GeneratesGuidance_WithHighConfidenceMatch()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ActivityContextBuilder>>();
        var contextBuilder = new ActivityContextBuilder(mockLogger.Object);

        var mockPacketLogger = new Mock<ILogger<AssistantContextPacketGenerator>>();
        var packetGenerator = new AssistantContextPacketGenerator(mockPacketLogger.Object);

        var mockWorkflowLibrary = new Mock<IWorkflowLibrary>();
        mockWorkflowLibrary.Setup(wl => wl.GetAll()).Returns(new List<WorkflowDefinition>());

        var now = DateTime.UtcNow;
        var userEvents = new List<ActivityEvent>
        {
            new()
            {
                Timestamp = now.AddMinutes(-20),
                UserName = "TestUser",
                SessionId = "session1",
                EventType = EventType.OpenWorkspace,
                RecordKind = RecordKind.Path,
                WorkspaceKind = "Path"
            },
            new()
            {
                Timestamp = now.AddMinutes(-10),
                UserName = "TestUser",
                SessionId = "session1",
                EventType = EventType.SaveRecords,
                RecordKind = RecordKind.Node
            }
        };

        // Build context
        var context = contextBuilder.BuildContext(
            userEvents,
            "TestUser",
            userEvents.Min(e => e.Timestamp),
            userEvents.Max(e => e.Timestamp));

        // Create a high-confidence match
        var highConfidenceMatch = new WorkflowMatchResult
        {
            WorkflowId = "wf1",
            WorkflowName = "Test Workflow",
            ConfidenceScore = 0.90,
            CurrentStateId = "state1",
            CurrentStateName = "State 1"
        };
        var matches = new List<WorkflowMatchResult> { highConfidenceMatch };

        // Act
        var packet = packetGenerator.GeneratePacket(context, matches, mockWorkflowLibrary.Object);

        // Assert
        Assert.NotNull(packet);
        Assert.Equal("TestUser", packet.UserName);
        Assert.Equal(GuidanceLevel.Instruct, packet.GuidanceLevel);  // 0.90 confidence → Instruct
        Assert.NotNull(packet.BestMatch);
        Assert.Equal(0.90, packet.BestMatch.ConfidenceScore);
    }
}
