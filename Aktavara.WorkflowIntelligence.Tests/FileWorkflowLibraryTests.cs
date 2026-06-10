using Aktavara.WorkflowIntelligence.Core.Interfaces;
using Aktavara.WorkflowIntelligence.Core.Models;
using Aktavara.WorkflowIntelligence.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aktavara.WorkflowIntelligence.Tests;

public class FileWorkflowLibraryTests : IAsyncLifetime
{
    private readonly string _testWorkflowDirectory;
    private readonly ILogger<FileWorkflowLibrary> _mockLogger;
    private FileWorkflowLibrary _library = null!;

    public FileWorkflowLibraryTests()
    {
        _testWorkflowDirectory = Path.Combine(Path.GetTempPath(), $"workflows-{Guid.NewGuid()}");
        _mockLogger = new Mock<ILogger<FileWorkflowLibrary>>().Object;
    }

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_testWorkflowDirectory);
        _library = new FileWorkflowLibrary(_testWorkflowDirectory, _mockLogger);
    }

    public async Task DisposeAsync()
    {
        if (Directory.Exists(_testWorkflowDirectory))
        {
            Directory.Delete(_testWorkflowDirectory, recursive: true);
        }
    }

    #region Basic Loading Tests

    [Fact]
    public async Task LoadAsync_WithEmptyDirectory_LoadsNoWorkflows()
    {
        await _library.LoadAsync();

        var all = _library.GetAll();
        Assert.Empty(all);
    }

    [Fact]
    public async Task LoadAsync_WithNonExistentDirectory_LogsWarning()
    {
        var nonExistentDir = Path.Combine(_testWorkflowDirectory, "nonexistent");
        var library = new FileWorkflowLibrary(nonExistentDir, _mockLogger);

        await library.LoadAsync();

        var all = library.GetAll();
        Assert.Empty(all);
    }

    [Fact]
    public async Task LoadAsync_WithValidWorkflowFile_LoadsWorkflow()
    {
        var workflow = CreateValidWorkflow("test-workflow-1", "Test Workflow 1");
        await WriteWorkflowFileAsync("test-workflow-1.workflow.json", workflow);

        await _library.LoadAsync();

        var loaded = _library.GetById("test-workflow-1");
        Assert.NotNull(loaded);
        Assert.Equal("test-workflow-1", loaded.WorkflowId);
        Assert.Equal("Test Workflow 1", loaded.Name);
    }

    [Fact]
    public async Task LoadAsync_WithMultipleWorkflows_LoadsAll()
    {
        await WriteWorkflowFileAsync("workflow-1.workflow.json", CreateValidWorkflow("workflow-1", "Workflow 1"));
        await WriteWorkflowFileAsync("workflow-2.workflow.json", CreateValidWorkflow("workflow-2", "Workflow 2"));
        await WriteWorkflowFileAsync("workflow-3.workflow.json", CreateValidWorkflow("workflow-3", "Workflow 3"));

        await _library.LoadAsync();

        var all = _library.GetAll();
        Assert.Equal(3, all.Count);
        Assert.Contains(all, w => w.WorkflowId == "workflow-1");
        Assert.Contains(all, w => w.WorkflowId == "workflow-2");
        Assert.Contains(all, w => w.WorkflowId == "workflow-3");
    }

    [Fact]
    public async Task LoadAsync_WithInvalidJson_SkipsFile()
    {
        var invalidPath = Path.Combine(_testWorkflowDirectory, "invalid.workflow.json");
        await File.WriteAllTextAsync(invalidPath, "{ invalid json }");

        await WriteWorkflowFileAsync("valid.workflow.json", CreateValidWorkflow("valid", "Valid"));

        await _library.LoadAsync();

        var all = _library.GetAll();
        Assert.Single(all);
        Assert.Equal("valid", all[0].WorkflowId);
    }

    [Fact]
    public async Task LoadAsync_WithDuplicateIds_UsesLastFile()
    {
        var workflow1 = CreateValidWorkflow("duplicate-id", "First Workflow");
        var workflow2 = CreateValidWorkflow("duplicate-id", "Second Workflow");

        await WriteWorkflowFileAsync("first-duplicate.workflow.json", workflow1);
        await WriteWorkflowFileAsync("second-duplicate.workflow.json", workflow2);

        await _library.LoadAsync();

        var loaded = _library.GetById("duplicate-id");
        Assert.NotNull(loaded);
        Assert.Equal("Second Workflow", loaded.Name);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithNullId_ReturnsNull()
    {
        await _library.LoadAsync();
        var result = _library.GetById(null);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetById_WithEmptyId_ReturnsNull()
    {
        await _library.LoadAsync();
        var result = _library.GetById(string.Empty);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetById_WithNonExistentId_ReturnsNull()
    {
        await WriteWorkflowFileAsync("existing.workflow.json", CreateValidWorkflow("existing", "Existing"));
        await _library.LoadAsync();

        var result = _library.GetById("nonexistent");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetById_WithExistingId_ReturnsWorkflow()
    {
        await WriteWorkflowFileAsync("test.workflow.json", CreateValidWorkflow("test", "Test"));
        await _library.LoadAsync();

        var result = _library.GetById("test");
        Assert.NotNull(result);
        Assert.Equal("test", result.WorkflowId);
    }

    #endregion

    #region GetByTag Tests

    [Fact]
    public async Task GetByTag_WithNullTag_ReturnsEmpty()
    {
        await _library.LoadAsync();
        var result = _library.GetByTag(null);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByTag_WithEmptyTag_ReturnsEmpty()
    {
        await _library.LoadAsync();
        var result = _library.GetByTag(string.Empty);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByTag_WithExistingTag_ReturnsMatchingWorkflows()
    {
        var w1 = CreateValidWorkflow("w1", "W1");
        w1.Tags.Add("path-operations");
        var w2 = CreateValidWorkflow("w2", "W2");
        w2.Tags.Add("path-operations");
        var w3 = CreateValidWorkflow("w3", "W3");
        w3.Tags.Add("topology-operations");

        await WriteWorkflowFileAsync("w1.workflow.json", w1);
        await WriteWorkflowFileAsync("w2.workflow.json", w2);
        await WriteWorkflowFileAsync("w3.workflow.json", w3);

        await _library.LoadAsync();

        var result = _library.GetByTag("path-operations");
        Assert.Equal(2, result.Count);
        Assert.All(result, w => Assert.Contains("path-operations", w.Tags));
    }

    [Fact]
    public async Task GetByTag_IsCaseInsensitive()
    {
        var workflow = CreateValidWorkflow("test", "Test");
        workflow.Tags.Add("Path-Operations");

        await WriteWorkflowFileAsync("test.workflow.json", workflow);
        await _library.LoadAsync();

        var result = _library.GetByTag("path-operations");
        Assert.Single(result);
        Assert.Equal("test", result[0].WorkflowId);
    }

    #endregion

    #region GetActive Tests

    [Fact]
    public async Task GetActive_WithMixedStatuses_ReturnsOnlyActive()
    {
        var active1 = CreateValidWorkflow("active1", "Active 1");
        active1.Status = WorkflowStatus.Active;

        var active2 = CreateValidWorkflow("active2", "Active 2");
        active2.Status = WorkflowStatus.Active;

        var inactive = CreateValidWorkflow("inactive", "Inactive");
        inactive.Status = WorkflowStatus.Inactive;

        var archived = CreateValidWorkflow("archived", "Archived");
        archived.Status = WorkflowStatus.Archived;

        await WriteWorkflowFileAsync("active1.workflow.json", active1);
        await WriteWorkflowFileAsync("active2.workflow.json", active2);
        await WriteWorkflowFileAsync("inactive.workflow.json", inactive);
        await WriteWorkflowFileAsync("archived.workflow.json", archived);

        await _library.LoadAsync();

        var result = _library.GetActive();
        Assert.Equal(2, result.Count);
        Assert.All(result, w => Assert.True(w.IsActive));
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task GetValidationErrors_WithValidWorkflow_ReturnsEmpty()
    {
        await WriteWorkflowFileAsync("valid.workflow.json", CreateValidWorkflow("valid", "Valid"));
        await _library.LoadAsync();

        var errors = _library.GetValidationErrors("valid");
        Assert.Empty(errors);
    }

    [Fact]
    public async Task GetValidationErrors_WithInvalidWorkflow_ReturnsErrors()
    {
        var invalid = new WorkflowDefinition
        {
            WorkflowId = "invalid",
            Name = "", // Invalid: empty name
            ActivitySignature = new() // Invalid: no signature rules
        };

        await WriteWorkflowFileAsync("invalid.workflow.json", invalid);
        await _library.LoadAsync();

        var errors = _library.GetValidationErrors("invalid");
        Assert.NotEmpty(errors);
        Assert.Contains("Name is required", errors[0]);
    }

    [Fact]
    public async Task GetValidationErrors_WithNonExistentWorkflow_ReturnsEmpty()
    {
        await _library.LoadAsync();
        var errors = _library.GetValidationErrors("nonexistent");
        Assert.Empty(errors);
    }

    #endregion

    #region Helper Methods

    private WorkflowDefinition CreateValidWorkflow(string workflowId, string name)
    {
        return new WorkflowDefinition
        {
            WorkflowId = workflowId,
            Name = name,
            Description = $"Test workflow: {name}",
            Version = "1.0",
            Status = WorkflowStatus.Active,
            ActivitySignature = new()
            {
                new WorkflowSignatureRule
                {
                    EventType = EventType.SearchRecords,
                    RecordKind = RecordKind.Path,
                    Required = true,
                    Weight = 1.0
                }
            },
            States = new()
            {
                new WorkflowStateDefinition
                {
                    StateId = "initial",
                    Name = "Initial State",
                    Description = "Starting state",
                    Sequence = 0,
                    IsTerminal = false,
                    NextStateId = null,
                    RequiredEvidence = new() { "SearchRecords" }
                }
            },
            MinimumConfidenceThreshold = 0.5,
            Tags = new() { "test" }
        };
    }

    private async Task WriteWorkflowFileAsync(string fileName, WorkflowDefinition workflow)
    {
        var filePath = Path.Combine(_testWorkflowDirectory, fileName);
        var json = System.Text.Json.JsonSerializer.Serialize(workflow, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(filePath, json);
    }

    #endregion
}
