using Aktavara.WorkflowIntelligence.Core.Interfaces;
using Aktavara.WorkflowIntelligence.Core.Models;
using Aktavara.WorkflowIntelligence.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aktavara.WorkflowIntelligence.Tests.Services;

public class KeywordSemanticWorkflowSearchTests
{
    private readonly Mock<IWorkflowLibrary> _mockLibrary;
    private readonly Mock<IHelpGuideStore> _mockHelpGuideStore;
    private readonly KeywordSemanticWorkflowSearch _service;

    public KeywordSemanticWorkflowSearchTests()
    {
        _mockLibrary = new Mock<IWorkflowLibrary>();
        _mockHelpGuideStore = new Mock<IHelpGuideStore>();
        _service = new KeywordSemanticWorkflowSearch(_mockLibrary.Object, _mockHelpGuideStore.Object);
    }

    [Fact]
    public void IsAvailable_ReturnsFalse()
    {
        Assert.False(_service.IsAvailable);
    }

    [Fact]
    public async Task SearchAsync_WithEmptyText_ReturnsEmptyList()
    {
        var result = await _service.SearchAsync(string.Empty, 5, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchAsync_WithOnlyStopWords_ReturnsEmptyList()
    {
        _mockLibrary.Setup(x => x.GetAll()).Returns(new List<WorkflowDefinition>());

        var result = await _service.SearchAsync("the a is and or", 5, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchAsync_WithNameMatch_ReturnsMatchingWorkflow()
    {
        var workflow = new WorkflowDefinition
        {
            WorkflowId = "wf1",
            Name = "add connector to path",
            Description = "Add a connector between two nodes",
            Tags = new List<string> { "connector", "path" }
        };

        _mockLibrary.Setup(x => x.GetAll()).Returns(new List<WorkflowDefinition> { workflow });
        _mockHelpGuideStore.Setup(x => x.GetAll()).Returns(new List<HelpGuide>());

        var result = await _service.SearchAsync("connect nodes", 5, CancellationToken.None);

        Assert.Single(result);
        Assert.Contains("connect", result[0].MatchedTerms);
    }

    [Fact]
    public async Task SearchAsync_FiltersBelowThreshold()
    {
        var workflow1 = new WorkflowDefinition
        {
            WorkflowId = "wf1",
            Name = "xyz",
            Description = "xyz description",
            Tags = new List<string>()
        };

        var workflow2 = new WorkflowDefinition
        {
            WorkflowId = "wf2",
            Name = "add connector to path",
            Description = "Add a connector between two nodes in a path",
            Tags = new List<string> { "connector" }
        };

        _mockLibrary.Setup(x => x.GetAll()).Returns(new List<WorkflowDefinition> { workflow1, workflow2 });
        _mockHelpGuideStore.Setup(x => x.GetAll()).Returns(new List<HelpGuide>());

        var result = await _service.SearchAsync("connector", 5, CancellationToken.None);

        // Only workflow2 should match
        Assert.Single(result);
        Assert.Equal("wf2", result[0].WorkflowId);
    }

    [Fact]
    public async Task SearchAsync_RespectsTopK()
    {
        var workflows = new List<WorkflowDefinition>
        {
            new() { WorkflowId = "wf1", Name = "add connector", Description = "connector", Tags = new() },
            new() { WorkflowId = "wf2", Name = "connector path", Description = "connector", Tags = new() },
            new() { WorkflowId = "wf3", Name = "update connector", Description = "connector", Tags = new() },
        };

        _mockLibrary.Setup(x => x.GetAll()).Returns(workflows);
        _mockHelpGuideStore.Setup(x => x.GetAll()).Returns(new List<HelpGuide>());

        var result = await _service.SearchAsync("connector", 2, CancellationToken.None);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task SearchAsync_ScoresOrderedByRelevance()
    {
        var workflows = new List<WorkflowDefinition>
        {
            new() { WorkflowId = "wf1", Name = "connector", Description = "test", Tags = new() },
            new() { WorkflowId = "wf2", Name = "add connector to path", Description = "test", Tags = new() },
        };

        _mockLibrary.Setup(x => x.GetAll()).Returns(workflows);
        _mockHelpGuideStore.Setup(x => x.GetAll()).Returns(new List<HelpGuide>());

        var result = await _service.SearchAsync("connector", 5, CancellationToken.None);

        Assert.True(result[0].Score >= result[1].Score);
    }

    [Fact]
    public async Task SearchAsync_PopulatesMatchedTerms()
    {
        var workflow = new WorkflowDefinition
        {
            WorkflowId = "wf1",
            Name = "add connector to path",
            Description = "Connect nodes and save",
            Tags = new List<string>()
        };

        _mockLibrary.Setup(x => x.GetAll()).Returns(new List<WorkflowDefinition> { workflow });
        _mockHelpGuideStore.Setup(x => x.GetAll()).Returns(new List<HelpGuide>());

        var result = await _service.SearchAsync("add connector", 5, CancellationToken.None);

        Assert.NotEmpty(result[0].MatchedTerms);
    }

    [Fact]
    public async Task SearchAsync_PopulatesMatchedFields()
    {
        var workflow = new WorkflowDefinition
        {
            WorkflowId = "wf1",
            Name = "add connector",
            Description = "test description",
            Tags = new List<string>()
        };

        _mockLibrary.Setup(x => x.GetAll()).Returns(new List<WorkflowDefinition> { workflow });
        _mockHelpGuideStore.Setup(x => x.GetAll()).Returns(new List<HelpGuide>());

        var result = await _service.SearchAsync("connector", 5, CancellationToken.None);

        Assert.Contains("name", result[0].MatchedFields);
    }
}
