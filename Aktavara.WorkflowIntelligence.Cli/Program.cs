using Aktavara.WorkflowIntelligence.Core.Interfaces;
using Aktavara.WorkflowIntelligence.Core.Models;
using Aktavara.WorkflowIntelligence.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole());
services.AddScoped<IActivityLogParser, ActivityLogParser>();
services.AddScoped<IAktaXmlExtractor, AktaXmlExtractor>();
services.AddScoped<IAktaJsonExtractor, AktaJsonExtractor>();
services.AddScoped<IRecordDiffService, RecordDiffService>();
services.AddScoped<IActivityEventNormalizer, ActivityEventNormalizer>();
services.AddScoped<IWorkflowProvider, StaticWorkflowProvider>();

// Register FileWorkflowLibrary with workflows directory path
// AppContext.BaseDirectory is: .../Cli/bin/Debug/net10.0
// We need: .../workflows
var workflowDirectory = Path.Combine(
    AppContext.BaseDirectory,
    "..", "..", "..", "..", "workflows"
);
var workflowDirectoryResolved = Path.GetFullPath(workflowDirectory);
services.AddScoped<IWorkflowLibrary>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<FileWorkflowLibrary>>();
    var library = new FileWorkflowLibrary(workflowDirectoryResolved, logger);
    // Load workflows synchronously
    library.LoadAsync().GetAwaiter().GetResult();
    return library;
});

services.AddScoped<IWorkflowMatcher, WorkflowMatcher>();
services.AddScoped<IActivityContextBuilder, ActivityContextBuilder>();
services.AddScoped<IAssistantContextPacketGenerator, AssistantContextPacketGenerator>();
services.AddScoped<IAssistantContextBuilder, AssistantContextBuilder>();

var serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

logger.LogInformation("Workflow Intelligence CLI started");

if (args.Length == 0)
{
    PrintUsage();
    return;
}

var command = args[0];
switch (command)
{
    case "parse":
        await ParseLogFile(serviceProvider, args);
        break;
    case "analyze":
        await AnalyzeLogFile(serviceProvider, args);
        break;
    case "guided":
        await GuidedMode(serviceProvider, args);
        break;
    case "list-workflows":
        await ListWorkflows(serviceProvider, args);
        break;
    case "validate":
        await ValidateWorkflows(serviceProvider, args);
        break;
    case "match":
        await MatchWorkflows(serviceProvider, args);
        break;
    case "context":
        await BuildContext(serviceProvider, args);
        break;
    default:
        Console.WriteLine($"Unknown command: {command}");
        PrintUsage();
        break;
}

void PrintUsage()
{
    Console.WriteLine("Workflow Intelligence CLI");
    Console.WriteLine("Usage:");
    Console.WriteLine("  parse <logfile>              Parse activity log file");
    Console.WriteLine("  analyze <logfile> [--verbose] Analyze log file with full diagnostics");
    Console.WriteLine("  guided --log <logfile>       Simulate runtime guidance mode");
    Console.WriteLine("           --user <username>   (--window <minutes> default 30)");
    Console.WriteLine("  list-workflows               Show all loaded workflows and status");
    Console.WriteLine("  validate <workflow-dir>      Validate all workflow JSON files");
}

async Task ParseLogFile(IServiceProvider sp, string[] cmdArgs)
{
    if (cmdArgs.Length < 2)
    {
        Console.WriteLine("Usage: parse <logfile>");
        return;
    }

    var filePath = cmdArgs[1];
    var parser = sp.GetRequiredService<IActivityLogParser>();

    try
    {
        var entries = await parser.ParseFileAsync(filePath);
        Console.WriteLine($"Parsed {entries.Count} log entries\n");

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            Console.WriteLine($"Entry {i + 1}:");
            Console.WriteLine($"  Timestamp: {entry.Timestamp}");
            Console.WriteLine($"  User: {entry.UserName}");
            Console.WriteLine($"  Session: {entry.SessionId}");
            Console.WriteLine($"  Direction: {entry.Direction}");
            Console.WriteLine($"  Action: {entry.ActionName}");
            Console.WriteLine($"  XML Payloads: {entry.RawXmlPayloads.Count}");
            Console.WriteLine();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

async Task AnalyzeLogFile(IServiceProvider sp, string[] cmdArgs)
{
    if (cmdArgs.Length < 2)
    {
        Console.WriteLine("Usage: analyze <logfile>");
        return;
    }

    var filePath = cmdArgs[1];
    var verbose = cmdArgs.Contains("--verbose");
    var parser = sp.GetRequiredService<IActivityLogParser>();
    var normalizer = sp.GetRequiredService<IActivityEventNormalizer>();
    var workflowProvider = sp.GetRequiredService<IWorkflowProvider>();
    var workflowMatcher = sp.GetRequiredService<IWorkflowMatcher>();
    var contextBuilder = sp.GetRequiredService<IActivityContextBuilder>();
    var packetGenerator = sp.GetRequiredService<IAssistantContextPacketGenerator>();
    var workflowLibrary = sp.GetRequiredService<IWorkflowLibrary>();

    try
    {
        var rawEntries = await parser.ParseFileAsync(filePath);
        Console.WriteLine($"Parsed {rawEntries.Count} log entries\n");

        var activityEvents = normalizer.Normalize(rawEntries);
        Console.WriteLine($"Normalized to {activityEvents.Count} activity events\n");
        Console.WriteLine("=".PadRight(100, '='));

        for (int i = 0; i < activityEvents.Count; i++)
        {
            var evt = activityEvents[i];
            Console.WriteLine($"\nEvent {i + 1}: {evt.GetSummary()}");
            Console.WriteLine($"  Event ID: {evt.EventId}");
            Console.WriteLine($"  Type: {evt.EventType}");
            Console.WriteLine($"  Timestamp: {evt.Timestamp}");
            Console.WriteLine($"  User: {evt.UserName}");
            Console.WriteLine($"  Session: {evt.SessionId}");
            Console.WriteLine($"  Success: {evt.IsSuccessful}");

            if (evt.RecordKind.HasValue)
                Console.WriteLine($"  Record Kind: {evt.RecordKind}");
            if (!string.IsNullOrEmpty(evt.RecordId))
                Console.WriteLine($"  Record ID: {evt.RecordId}");
            if (!string.IsNullOrEmpty(evt.RecordName))
                Console.WriteLine($"  Record Name: {evt.RecordName}");
            if (!string.IsNullOrEmpty(evt.TypeId))
                Console.WriteLine($"  Type ID: {evt.TypeId}");
            if (!string.IsNullOrEmpty(evt.RecordState))
                Console.WriteLine($"  Record State: {evt.RecordState}");
            if (!string.IsNullOrEmpty(evt.WorkspaceKind))
                Console.WriteLine($"  Workspace Kind: {evt.WorkspaceKind}");

            if (evt.RelatedRecordIds.Count > 0)
            {
                Console.WriteLine($"  Related Records ({evt.RelatedRecordIds.Count}):");
                foreach (var relatedId in evt.RelatedRecordIds.Take(5))
                {
                    Console.WriteLine($"    - {relatedId}");
                }
                if (evt.RelatedRecordIds.Count > 5)
                    Console.WriteLine($"    ... and {evt.RelatedRecordIds.Count - 5} more");
            }

            if (evt.ChangedAttributes.Count > 0)
            {
                Console.WriteLine($"  Changed Attributes ({evt.ChangedAttributes.Count}):");
                foreach (var attr in evt.ChangedAttributes)
                {
                    Console.WriteLine($"    - {attr.AttributeId}");
                    if (attr.FromValue != null)
                        Console.WriteLine($"      From: {attr.FromValue}");
                    if (attr.ToValue != null)
                        Console.WriteLine($"      To: {attr.ToValue}");
                    if (!string.IsNullOrEmpty(attr.ValueType))
                        Console.WriteLine($"      Type: {attr.ValueType}");
                }
            }

            if (evt.Evidence.Count > 0)
            {
                Console.WriteLine($"  Evidence:");
                foreach (var evidence in evt.Evidence)
                {
                    Console.WriteLine($"    - {evidence}");
                }
            }
        }

        Console.WriteLine("\n" + "=".PadRight(100, '='));
        Console.WriteLine("\nACTIVITY CONTEXT");
        Console.WriteLine("=".PadRight(100, '='));

        // Build activity context from events
        var userName = rawEntries.FirstOrDefault()?.UserName ?? "Unknown";
        var timeWindowStart = activityEvents.Count > 0 ? activityEvents.Min(e => e.Timestamp) : DateTime.UtcNow;
        var timeWindowEnd = activityEvents.Count > 0 ? activityEvents.Max(e => e.Timestamp) : DateTime.UtcNow;

        var context = contextBuilder.BuildContext(activityEvents, userName, timeWindowStart, timeWindowEnd);

        Console.WriteLine($"\nSummary: {context.Summary}");
        Console.WriteLine($"Current State: {context.CurrentState}");
        Console.WriteLine($"Session ID: {context.SessionId ?? "(none)"}");

        if (context.ActiveEntities.Count > 0)
        {
            Console.WriteLine("\nActive Entities:");
            foreach (var entity in context.ActiveEntities.Take(5))
            {
                Console.WriteLine($"  - {entity.RecordKind}: {entity.Name} (ID: {entity.RecordId}, Type: {entity.TypeId})");
            }
        }

        if (context.WorkflowHints.Count > 0)
        {
            Console.WriteLine("\nWorkflow Hints:");
            foreach (var hint in context.WorkflowHints)
            {
                Console.WriteLine($"  - {hint}");
            }
        }

        Console.WriteLine("\n" + "=".PadRight(100, '='));
        Console.WriteLine("\nWORKFLOW MATCHING RESULTS");
        Console.WriteLine("=".PadRight(100, '='));

        // Get all workflows and convert to definitions
        var parsedWorkflows = await workflowProvider.GetAllWorkflowsAsync();
        var workflowDefinitions = parsedWorkflows
            .Select(pw => new WorkflowDefinition
            {
                WorkflowId = pw.Id,
                Name = pw.Name,
                Description = pw.Description,
                ActivitySignature = new List<WorkflowSignatureRule>
                {
                    new()
                    {
                        EventType = EventType.SearchRecords,
                        Description = "Search records",
                        Weight = 0.3,
                        Required = false,
                        MissingPenalty = 0.1
                    },
                    new()
                    {
                        EventType = EventType.OpenWorkspace,
                        Description = "Open workspace",
                        Weight = 0.3,
                        Required = false,
                        MissingPenalty = 0.1
                    },
                    new()
                    {
                        EventType = EventType.SaveRecords,
                        Description = "Save records",
                        Weight = 0.4,
                        Required = true,
                        MissingPenalty = 0.2
                    }
                },
                States = new List<WorkflowStateDefinition>
                {
                    new()
                    {
                        StateId = "initial",
                        Name = "Initial",
                        Sequence = 0,
                        IsTerminal = false,
                        NextStateId = "active",
                        RequiredEvidence = new List<string> { }
                    },
                    new()
                    {
                        StateId = "active",
                        Name = "Active",
                        Sequence = 1,
                        IsTerminal = false,
                        NextStateId = "complete",
                        RequiredEvidence = new List<string> { "SearchRecords", "OpenWorkspace" }
                    },
                    new()
                    {
                        StateId = "complete",
                        Name = "Complete",
                        Sequence = 2,
                        IsTerminal = true,
                        RequiredEvidence = new List<string> { "SaveRecords" }
                    }
                }
            })
            .ToList();

        // Load real workflow definitions from library instead of using test data
        try
        {
            var libraryWorkflows = workflowLibrary.GetAll();
            if (libraryWorkflows.Count > 0)
            {
                workflowDefinitions = libraryWorkflows.ToList();
                Console.WriteLine($"Loaded {workflowDefinitions.Count} workflow definitions from library\n");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Note: Could not load workflows from library ({ex.Message}), using test definitions\n");
        }

        Console.WriteLine($"\nMatching against {workflowDefinitions.Count} workflow definitions...\n");

        // Find matches
        var matches = workflowMatcher.FindMatches(context, workflowDefinitions);

        if (matches.Count == 0)
        {
            Console.WriteLine("No workflows matched.");
        }
        else
        {
            // Display results sorted by confidence
            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                if (match.ConfidenceScore <= 0)
                    continue;

                Console.WriteLine($"\nMatch {i + 1}: {match.WorkflowName}");
                Console.WriteLine($"  Workflow ID: {match.WorkflowId}");
                Console.WriteLine($"  Confidence Score: {match.ConfidenceScore:P0}");
                Console.WriteLine($"  Confidence Level: {match.ConfidenceLevel}");

                if (!string.IsNullOrEmpty(match.CurrentStateName))
                {
                    Console.WriteLine($"  Current State: {match.CurrentStateName}");
                }

                // Display score breakdown
                var breakdown = match.ScoreBreakdown;
                Console.WriteLine($"\n  Score Breakdown:");
                Console.WriteLine($"    Matched Rules Weight: {breakdown.MatchedRulesWeight:F3}");
                Console.WriteLine($"    Missing Rules Penalty: -{breakdown.MissingRulesPenalty:F3}");
                Console.WriteLine($"    Sequence Bonus: +{breakdown.SequenceBonus:F3}");
                Console.WriteLine($"    Entity Correlation Bonus: +{breakdown.EntityCorrelationBonus:F3}");
                if (breakdown.StalenesssPenalty > 0)
                    Console.WriteLine($"    Staleness Penalty: -{breakdown.StalenesssPenalty:F3}");
                Console.WriteLine($"    Raw Score: {breakdown.RawScore:F3}");
                Console.WriteLine($"    Final Score (clamped): {breakdown.FinalScore:F3}");

                // Display rule scores
                if (match.RuleScores.Count > 0)
                {
                    Console.WriteLine($"\n  Rule Matches:");
                    foreach (var rule in match.RuleScores.OrderByDescending(r => r.Value))
                    {
                        Console.WriteLine($"    - {rule.Key}: {rule.Value:F3}");
                    }
                }

                if (breakdown.Details.Count > 0)
                {
                    Console.WriteLine($"\n  Details:");
                    foreach (var detail in breakdown.Details)
                    {
                        Console.WriteLine($"    - {detail.Key}: {detail.Value}");
                    }
                }

                if (match.MatchedEvidence.Count > 0)
                {
                    Console.WriteLine($"\n  Matched Evidence ({match.MatchedEvidence.Count}):");
                    foreach (var evidence in match.MatchedEvidence.Take(5))
                    {
                        Console.WriteLine($"    - {evidence.EventType}: {evidence.GetSummary()}");
                    }
                    if (match.MatchedEvidence.Count > 5)
                        Console.WriteLine($"    ... and {match.MatchedEvidence.Count - 5} more");
                }

                if (match.MissingEvidence.Count > 0)
                {
                    Console.WriteLine($"  Missing Evidence ({match.MissingEvidence.Count}):");
                    foreach (var missing in match.MissingEvidence.Take(3))
                    {
                        Console.WriteLine($"    - {missing}");
                    }
                    if (match.MissingEvidence.Count > 3)
                        Console.WriteLine($"    ... and {match.MissingEvidence.Count - 3} more");
                }
            }
        }

        // Generate and display AssistantContextPacket
        Console.WriteLine("\n" + "=".PadRight(100, '='));
        Console.WriteLine("\nASSISTANT CONTEXT PACKET");
        Console.WriteLine("=".PadRight(100, '='));

        try
        {
            var assistantPacket = packetGenerator.GeneratePacket(context, matches, workflowLibrary);

            Console.WriteLine($"\nGuidance Level: {assistantPacket.GuidanceLevel}");
            if (!string.IsNullOrEmpty(assistantPacket.RecommendedNextStep))
            {
                Console.WriteLine($"Recommended Next Step: {assistantPacket.RecommendedNextStep}");
            }

            Console.WriteLine($"\nContext Narrative:\n{assistantPacket.ContextNarrative}");

            // Display as JSON (for API integration in next prompt)
            if (verbose)
            {
                Console.WriteLine($"\nJSON Packet (for LLM API):");
                Console.WriteLine(assistantPacket.ToJson());
            }
        }
        catch (Exception packetEx)
        {
            Console.WriteLine($"Note: Could not generate assistant packet ({packetEx.Message})");
        }

        Console.WriteLine("\n" + "=".PadRight(100, '='));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        if (ex.InnerException != null)
            Console.WriteLine($"Inner Error: {ex.InnerException.Message}");
    }
}

async Task MatchWorkflows(IServiceProvider sp, string[] cmdArgs)
{
    if (cmdArgs.Length < 2)
    {
        Console.WriteLine("Usage: match <logfile>");
        return;
    }

    var filePath = cmdArgs[1];
    var parser = sp.GetRequiredService<IActivityLogParser>();

    try
    {
        var entries = await parser.ParseFileAsync(filePath);
        Console.WriteLine($"Parsed {entries.Count} log entries");
        Console.WriteLine("Workflow matching not yet implemented");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

async Task BuildContext(IServiceProvider sp, string[] cmdArgs)
{
    if (cmdArgs.Length < 3)
    {
        Console.WriteLine("Usage: context <workOrderId> <logfile>");
        return;
    }

    var workOrderId = cmdArgs[1];
    var filePath = cmdArgs[2];
    var parser = sp.GetRequiredService<IActivityLogParser>();

    try
    {
        var entries = await parser.ParseFileAsync(filePath);
        Console.WriteLine($"Parsed {entries.Count} log entries for work order {workOrderId}");
        Console.WriteLine("Context building not yet implemented");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

async Task GuidedMode(IServiceProvider sp, string[] cmdArgs)
{
    var logFile = ExtractArgValue(cmdArgs, "--log");
    var userName = ExtractArgValue(cmdArgs, "--user");
    var windowStr = ExtractArgValue(cmdArgs, "--window") ?? "30";

    if (string.IsNullOrEmpty(logFile) || string.IsNullOrEmpty(userName))
    {
        Console.WriteLine("Usage: guided --log <logfile> --user <username> [--window <minutes>]");
        return;
    }

    if (!int.TryParse(windowStr, out var windowMinutes))
        windowMinutes = 30;

    var parser = sp.GetRequiredService<IActivityLogParser>();
    var normalizer = sp.GetRequiredService<IActivityEventNormalizer>();
    var contextBuilder = sp.GetRequiredService<IActivityContextBuilder>();
    var workflowMatcher = sp.GetRequiredService<IWorkflowMatcher>();
    var packetGenerator = sp.GetRequiredService<IAssistantContextPacketGenerator>();
    var workflowLibrary = sp.GetRequiredService<IWorkflowLibrary>();

    try
    {
        var rawEntries = await parser.ParseFileAsync(logFile);
        var allEvents = normalizer.Normalize(rawEntries);

        // Filter to user and time window
        var now = allEvents.Count > 0 ? allEvents.Max(e => e.Timestamp) : DateTime.UtcNow;
        var windowStart = now.AddMinutes(-windowMinutes);
        var userEvents = allEvents
            .Where(e => e.UserName == userName && e.Timestamp >= windowStart && e.Timestamp <= now)
            .ToList();

        if (userEvents.Count == 0)
        {
            Console.WriteLine($"No events found for user '{userName}' in the last {windowMinutes} minutes");
            return;
        }

        // Build activity context
        var context = contextBuilder.BuildContext(
            userEvents,
            userName,
            userEvents.Min(e => e.Timestamp),
            userEvents.Max(e => e.Timestamp));

        // Get workflow definitions and find matches
        var workflowDefs = workflowLibrary.GetAll().ToList();
        var matches = workflowMatcher.FindMatches(context, workflowDefs);

        // Display guided mode output
        Console.WriteLine($"\n=== GUIDED ASSISTANCE MODE ===\n");
        Console.WriteLine($"User: {userName}");
        Console.WriteLine($"Time Window: Last {windowMinutes} minutes");
        Console.WriteLine($"Events in window: {userEvents.Count}\n");

        Console.WriteLine("=== CURRENT ACTIVITY ===");
        Console.WriteLine($"State: {context.CurrentState}");
        Console.WriteLine($"Summary: {context.Summary}\n");

        if (matches.Count > 0 && matches[0].ConfidenceScore > 0)
        {
            var bestMatch = matches[0];
            Console.WriteLine("=== DETECTED WORKFLOW ===");
            Console.WriteLine($"Workflow: {bestMatch.WorkflowName}");
            Console.WriteLine($"Confidence: {bestMatch.ConfidenceScore:P0}");
            Console.WriteLine($"Level: {bestMatch.ConfidenceLevel}\n");

            // Generate packet for guidance
            var packet = packetGenerator.GeneratePacket(context, matches, workflowLibrary);
            Console.WriteLine("=== GUIDANCE ===");
            Console.WriteLine($"Guidance Level: {packet.GuidanceLevel}");
            if (!string.IsNullOrEmpty(packet.RecommendedNextStep))
                Console.WriteLine($"Next Step: {packet.RecommendedNextStep}");
            Console.WriteLine($"\n{packet.ContextNarrative}");
        }
        else
        {
            Console.WriteLine("No matching workflows detected. Cannot provide guidance.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

async Task ListWorkflows(IServiceProvider sp, string[] cmdArgs)
{
    var workflowLibrary = sp.GetRequiredService<IWorkflowLibrary>();

    try
    {
        var workflows = workflowLibrary.GetAll();

        Console.WriteLine("\n=== LOADED WORKFLOWS ===\n");
        Console.WriteLine($"Total: {workflows.Count}\n");

        foreach (var workflow in workflows.OrderBy(w => w.Name))
        {
            Console.WriteLine($"ID: {workflow.WorkflowId}");
            Console.WriteLine($"Name: {workflow.Name}");
            Console.WriteLine($"Status: {workflow.Status}");
            Console.WriteLine($"Rules: {workflow.ActivitySignature.Count}");
            Console.WriteLine($"States: {workflow.States.Count}");

            var errors = workflowLibrary.GetValidationErrors(workflow.WorkflowId);
            if (errors.Count > 0)
            {
                Console.WriteLine($"⚠ Validation Errors:");
                foreach (var error in errors)
                    Console.WriteLine($"  - {error}");
            }
            else
            {
                Console.WriteLine("✓ Valid");
            }
            Console.WriteLine();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

async Task ValidateWorkflows(IServiceProvider sp, string[] cmdArgs)
{
    if (cmdArgs.Length < 2)
    {
        Console.WriteLine("Usage: validate <workflow-directory>");
        return;
    }

    var workflowDir = cmdArgs[1];

    try
    {
        if (!Directory.Exists(workflowDir))
        {
            Console.WriteLine($"Directory not found: {workflowDir}");
            return;
        }

        var workflowLibrary = sp.GetRequiredService<IWorkflowLibrary>();
        var workflows = workflowLibrary.GetAll();
        var workflowFiles = Directory.GetFiles(workflowDir, "*.workflow.json");

        Console.WriteLine($"\n=== WORKFLOW VALIDATION ===\n");
        Console.WriteLine($"Found {workflowFiles.Length} workflow files\n");

        int validCount = 0;
        int errorCount = 0;

        foreach (var file in workflowFiles.OrderBy(f => f))
        {
            var fileName = Path.GetFileName(file);
            var fileBase = Path.GetFileNameWithoutExtension(fileName).Replace(".workflow", "");
            var workflow = workflows.FirstOrDefault(w => w.WorkflowId == fileBase);

            if (workflow == null)
            {
                Console.WriteLine($"⚠ {fileName}: NOT LOADED");
                errorCount++;
                continue;
            }

            var errors = workflowLibrary.GetValidationErrors(workflow.WorkflowId);
            if (errors.Count > 0)
            {
                Console.WriteLine($"✗ {fileName}:");
                foreach (var error in errors)
                    Console.WriteLine($"    {error}");
                errorCount++;
            }
            else
            {
                Console.WriteLine($"✓ {fileName}");
                validCount++;
            }
        }

        Console.WriteLine($"\n=== SUMMARY ===");
        Console.WriteLine($"Valid: {validCount}");
        Console.WriteLine($"Errors: {errorCount}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

string? ExtractArgValue(string[] args, string argName)
{
    for (int i = 0; i < args.Length - 1; i++)
    {
        if (args[i] == argName)
            return args[i + 1];
    }
    return null;
}
