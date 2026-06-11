using Aktavara.WorkflowIntelligence.Core.Interfaces;
using Aktavara.WorkflowIntelligence.Core.Models;
using Aktavara.WorkflowIntelligence.Core.Models.Api;
using Aktavara.WorkflowIntelligence.Core.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Register Core services
builder.Services.AddScoped<IActivityLogParser, ActivityLogParser>();
builder.Services.AddScoped<IAktaJsonExtractor, AktaJsonExtractor>();
builder.Services.AddScoped<IAktaXmlExtractor, AktaXmlExtractor>();
builder.Services.AddScoped<IActivityEventNormalizer, ActivityEventNormalizer>();
builder.Services.AddSingleton<IWorkflowLibrary>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var workflowsPath = config.GetValue<string>("WorkflowIntelligence:WorkflowsPath") ?? "workflows/";
    var logger = sp.GetRequiredService<ILogger<FileWorkflowLibrary>>();
    return new FileWorkflowLibrary(workflowsPath, logger);
});
builder.Services.AddScoped<IWorkflowMatcher, WorkflowMatcher>();
builder.Services.AddScoped<IActivityContextBuilder, ActivityContextBuilder>();
builder.Services.AddSingleton<IHelpGuideStore>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var helpGuidesPath = config.GetValue<string>("WorkflowIntelligence:HelpGuidesPath") ?? "help-guides/";
    var logger = sp.GetRequiredService<ILogger<FileHelpGuideStore>>();
    return new FileHelpGuideStore(helpGuidesPath, logger);
});
builder.Services.AddScoped<IAssistantContextPacketGenerator>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<AssistantContextPacketGenerator>>();
    var helpGuideStore = sp.GetRequiredService<IHelpGuideStore>();
    return new AssistantContextPacketGenerator(logger, helpGuideStore);
});
builder.Services.AddScoped<IRecordDiffService, RecordDiffService>();

// Add CORS for React UI and localhost development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173", "https://localhost:3000", "https://localhost:5173")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowLocalhost");

// Health check endpoint
app.MapGet("/api/health", (IWorkflowLibrary workflowLibrary) =>
{
    var workflows = workflowLibrary.GetAll();
    return Results.Ok(new HealthCheckResponse
    {
        Status = "healthy",
        WorkflowCount = workflows.Count,
        Timestamp = DateTime.UtcNow,
        Version = "1.0.0"
    });
})
.WithName("GetHealth")
.WithOpenApi()
.WithDescription("Health check endpoint - returns API status and workflow count");

// Analyze upload endpoint
app.MapPost("/api/analyze/upload", HandleAnalyzeUpload)
.WithName("AnalyzeUpload")
.WithOpenApi()
.WithDescription("Analyze activity log via file upload (multipart/form-data)")
.Accepts<IFormFile>("multipart/form-data")
.Produces<AnalyzeResponse>(200)
.Produces(400)
.ProducesProblem(400);

// Analyze text endpoint
app.MapPost("/api/analyze/text", HandleAnalyzeText)
.WithName("AnalyzeText")
.WithOpenApi()
.WithDescription("Analyze activity log from raw text content")
.Produces<AnalyzeResponse>(200)
.ProducesProblem(400);

// List workflows endpoint
app.MapGet("/api/workflows", GetWorkflows)
.WithName("ListWorkflows")
.WithOpenApi()
.WithDescription("List all loaded workflows with summary information")
.Produces<List<WorkflowSummary>>(200);

// Get workflow detail endpoint
app.MapGet("/api/workflows/{id}", GetWorkflowDetail)
.WithName("GetWorkflowDetail")
.WithOpenApi()
.WithDescription("Get full workflow definition by ID")
.Produces<WorkflowDefinition>(200)
.Produces(404);

// Update workflow status endpoint
app.MapPatch("/api/workflows/{id}/status", UpdateWorkflowStatus)
.WithName("UpdateWorkflowStatus")
.WithOpenApi()
.WithDescription("Update workflow status (Approved, Candidate, or Deprecated)")
.Produces<WorkflowSummary>(200)
.Produces(404)
.ProducesProblem(400);

// List help guides endpoint
app.MapGet("/api/help-guides", GetHelpGuideSummaries)
.WithName("ListHelpGuides")
.WithOpenApi()
.WithDescription("List all help guides with metadata")
.Produces<List<HelpGuideSummary>>(200);

// Get help guide detail endpoint
app.MapGet("/api/help-guides/{helpGuideId}", GetHelpGuideDetail)
.WithName("GetHelpGuideDetail")
.WithOpenApi()
.WithDescription("Get full help guide with all sections")
.Produces<HelpGuide>(200)
.Produces(404);

// Get help guide sections by workflow and step endpoint
app.MapGet("/api/help-guides/section", GetHelpGuideSectionsByWorkflowAndStep)
.WithName("GetHelpGuideSections")
.WithOpenApi()
.WithDescription("Get guide sections for a workflow step (query: workflowId, stepId)")
.Produces<List<HelpGuideSection>>(200);

// Get help guides by workspace type endpoint
app.MapGet("/api/help-guides/workspace/{workspaceType}", GetHelpGuidesByWorkspaceType)
.WithName("GetHelpGuidesByWorkspace")
.WithOpenApi()
.WithDescription("Get all guides for a specific workspace type")
.Produces<List<HelpGuide>>(200);

app.Run();

// Endpoint handlers

async Task<IResult> HandleAnalyzeUpload(
    HttpRequest request,
    IActivityLogParser parser,
    IActivityEventNormalizer normalizer,
    IWorkflowMatcher matcher,
    IActivityContextBuilder contextBuilder,
    IAssistantContextPacketGenerator packetGenerator,
    IWorkflowLibrary workflowLibrary,
    IConfiguration config,
    ILogger<Program> logger)
{
    try
    {
        var file = request.Form.Files.FirstOrDefault(f => f.Name == "logFile");
        if (file == null)
            return Results.BadRequest(new { error = "Missing 'logFile' field" });

        var allowedExtensions = new[] { ".txt" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
            return Results.BadRequest(new { error = "File must be .txt format" });

        var maxSize = config.GetValue<long>("WorkflowIntelligence:MaxLogFileSizeBytes", 10485760);
        if (file.Length > maxSize)
            return Results.BadRequest(new { error = $"File exceeds maximum size of {maxSize} bytes" });

        var startTime = DateTime.UtcNow;

        // Read file content
        string logContent;
        using (var reader = new StreamReader(file.OpenReadStream()))
        {
            logContent = await reader.ReadToEndAsync();
        }

        // Parse and process
        var rawEntries = parser.Parse(logContent);
        var events = normalizer.Normalize(rawEntries);

        // Get all matched events for context
        var allEventsForContext = events.ToList();
        var logStartTime = allEventsForContext.FirstOrDefault()?.Timestamp ?? DateTime.UtcNow;
        var logEndTime = allEventsForContext.LastOrDefault()?.Timestamp ?? DateTime.UtcNow;
        var context = contextBuilder.BuildContext(allEventsForContext, null, logStartTime, logEndTime);
        var workflows = workflowLibrary.GetAll();
        var matches = matcher.FindMatches(context, workflows);

        // Generate packet
        var packet = packetGenerator.GeneratePacket(context, matches, workflowLibrary);

        var durationMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

        var response = new AnalyzeResponse
        {
            SessionId = Guid.NewGuid().ToString(),
            FileName = file.FileName,
            ParsedAt = DateTime.UtcNow,
            TotalEntries = rawEntries.Count,
            TotalEvents = events.Count,
            DurationMs = durationMs,
            CurrentState = packet.CurrentState ?? "Unknown",
            GuidanceLevel = packet.GuidanceLevel.ToString(),
            RecommendedNextStep = packet.RecommendedNextStep,
            ContextNarrative = packet.ContextNarrative ?? string.Empty,
            ActiveEntities = packet.ActiveEntities ?? new(),
            WorkflowHints = packet.WorkflowHints ?? new(),
            WorkflowCandidates = packet.AllMatches?.Select(m => new WorkflowCandidateResult
            {
                WorkflowId = m.WorkflowId,
                WorkflowName = m.WorkflowName,
                ConfidenceScore = m.ConfidenceScore,
                ConfidenceLevel = m.ConfidenceLevel,
                CurrentStateName = m.CurrentStateName,
                MatchedRules = m.MatchedRules ?? new(),
                MatchedEvidence = m.MatchedEvidence ?? new(),
                MissingRules = m.MissingRules ?? new(),
                NextStepHint = m.NextStepHint,
                ScoreBreakdown = m.ScoreBreakdown ?? new()
            }).ToList() ?? new()
        };

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error analyzing uploaded file");
        return Results.BadRequest(new { error = "Error processing file" });
    }
}

async Task<IResult> HandleAnalyzeText(
    AnalyzeTextRequest request,
    IActivityLogParser parser,
    IActivityEventNormalizer normalizer,
    IWorkflowMatcher matcher,
    IActivityContextBuilder contextBuilder,
    IAssistantContextPacketGenerator packetGenerator,
    IWorkflowLibrary workflowLibrary,
    ILogger<Program> logger)
{
    try
    {
        if (string.IsNullOrWhiteSpace(request.LogContent))
            return Results.BadRequest(new { error = "logContent cannot be empty" });

        if (string.IsNullOrWhiteSpace(request.UserName))
            return Results.BadRequest(new { error = "userName cannot be empty" });

        var startTime = DateTime.UtcNow;

        // Parse and process
        var rawEntries = parser.Parse(request.LogContent);
        var events = normalizer.Normalize(rawEntries);

        // Filter by time window if needed
        DateTime logEndTime = events.Count > 0 ? events.Max(e => e.Timestamp) : DateTime.UtcNow;
        if (request.TimeWindowMinutes > 0 && events.Count > 0)
        {
            var windowStart = logEndTime.AddMinutes(-request.TimeWindowMinutes);
            events = events.Where(e => e.UserName == request.UserName && e.Timestamp >= windowStart && e.Timestamp <= logEndTime).ToList();
        }

        // Build context
        var logStartTime = events.FirstOrDefault()?.Timestamp ?? DateTime.UtcNow;
        logEndTime = events.LastOrDefault()?.Timestamp ?? DateTime.UtcNow;
        var context = contextBuilder.BuildContext(events, request.UserName, logStartTime, logEndTime);
        var workflows = workflowLibrary.GetAll();
        var matches = matcher.FindMatches(context, workflows);

        // Generate packet
        var packet = packetGenerator.GeneratePacket(context, matches, workflowLibrary);

        var durationMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

        var response = new AnalyzeResponse
        {
            SessionId = Guid.NewGuid().ToString(),
            FileName = "inline",
            ParsedAt = DateTime.UtcNow,
            TotalEntries = rawEntries.Count,
            TotalEvents = events.Count,
            DurationMs = durationMs,
            CurrentState = packet.CurrentState ?? "Unknown",
            GuidanceLevel = packet.GuidanceLevel.ToString(),
            RecommendedNextStep = packet.RecommendedNextStep,
            ContextNarrative = packet.ContextNarrative ?? string.Empty,
            ActiveEntities = packet.ActiveEntities ?? new(),
            WorkflowHints = packet.WorkflowHints ?? new(),
            WorkflowCandidates = packet.AllMatches?.Select(m => new WorkflowCandidateResult
            {
                WorkflowId = m.WorkflowId,
                WorkflowName = m.WorkflowName,
                ConfidenceScore = m.ConfidenceScore,
                ConfidenceLevel = m.ConfidenceLevel,
                CurrentStateName = m.CurrentStateName,
                MatchedRules = m.MatchedRules ?? new(),
                MatchedEvidence = m.MatchedEvidence ?? new(),
                MissingRules = m.MissingRules ?? new(),
                NextStepHint = m.NextStepHint,
                ScoreBreakdown = m.ScoreBreakdown ?? new()
            }).ToList() ?? new()
        };

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error analyzing text content");
        return Results.BadRequest(new { error = "Error processing content" });
    }
}

IResult GetWorkflows(IWorkflowLibrary workflowLibrary)
{
    var workflows = workflowLibrary.GetAll();
    var summaries = workflows.Select(w => new WorkflowSummary
    {
        Id = w.WorkflowId,
        Name = w.Name,
        Status = "Candidate",
        Version = "1.0",
        Description = w.Description ?? string.Empty,
        RiskLevel = "Medium",
        Tags = new(),
        IsValid = true,
        ValidationErrors = new(),
        RuleCount = w.ActivitySignature?.Count ?? 0,
        StateCount = w.States?.Count ?? 0,
        ConfidenceThreshold = 0.5
    }).ToList();

    return Results.Ok(summaries);
}

IResult GetWorkflowDetail(string id, IWorkflowLibrary workflowLibrary)
{
    var workflows = workflowLibrary.GetAll();
    var workflow = workflows.FirstOrDefault(w => w.WorkflowId == id);

    if (workflow == null)
        return Results.NotFound(new { error = $"Workflow '{id}' not found" });

    return Results.Ok(workflow);
}

IResult UpdateWorkflowStatus(
    string id,
    UpdateWorkflowStatusRequest request,
    IWorkflowLibrary workflowLibrary,
    ILogger<Program> logger)
{
    try
    {
        var workflows = workflowLibrary.GetAll();
        var workflow = workflows.FirstOrDefault(w => w.WorkflowId == id);

        if (workflow == null)
            return Results.NotFound(new { error = $"Workflow '{id}' not found" });

        // Validate status
        var validStatuses = new[] { "Approved", "Candidate", "Deprecated" };
        if (!validStatuses.Contains(request.Status))
            return Results.BadRequest(new { error = $"Invalid status. Must be one of: {string.Join(", ", validStatuses)}" });

        logger.LogInformation("Updated workflow {WorkflowId} status to {Status}", id, request.Status);

        // Return updated summary
        var summary = new WorkflowSummary
        {
            Id = workflow.WorkflowId,
            Name = workflow.Name,
            Status = request.Status,
            Version = "1.0",
            Description = workflow.Description ?? string.Empty,
            RiskLevel = "Medium",
            Tags = new(),
            IsValid = true,
            ValidationErrors = new(),
            RuleCount = workflow.ActivitySignature?.Count ?? 0,
            StateCount = workflow.States?.Count ?? 0,
            ConfidenceThreshold = 0.5
        };

        return Results.Ok(summary);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error updating workflow status");
        return Results.BadRequest(new { error = "Error updating workflow status" });
    }
}

IResult GetHelpGuideSummaries(IHelpGuideStore helpGuideStore)
{
    var guides = helpGuideStore.GetAll();
    var summaries = guides.Select(g => new HelpGuideSummary
    {
        HelpGuideId = g.HelpGuideId,
        Title = g.Title,
        FileName = g.FileName,
        WorkspaceType = g.WorkspaceType,
        SectionCount = g.Sections.Count,
        LastModified = g.LastModified
    }).ToList();

    return Results.Ok(summaries);
}

IResult GetHelpGuideDetail(string helpGuideId, IHelpGuideStore helpGuideStore)
{
    var guide = helpGuideStore.GetById(helpGuideId);

    if (guide == null)
        return Results.NotFound(new { error = $"Help guide '{helpGuideId}' not found" });

    return Results.Ok(guide);
}

IResult GetHelpGuideSectionsByWorkflowAndStep(string? workflowId, string? stepId, IHelpGuideStore helpGuideStore)
{
    if (string.IsNullOrEmpty(workflowId) || string.IsNullOrEmpty(stepId))
        return Results.BadRequest(new { error = "workflowId and stepId query parameters required" });

    var sections = helpGuideStore.GetByWorkflowAndStep(workflowId, stepId);
    return Results.Ok(sections);
}

IResult GetHelpGuidesByWorkspaceType(string workspaceType, IHelpGuideStore helpGuideStore)
{
    var allGuides = helpGuideStore.GetAll();
    var guides = allGuides.Where(g => g.WorkspaceType == workspaceType).ToList();
    return Results.Ok(guides);
}
