using Aktavara.WorkflowIntelligence.Core.Interfaces;
using Aktavara.WorkflowIntelligence.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole());
services.AddScoped<IActivityLogParser, ActivityLogParser>();
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
        Console.WriteLine($"Parsed {entries.Count} log entries");

        if (entries.Count > 0)
        {
            Console.WriteLine("\nFirst entry:");
            var first = entries[0];
            Console.WriteLine($"  Timestamp: {first.Timestamp}");
            Console.WriteLine($"  User: {first.UserName}");
            Console.WriteLine($"  Session: {first.SessionId}");
            Console.WriteLine($"  Direction: {first.Direction}");
            Console.WriteLine($"  Action: {first.ActionName}");
            Console.WriteLine($"  XML Payloads: {first.RawXmlPayloads.Count}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
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
