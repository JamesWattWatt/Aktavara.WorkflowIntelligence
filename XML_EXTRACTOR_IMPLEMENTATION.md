# Aktavara XML Extractor Implementation

## Overview

A namespace-tolerant XML extractor for Aktavara activity log payloads. Converts raw XML data into strongly-typed snapshot objects representing records, workspace structures, and pagination information.

## Architecture

The XML extractor is built on three layers:

1. **IAktaXmlExtractor Interface**: Defines extraction contracts
2. **AktaXmlExtractor Service**: Implements extraction logic using System.Xml.Linq
3. **Snapshot Models**: Immutable data transfer objects for extracted data

## Interfaces

### IAktaXmlExtractor

```csharp
public interface IAktaXmlExtractor
{
    IReadOnlyList<AktaRecordSnapshot> ExtractRecords(string xml);
    PageInfoSnapshot? ExtractPageInfo(string xml);
    PathWorkspaceSnapshot? ExtractPathWorkspace(string xml);
    bool? ExtractBooleanResult(string xml);
}
```

## Core Models

### AktaRecordSnapshot

Represents an Aktavara record extracted from XML.

```csharp
public class AktaRecordSnapshot
{
    public string TypeKind { get; set; }              // "Path", "Node", "Connector", etc.
    public string TypeId { get; set; }                // Record type identifier
    public string RecordId { get; set; }              // Unique record ID
    public DateTime? LastChangedDate { get; set; }    // Optional change timestamp
    public string RecordState { get; set; }           // "Active", "Draft", "Released", etc.
    public string? StageId { get; set; }              // Optional stage identifier
    public List<AktaRecordPropertySnapshot> Properties { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
    
    // Helper methods
    public AktaRecordPropertySnapshot? FindProperty(string attributeId);
    public object? GetPropertyValue(string attributeId);
    public string GetSummary();
}
```

### AktaRecordPropertySnapshot

Represents an attribute/property of a record.

```csharp
public class AktaRecordPropertySnapshot
{
    public string AttributeId { get; set; }           // Attribute identifier
    public object? Value { get; set; }                // Attribute value
    public string? ValueType { get; set; }            // Data type (String, Integer, Boolean, etc.)
    public string? XsiType { get; set; }              // XML Schema instance type
}
```

### PathWorkspaceSnapshot

Represents a complete Path workspace structure with nodes, connectors, and edges.

```csharp
public class PathWorkspaceSnapshot
{
    public AktaRecordSnapshot PathRecord { get; set; }     // The Path itself
    public List<AktaRecordSnapshot> Nodes { get; set; }    // Vertex records
    public List<AktaRecordSnapshot> Connectors { get; set; } // Edge connector records
    public List<AktaEdgeSnapshot> Edges { get; set; }      // Relationships between nodes
    public string? History { get; set; }                     // Optional history
    public Dictionary<string, object> Metadata { get; set; }
    
    // Helper methods
    public int TotalEntityCount { get; }                     // Nodes + Connectors
    public int TotalRelationshipCount { get; }               // Edges count
    public AktaRecordSnapshot? FindNode(string recordId);
    public AktaRecordSnapshot? FindConnector(string recordId);
    public List<AktaEdgeSnapshot> FindEdgesForNode(string nodeRecordId);
    public string GetSummary();
}
```

### AktaEdgeSnapshot

Represents a relationship between two nodes.

```csharp
public class AktaEdgeSnapshot
{
    public string StartNodeRecordId { get; set; }    // Source node ID
    public string EndNodeRecordId { get; set; }      // Target node ID
    public string ConnectorRecordId { get; set; }    // Connector/relationship ID
    public Dictionary<string, object> Metadata { get; set; }
    
    public string GetSummary();  // Returns: "N1 -[C1]-> N2"
}
```

### PageInfoSnapshot

Represents pagination information from search results.

```csharp
public class PageInfoSnapshot
{
    public int PageNumber { get; set; }      // Current page (1-based)
    public int TotalRecords { get; set; }    // Total records in result set
    public int PageSize { get; set; }        // Records per page
    public int StartIndex { get; set; }      // Starting record index
    public bool HasMorePages { get; set; }   // More pages available?
    
    public int TotalPages { get; }           // Calculated: ceil(TotalRecords / PageSize)
    public int RecordCountOnPage { get; }    // Records on current page
}
```

## Implementation Details

### Extraction Strategy

**1. ExtractRecords()**
- Uses XDocument.Parse() with exception handling
- Searches for Record elements using LocalName (namespace-agnostic)
- Recursively extracts nested Attribute elements
- Handles missing properties gracefully
- Returns immutable IReadOnlyList<AktaRecordSnapshot>

**2. ExtractPageInfo()**
- Searches for PageInfo element
- Extracts numeric fields with fallback parsing
- Calculates derived properties (TotalPages, RecordCountOnPage)
- Returns null if PageInfo not found

**3. ExtractPathWorkspace()**
- Finds PathWkData root element
- Extracts main Path record
- Recursively finds and extracts:
  - StartVertex nodes
  - EndVertex nodes (deduplicating)
  - Edge elements with connector records
- Builds complete object graph
- Returns null if PathWkData not found

**4. ExtractBooleanResult()**
- Searches for common result element names:
  - Result, IsSuccess, Success, IsValid, Valid
- Falls back to parsing root element value
- Returns null if not found

### Namespace Handling

The implementation is **namespace-tolerant**:

```csharp
// XElement.Name.LocalName ignores namespace
element.Descendants()
    .Where(e => e.Name.LocalName == "Record")  // Works regardless of namespace

// Explicit namespace handling for known namespaces
private static readonly XNamespace XsiNamespace = 
    "http://www.w3.org/2001/XMLSchema-instance";

element.Attribute(XsiNamespace + "type")?.Value
```

### Error Handling

- Invalid XML → returns empty list / null
- Missing elements → gracefully skipped
- Type conversion failures → logged, operation continues
- All exceptions caught with logging

## Test Coverage

### Test Statistics
- **Total Tests**: 27
- **Passing**: 27 (100%)
- **Execution Time**: ~277ms

### Test Categories

1. **ExtractRecords Tests (11 tests)**
   - Empty/null/invalid input
   - Single and multiple records
   - Property extraction with types
   - DateTime parsing
   - Stage ID extraction
   - XSI type capture

2. **ExtractPageInfo Tests (5 tests)**
   - Valid page info extraction
   - Calculated properties (TotalPages, RecordCountOnPage)
   - Missing page info handling
   - Empty XML handling

3. **ExtractPathWorkspace Tests (5 tests)**
   - Complete path structure extraction
   - Node list extraction
   - Edge relationship extraction
   - Entity count calculations
   - Missing path data handling

4. **ExtractBooleanResult Tests (5 tests)**
   - True/false extraction
   - Alternative element names (IsSuccess, IsValid, etc.)
   - Root element as boolean value
   - Missing result handling

5. **Integration Tests (1 test)**
   - Property value lookup
   - Summary formatting
   - Edge relationship formatting

## Usage Examples

### Extract Records from Search Response

```csharp
var extractor = new AktaXmlExtractor(logger);

var xml = """
    <?xml version="1.0" encoding="utf-16"?>
    <SearchResponse>
      <Record TypeKind="Node" TypeId="MyNode" RecordId="N-001" State="Active">
        <Attribute AttributeId="Name">
          <AttributeValue ValueType="String">Component A</AttributeValue>
        </Attribute>
      </Record>
    </SearchResponse>
    """;

var records = extractor.ExtractRecords(xml);

foreach (var record in records)
{
    Console.WriteLine($"{record.TypeKind}: {record.RecordId}");
    
    var nameValue = record.GetPropertyValue("Name");
    Console.WriteLine($"  Name: {nameValue}");
}
```

### Extract Path Workspace Structure

```csharp
var pathXml = """
    <?xml version="1.0" encoding="utf-16"?>
    <PathWkData>
      <Path TypeKind="Path" TypeId="PathType" RecordId="PATH-001" State="Active"/>
      <StartVertex>
        <Record TypeKind="Node" TypeId="NodeType" RecordId="N1" State="Active"/>
      </StartVertex>
      <StartVertex>
        <Record TypeKind="Node" TypeId="NodeType" RecordId="N2" State="Active"/>
      </StartVertex>
      <Edge StartNodeRecordId="N1" EndNodeRecordId="N2" ConnectorRecordId="C1">
        <Connector>
          <Record TypeKind="Connector" TypeId="ConnType" RecordId="C1" State="Active"/>
        </Connector>
      </Edge>
    </PathWkData>
    """;

var pathWorkspace = extractor.ExtractPathWorkspace(pathXml);

if (pathWorkspace != null)
{
    Console.WriteLine($"Path: {pathWorkspace.PathRecord.RecordId}");
    Console.WriteLine($"Nodes: {pathWorkspace.Nodes.Count}");
    Console.WriteLine($"Connectors: {pathWorkspace.Connectors.Count}");
    Console.WriteLine($"Relationships: {pathWorkspace.Edges.Count}");
    
    // Find node relationships
    foreach (var edge in pathWorkspace.Edges)
    {
        Console.WriteLine($"  {edge.GetSummary()}");
    }
}
```

### Extract Pagination Info

```csharp
var pageInfo = extractor.ExtractPageInfo(xml);

if (pageInfo != null)
{
    Console.WriteLine($"Page {pageInfo.PageNumber} of {pageInfo.TotalPages}");
    Console.WriteLine($"Records: {pageInfo.RecordCountOnPage}/{pageInfo.TotalRecords}");
    
    if (pageInfo.HasMorePages)
        Console.WriteLine("Load more pages...");
}
```

### Extract Operation Result

```csharp
var success = extractor.ExtractBooleanResult(responseXml);

if (success == true)
    Console.WriteLine("Operation succeeded");
else if (success == false)
    Console.WriteLine("Operation failed");
else
    Console.WriteLine("Could not determine operation result");
```

## Performance Characteristics

- **Time Complexity**: O(n) where n = XML document size
- **Space Complexity**: O(m) where m = number of elements
- **No Streaming**: Entire XML loaded into memory
- **No External Dependencies**: Only System.Xml.Linq

## Limitations & Future Enhancements

### Current Limitations
1. **No Streaming**: Full XML in memory
2. **No Schema Validation**: Structure assumed valid
3. **Limited Type Inference**: Basic type detection only
4. **Namespace Declaration**: Must match expected namespaces

### Future Enhancements
1. **Streaming Parser**: For large XML documents
2. **Schema Validation**: XSD validation of structure
3. **Type Inference**: Automatic type detection from values
4. **Caching**: LRU cache for repeated extractions
5. **Path Expressions**: XPath support for custom queries
6. **Batch Operations**: Parallel extraction of multiple documents

## Related Components

- **ActivityLogParser**: Raw log parsing → RawActivityLogEntry
- **AktaXmlExtractor**: XML extraction → Snapshot objects (this component)
- **ActivityEventEnricher** (next): Snapshots → ActivityEvent
- **WorkflowMatcher**: ActivityEvents → WorkflowMatch

## Data Flow

```
Raw Log Text
    ↓
ActivityLogParser.Parse()
    ↓ 
RawActivityLogEntry (with raw XML)
    ↓
AktaXmlExtractor.ExtractRecords()
    ↓
AktaRecordSnapshot (structured data)
    ↓
ActivityEventEnricher.Enrich() [future]
    ↓
ActivityEvent (enriched with context)
```

## Dependencies

- System.Xml.Linq (built-in)
- Microsoft.Extensions.Logging.Abstractions

## Testing

Run all tests:
```bash
dotnet test
```

Run specific test class:
```bash
dotnet test --filter "AktaXmlExtractorTests"
```

Run specific test:
```bash
dotnet test --filter "ExtractRecords_WithSingleRecord_ExtractsSuccessfully"
```

View test output:
```bash
dotnet test --logger "console;verbosity=detailed"
```
