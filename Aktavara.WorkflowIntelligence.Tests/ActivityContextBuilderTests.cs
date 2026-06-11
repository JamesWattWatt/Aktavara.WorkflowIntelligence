using Aktavara.WorkflowIntelligence.Core.Interfaces;
using Aktavara.WorkflowIntelligence.Core.Models;
using Aktavara.WorkflowIntelligence.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aktavara.WorkflowIntelligence.Tests;

/// <summary>
/// Tests for ActivityContextBuilder - verifies correct context building from activity events.
/// </summary>
public class ActivityContextBuilderTests
{
    private readonly IActivityContextBuilder _builder;

    public ActivityContextBuilderTests()
    {
        var mockLogger = new Mock<ILogger<ActivityContextBuilder>>();
        _builder = new ActivityContextBuilder(mockLogger.Object);
    }

    /// <summary>
    /// Test 1: Full "create path" sequence → CurrentState = path_created
    /// </summary>
    [Fact]
    public void BuildContext_PathCreationSequence_StateIsPathCreated()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var events = new List<ActivityEvent>
        {
            new()
            {
                Timestamp = now.AddSeconds(10),
                UserName = "User1",
                EventType = EventType.SearchRecords,
                RecordKind = RecordKind.Path,
                RecordId = "P1",
                RecordName = "Path1"
            },
            new()
            {
                Timestamp = now.AddSeconds(20),
                UserName = "User1",
                EventType = EventType.OpenWorkspace,
                RecordKind = RecordKind.Path,
                WorkspaceKind = "Path",
                RecordId = "P1",
                RecordName = "Path1",
                TypeId = "PathType1"
            },
            new()
            {
                Timestamp = now.AddSeconds(30),
                UserName = "User1",
                EventType = EventType.SaveRecords,
                RecordKind = RecordKind.Node,
                RecordId = "N1",
                RecordName = "Node1",
                RecordState = "Modified"
            },
            new()
            {
                Timestamp = now.AddSeconds(40),
                UserName = "User1",
                EventType = EventType.SaveRecords,
                RecordKind = RecordKind.Path,
                RecordId = "P1",
                RecordName = "Path1",
                RecordState = "Added"  // New path being saved
            }
        };

        // Act
        var context = _builder.BuildContext(events, "User1", now, now.AddMinutes(1));

        // Assert
        Assert.Equal(CurrentState.PathCreated, context.CurrentState);
        Assert.NotEmpty(context.Summary);
        Assert.Equal("User1", context.UserName);
        Assert.Equal(4, context.RecentEvents.Count);
    }

    /// <summary>
    /// Test 2: Full "update node" sequence → CurrentState = node_saved
    /// </summary>
    [Fact]
    public void BuildContext_UpdateNodeSequence_StateIsNodeSaved()
    {
        var now = DateTime.UtcNow;
        var events = new List<ActivityEvent>
        {
            new()
            {
                Timestamp = now.AddSeconds(10),
                UserName = "User1",
                EventType = EventType.SearchRecords,
                RecordKind = RecordKind.Path,
                RecordId = "P1"
            },
            new()
            {
                Timestamp = now.AddSeconds(20),
                UserName = "User1",
                EventType = EventType.OpenWorkspace,
                RecordKind = RecordKind.Path,
                WorkspaceKind = "Path",
                RecordId = "P1",
                TypeId = "PathType1"
            },
            new()
            {
                Timestamp = now.AddSeconds(30),
                UserName = "User1",
                EventType = EventType.SaveRecords,
                RecordKind = RecordKind.Node,
                RecordId = "N1",
                RecordName = "Node1",
                RecordState = "Modified"
            }
        };

        var context = _builder.BuildContext(events, "User1", now, now.AddMinutes(1));

        Assert.Equal(CurrentState.NodeSaved, context.CurrentState);
        Assert.Equal(3, context.RecentEvents.Count);
    }

    /// <summary>
    /// Test 3: OpenWorkspace only → CurrentState = path_opened
    /// </summary>
    [Fact]
    public void BuildContext_OnlyOpenWorkspace_StateIsPathOpened()
    {
        var now = DateTime.UtcNow;
        var events = new List<ActivityEvent>
        {
            new()
            {
                Timestamp = now.AddSeconds(10),
                UserName = "User1",
                EventType = EventType.OpenWorkspace,
                RecordKind = RecordKind.Path,
                WorkspaceKind = "Path",
                RecordId = "P1",
                TypeId = "PathType1"
            }
        };

        var context = _builder.BuildContext(events, "User1", now, now.AddMinutes(1));

        Assert.Equal(CurrentState.PathOpened, context.CurrentState);
    }

    /// <summary>
    /// Test 4: No events → CurrentState = no_activity
    /// </summary>
    [Fact]
    public void BuildContext_NoEvents_StateIsNoActivity()
    {
        var now = DateTime.UtcNow;
        var events = new List<ActivityEvent>();

        var context = _builder.BuildContext(events, "User1", now, now.AddMinutes(1));

        Assert.Equal(CurrentState.NoActivity, context.CurrentState);
        Assert.Empty(context.RecentEvents);
        Assert.Contains("No activity", context.Summary);
    }

    /// <summary>
    /// Test 5: Node save without prior open → CurrentState = node_saved
    /// </summary>
    [Fact]
    public void BuildContext_NodeSaveWithoutOpen_StateIsNodeSaved()
    {
        var now = DateTime.UtcNow;
        var events = new List<ActivityEvent>
        {
            new()
            {
                Timestamp = now.AddSeconds(10),
                UserName = "User1",
                EventType = EventType.SaveRecords,
                RecordKind = RecordKind.Node,
                RecordId = "N1",
                RecordName = "Node1"
            }
        };

        var context = _builder.BuildContext(events, "User1", now, now.AddMinutes(1));

        Assert.Equal(CurrentState.NodeSaved, context.CurrentState);
    }

    /// <summary>
    /// Test 6: Time window filtering → events outside window excluded
    /// </summary>
    [Fact]
    public void BuildContext_TimeWindowFiltering_ExcludesOutsideWindow()
    {
        var now = DateTime.UtcNow;
        var windowStart = now.AddSeconds(20);
        var windowEnd = now.AddSeconds(60);

        var events = new List<ActivityEvent>
        {
            new()
            {
                Timestamp = now.AddSeconds(5),  // Before window
                UserName = "User1",
                EventType = EventType.SearchRecords
            },
            new()
            {
                Timestamp = now.AddSeconds(30),  // Inside window
                UserName = "User1",
                EventType = EventType.OpenWorkspace,
                RecordKind = RecordKind.Path,
                WorkspaceKind = "Path"
            },
            new()
            {
                Timestamp = now.AddSeconds(90),  // After window
                UserName = "User1",
                EventType = EventType.SaveRecords
            }
        };

        var context = _builder.BuildContext(events, "User1", windowStart, windowEnd);

        Assert.Single(context.RecentEvents);
        Assert.Equal(EventType.OpenWorkspace, context.RecentEvents[0].EventType);
    }

    /// <summary>
    /// Test 7: User filtering → other users' events excluded
    /// </summary>
    [Fact]
    public void BuildContext_UserFiltering_ExcludesOtherUsers()
    {
        var now = DateTime.UtcNow;
        var events = new List<ActivityEvent>
        {
            new()
            {
                Timestamp = now.AddSeconds(10),
                UserName = "User1",
                EventType = EventType.SearchRecords
            },
            new()
            {
                Timestamp = now.AddSeconds(20),
                UserName = "User2",
                EventType = EventType.OpenWorkspace
            },
            new()
            {
                Timestamp = now.AddSeconds(30),
                UserName = "User1",
                EventType = EventType.SaveRecords
            }
        };

        var context = _builder.BuildContext(events, "User1", now, now.AddMinutes(1));

        Assert.Equal(2, context.RecentEvents.Count);
        Assert.All(context.RecentEvents, e => Assert.Equal("User1", e.UserName));
    }

    /// <summary>
    /// Test 8: WorkflowHints → rapid sequence detected and described
    /// </summary>
    [Fact]
    public void BuildContext_RapidSequence_GeneratesHint()
    {
        var now = DateTime.UtcNow;
        var events = new List<ActivityEvent>
        {
            new()
            {
                Timestamp = now.AddSeconds(10),
                UserName = "User1",
                EventType = EventType.OpenWorkspace,
                RecordKind = RecordKind.Path,
                WorkspaceKind = "Path",
                RecordId = "P1",
                RecordName = "Path1"
            },
            new()
            {
                Timestamp = now.AddSeconds(20),  // 10 seconds later
                UserName = "User1",
                EventType = EventType.SaveRecords,
                RecordKind = RecordKind.Node,
                RecordId = "N1"
            }
        };

        var context = _builder.BuildContext(events, "User1", now, now.AddMinutes(1));

        Assert.NotEmpty(context.WorkflowHints);
        Assert.Contains("opened", context.WorkflowHints[0].ToLower());
        Assert.Contains("saved", context.WorkflowHints[0].ToLower());
    }

    /// <summary>
    /// Test 9: ActiveEntities → correct Path and Node identified
    /// </summary>
    [Fact]
    public void BuildContext_ActiveEntities_IdentifiesPathAndNode()
    {
        var now = DateTime.UtcNow;
        var events = new List<ActivityEvent>
        {
            new()
            {
                Timestamp = now.AddSeconds(10),
                UserName = "User1",
                EventType = EventType.OpenWorkspace,
                RecordKind = RecordKind.Path,
                RecordId = "P1",
                RecordName = "Path1",
                TypeId = "PathType1"
            },
            new()
            {
                Timestamp = now.AddSeconds(20),
                UserName = "User1",
                EventType = EventType.SaveRecords,
                RecordKind = RecordKind.Node,
                RecordId = "N1",
                RecordName = "Node1",
                TypeId = "NodeType1"
            }
        };

        var context = _builder.BuildContext(events, "User1", now, now.AddMinutes(1));

        Assert.NotEmpty(context.ActiveEntities);

        var pathEntity = context.ActiveEntities.FirstOrDefault(e => e.RecordKind == RecordKind.Path);
        Assert.NotNull(pathEntity);
        Assert.Equal("P1", pathEntity.RecordId);
        Assert.Equal("Path1", pathEntity.Name);

        var nodeEntity = context.ActiveEntities.FirstOrDefault(e => e.RecordKind == RecordKind.Node);
        Assert.NotNull(nodeEntity);
        Assert.Equal("N1", nodeEntity.RecordId);
        Assert.Equal("Node1", nodeEntity.Name);
    }

    /// <summary>
    /// Test 10: Summary → human-readable and references real record names
    /// </summary>
    [Fact]
    public void BuildContext_Summary_IsHumanReadableWithRecordNames()
    {
        var now = DateTime.UtcNow;
        var events = new List<ActivityEvent>
        {
            new()
            {
                Timestamp = now.AddSeconds(10),
                UserName = "User1",
                EventType = EventType.OpenWorkspace,
                RecordKind = RecordKind.Path,
                WorkspaceKind = "Path",
                RecordId = "P1",
                RecordName = "Important Path"
            },
            new()
            {
                Timestamp = now.AddSeconds(20),
                UserName = "User1",
                EventType = EventType.SaveRecords,
                RecordKind = RecordKind.Node,
                RecordId = "N1",
                RecordName = "Critical Node"
            }
        };

        var context = _builder.BuildContext(events, "User1", now, now.AddMinutes(1));

        Assert.NotEmpty(context.Summary);
        Assert.Contains("User1", context.Summary);
        Assert.Contains("Path", context.Summary);
        Assert.Contains("Node", context.Summary);
    }

    /// <summary>
    /// Additional test: Connector creation → CurrentState = connector_created
    /// </summary>
    [Fact]
    public void BuildContext_ConnectorCreation_StateIsConnectorCreated()
    {
        var now = DateTime.UtcNow;
        var events = new List<ActivityEvent>
        {
            new()
            {
                Timestamp = now.AddSeconds(10),
                UserName = "User1",
                EventType = EventType.SaveRecords,
                RecordKind = RecordKind.Connector,
                RecordId = "C1",
                RecordName = "Connector1"
            }
        };

        var context = _builder.BuildContext(events, "User1", now, now.AddMinutes(1));

        Assert.Equal(CurrentState.ConnectorCreated, context.CurrentState);
    }

    /// <summary>
    /// Additional test: SessionId is populated from most recent event
    /// </summary>
    [Fact]
    public void BuildContext_SessionId_FromMostRecentEvent()
    {
        var now = DateTime.UtcNow;
        var sessionId = "Session123";
        var events = new List<ActivityEvent>
        {
            new()
            {
                Timestamp = now.AddSeconds(10),
                UserName = "User1",
                SessionId = "OldSession",
                EventType = EventType.SearchRecords
            },
            new()
            {
                Timestamp = now.AddSeconds(30),
                UserName = "User1",
                SessionId = sessionId,
                EventType = EventType.OpenWorkspace,
                RecordKind = RecordKind.Path,
                WorkspaceKind = "Path"
            }
        };

        var context = _builder.BuildContext(events, "User1", now, now.AddMinutes(1));

        Assert.Equal(sessionId, context.SessionId);
    }
}
