using Aktavara.WorkflowIntelligence.Api.Services;
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
builder.Services.AddSingleton<ISemanticWorkflowSearch>(sp =>
{
    var workflowLibrary = sp.GetRequiredService<IWorkflowLibrary>();
    var helpGuideStore = sp.GetRequiredService<IHelpGuideStore>();
    return new KeywordSemanticWorkflowSearch(workflowLibrary, helpGuideStore);
});
builder.Services.AddScoped<IAssistantContextPacketGenerator>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<AssistantContextPacketGenerator>>();
    var helpGuideStore = sp.GetRequiredService<IHelpGuideStore>();
    var semanticSearch = sp.GetRequiredService<ISemanticWorkflowSearch>();
    return new AssistantContextPacketGenerator(logger, helpGuideStore, semanticSearch);
});
builder.Services.AddScoped<IRecordDiffService, RecordDiffService>();
builder.Services.AddScoped<IIntelligentHelpGuideMatcher, IntelligentHelpGuideMatcher>();
builder.Services.AddScoped<IHelpGuideMappingWriter, HelpGuideMappingWriter>();
builder.Services.AddScoped<IOfflineDiscoveryService, OfflineDiscoveryService>();
builder.Services.AddHttpClient<IntelligentHelpGuideMatcher>();
builder.Services.AddScoped<IWorkshopQuestionGenerator, WorkshopQuestionGenerator>();
builder.Services.AddHttpClient<WorkshopQuestionGenerator>();

// Add CORS for React UI and localhost development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins(
            "http://localhost:3000",
            "http://localhost:5173",
            "http://localhost:5174",
            "https://localhost:3000",
            "https://localhost:5173",
            "https://localhost:5174")
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

// Reload workflows endpoint (development only)
app.MapPost("/api/workflows/reload", ReloadWorkflows)
.WithName("ReloadWorkflows")
.WithOpenApi()
.WithDescription("Force reload of all workflow definitions from disk (development only)")
.Produces<ReloadResponse>(200)
.ProducesProblem(403);

// Backfill workshop questions endpoint (development only)
app.MapPost("/api/workflows/backfill-questions", BackfillWorkshopQuestions)
.WithName("BackfillWorkshopQuestions")
.WithOpenApi()
.WithDescription("Backfill workshop questions for all workflows (development only)")
.Produces<object>(200)
.ProducesProblem(403);

// Backfill guide mappings endpoint (development only)
app.MapPost("/api/workflows/backfill-guide-mappings", BackfillGuideMappings)
.WithName("BackfillGuideMappings")
.WithOpenApi()
.WithDescription("Auto-suggest and backfill guide mappings for all workflow states (development only)")
.Produces<object>(200)
.ProducesProblem(403);

// Get workflows library endpoint
app.MapGet("/api/workflows/library", GetWorkflowsLibrary)
.WithName("GetWorkflowsLibrary")
.WithOpenApi()
.WithDescription("Get all workflows in library with metadata")
.Produces<List<WorkflowLibraryItem>>(200);

// Infer workflow endpoint
app.MapPost("/api/workflows/infer", InferWorkflow)
.WithName("InferWorkflow")
.WithOpenApi()
.WithDescription("Infer a workflow from activity logs")
.Produces<InferredWorkflowSuggestion>(200)
.ProducesProblem(400);

// Infer workflow name endpoint
app.MapPost("/api/workflows/infer/name", InferWorkflowName)
.WithName("InferWorkflowName")
.WithOpenApi()
.WithDescription("Get LLM-suggested name and description for inferred workflow")
.Produces<InferredNameSuggestion>(200)
.ProducesProblem(503);

// Create workflow endpoint
app.MapPost("/api/workflows", CreateWorkflow)
.WithName("CreateWorkflow")
.WithOpenApi()
.WithDescription("Create a new workflow")
.Produces<WorkflowLibraryItem>(201)
.ProducesProblem(400);

// Update workflow endpoint
app.MapPut("/api/workflows/{id}", UpdateWorkflow)
.WithName("UpdateWorkflow")
.WithOpenApi()
.WithDescription("Update an existing workflow")
.Produces<WorkflowLibraryItem>(200)
.ProducesProblem(400)
.Produces(404);

// Generate workshop questions for workflow endpoint
app.MapPost("/api/workflows/{id}/generate-questions", GenerateWorkflowQuestions)
.WithName("GenerateWorkflowQuestions")
.WithOpenApi()
.WithDescription("Generate workshop questions for all states in a workflow")
.Produces<WorkflowDefinition>(200)
.ProducesProblem(400)
.Produces(404);

// Delete workflow endpoint
app.MapDelete("/api/workflows/{id}", DeleteWorkflow)
.WithName("DeleteWorkflow")
.WithOpenApi()
.WithDescription("Delete a workflow")
.Produces(204)
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

// Suggest help guide mapping endpoint
app.MapPost("/api/help-guides/suggest", SuggestGuideMapping)
.WithName("SuggestGuideMapping")
.WithOpenApi()
.WithDescription("Get LLM-suggested help guide for a workflow step")
.Produces<GuideSuggestion>(200)
.ProducesProblem(400);

// Save help guide mapping endpoint
app.MapPost("/api/help-guides/mapping", SaveGuideMapping)
.WithName("SaveGuideMapping")
.WithOpenApi()
.WithDescription("Save a help guide mapping for a workflow step")
.Produces(200)
.ProducesProblem(400);

// Log Anthropic API key status for diagnostics
var apiKey = builder.Configuration["Anthropic:ApiKey"];
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Anthropic API key loaded: {Key}", apiKey?[..10] + "..." ?? "NOT SET");

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
    Console.WriteLine("\n=== [Upload] STARTING FILE UPLOAD HANDLER ===");
    logger.LogInformation("[Upload] Starting file upload analysis");

    try
    {
        var file = request.Form.Files.FirstOrDefault(f => f.Name == "logFile");
        if (file == null)
            return Results.BadRequest(new { error = "Missing 'logFile' field" });

        Console.WriteLine($"[Upload] File received: {file.FileName} ({file.Length} bytes)");
        logger.LogInformation("[Upload] File received: {FileName} ({FileSize} bytes)", file.FileName, file.Length);

        var allowedExtensions = new[] { ".txt" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
            return Results.BadRequest(new { error = "File must be .txt format" });

        var maxSize = config.GetValue<long>("WorkflowIntelligence:MaxLogFileSizeBytes", 10485760);
        if (file.Length > maxSize)
            return Results.BadRequest(new { error = $"File exceeds maximum size of {maxSize} bytes" });

        var startTime = DateTime.UtcNow;

        // Read file content
        Console.WriteLine("[Upload] Reading file content...");
        logger.LogInformation("[Upload] Reading file content");
        string logContent;
        try
        {
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                logContent = await reader.ReadToEndAsync();
            }
            Console.WriteLine($"[Upload] File content read: {logContent.Length} characters");
            logger.LogInformation("[Upload] File content read: {ContentLength} characters", logContent.Length);
        }
        catch (Exception readEx)
        {
            Console.WriteLine($"[Upload] ERROR READING FILE: {readEx.GetType().Name}: {readEx.Message}");
            logger.LogError(readEx, "[Upload] ERROR READING FILE");
            throw;
        }

        // Extract user question if provided
        var userQuestion = request.Form.TryGetValue("userQuestion", out var questionValues)
            ? questionValues.FirstOrDefault()
            : null;

        // Parse and process
        Console.WriteLine("[Upload] Parsing log entries...");
        logger.LogInformation("[Upload] Parsing log entries");
        IReadOnlyList<RawActivityLogEntry> rawEntries;
        try
        {
            rawEntries = parser.Parse(logContent);
            Console.WriteLine($"[Upload] Parsed {rawEntries.Count} raw entries");
            logger.LogInformation("[Upload] Parsed {EntryCount} raw entries", rawEntries.Count);
        }
        catch (Exception parseEx)
        {
            Console.WriteLine($"[Upload] ERROR PARSING: {parseEx.GetType().Name}: {parseEx.Message}\n{parseEx.StackTrace}");
            logger.LogError(parseEx, "[Upload] ERROR PARSING");
            throw;
        }

        Console.WriteLine("[Upload] Normalizing events...");
        logger.LogInformation("[Upload] Normalizing events");
        IReadOnlyList<ActivityEvent> events;
        try
        {
            events = normalizer.Normalize(rawEntries);
            Console.WriteLine($"[Upload] Normalized to {events.Count} activity events");
            logger.LogInformation("[Upload] Normalized to {EventCount} activity events", events.Count);
        }
        catch (Exception normEx)
        {
            Console.WriteLine($"[Upload] ERROR NORMALIZING: {normEx.GetType().Name}: {normEx.Message}\n{normEx.StackTrace}");
            logger.LogError(normEx, "[Upload] ERROR NORMALIZING");
            throw;
        }

        // Get all matched events for context
        var allEventsForContext = events.ToList();
        var logEndTime = allEventsForContext.LastOrDefault()?.Timestamp ?? DateTime.UtcNow;

        // For uploaded logs, apply time window relative to last event (not current time)
        var timeWindowMinutes = config.GetValue<int>("WorkflowIntelligence:TimeWindowMinutes", 30);
        var logStartTime = logEndTime.AddMinutes(-timeWindowMinutes);

        Console.WriteLine($"[Upload] Time window: {logStartTime:HH:mm:ss} to {logEndTime:HH:mm:ss} ({timeWindowMinutes} min window, {allEventsForContext.Count} total events)");
        logger.LogInformation("[Upload] Time window: {Start:HH:mm:ss} to {End:HH:mm:ss}", logStartTime, logEndTime);

        // Filter events to those within the time window and for the detected user
        var detectedUser = allEventsForContext.LastOrDefault()?.UserName;
        var windowedEvents = allEventsForContext
            .Where(e => e.Timestamp >= logStartTime && e.Timestamp <= logEndTime && (detectedUser == null || e.UserName == detectedUser))
            .ToList();

        Console.WriteLine($"[Upload] Windowed to {windowedEvents.Count} events (user: {detectedUser ?? "unknown"})");
        logger.LogInformation("[Upload] Windowed to {EventCount} events", windowedEvents.Count);

        Console.WriteLine("[Upload] Building activity context...");
        logger.LogInformation("[Upload] Building activity context");
        ActivityContext context;
        try
        {
            context = contextBuilder.BuildContext(windowedEvents, detectedUser, logStartTime, logEndTime);
            Console.WriteLine($"[Upload] Activity context built");
            logger.LogInformation("[Upload] Activity context built");
        }
        catch (Exception contextEx)
        {
            Console.WriteLine($"[Upload] ERROR BUILDING CONTEXT: {contextEx.GetType().Name}: {contextEx.Message}\n{contextEx.StackTrace}");
            logger.LogError(contextEx, "[Upload] ERROR BUILDING CONTEXT");
            throw;
        }

        Console.WriteLine("[Upload] Matching workflows...");
        logger.LogInformation("[Upload] Matching workflows");
        IReadOnlyList<WorkflowDefinition> workflows;
        IReadOnlyList<WorkflowMatchResult> matches;
        try
        {
            workflows = workflowLibrary.GetAll();
            matches = matcher.FindMatches(context, workflows);
            Console.WriteLine($"[Upload] Found {matches.Count} workflow matches");
            logger.LogInformation("[Upload] Found {MatchCount} workflow matches", matches.Count);
        }
        catch (Exception matchEx)
        {
            Console.WriteLine($"[Upload] ERROR MATCHING: {matchEx.GetType().Name}: {matchEx.Message}\n{matchEx.StackTrace}");
            logger.LogError(matchEx, "[Upload] ERROR MATCHING");
            throw;
        }

        // Generate packet
        Console.WriteLine("[Upload] Generating assistant context packet...");
        logger.LogInformation("[Upload] Generating assistant context packet");
        AssistantContextPacket packet;
        try
        {
            packet = packetGenerator.GeneratePacket(context, matches, workflowLibrary, userQuestion);
            Console.WriteLine($"[Upload] Assistant context packet generated");
            logger.LogInformation("[Upload] Assistant context packet generated");
        }
        catch (Exception packetEx)
        {
            Console.WriteLine($"[Upload] ERROR GENERATING PACKET: {packetEx.GetType().Name}: {packetEx.Message}\n{packetEx.StackTrace}");
            logger.LogError(packetEx, "[Upload] ERROR GENERATING PACKET");
            throw;
        }

        var durationMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

        Console.WriteLine("[Upload] Building response...");
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
            SemanticMatches = packet.SemanticMatches ?? new(),
            Ambiguity = packet.Ambiguity,
            WorkflowCandidates = packet.AllMatches?.Select(m =>
            {
                var workshopQuestions = new List<string>();
                try
                {
                    var workflow = workflows.FirstOrDefault(w => w.WorkflowId == m.WorkflowId);
                    Console.WriteLine($"[Upload] Workflow: {m.WorkflowId}, CurrentStateName: '{m.CurrentStateName}'");
                    if (workflow?.States != null && !string.IsNullOrEmpty(m.CurrentStateName))
                    {
                        var normalizedStateName = NormalizeStateId(m.CurrentStateName);
                        Console.WriteLine($"[Upload] Normalized: '{m.CurrentStateName}' → '{normalizedStateName}'");
                        Console.WriteLine($"[Upload] Available states: {string.Join(", ", workflow.States.Select(s => s.StateId))}");
                        var state = workflow.States.FirstOrDefault(s => s.StateId == normalizedStateName);
                        if (state != null)
                        {
                            Console.WriteLine($"[Upload] ✓ State matched: {state.StateId} (name: {state.Name})");
                            if (state.WorkshopQuestions != null && state.WorkshopQuestions.Count > 0)
                            {
                                workshopQuestions = state.WorkshopQuestions;
                                Console.WriteLine($"[Upload] ✓ Extracted {workshopQuestions.Count} questions from state");
                            }
                            else
                            {
                                Console.WriteLine($"[Upload] ✗ No workshopQuestions on state");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[Upload] ✗ State NOT matched for '{normalizedStateName}'");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[Upload] ✗ Workflow states null or CurrentStateName empty");
                    }
                }
                catch (Exception wsEx)
                {
                    Console.WriteLine($"[Upload] Warning: Failed to extract workshop questions: {wsEx.Message}");
                }

                return new WorkflowCandidateResult
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
                    ScoreBreakdown = m.ScoreBreakdown ?? new(),
                    WorkshopQuestions = workshopQuestions
                };
            }).ToList() ?? new()
        };

        Console.WriteLine($"[Upload] SUCCESS: Response ready ({durationMs}ms)");
        logger.LogInformation("[Upload] Analysis complete, returning response");
        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        Console.WriteLine("\n╔══════════════════════════════════════════════════════╗");
        Console.WriteLine("║           [Upload] UNHANDLED EXCEPTION               ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════╝");
        Console.WriteLine($"Exception Type: {ex.GetType().FullName}");
        Console.WriteLine($"Message: {ex.Message}");
        Console.WriteLine($"Stack Trace:\n{ex.StackTrace}");

        var innerException = ex.InnerException;
        var depth = 1;
        while (innerException != null && depth < 5)
        {
            Console.WriteLine($"\n--- Inner Exception {depth} ---");
            Console.WriteLine($"Type: {innerException.GetType().FullName}");
            Console.WriteLine($"Message: {innerException.Message}");
            Console.WriteLine($"Stack: {innerException.StackTrace}");
            innerException = innerException.InnerException;
            depth++;
        }
        Console.WriteLine("╚══════════════════════════════════════════════════════╝\n");

        logger.LogError(ex, "[Upload] CRITICAL ERROR: {ExceptionType}: {ExceptionMessage}", ex.GetType().FullName, ex.Message);
        logger.LogError("[Upload] Stack Trace: {StackTrace}", ex.StackTrace);

        return Results.BadRequest(new { error = "Error processing file", details = ex.Message });
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

        var startTime = DateTime.UtcNow;

        // Parse and process
        var rawEntries = parser.Parse(request.LogContent);
        var events = normalizer.Normalize(rawEntries);
        var userName = request.UserName;
        var userNameSpecified = !string.IsNullOrEmpty(request.UserName);

        Console.WriteLine($"[Text] AutoDetectUser={request.AutoDetectUser}, UserName='{request.UserName}', TimeWindowMinutes={request.TimeWindowMinutes}");
        Console.WriteLine($"[Text] Total events in log: {events.Count}");

        // Determine user and filter events
        DateTime logEndTime = events.Count > 0 ? events.Max(e => e.Timestamp) : DateTime.UtcNow;
        var windowStart = logEndTime.AddMinutes(-request.TimeWindowMinutes);

        // Step 1: Filter by specified userName if provided
        if (userNameSpecified)
        {
            var userEvents = events.Where(e => e.UserName == request.UserName).ToList();

            if (request.TimeWindowMinutes > 0)
            {
                userEvents = userEvents.Where(e => e.Timestamp >= windowStart && e.Timestamp <= logEndTime).ToList();
            }

            if (userEvents.Count == 0)
            {
                Console.WriteLine($"[Text] No events found for user '{request.UserName}', falling back to all users in log");
                events = events;  // Use all events
                userNameSpecified = false;  // Mark that we didn't find the specified user
            }
            else
            {
                events = userEvents;
            }
        }
        else
        {
            // No userName specified, apply time window only
            if (request.TimeWindowMinutes > 0 && events.Count > 0)
            {
                events = events.Where(e => e.Timestamp >= windowStart && e.Timestamp <= logEndTime).ToList();
            }
        }

        // Step 2: Auto-detect user if enabled and we don't have a specified user with matches
        if (request.AutoDetectUser && !userNameSpecified && events.Count > 0)
        {
            var mostActiveUser = events
                .GroupBy(e => e.UserName)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()
                ?.Key;

            if (!string.IsNullOrEmpty(mostActiveUser) && mostActiveUser != userName)
            {
                Console.WriteLine($"[Text] AutoDetectUser enabled: using most active user '{mostActiveUser}' with {events.Count(e => e.UserName == mostActiveUser)} events");
                userName = mostActiveUser;
                events = events.Where(e => e.UserName == mostActiveUser).ToList();
            }
        }

        // Build context
        var logStartTime = events.FirstOrDefault()?.Timestamp ?? DateTime.UtcNow;
        logEndTime = events.LastOrDefault()?.Timestamp ?? DateTime.UtcNow;
        var context = contextBuilder.BuildContext(events, userName, logStartTime, logEndTime);
        var workflows = workflowLibrary.GetAll();

        Console.WriteLine($"[Text] Loaded {workflows.Count} workflows");
        foreach (var wf in workflows)
        {
            Console.WriteLine($"[Text]   {wf.WorkflowId}: {wf.States.Count} states - {string.Join(", ", wf.States.Select(s => s.StateId))}");
        }

        var matches = matcher.FindMatches(context, workflows);

        // Generate packet
        var packet = packetGenerator.GeneratePacket(context, matches, workflowLibrary, request.UserQuestion);

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
            SemanticMatches = packet.SemanticMatches ?? new(),
            Ambiguity = packet.Ambiguity,
            WorkflowCandidates = packet.AllMatches?.Select(m =>
            {
                var workshopQuestions = new List<string>();
                try
                {
                    var workflow = workflows.FirstOrDefault(w => w.WorkflowId == m.WorkflowId);
                    Console.WriteLine($"[Text] Workflow: {m.WorkflowId}, CurrentStateName: '{m.CurrentStateName}'");
                    if (workflow?.States != null && !string.IsNullOrEmpty(m.CurrentStateName))
                    {
                        var normalizedStateName = NormalizeStateId(m.CurrentStateName);
                        Console.WriteLine($"[Text] Normalized: '{m.CurrentStateName}' → '{normalizedStateName}'");
                        Console.WriteLine($"[Text] Available states: {string.Join(", ", workflow.States.Select(s => s.StateId))}");
                        var state = workflow.States.FirstOrDefault(s => s.StateId == normalizedStateName);
                        if (state != null)
                        {
                            Console.WriteLine($"[Text] ✓ State matched: {state.StateId} (name: {state.Name})");
                            if (state.WorkshopQuestions != null && state.WorkshopQuestions.Count > 0)
                            {
                                workshopQuestions = state.WorkshopQuestions;
                                Console.WriteLine($"[Text] ✓ Extracted {workshopQuestions.Count} questions from state");
                            }
                            else
                            {
                                Console.WriteLine($"[Text] ✗ No workshopQuestions on state");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[Text] ✗ State NOT matched for '{normalizedStateName}'");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[Text] ✗ Workflow states null or CurrentStateName empty");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Text] Warning: Failed to extract workshop questions: {ex.Message}");
                }

                return new WorkflowCandidateResult
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
                    ScoreBreakdown = m.ScoreBreakdown ?? new(),
                    WorkshopQuestions = workshopQuestions
                };
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
        Status = w.Status.ToString(),
        Version = w.Version ?? "1.0",
        Description = w.Description ?? string.Empty,
        RiskLevel = w.Metadata?.ContainsKey("riskLevel") == true ? w.Metadata["riskLevel"]?.ToString() ?? "Medium" : "Medium",
        Tags = w.Tags ?? new(),
        IsValid = true,
        ValidationErrors = new(),
        RuleCount = w.ActivitySignature?.Count ?? 0,
        StateCount = w.States?.Count ?? 0,
        ConfidenceThreshold = w.MinimumConfidenceThreshold
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
    IConfiguration config,
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

        // Map UI status to workflow JSON status
        var statusMap = new Dictionary<string, string>
        {
            { "Approved", "Active" },
            { "Candidate", "Draft" },
            { "Deprecated", "Deprecated" }
        };
        var jsonStatus = statusMap[request.Status];

        // Update workflow JSON file
        var workflowsPath = config.GetValue<string>("WorkflowIntelligence:WorkflowsPath") ?? "workflows";
        var workflowFile = Path.Combine(workflowsPath, $"{id}.workflow.json");

        if (!File.Exists(workflowFile))
            return Results.NotFound(new { error = $"Workflow file '{workflowFile}' not found" });

        try
        {
            var jsonContent = File.ReadAllText(workflowFile);
            using (var doc = System.Text.Json.JsonDocument.Parse(jsonContent))
            {
                var root = doc.RootElement.Clone();
                var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };

                // Parse as mutable object and update
                var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent, options);
                if (dict != null)
                {
                    dict["status"] = jsonStatus;
                    dict["lastModifiedDate"] = DateTime.UtcNow.ToString("O");

                    var updatedJson = System.Text.Json.JsonSerializer.Serialize(dict, options);
                    File.WriteAllText(workflowFile, updatedJson);
                }
                else
                {
                    throw new InvalidOperationException("Failed to deserialize workflow JSON");
                }
            }
        }
        catch (Exception fileEx)
        {
            logger.LogError(fileEx, "Failed to update workflow file {WorkflowFile}: {Error}", workflowFile, fileEx.Message);
            return Results.BadRequest(new { error = "Failed to persist workflow status change" });
        }

        logger.LogInformation("Updated workflow {WorkflowId} status to {Status}", id, request.Status);

        // Return updated summary
        var summary = new WorkflowSummary
        {
            Id = workflow.WorkflowId,
            Name = workflow.Name,
            Status = request.Status,
            Version = workflow.Version ?? "1.0",
            Description = workflow.Description ?? string.Empty,
            RiskLevel = workflow.Metadata?.ContainsKey("riskLevel") == true ? workflow.Metadata["riskLevel"]?.ToString() ?? "Medium" : "Medium",
            Tags = workflow.Tags ?? new(),
            IsValid = true,
            ValidationErrors = new(),
            RuleCount = workflow.ActivitySignature?.Count ?? 0,
            StateCount = workflow.States?.Count ?? 0,
            ConfidenceThreshold = workflow.MinimumConfidenceThreshold
        };

        return Results.Ok(summary);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error updating workflow status");
        return Results.BadRequest(new { error = "Error updating workflow status" });
    }
}

IResult ReloadWorkflows(
    IWorkflowLibrary workflowLibrary,
    IHostEnvironment env,
    ILogger<Program> logger)
{
    // Only allow in development mode
    if (!env.IsDevelopment())
        return Results.StatusCode(StatusCodes.Status403Forbidden);

    try
    {
        var before = workflowLibrary.GetAll().Count;

        // Force reload by calling GetAll which should trigger fresh load
        var workflows = workflowLibrary.GetAll();
        var after = workflows.Count;

        logger.LogInformation("Reloaded {WorkflowCount} workflows", after);

        var response = new ReloadResponse
        {
            ReloadedCount = after,
            Timestamp = DateTime.UtcNow,
            LoadedWorkflowIds = workflows.Select(w => w.WorkflowId).ToList()
        };

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error reloading workflows");
        return Results.BadRequest(new { error = "Error reloading workflows" });
    }
}

async Task<IResult> BackfillWorkshopQuestions(
    IWorkflowLibrary workflowLibrary,
    IWorkshopQuestionGenerator questionGenerator,
    IHostEnvironment env,
    ILogger<Program> logger)
{
    // Only allow in development mode
    if (!env.IsDevelopment())
        return Results.StatusCode(StatusCodes.Status403Forbidden);

    try
    {
        var workflows = workflowLibrary.GetAll();
        int workflowsUpdated = 0;
        int statesUpdated = 0;

        foreach (var workflow in workflows)
        {
            bool hasChanges = false;

            if (workflow.States != null && workflow.States.Count > 0)
            {
                var riskLevel = workflow.Metadata?.ContainsKey("riskLevel") == true
                    ? workflow.Metadata["riskLevel"]?.ToString() ?? "Medium"
                    : "Medium";
                var tags = workflow.Tags ?? new();
                var rules = workflow.ActivitySignature?.Select(r => r.Description).ToList() ?? new();

                foreach (var state in workflow.States)
                {
                    if (state.WorkshopQuestions == null || state.WorkshopQuestions.Count == 0)
                    {
                        try
                        {
                            var questions = await questionGenerator.GenerateQuestionsAsync(
                                workflow.Name,
                                state.StateId,
                                state.Name,
                                state.Description,
                                rules,
                                tags,
                                riskLevel);

                            state.WorkshopQuestions = questions;
                            statesUpdated++;
                            hasChanges = true;
                            logger.LogInformation("Generated {QuestionCount} questions for state {StateId}", questions.Count, state.StateId);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error generating questions for state {StateId}", state.StateId);
                        }
                    }
                }

                if (hasChanges)
                {
                    await workflowLibrary.SaveWorkflowAsync(workflow);
                    workflowsUpdated++;
                }
            }
        }

        return Results.Ok(new
        {
            updated = workflowsUpdated,
            states = statesUpdated
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error backfilling workshop questions");
        return Results.BadRequest(new { error = "Error backfilling workshop questions" });
    }
}

async Task<IResult> BackfillGuideMappings(
    IWorkflowLibrary workflowLibrary,
    IIntelligentHelpGuideMatcher guideMatcher,
    IHelpGuideMappingWriter mappingWriter,
    IHostEnvironment env,
    ILogger<Program> logger)
{
    // Only allow in development mode
    if (!env.IsDevelopment())
        return Results.StatusCode(StatusCodes.Status403Forbidden);

    try
    {
        var workflows = workflowLibrary.GetAll();
        int workflowsUpdated = 0;
        int mappingsAdded = 0;

        foreach (var workflow in workflows)
        {
            if (workflow.States != null && workflow.States.Count > 0)
            {
                var rules = workflow.ActivitySignature?.Select(r => r.Description).ToList() ?? new();
                var evidence = new List<string>();

                foreach (var state in workflow.States)
                {
                    try
                    {
                        var suggestion = await guideMatcher.SuggestAsync(
                            workflow.WorkflowId,
                            workflow.Name,
                            state.StateId,
                            state.Name,
                            rules,
                            evidence);

                        if (suggestion != null)
                        {
                            await mappingWriter.SaveMappingAsync(
                                workflow.WorkflowId,
                                state.StateId,
                                suggestion.SuggestedGuideFile,
                                suggestion.SuggestedSectionId,
                                isAutoGenerated: true);

                            mappingsAdded++;
                            logger.LogInformation("Backfilled guide mapping: {WorkflowId}:{StateId} -> {GuideFile}",
                                workflow.WorkflowId, state.StateId, suggestion.SuggestedGuideFile);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error backfilling guide mapping for state {StateId}", state.StateId);
                    }
                }

                if (mappingsAdded > 0)
                {
                    workflowsUpdated++;
                }
            }
        }

        return Results.Ok(new
        {
            workflows = workflowsUpdated,
            mappingsAdded = mappingsAdded
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error backfilling guide mappings");
        return Results.BadRequest(new { error = "Error backfilling guide mappings" });
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

async Task<IResult> SuggestGuideMapping(
    SuggestGuideMappingRequest request,
    IIntelligentHelpGuideMatcher matcher,
    ILogger<Program> logger)
{
    try
    {
        if (string.IsNullOrEmpty(request.WorkflowId) || string.IsNullOrEmpty(request.StepId))
            return Results.BadRequest(new { error = "workflowId and stepId are required" });

        var suggestion = await matcher.SuggestAsync(
            request.WorkflowId,
            request.WorkflowName,
            request.StepId,
            request.CurrentStateName,
            request.MatchedRules ?? new List<string>(),
            request.MatchedEvidence ?? new List<string>());

        if (suggestion == null)
            return Results.BadRequest(new { error = "Failed to generate guide suggestion" });

        return Results.Ok(suggestion);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error suggesting guide mapping");
        return Results.BadRequest(new { error = "Error suggesting guide mapping" });
    }
}

async Task<IResult> SaveGuideMapping(
    SaveGuideMappingRequest request,
    IHelpGuideMappingWriter writer,
    ILogger<Program> logger)
{
    try
    {
        if (string.IsNullOrEmpty(request.WorkflowId) || string.IsNullOrEmpty(request.StepId) || string.IsNullOrEmpty(request.GuideFile))
            return Results.BadRequest(new { error = "workflowId, stepId, and guideFile are required" });

        await writer.SaveMappingAsync(request.WorkflowId, request.StepId, request.GuideFile, request.SectionId);

        return Results.Ok();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error saving guide mapping");
        return Results.BadRequest(new { error = "Error saving guide mapping" });
    }
}

IResult GetWorkflowsLibrary(IWorkflowLibrary workflowLibrary)
{
    var workflows = workflowLibrary.GetAll();
    var items = workflows.Select(w => new WorkflowLibraryItem
    {
        Id = w.WorkflowId,
        Name = w.Name,
        Status = w.Status.ToString(),
        Version = w.Version ?? "1.0",
        RiskLevel = w.Metadata?.ContainsKey("riskLevel") == true ? w.Metadata["riskLevel"]?.ToString() ?? "Medium" : "Medium",
        Tags = w.Tags ?? new(),
        Description = w.Description ?? string.Empty,
        IsValid = true,
        ValidationErrors = new(),
        RuleCount = w.ActivitySignature?.Count ?? 0,
        StateCount = w.States?.Count ?? 0,
        LastModified = DateTime.UtcNow,
        FileName = $"{w.WorkflowId}.workflow.json"
    }).ToList();

    return Results.Ok(items);
}

async Task<IResult> InferWorkflow(
    InferWorkflowRequest request,
    IActivityLogParser parser,
    IActivityEventNormalizer normalizer,
    IOfflineDiscoveryService discoveryService,
    ILogger<Program> logger)
{
    try
    {
        if (string.IsNullOrEmpty(request.RawLogContent))
            return Results.BadRequest(new { error = "rawLogContent is required" });

        var rawEntries = parser.Parse(request.RawLogContent);
        var events = normalizer.Normalize(rawEntries);

        var suggestion = await discoveryService.InferWorkflowSuggestionAsync(events, request.CandidateWorkflowId);

        return Results.Ok(suggestion);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error inferring workflow");
        return Results.BadRequest(new { error = "Error inferring workflow" });
    }
}

async Task<IResult> InferWorkflowName(
    InferredWorkflowSuggestion suggestion,
    IConfiguration config,
    ILogger<Program> logger)
{
    try
    {
        var apiKey = config.GetValue<string>("Anthropic:ApiKey");
        if (string.IsNullOrEmpty(apiKey))
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);

        var rulesText = string.Join("\n", suggestion.SuggestedRules.Select(r => $"- {r.Description}"));
        var statesText = string.Join(", ", suggestion.SuggestedStates.Select(s => s.Name));
        var tagsText = string.Join(", ", suggestion.SuggestedTags);

        var prompt = $@"Workflow steps detected:
{rulesText}

States: {statesText}
Tags: {tagsText}
Risk level: {suggestion.SuggestedRiskLevel}

Suggest a short business-friendly name (3-5 words) and a one-sentence description for this workflow.";

        var request = new
        {
            model = "claude-sonnet-4-6",
            max_tokens = 300,
            messages = new object[]
            {
                new { role = "user", content = prompt }
            },
            system = "You are helping name Aktavara workflows detected from activity logs. Respond only with JSON: { \"name\": \"...\", \"description\": \"...\", \"alternativeNames\": [\"...\", \"...\"] }"
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
        {
            Content = System.Net.Http.Json.JsonContent.Create(request)
        };

        httpRequest.Headers.Add("x-api-key", apiKey);
        httpRequest.Headers.Add("anthropic-version", "2023-06-01");

        using var httpClient = new HttpClient();
        var response = await httpClient.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(responseContent);
        var root = doc.RootElement;

        if (!root.TryGetProperty("content", out var contentArray) || contentArray.GetArrayLength() == 0)
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);

        var firstContent = contentArray[0];
        if (!firstContent.TryGetProperty("text", out var textElement))
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);

        var text = textElement.GetString() ?? "";
        var jsonStart = text.IndexOf('{');
        var jsonEnd = text.LastIndexOf('}');

        if (jsonStart < 0 || jsonEnd < 0)
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);

        var jsonStr = text[jsonStart..(jsonEnd + 1)];
        using var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonStr);
        var nameData = jsonDoc.RootElement;

        var result = new InferredNameSuggestion
        {
            SuggestedName = nameData.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
            SuggestedDescription = nameData.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "",
            AlternativeNames = nameData.TryGetProperty("alternativeNames", out var alt) ?
                alt.EnumerateArray().Select(a => a.GetString() ?? "").ToList() : new()
        };

        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error inferring workflow name");
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
}

async Task<IResult> CreateWorkflow(
    WorkflowDefinition workflow,
    IWorkflowLibrary workflowLibrary,
    IWorkshopQuestionGenerator questionGenerator,
    IIntelligentHelpGuideMatcher guideMatcher,
    IHelpGuideMappingWriter mappingWriter,
    ILogger<Program> logger)
{
    try
    {
        if (string.IsNullOrEmpty(workflow.WorkflowId))
            return Results.BadRequest(new { error = "WorkflowId is required" });

        if (string.IsNullOrEmpty(workflow.Name))
            return Results.BadRequest(new { error = "Name is required" });

        var success = await workflowLibrary.SaveWorkflowAsync(workflow);
        if (!success)
            return Results.BadRequest(new { error = "Failed to save workflow" });

        // Generate workshop questions for states with empty workshopQuestions
        await GenerateWorkshopQuestionsForWorkflow(workflow, questionGenerator, workflowLibrary, logger);

        // Auto-suggest guide mappings for states
        await AutoSuggestGuideMapingsForWorkflow(workflow, guideMatcher, mappingWriter, logger);

        var item = new WorkflowLibraryItem
        {
            Id = workflow.WorkflowId,
            Name = workflow.Name,
            Status = workflow.Status.ToString(),
            Version = workflow.Version ?? "1.0",
            RiskLevel = workflow.Metadata?.ContainsKey("riskLevel") == true ? workflow.Metadata["riskLevel"]?.ToString() ?? "Medium" : "Medium",
            Tags = workflow.Tags ?? new(),
            Description = workflow.Description ?? string.Empty,
            IsValid = true,
            ValidationErrors = new(),
            RuleCount = workflow.ActivitySignature?.Count ?? 0,
            StateCount = workflow.States?.Count ?? 0,
            LastModified = DateTime.UtcNow,
            FileName = $"{workflow.WorkflowId}.workflow.json"
        };

        return Results.Created($"/api/workflows/{workflow.WorkflowId}", item);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error creating workflow");
        return Results.BadRequest(new { error = "Error creating workflow" });
    }
}

async Task<IResult> UpdateWorkflow(
    string id,
    WorkflowDefinition workflow,
    IWorkflowLibrary workflowLibrary,
    IWorkshopQuestionGenerator questionGenerator,
    IIntelligentHelpGuideMatcher guideMatcher,
    IHelpGuideMappingWriter mappingWriter,
    ILogger<Program> logger)
{
    try
    {
        if (string.IsNullOrEmpty(id))
            return Results.BadRequest(new { error = "Workflow ID is required" });

        // Ensure the ID matches
        workflow.WorkflowId = id;

        if (string.IsNullOrEmpty(workflow.Name))
            return Results.BadRequest(new { error = "Name is required" });

        var success = await workflowLibrary.SaveWorkflowAsync(workflow);
        if (!success)
            return Results.BadRequest(new { error = "Failed to update workflow" });

        // Generate workshop questions for states with empty workshopQuestions
        await GenerateWorkshopQuestionsForWorkflow(workflow, questionGenerator, workflowLibrary, logger);

        // Auto-suggest guide mappings for states
        await AutoSuggestGuideMapingsForWorkflow(workflow, guideMatcher, mappingWriter, logger);

        var item = new WorkflowLibraryItem
        {
            Id = workflow.WorkflowId,
            Name = workflow.Name,
            Status = workflow.Status.ToString(),
            Version = workflow.Version ?? "1.0",
            RiskLevel = workflow.Metadata?.ContainsKey("riskLevel") == true ? workflow.Metadata["riskLevel"]?.ToString() ?? "Medium" : "Medium",
            Tags = workflow.Tags ?? new(),
            Description = workflow.Description ?? string.Empty,
            IsValid = true,
            ValidationErrors = new(),
            RuleCount = workflow.ActivitySignature?.Count ?? 0,
            StateCount = workflow.States?.Count ?? 0,
            LastModified = DateTime.UtcNow,
            FileName = $"{workflow.WorkflowId}.workflow.json"
        };

        return Results.Ok(item);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error updating workflow");
        return Results.BadRequest(new { error = "Error updating workflow" });
    }
}

async Task<IResult> GenerateWorkflowQuestions(
    string id,
    string? stateId,
    IWorkflowLibrary workflowLibrary,
    IWorkshopQuestionGenerator questionGenerator,
    ILogger<Program> logger)
{
    try
    {
        if (string.IsNullOrEmpty(id))
            return Results.BadRequest(new { error = "Workflow ID is required" });

        var workflow = workflowLibrary.GetById(id);
        if (workflow == null)
            return Results.NotFound(new { error = "Workflow not found" });

        if (!string.IsNullOrEmpty(stateId))
        {
            await GenerateWorkshopQuestionsForState(workflow, stateId, questionGenerator, workflowLibrary, logger);
        }
        else
        {
            await GenerateWorkshopQuestionsForWorkflow(workflow, questionGenerator, workflowLibrary, logger);
        }

        return Results.Ok(workflow);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error generating questions for workflow {WorkflowId}", id);
        return Results.BadRequest(new { error = "Error generating questions" });
    }
}

async Task<IResult> DeleteWorkflow(
    string id,
    IWorkflowLibrary workflowLibrary,
    ILogger<Program> logger)
{
    try
    {
        if (string.IsNullOrEmpty(id))
            return Results.BadRequest(new { error = "Workflow ID is required" });

        var success = await workflowLibrary.DeleteWorkflowAsync(id);
        if (!success)
            return Results.BadRequest(new { error = "Failed to delete workflow" });

        return Results.NoContent();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error deleting workflow");
        return Results.BadRequest(new { error = "Error deleting workflow" });
    }
}

async Task GenerateWorkshopQuestionsForWorkflow(
    WorkflowDefinition workflow,
    IWorkshopQuestionGenerator questionGenerator,
    IWorkflowLibrary workflowLibrary,
    ILogger<Program> logger)
{
    if (workflow.States == null || workflow.States.Count == 0)
        return;

    var riskLevel = workflow.Metadata?.ContainsKey("riskLevel") == true
        ? workflow.Metadata["riskLevel"]?.ToString() ?? "Medium"
        : "Medium";
    var tags = workflow.Tags ?? new();
    var rules = workflow.ActivitySignature?.Select(r => r.Description).ToList() ?? new();

    bool hasChanges = false;
    foreach (var state in workflow.States)
    {
        if (state.WorkshopQuestions == null || state.WorkshopQuestions.Count == 0)
        {
            try
            {
                var questions = await questionGenerator.GenerateQuestionsAsync(
                    workflow.Name,
                    state.StateId,
                    state.Name,
                    state.Description,
                    rules,
                    tags,
                    riskLevel);

                state.WorkshopQuestions = questions;
                logger.LogInformation("Generated {QuestionCount} questions for state {StateId}", questions.Count, state.StateId);
                hasChanges = true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error generating questions for state {StateId}", state.StateId);
            }
        }
    }

    if (hasChanges)
    {
        await workflowLibrary.SaveWorkflowAsync(workflow);
    }
}

async Task GenerateWorkshopQuestionsForState(
    WorkflowDefinition workflow,
    string stateId,
    IWorkshopQuestionGenerator questionGenerator,
    IWorkflowLibrary workflowLibrary,
    ILogger<Program> logger)
{
    if (workflow.States == null || workflow.States.Count == 0)
        return;

    var state = workflow.States.FirstOrDefault(s => s.StateId == stateId);
    if (state == null)
    {
        logger.LogWarning("State {StateId} not found in workflow {WorkflowId}", stateId, workflow.WorkflowId);
        return;
    }

    var riskLevel = workflow.Metadata?.ContainsKey("riskLevel") == true
        ? workflow.Metadata["riskLevel"]?.ToString() ?? "Medium"
        : "Medium";
    var tags = workflow.Tags ?? new();
    var rules = workflow.ActivitySignature?.Select(r => r.Description).ToList() ?? new();

    try
    {
        var questions = await questionGenerator.GenerateQuestionsAsync(
            workflow.Name,
            state.StateId,
            state.Name,
            state.Description,
            rules,
            tags,
            riskLevel);

        state.WorkshopQuestions = questions;
        logger.LogInformation("Generated {QuestionCount} questions for state {StateId}", questions.Count, state.StateId);

        await workflowLibrary.SaveWorkflowAsync(workflow);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error generating questions for state {StateId}", state.StateId);
    }
}

async Task AutoSuggestGuideMapingsForWorkflow(
    WorkflowDefinition workflow,
    IIntelligentHelpGuideMatcher guideMatcher,
    IHelpGuideMappingWriter mappingWriter,
    ILogger<Program> logger)
{
    if (workflow.States == null || workflow.States.Count == 0)
        return;

    var rules = workflow.ActivitySignature?.Select(r => r.Description).ToList() ?? new();
    var evidence = new List<string>();

    foreach (var state in workflow.States)
    {
        try
        {
            var suggestion = await guideMatcher.SuggestAsync(
                workflow.WorkflowId,
                workflow.Name,
                state.StateId,
                state.Name,
                rules,
                evidence);

            if (suggestion != null)
            {
                await mappingWriter.SaveMappingAsync(
                    workflow.WorkflowId,
                    state.StateId,
                    suggestion.SuggestedGuideFile,
                    suggestion.SuggestedSectionId,
                    isAutoGenerated: true);

                logger.LogInformation("Auto-suggested guide for {WorkflowId}:{StateId} -> {GuideFile}",
                    workflow.WorkflowId, state.StateId, suggestion.SuggestedGuideFile);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error auto-suggesting guide mapping for state {StateId}", state.StateId);
        }
    }
}

static string NormalizeStateId(string stepId)
{
    return stepId
        .ToLowerInvariant()
        .Trim()
        .Replace(" ", "_")
        .Replace("-", "_");
}
