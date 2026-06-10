using Aktavara.WorkflowIntelligence.Core.Interfaces;
using Aktavara.WorkflowIntelligence.Core.Models;
using Aktavara.WorkflowIntelligence.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aktavara.WorkflowIntelligence.Tests;

public class WorkflowMatcherTests
{
    private readonly IWorkflowMatcher _matcher;

    public WorkflowMatcherTests()
    {
        var mockLogger = new Mock<ILogger<WorkflowMatcher>>().Object;
        _matcher = new WorkflowMatcher(mockLogger);
    }

    [Fact]
    public void ScoreWorkflow_SearchPathOpenPathSaveNode_HighConfidence()
    {
        var workflow = CreateUpdateNodeWorkflow();
        var context = new ActivityContext
        {
            RecentEvents = new List<ActivityEvent>
            {
                CreateSearchPathEvent(),
                CreateOpenPathEvent(),
                CreateSaveNodeEvent()
            }
        };

        var result = _matcher.ScoreWorkflow(workflow, context);

        Assert.True(result.ConfidenceScore >= 0.85);
        Assert.Equal(ConfidenceLevel.High, result.ConfidenceLevel);
    }

    [Fact]
    public void ScoreWorkflow_SearchPathOnly_LowConfidence()
    {
        var workflow = CreateUpdateNodeWorkflow();
        var context = new ActivityContext
        {
            RecentEvents = new List<ActivityEvent> { CreateSearchPathEvent() }
        };

        var result = _matcher.ScoreWorkflow(workflow, context);

        Assert.True(result.ConfidenceScore < 0.55);
        Assert.Equal(ConfidenceLevel.Low, result.ConfidenceLevel);
    }

    [Fact]
    public void ScoreWorkflow_OpenPathConnectorSave_ConnectorHigherScore()
    {
        var context = new ActivityContext
        {
            RecentEvents = new List<ActivityEvent>
            {
                CreateOpenPathEvent(),
                CreateSaveConnectorEvent(),
                CreateSavePathEvent()
            }
        };

        var updateResult = _matcher.ScoreWorkflow(CreateUpdateNodeWorkflow(), context);
        var connectorResult = _matcher.ScoreWorkflow(CreateConnectorWorkflow(), context);

        Assert.True(connectorResult.ConfidenceScore > updateResult.ConfidenceScore);
    }

    [Fact]
    public void ScoreWorkflow_UnrelatedSaves_NoHighConfidence()
    {
        var workflow = CreateUpdateNodeWorkflow();
        var context = new ActivityContext
        {
            RecentEvents = new List<ActivityEvent>
            {
                CreateSaveUnrelatedEvent(),
                CreateSaveUnrelatedEvent()
            }
        };

        var result = _matcher.ScoreWorkflow(workflow, context);

        Assert.True(result.ConfidenceScore < 0.85);
    }

    [Fact]
    public void ScoreWorkflow_DeterminesState()
    {
        var workflow = CreateUpdateNodeWorkflow();
        var context = new ActivityContext
        {
            RecentEvents = new List<ActivityEvent>
            {
                CreateOpenPathEvent(),
                CreateSaveNodeEvent()
            }
        };

        var result = _matcher.ScoreWorkflow(workflow, context);

        Assert.NotNull(result.CurrentStateId);
        Assert.NotNull(result.CurrentStateName);
    }

    [Fact]
    public void FindBestMatch_HighConfidence_ReturnsWorkflow()
    {
        var workflows = new[] { CreateUpdateNodeWorkflow() };
        var context = new ActivityContext
        {
            RecentEvents = new List<ActivityEvent>
            {
                CreateSearchPathEvent(),
                CreateOpenPathEvent(),
                CreateSaveNodeEvent()
            }
        };

        var result = _matcher.FindBestMatch(context, workflows, 0.85);

        Assert.NotNull(result);
        Assert.Equal("update-node-in-path", result.WorkflowId);
    }

    [Fact]
    public void FindBestMatch_BelowThreshold_ReturnsNull()
    {
        var workflows = new[] { CreateUpdateNodeWorkflow() };
        var context = new ActivityContext
        {
            RecentEvents = new List<ActivityEvent> { CreateSearchPathEvent() }
        };

        var result = _matcher.FindBestMatch(context, workflows, 0.85);

        Assert.Null(result);
    }

    [Fact]
    public void FindMatches_ReturnsRanked()
    {
        var workflows = new[] { CreateUpdateNodeWorkflow(), CreateConnectorWorkflow() };
        var context = new ActivityContext
        {
            RecentEvents = new List<ActivityEvent>
            {
                CreateOpenPathEvent(),
                CreateSaveConnectorEvent()
            }
        };

        var results = _matcher.FindMatches(context, workflows);

        Assert.Equal(2, results.Count);
        Assert.True(results[0].ConfidenceScore >= results[1].ConfidenceScore);
    }

    private WorkflowDefinition CreateUpdateNodeWorkflow() => new()
    {
        WorkflowId = "update-node-in-path",
        Name = "Update node in path",
        ActivitySignature = new()
        {
            new WorkflowSignatureRule { EventType = EventType.SearchRecords, RecordKind = RecordKind.Path, Required = false, Weight = 0.15 },
            new WorkflowSignatureRule { EventType = EventType.OpenWorkspace, RecordKind = RecordKind.Path, Required = true, Weight = 0.35 },
            new WorkflowSignatureRule { EventType = EventType.SaveRecords, RecordKind = RecordKind.Node, Required = true, Weight = 0.35 },
            new WorkflowSignatureRule { EventType = EventType.SaveRecords, RecordKind = RecordKind.Path, Required = false, Weight = 0.15 }
        },
        States = new()
        {
            new WorkflowStateDefinition { StateId = "initial", Sequence = 0, RequiredEvidence = new() { "OpenWorkspace" } },
            new WorkflowStateDefinition { StateId = "complete", Sequence = 1, IsTerminal = true, RequiredEvidence = new() { "SaveRecords" } }
        }
    };

    private WorkflowDefinition CreateConnectorWorkflow() => new()
    {
        WorkflowId = "add-connector-to-path",
        Name = "Add connector to path",
        ActivitySignature = new()
        {
            new WorkflowSignatureRule { EventType = EventType.SearchRecords, RecordKind = RecordKind.Path, Required = false, Weight = 0.12 },
            new WorkflowSignatureRule { EventType = EventType.OpenWorkspace, RecordKind = RecordKind.Path, Required = true, Weight = 0.30 },
            new WorkflowSignatureRule { EventType = EventType.SaveRecords, RecordKind = RecordKind.Connector, Required = true, Weight = 0.50 },
            new WorkflowSignatureRule { EventType = EventType.SaveRecords, RecordKind = RecordKind.Path, Required = false, Weight = 0.08 }
        },
        States = new()
        {
            new WorkflowStateDefinition { StateId = "initial", Sequence = 0, RequiredEvidence = new() { "OpenWorkspace" } }
        }
    };

    private ActivityEvent CreateSearchPathEvent() => new()
    {
        EventId = Guid.NewGuid().ToString(),
        EventType = EventType.SearchRecords,
        RecordKind = RecordKind.Path,
        Timestamp = DateTime.UtcNow.AddMinutes(-5)
    };

    private ActivityEvent CreateOpenPathEvent() => new()
    {
        EventId = Guid.NewGuid().ToString(),
        EventType = EventType.OpenWorkspace,
        RecordKind = RecordKind.Path,
        WorkspaceKind = "Path",
        Timestamp = DateTime.UtcNow.AddMinutes(-3)
    };

    private ActivityEvent CreateSaveNodeEvent() => new()
    {
        EventId = Guid.NewGuid().ToString(),
        EventType = EventType.SaveRecords,
        RecordKind = RecordKind.Node,
        Timestamp = DateTime.UtcNow
    };

    private ActivityEvent CreateSaveConnectorEvent() => new()
    {
        EventId = Guid.NewGuid().ToString(),
        EventType = EventType.SaveRecords,
        RecordKind = RecordKind.Connector,
        Timestamp = DateTime.UtcNow
    };

    private ActivityEvent CreateSavePathEvent() => new()
    {
        EventId = Guid.NewGuid().ToString(),
        EventType = EventType.SaveRecords,
        RecordKind = RecordKind.Path,
        Timestamp = DateTime.UtcNow.AddMilliseconds(100)
    };

    private ActivityEvent CreateSaveUnrelatedEvent() => new()
    {
        EventId = Guid.NewGuid().ToString(),
        EventType = EventType.SaveRecords,
        RecordKind = RecordKind.Other,
        Timestamp = DateTime.UtcNow
    };
}
