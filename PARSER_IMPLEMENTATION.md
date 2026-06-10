# Aktavara Activity Log Parser Implementation

## Overview

A deterministic, dependency-free parser for Aktavara user activity logs. Converts raw log text into structured `RawActivityLogEntry` objects for further processing.

## Log Format

The parser expects logs in the following format:

```
[YYYY-MM-DD HH:MM:SS] username (sessionid): Direction: ActionName
Input/Output:
<?xml version="1.0" encoding="utf-16"?>
<XmlContent>...</XmlContent>
```

### Components

- **Timestamp**: `[2026-06-08 11:13:20]` - Date and time in ISO format
- **Username**: `istvan.vencz` - User identifier
- **SessionId**: `(17)` - Numeric session identifier in parentheses
- **Direction**: `Request` or `Response` - Type of activity
- **ActionName**: `Search records` - Description of the action (can contain spaces)
- **Payloads**: `Input:` or `Output:` followed by XML content

## Implementation Details

### `IActivityLogParser` Interface

```csharp
public interface IActivityLogParser
{
    IReadOnlyList<RawActivityLogEntry> Parse(string logContent);
    Task<IReadOnlyList<RawActivityLogEntry>> ParseFileAsync(string filePath);
}
```

### `ActivityLogParser` Implementation

**Key Features:**

1. **Deterministic Parsing**: Uses compiled regex patterns for consistent results
2. **No External Dependencies**: Only uses standard .NET libraries
3. **Multi-line XML Support**: Handles XML payloads that span multiple lines
4. **Multiple Payloads**: Supports multiple Input/Output blocks per log entry
5. **Blank Line Tolerance**: Handles blank lines gracefully
6. **Raw Preservation**: Stores exact XML text without modification

### Regex Patterns

1. **Log Entry Header**:
   ```
   ^\s*\[(\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2})\]\s+(\S+)\s+\((\d+)\):\s+(Request|Response):\s+(.+?)$
   ```
   - Captures: timestamp, username, sessionid, direction, actionname

2. **Input/Output Marker**:
   ```
   ^\s*(Input|Output):\s*$
   ```
   - Identifies payload blocks

### Parsing Algorithm

1. **Block Splitting**: Splits log content into individual entry blocks (each starts with a timestamp)
2. **Header Parsing**: Extracts metadata using regex on the first line
3. **Payload Extraction**: Collects all XML following Input/Output markers
4. **Payload Boundaries**: Stops at:
   - Another Input/Output marker
   - A new log entry header
   - End of file

## Data Structure

### RawActivityLogEntry

```csharp
public class RawActivityLogEntry
{
    public DateTime Timestamp { get; set; }
    public string UserName { get; set; }
    public string? SessionId { get; set; }
    public EventType Direction { get; set; }  // RequestInitiated or ResponseReceived
    public string ActionName { get; set; }
    public string RawText { get; set; }  // Full unparsed entry
    public List<string> RawXmlPayloads { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}
```

## Test Coverage

### Test Cases (18 tests - all passing)

1. **Empty/Null Input**: Empty string, null input, whitespace-only
2. **Single Entries**:
   - Search records request
   - Search records response
   - Open workspace Path request
   - Save records request
3. **Multiple Entries**: Multiple log entries in sequence
4. **Multiple Payloads**: Multiple Input/Output blocks per entry
5. **Formatting**:
   - Blank lines within XML
   - Different date formats
   - Different usernames and session IDs
6. **Preservation**:
   - Raw text preservation
   - XML structure preservation
7. **File Operations**:
   - ParseFileAsync with valid file
   - ParseFileAsync with missing file (throws FileNotFoundException)
8. **Request/Response Pairs**: Both Request and Response with matching metadata

### Sample Test Data

```csharp
var logContent = """
    [2026-06-08 11:13:20] istvan.vencz (17): Request: Search records
    Input:
    <?xml version="1.0" encoding="utf-16"?>
    <SearchRequest>
      <Criteria>
        <Name>Test*</Name>
      </Criteria>
    </SearchRequest>
    """;

var result = parser.Parse(logContent);
// result contains one RawActivityLogEntry with:
// - Timestamp: 2026-06-08 11:13:20
// - UserName: istvan.vencz
// - SessionId: 17
// - Direction: RequestInitiated
// - ActionName: Search records
// - RawXmlPayloads: ["<?xml version="1.0" encoding="utf-16"?>..."]
```

## Usage Examples

### Parse from String

```csharp
var parser = new ActivityLogParser(logger);
var entries = parser.Parse(logContent);

foreach (var entry in entries)
{
    Console.WriteLine($"{entry.ActionName} by {entry.UserName}");
    Console.WriteLine($"  XML Payloads: {entry.RawXmlPayloads.Count}");
}
```

### Parse from File

```csharp
var entries = await parser.ParseFileAsync("activity.log");
Console.WriteLine($"Parsed {entries.Count} entries");
```

### CLI Usage

```bash
# Parse activity log file
dotnet run --project Aktavara.WorkflowIntelligence.Cli -- parse activity.log

# Output:
# Parsed 5 log entries
# First entry:
#   Timestamp: 6/8/2026 11:13:20 AM
#   User: istvan.vencz
#   Session: 17
#   Direction: Request
#   Action: Search records
#   XML Payloads: 1
```

## Performance Characteristics

- **Time Complexity**: O(n) where n = number of characters in log content
- **Space Complexity**: O(m) where m = number of log entries
- **No Streaming**: Loads entire file into memory (suitable for moderate log files)

## Limitations & Future Enhancements

### Current Limitations

1. **No XML Validation**: Raw XML stored as strings, not parsed/validated
2. **No Compression**: Multiple Input/Output blocks create separate payload entries
3. **File Size**: Full file loaded into memory (not suitable for multi-GB logs)
4. **No Streaming**: Parse() processes entire content at once

### Future Enhancements

1. **XML Conversion**: Convert raw XML to DOM or strongly-typed objects
2. **Event Enrichment**: Convert RawActivityLogEntry to ActivityEvent with type inference
3. **Streaming Parser**: Process files line-by-line for large logs
4. **Binary Payloads**: Support non-XML payloads
5. **Format Extensions**: Support additional log formats (JSON, CSV, etc.)

## Related Components

- **ActivityLogParser**: This implementation (raw log parsing)
- **ActivityEventEnricher** (next step): Converts RawActivityLogEntry → ActivityEvent
- **ActivityContextBuilder**: Aggregates events into context summaries
- **WorkflowMatcher**: Matches event sequences to workflow definitions

## Testing

Run tests:
```bash
dotnet test
```

Run specific test:
```bash
dotnet test --filter "Parse_SearchRecordsRequest_ParsesSuccessfully"
```

## Dependencies

- Microsoft.Extensions.Logging.Abstractions
- System (for Regex, DateTime, etc.)

No third-party dependencies required.
