using Aktavara.WorkflowIntelligence.Core.Models;

namespace Aktavara.WorkflowIntelligence.Core.Interfaces;

/// <summary>
/// Parses Aktavara user activity logs into structured RawActivityLogEntry objects.
/// The parser is deterministic and does not require external dependencies.
/// </summary>
public interface IActivityLogParser
{
    /// <summary>
    /// Parses raw log text content into a list of activity log entries.
    /// </summary>
    /// <param name="logContent">The raw log text to parse.</param>
    /// <returns>A read-only list of parsed activity log entries.</returns>
    IReadOnlyList<RawActivityLogEntry> Parse(string logContent);

    /// <summary>
    /// Parses activity log from a file.
    /// </summary>
    /// <param name="filePath">The path to the log file.</param>
    /// <returns>A read-only list of parsed activity log entries.</returns>
    Task<IReadOnlyList<RawActivityLogEntry>> ParseFileAsync(string filePath);
}

