# Record Diff Service Implementation

## Overview

The RecordDiffService computes attribute-level differences between before and after snapshots of Aktavara records. It identifies which properties changed, their old and new values, and integrates seamlessly into the activity event normalization pipeline.

## Architecture

```
AktaRecordSnapshot (before)
    ↓
RecordDiffService.Diff()
    ↓
DiffOptions (configuration)
    ↓
IReadOnlyList<ChangedAttribute>
```

## Interface

```csharp
public interface IRecordDiffService
{
    IReadOnlyList<ChangedAttribute> Diff(
        AktaRecordSnapshot before,
        AktaRecordSnapshot after);

    IReadOnlyList<ChangedAttribute> Diff(
        AktaRecordSnapshot before,
        AktaRecordSnapshot after,
        DiffOptions options);
}
```

## DiffOptions Configuration

```csharp
public class DiffOptions
{
    // Attribute IDs to ignore when diffing
    public HashSet<string> IgnoredAttributeIds { get; set; }
    
    // Treat empty strings as equivalent to null
    public bool TreatEmptyAsNull { get; set; }
    
    // Case-sensitive value comparison
    public bool CaseSensitiveComparison { get; set; }
}
```

### Predefined Options

**Default** (system attributes ignored):
```csharp
DiffOptions.Default
// Ignores: LastChangedDate, LastChangedUser, CreatedDate, CreatedUser, 
//          RecordId, TypeId, TypeKind
```

**IncludeAll** (no attributes ignored):
```csharp
DiffOptions.IncludeAll
// Ignores nothing, diffs all changes
```

## Diff Algorithm

1. **Record Validation**: Verify both records have same RecordId
2. **Property Mapping**: Build dictionaries of properties by AttributeId
3. **Attribute Enumeration**: Get all unique AttributeIds from both records
4. **Change Detection**: For each attribute:
   - Skip if in IgnoredAttributeIds
   - Compare before and after values
   - Apply case sensitivity rules
   - Create ChangedAttribute if different
5. **Return**: Immutable list of changes

## Change Detection Rules

| Before | After | Result |
|--------|-------|--------|
| Value A | Value A | No change |
| Value A | Value B | Changed |
| Value A | null/missing | Changed |
| null/missing | Value A | Changed |
| null | null | No change |

## Example Usage

### Basic Diffing

```csharp
var before = new AktaRecordSnapshot
{
    RecordId = "NODE-658",
    Properties = new List<AktaRecordPropertySnapshot>
    {
        new() { AttributeId = "Name", Value = "NE4" },
        new() { AttributeId = "Description", Value = "Old desc" }
    }
};

var after = new AktaRecordSnapshot
{
    RecordId = "NODE-658",
    Properties = new List<AktaRecordPropertySnapshot>
    {
        new() { AttributeId = "Name", Value = "NE411" },
        new() { AttributeId = "Description", Value = "Old desc" }  // No change
    }
};

var diffService = new RecordDiffService(logger);
var changes = diffService.Diff(before, after);

// Result: Single change
// - AttributeId: "Name"
// - FromValue: "NE4"
// - ToValue: "NE411"
```

### Custom Options

```csharp
var options = new DiffOptions
{
    CaseSensitiveComparison = false,
    IgnoredAttributeIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "Status",
        "Timestamp"
    }
};

var changes = diffService.Diff(before, after, options);
```

## Integration with ActivityEventNormalizer

The RecordDiffService integrates into the ActivityEventNormalizer to automatically detect and populate ChangedAttributes when:

1. An OpenWorkspace event extracts path snapshots
2. A later SaveRecords event operates on the same record ID
3. The diff detects changes from the earlier snapshot

### Integration Flow

```
Raw Log Entries
    ↓
First Pass: Extract OpenWorkspace Snapshots
    - Extract Path, Nodes, Connectors from PathWkData
    - Store in recordSnapshots dictionary by RecordId
    ↓
Second Pass: Process All Events
    - SearchRecords → Create SearchRecords event
    - OpenWorkspace → Create OpenWorkspace event
    - SaveRecords → For each saved record:
        - Check if recordSnapshots has earlier snapshot
        - If yes: Diff snapshots, populate ChangedAttributes
        - If no: Extract from properties directly
        - Correlate with response for success status
    ↓
Activity Events with populated ChangedAttributes
```

### Example: Search → Open → Save → Success

**Input**: 4 raw log entries

```
1. Search for Path type
2. Open workspace Path (response with node snapshots)
3. Save record NODE-001 (modified)
4. Save response (success)
```

**Processing**:

1. **First Pass**: Extract snapshot of NODE-001 from OpenWorkspace response
   - Stores: NODE-001 with Name="Original", Description="Desc A"

2. **Second Pass**: 
   - Create SearchRecords event
   - Create OpenWorkspace event
   - Process SaveRecords entry for NODE-001:
     - Finds snapshot: Name="Original", Description="Desc A"
     - Record in request: Name="Updated", Description="Desc A"
     - Diff detects: Name changed from "Original" to "Updated"
     - Populates: `ChangedAttributes = [Name change]`
     - Correlates with response: IsSuccess = true

**Output**: ActivityEvent with:
```csharp
EventType = SaveRecords
RecordId = "NODE-001"
ChangedAttributes = [
    new ChangedAttribute 
    { 
        AttributeId = "Name",
        FromValue = "Original",
        ToValue = "Updated",
        ValueType = "String"
    }
]
IsSuccess = true
Evidence = [
    "Detected 1 attribute changes from previous snapshot",
    "Saving Node record NODE-001 (State: Modified)",
    "Response indicates success"
]
Metadata = {
    ["diff_from_snapshot"] = true,
    ["correlated_response_index"] = 3
}
```

## Test Coverage

### Test Statistics
- **Total Tests**: 17 new diff service tests
- **Passing**: 17 (100%)
- **Categories**: 6 major categories with edge cases

### Test Categories

1. **Basic Diffing** (3 tests)
   - Identical records → empty list
   - Single attribute changed
   - Multiple attributes with some unchanged

2. **Missing/Added Attributes** (3 tests)
   - Attribute added in after
   - Attribute removed in after
   - Null to value transition

3. **Ignored Attributes** (3 tests)
   - Default options ignore system attrs
   - Custom ignored attributes
   - IncludeAll options

4. **Case Sensitivity** (2 tests)
   - Case-sensitive comparison
   - Case-insensitive comparison

5. **Error Handling** (3 tests)
   - Null before/after
   - Null options
   - Different record IDs

6. **Integration** (3 tests)
   - Real-world node update scenario
   - Empty property lists
   - RealWorld test with actual diff

### Sample Test: NE4 → NE411

```csharp
[Fact]
public void Diff_SingleAttributeChanged_ReturnsSingleChange()
{
    var before = new AktaRecordSnapshot
    {
        RecordId = "REC-001",
        Properties = new List<AktaRecordPropertySnapshot>
        {
            new() { AttributeId = "Name", Value = "NE4", ValueType = "String" }
        }
    };

    var after = new AktaRecordSnapshot
    {
        RecordId = "REC-001",
        Properties = new List<AktaRecordPropertySnapshot>
        {
            new() { AttributeId = "Name", Value = "NE411", ValueType = "String" }
        }
    };

    var result = _diffService.Diff(before, after);

    Assert.Single(result);
    var change = result[0];
    Assert.Equal("Name", change.AttributeId);
    Assert.Equal("NE4", change.FromValue);
    Assert.Equal("NE411", change.ToValue);
}
```

## Performance Characteristics

- **Time Complexity**: O(n) where n = number of unique attributes
- **Space Complexity**: O(n) for change list
- **No Streaming**: Full records in memory
- **No External Dependencies**: Uses only built-in types

## API Integration Points

### ActivityEventNormalizer Changes

1. **Constructor**: Accepts optional `IRecordDiffService`
   ```csharp
   public ActivityEventNormalizer(
       IAktaXmlExtractor xmlExtractor,
       ILogger<ActivityEventNormalizer> logger,
       IRecordDiffService? diffService = null)
   ```

2. **Two-Pass Processing**:
   - **Pass 1**: Extract snapshots from OpenWorkspace events
   - **Pass 2**: Create events, diff SaveRecords against snapshots

3. **Snapshot Storage**:
   - Dictionary<RecordId, AktaRecordSnapshot>
   - Includes Path, Nodes, Connectors from PathWkData

4. **Diff Population**:
   - If snapshot exists: diff against it
   - If no snapshot: extract from properties directly
   - Result: ChangedAttributes populated either way

## Benefits

✅ **Accurate Change Tracking**: Diff shows exactly what changed vs. original
✅ **Context Preservation**: Maintains before/after values for analysis
✅ **Deterministic**: Same input always produces same output
✅ **Flexible**: Configurable ignored attributes and comparison rules
✅ **Integrated**: Seamlessly works with event normalization pipeline
✅ **Well-Tested**: 17 comprehensive unit tests

## Limitations & Future Enhancements

### Current Limitations
1. **String-Based Values**: All values converted to strings for comparison
2. **No Type Coercion**: "1" != 1 (both treated as strings)
3. **Flat Comparison**: No deep object comparison
4. **One-Way Diff**: Only before→after, not bidirectional

### Future Enhancements
1. **Type-Aware Diffing**: Respect ValueType in comparisons
2. **Nested Object Diffing**: Compare complex structures
3. **Delta Format**: Compact representation of changes
4. **Diff Merging**: Combine multiple diffs
5. **Change Rollup**: Summarize sequences of changes
6. **Change Reasoning**: Infer why changes occurred

## Related Components

- **AktaRecordSnapshot**: Input model (before/after)
- **ChangedAttribute**: Output model (detected changes)
- **DiffOptions**: Configuration
- **ActivityEventNormalizer**: Consumer (populates ChangedAttributes)
- **ActivityEvent**: Container for diff results

## Dependencies

- System (for Dictionary, List, etc.)
- Microsoft.Extensions.Logging (for ILogger)

## Testing

Run diff service tests only:
```bash
dotnet test --filter "RecordDiffServiceTests"
```

Run specific test:
```bash
dotnet test --filter "Diff_SingleAttributeChanged_ReturnsSingleChange"
```

All tests (including normalizer integration):
```bash
dotnet test
```
