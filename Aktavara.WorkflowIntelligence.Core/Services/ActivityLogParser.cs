using System.Text.RegularExpressions;
using Aktavara.WorkflowIntelligence.Core.Interfaces;
using Aktavara.WorkflowIntelligence.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aktavara.WorkflowIntelligence.Core.Services;

/// <summary>
/// Parses Aktavara user activity logs in the standard format:
/// [timestamp] username (sessionid): Direction: ActionName
/// Input/Output:
/// &lt;xml content&gt;
/// </summary>
public class ActivityLogParser : IActivityLogParser
{
    private readonly ILogger<ActivityLogParser> _logger;

    // Regex pattern for log entry header: [timestamp] username (sessionid): Direction: ActionName
    private static readonly Regex LogEntryHeaderPattern = new(
        @"^\s*\[(\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2})\]\s+(\S+)\s+\((\d+)\):\s+(Request|Response):\s+(.+?)$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    // Regex pattern for Input/Output blocks
    private static readonly Regex InputOutputPattern = new(
        @"^\s*(Input|Output):\s*$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    public ActivityLogParser(ILogger<ActivityLogParser> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Parses raw log text into activity log entries.
    /// </summary>
    public IReadOnlyList<RawActivityLogEntry> Parse(string logContent)
    {
        _logger.LogInformation("Parsing activity log content");

        if (string.IsNullOrWhiteSpace(logContent))
        {
            _logger.LogInformation("Log content is empty");
            return Array.Empty<RawActivityLogEntry>();
        }

        var entries = new List<RawActivityLogEntry>();

        // Split content into potential log entry blocks
        var logBlocks = SplitLogBlocks(logContent);

        foreach (var block in logBlocks)
        {
            if (string.IsNullOrWhiteSpace(block))
                continue;

            var entry = ParseLogBlock(block);
            if (entry != null)
            {
                entries.Add(entry);
            }
        }

        _logger.LogInformation("Parsed {Count} activity log entries", entries.Count);
        return entries.AsReadOnly();
    }

    /// <summary>
    /// Parses activity log from a file.
    /// </summary>
    public async Task<IReadOnlyList<RawActivityLogEntry>> ParseFileAsync(string filePath)
    {
        _logger.LogInformation("Parsing activity log file: {FilePath}", filePath);

        if (!File.Exists(filePath))
        {
            _logger.LogError("Log file not found: {FilePath}", filePath);
            throw new FileNotFoundException($"Log file not found: {filePath}");
        }

        var content = await File.ReadAllTextAsync(filePath);
        return Parse(content);
    }

    /// <summary>
    /// Splits log content into individual log entry blocks.
    /// A log block starts with a timestamp line and extends until the next timestamp or end of file.
    /// </summary>
    private static List<string> SplitLogBlocks(string logContent)
    {
        var blocks = new List<string>();
        var lines = logContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        var currentBlock = new List<string>();

        foreach (var line in lines)
        {
            // Check if this line starts a new log entry (contains a timestamp)
            if (IsLogEntryStart(line))
            {
                // Save previous block if it has content
                if (currentBlock.Count > 0)
                {
                    blocks.Add(string.Join("\n", currentBlock));
                }
                currentBlock = new List<string> { line };
            }
            else
            {
                currentBlock.Add(line);
            }
        }

        // Don't forget the last block
        if (currentBlock.Count > 0)
        {
            blocks.Add(string.Join("\n", currentBlock));
        }

        return blocks;
    }

    /// <summary>
    /// Determines if a line starts a new log entry.
    /// </summary>
    private static bool IsLogEntryStart(string line)
    {
        return LogEntryHeaderPattern.IsMatch(line);
    }

    /// <summary>
    /// Parses a single log block into a RawActivityLogEntry.
    /// </summary>
    private RawActivityLogEntry? ParseLogBlock(string block)
    {
        var lines = block.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        if (lines.Length == 0)
            return null;

        // Parse the header line
        var headerLine = lines[0];
        var match = LogEntryHeaderPattern.Match(headerLine);

        if (!match.Success)
        {
            _logger.LogWarning("Could not parse log entry header: {HeaderLine}", headerLine);
            return null;
        }

        var entry = new RawActivityLogEntry
        {
            Timestamp = DateTime.Parse(match.Groups[1].Value),
            UserName = match.Groups[2].Value,
            SessionId = match.Groups[3].Value,
            Direction = match.Groups[4].Value == "Request" ? EventType.RequestInitiated : EventType.ResponseReceived,
            ActionName = match.Groups[5].Value,
            RawText = block
        };

        // Extract XML payloads
        var xmlPayloads = ExtractXmlPayloads(lines);
        entry.RawXmlPayloads.AddRange(xmlPayloads);

        return entry;
    }

    /// <summary>
    /// Extracts all XML payloads from a set of log lines.
    /// XML payloads follow "Input:" or "Output:" markers.
    /// </summary>
    private static List<string> ExtractXmlPayloads(string[] lines)
    {
        var payloads = new List<string>();
        int i = 0;

        while (i < lines.Length)
        {
            var line = lines[i];

            // Look for Input/Output marker
            if (InputOutputPattern.IsMatch(line))
            {
                // Start collecting XML content
                i++;
                var xmlLines = new List<string>();

                // Collect lines until we hit an empty line followed by another marker or next log entry
                while (i < lines.Length)
                {
                    var currentLine = lines[i];

                    // Stop if we hit another Input/Output marker or a log entry header
                    if (InputOutputPattern.IsMatch(currentLine) || LogEntryHeaderPattern.IsMatch(currentLine))
                    {
                        break;
                    }

                    // Include the line if it has content
                    if (!string.IsNullOrWhiteSpace(currentLine))
                    {
                        xmlLines.Add(currentLine);
                    }
                    else if (xmlLines.Count > 0)
                    {
                        // Stop at blank line only if we've already collected some XML
                        // But check if next non-blank line is XML continuation
                        int j = i + 1;
                        while (j < lines.Length && string.IsNullOrWhiteSpace(lines[j]))
                            j++;

                        if (j < lines.Length && IsXmlLine(lines[j]))
                        {
                            xmlLines.Add(currentLine);
                        }
                        else
                        {
                            break;
                        }
                    }

                    i++;
                }

                if (xmlLines.Count > 0)
                {
                    var xmlContent = string.Join("\n", xmlLines).Trim();
                    payloads.Add(xmlContent);
                }
            }
            else
            {
                i++;
            }
        }

        return payloads;
    }

    /// <summary>
    /// Determines if a line is likely part of XML content.
    /// </summary>
    private static bool IsXmlLine(string line)
    {
        var trimmed = line.Trim();
        return trimmed.StartsWith("<?xml") ||
               trimmed.StartsWith("<") ||
               trimmed.EndsWith(">") ||
               trimmed.StartsWith("</");
    }
}
