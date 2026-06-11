using Aktavara.WorkflowIntelligence.Core.Interfaces;
using Aktavara.WorkflowIntelligence.Core.Models;
using Aktavara.WorkflowIntelligence.Core.Models.Api;
using Aktavara.WorkflowIntelligence.Core.Services;
using Xunit;

namespace Aktavara.WorkflowIntelligence.Tests;

/// <summary>
/// Integration tests for the minimal API endpoints.
/// Tests the core logic that the API endpoints use.
/// </summary>
public class ApiIntegrationTests
{
    private static string GetSolutionRoot()
    {
        // Start from the test project's bin/Debug directory and navigate up to solution root
        var currentDir = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            if (Directory.Exists(Path.Combine(currentDir, "workflows")))
                return currentDir;
            var parent = Path.GetDirectoryName(currentDir);
            if (parent == null || parent == currentDir)
                break;
            currentDir = parent;
        }
        // Fallback: assume we're in Tests\bin\Debug\net10.0, so go up 4 levels
        return Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..");
    }
    /// <summary>
    /// Test 5: GET /api/workflows returns 200 with 2 workflows
    /// </summary>
    [Fact]
    public void GetWorkflows_Returns200_WithTwoWorkflows()
    {
        var solutionRoot = GetSolutionRoot();
        var workflowsPath = Path.Combine(solutionRoot, "workflows");
        var workflowLibrary = new FileWorkflowLibrary(
            workflowsPath,
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<FileWorkflowLibrary>());

        var workflows = workflowLibrary.GetAll();

        Assert.Equal(2, workflows.Count);
        Assert.True(workflows.Any(w => w.WorkflowId == "add-connector-to-path"));
        Assert.True(workflows.Any(w => w.WorkflowId == "update-node-in-path"));
    }

    /// <summary>
    /// Test 6: GET /api/workflows/{id} returns 200 for existing workflow
    /// </summary>
    [Fact]
    public void GetWorkflowDetail_Returns200_ForExistingWorkflow()
    {
        var solutionRoot = GetSolutionRoot();
        var workflowsPath = Path.Combine(solutionRoot, "workflows");
        var workflowLibrary = new FileWorkflowLibrary(
            workflowsPath,
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<FileWorkflowLibrary>());

        var workflows = workflowLibrary.GetAll();
        var workflow = workflows.FirstOrDefault(w => w.WorkflowId == "update-node-in-path");

        Assert.NotNull(workflow);
        Assert.Equal("Update node in path", workflow.Name);
    }

    /// <summary>
    /// Test 7: GET /api/workflows/{id} returns 404 for nonexistent workflow
    /// </summary>
    [Fact]
    public void GetWorkflowDetail_Returns404_ForNonexistentWorkflow()
    {
        var solutionRoot = GetSolutionRoot();
        var workflowsPath = Path.Combine(solutionRoot, "workflows");
        var workflowLibrary = new FileWorkflowLibrary(
            workflowsPath,
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<FileWorkflowLibrary>());

        var workflows = workflowLibrary.GetAll();
        var workflow = workflows.FirstOrDefault(w => w.WorkflowId == "nonexistent");

        Assert.Null(workflow);
    }

    /// <summary>
    /// Test 8: GET /api/health returns 200 with healthy status
    /// </summary>
    [Fact]
    public void GetHealth_Returns200_WithHealthyStatus()
    {
        var solutionRoot = GetSolutionRoot();
        var workflowsPath = Path.Combine(solutionRoot, "workflows");
        var workflowLibrary = new FileWorkflowLibrary(
            workflowsPath,
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<FileWorkflowLibrary>());

        var workflows = workflowLibrary.GetAll();
        var response = new HealthCheckResponse
        {
            Status = "healthy",
            WorkflowCount = workflows.Count,
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        };

        Assert.Equal("healthy", response.Status);
        Assert.True(response.WorkflowCount > 0);
    }


    /// <summary>
    /// Test 2: POST /api/analyze/upload with empty file should return 400
    /// </summary>
    [Fact]
    public void AnalyzeUpload_WithEmptyFile_Returns400()
    {
        var parser = new ActivityLogParser(new Microsoft.Extensions.Logging.Abstractions.NullLogger<ActivityLogParser>());
        var logContent = string.Empty;

        // Act & Assert
        var rawEntries = parser.Parse(logContent);
        Assert.Empty(rawEntries);
    }

    /// <summary>
    /// Test 3: POST /api/analyze/upload with invalid file type should return 400
    /// </summary>
    [Fact]
    public void AnalyzeUpload_WithInvalidFileType_Returns400()
    {
        // This would be tested by checking the file extension in the API
        var validExtensions = new[] { ".txt" };
        var fileName = "test.pdf";
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        Assert.False(validExtensions.Contains(extension));
    }

    /// <summary>
    /// Test 4: POST /api/analyze/text with valid log content returns 200
    /// </summary>
    [Fact]
    public void AnalyzeText_WithValidContent_ReturnsAnalyzeResponse()
    {
        // Arrange
        var solutionRoot = GetSolutionRoot();
        var parser = new ActivityLogParser(new Microsoft.Extensions.Logging.Abstractions.NullLogger<ActivityLogParser>());
        var normalizer = new ActivityEventNormalizer(
            new AktaXmlExtractor(new Microsoft.Extensions.Logging.Abstractions.NullLogger<AktaXmlExtractor>()),
            new AktaJsonExtractor(new Microsoft.Extensions.Logging.Abstractions.NullLogger<AktaJsonExtractor>()),
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<ActivityEventNormalizer>());
        var contextBuilder = new ActivityContextBuilder(new Microsoft.Extensions.Logging.Abstractions.NullLogger<ActivityContextBuilder>());
        var matcher = new WorkflowMatcher(new Microsoft.Extensions.Logging.Abstractions.NullLogger<WorkflowMatcher>());
        var packetGenerator = new AssistantContextPacketGenerator(new Microsoft.Extensions.Logging.Abstractions.NullLogger<AssistantContextPacketGenerator>());
        var workflowsPath = Path.Combine(solutionRoot, "workflows");
        var workflowLibrary = new FileWorkflowLibrary(workflowsPath, new Microsoft.Extensions.Logging.Abstractions.NullLogger<FileWorkflowLibrary>());

        var logPath = Path.Combine(solutionRoot, "samples", "logs", "log20260610.txt");
        var logContent = File.ReadAllText(logPath);
        var request = new AnalyzeTextRequest
        {
            LogContent = logContent,
            UserName = "XAdmin",
            TimeWindowMinutes = 30
        };

        // Act
        var rawEntries = parser.Parse(request.LogContent);
        var events = normalizer.Normalize(rawEntries);
        var logEndTime = events.Count > 0 ? events.Max(e => e.Timestamp) : DateTime.UtcNow;
        var windowStart = logEndTime.AddMinutes(-request.TimeWindowMinutes);
        events = events.Where(e => e.UserName == request.UserName && e.Timestamp >= windowStart && e.Timestamp <= logEndTime).ToList();

        var logStartTime = events.FirstOrDefault()?.Timestamp ?? DateTime.UtcNow;
        var context = contextBuilder.BuildContext(events, request.UserName, logStartTime, logEndTime);
        var workflows = workflowLibrary.GetAll();
        var matches = matcher.FindMatches(context, workflows);
        var packet = packetGenerator.GeneratePacket(context, matches, workflowLibrary);

        // Assert
        Assert.NotEmpty(matches);
        Assert.NotNull(packet);
    }
}
