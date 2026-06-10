using Aktavara.WorkflowIntelligence.Core.Interfaces;
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
services.AddScoped<IWorkflowMatcher, WorkflowMatcher>();
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
    Console.WriteLine("  parse <logfile>          Parse activity log file");
    Console.WriteLine("  analyze <logfile>        Analyze log file (parse, extract, normalize, diff)");
    Console.WriteLine("  match <logfile>          Match workflows from activity log (TODO)");
    Console.WriteLine("  context <id> <logfile>   Build assistant context (TODO)");
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
    var parser = sp.GetRequiredService<IActivityLogParser>();
    var normalizer = sp.GetRequiredService<IActivityEventNormalizer>();

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
