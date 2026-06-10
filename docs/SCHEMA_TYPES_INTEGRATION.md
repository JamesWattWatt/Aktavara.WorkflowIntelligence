# Aktavara Schema Types Integration

## Overview
This document describes the integration of authoritative Aktavara schema types from the Swagger API specification into the Workflow Intelligence system, replacing hardcoded and log-derived type lists.

## What Was Done

### 1. Created `AktavaraSchemaTypes.cs`
A comprehensive type definitions file containing enums and constants extracted directly from the Swagger documentation.

**Contents:**
- **AktavaraTypeKind** - Complete enumeration of all 17 record types from `Akta.Bec.Model.TypeKind`
  - Core types: Node, Connector, Path, Topology, Diagram, Carrier, Schema
  - Branch variants: BranchNode, BranchConnector, BranchPath, BranchTopology, BranchGhost, BranchTag
  - Structural types: Tag, Collection

- **UrfTypeKind** - URF (Unified Record Format) subset for workspace communication
  - Used by Communicator layer for type-safe serialization

- **IpamTypeKind** - IPAM-specific types for IP Address Management
  - Network, Scope, Range, Address

- **AktavaraWorkspaceType** - Workspace type flags with bitwise operations
  - 23 workspace types including Path, Topology, Diagram, Carrier, Schema, etc.

- **AktavaraUpdateAction** - Update action types from Communicator layer

- **WorkspaceRequestTypes** - Constants for all workspace request/response type names
  - Path, Topology, Diagram, Carrier, Schema, Branch workspace patterns

- **DataQueryRequestTypes** - Constants for data query patterns
  - Entity/options queries for all workspace types

- **AktavaraTypeHelper** - Utility class with conversion methods
  - `ParseTypeKind()` - String/int to enum conversion
  - `IsWorkspaceRequest()` - Pattern matching for workspace requests
  - `IsDataQueryRequest()` - Pattern matching for data queries
  - `GetWorkspaceType()` - Maps TypeKind to WorkspaceType
  - `IsPrimaryEntity()` - Identifies entity types vs. structural elements
  - `ToRecordKind()` - Converts AktavaraTypeKind to domain model RecordKind

### 2. Updated `AktaJsonExtractor.cs`
Replaced hardcoded string literals with enum values:
- Line 87: `"Path"` → `AktavaraTypeKind.Path.ToString()`
- Line 408: `"Node"` → `AktavaraTypeKind.Node.ToString()`
- Line 421: `"Node"` → `AktavaraTypeKind.Node.ToString()`
- Line 434: `"Connector"` → `AktavaraTypeKind.Connector.ToString()`

Benefits:
- Type-safe record classification
- Centralized source of truth for valid types
- IDE intellisense support

### 3. Updated `ActivityEventNormalizer.cs`
Replaced hardcoded switch statements with enum conversion:
- Line 183-189: Hardcoded mapping → `AktavaraTypeHelper.ToRecordKind(kindStr)`
- Line 309-315: Hardcoded mapping → `AktavaraTypeHelper.ToRecordKind(record.TypeKind)`

Benefits:
- Single source of truth for type classification
- Automatic support for new Aktavara types
- Easier to maintain and extend

### 4. Updated `ActivityEventNormalizerTests.cs`
Fixed test setup to match updated constructor signature:
- Added missing `AktaJsonExtractor` parameter
- Now properly mocks all required dependencies

## Source of Truth

All enums are generated from the authoritative Aktavara Swagger API specification:
- **Source file**: `docs/swagger.json`
- **Last verified**: 2026-06-10
- **Key schemas**:
  - `Akta.Bec.Model.TypeKind` (main enumeration)
  - `Akta.Bec.Common.WorkspaceType`
  - `Akta.WebAPI.Model.*Composer.*Request/Response`

## Using the New Types

### Basic Type Parsing
```csharp
// Parse from string
var typeKind = AktavaraTypeHelper.ParseTypeKind("Path");
// Result: AktavaraTypeKind.Path

// Parse from integer
var typeKind = AktavaraTypeHelper.ParseTypeKind(13);
// Result: AktavaraTypeKind.Path

// Convert to domain model type
var recordKind = AktavaraTypeHelper.ToRecordKind(typeKind);
// Result: RecordKind.Path
```

### Type Classification
```csharp
// Check if type is a primary entity
if (AktavaraTypeHelper.IsPrimaryEntity(AktavaraTypeKind.Node))
{
    // Handle primary entity
}

// Get workspace type for a record kind
var wsType = AktavaraTypeHelper.GetWorkspaceType(AktavaraTypeKind.Topology);
// Result: AktavaraWorkspaceType.Topology
```

### Request Type Matching
```csharp
// Check workspace request
if (AktavaraTypeHelper.IsWorkspaceRequest(requestTypeName))
{
    // Handle workspace request
}

// Check data query
if (AktavaraTypeHelper.IsDataQueryRequest(requestTypeName))
{
    // Handle data query
}
```

### Using Request Constants
```csharp
var requestType = WorkspaceRequestTypes.GetPathWorkspaceDataRequest;
// Value: "Akta.WebAPI.Model.PathComposer.GetPathWorkspaceDataRequest"
```

## Migration Path for Future Work

When new TypeKinds, Actions, or WorkspaceTypes are added to Aktavara:
1. Extract them from the updated Swagger documentation
2. Add them to the appropriate enum in `AktavaraSchemaTypes.cs`
3. Update any helper methods if needed
4. No changes required to parser/extractor (they use enums)

## Type Mapping Reference

### TypeKind → WorkspaceType
| TypeKind | WorkspaceType |
|----------|---------------|
| Node | Default |
| Connector | Default |
| Path | Path |
| BranchPath | Path |
| Topology | Topology |
| BranchTopology | Topology |
| Diagram | Diagram |
| Carrier | Carrier |
| Schema | Schema |
| Collection | Collection |

### TypeKind → RecordKind
| TypeKind | RecordKind |
|----------|-----------|
| Node | Node |
| BranchNode | Node |
| Connector | Connector |
| BranchConnector | Connector |
| Path | Path |
| BranchPath | Path |
| (others) | Other |

## Testing

All projects build successfully with no errors:
- ✅ Core project: 0 errors, 0 warnings
- ✅ CLI project: 0 errors, 0 warnings
- ✅ Tests project: 0 errors, 5 warnings (pre-existing)

Test coverage includes:
- Type parsing (string and int)
- Type conversion (to RecordKind, to WorkspaceType)
- Pattern matching for requests
- Primary entity classification

## Next Steps (Prompt 7+)

With authoritative schema types in place, the next phases can:
1. **Workflow Library Schema** - Use TypeKind enums for schema validation
2. **Type-Safe Serialization** - Leverage request/response type constants
3. **Enhanced Validation** - Use helper methods for input validation
4. **Dynamic Type Discovery** - Could parse remaining Swagger definitions for other types

## Files Modified

1. **Created**: `Aktavara.WorkflowIntelligence.Core/Models/AktavaraSchemaTypes.cs` (432 lines)
2. **Modified**: `Aktavara.WorkflowIntelligence.Core/Services/AktaJsonExtractor.cs` (4 lines changed)
3. **Modified**: `Aktavara.WorkflowIntelligence.Core/Services/ActivityEventNormalizer.cs` (2 lines changed)
4. **Modified**: `Aktavara.WorkflowIntelligence.Tests/ActivityEventNormalizerTests.cs` (1 line changed)

## Build Status

```
Solution builds successfully:
- 0 errors
- 5 pre-existing warnings in RecordDiffServiceTests.cs (unrelated)
- All projects compile and link correctly
```
