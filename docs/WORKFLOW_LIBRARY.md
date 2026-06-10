# Workflow Library Implementation - Prompt 7

## Overview

The workflow library provides a JSON-based, file-driven system for defining, loading, and validating workflow patterns that can be recognized in Aktavara user activity logs. Workflows form the foundation of activity intelligence, enabling the system to understand repeatable business processes.

## Architecture

### Core Components

#### 1. **IWorkflowLibrary Interface** (`Interfaces/IWorkflowLibrary.cs`)
Defines the contract for workflow access:
- `GetAll()` - Returns all available workflows
- `GetById(workflowId)` - Retrieves a specific workflow
- `GetByTag(tag)` - Filters workflows by category/domain
- `GetActive()` - Returns only active/enabled workflows
- `GetValidationErrors(workflowId)` - Validates workflow definitions

#### 2. **FileWorkflowLibrary Implementation** (`Services/FileWorkflowLibrary.cs`)
Loads and manages workflows from `.workflow.json` files:
- **Directory scanning**: Monitors a configured folder for `*.workflow.json` files
- **Lazy loading**: Uses `LoadAsync()` to load workflows on demand or explicitly
- **Validation on load**: Validates each workflow and logs errors
- **Duplicate handling**: Uses last file alphabetically if duplicate IDs exist
- **Error resilience**: Skips invalid files without failing the entire load

#### 3. **WorkflowDefinition Model** (`Models/WorkflowDefinition.cs`)
Represents a complete workflow with:
- **Basic metadata**: ID, name, description, version, status, category
- **Activity signature**: List of `WorkflowSignatureRule` defining the workflow's fingerprint
- **States**: Ordered progression through workflow phases (`WorkflowStateDefinition`)
- **Actions**: Available user actions within each state (`WorkflowAction`)
- **Confidence threshold**: Minimum match score (0.0-1.0)
- **Tags**: Domain categorization (path-operations, topology-management, etc.)

#### 4. **WorkflowStateDefinition Model** (`Models/WorkflowStateDefinition.cs`)
Represents workflow phases:
- **StateId**: Unique identifier within the workflow
- **Sequence**: Order in the workflow (0 = initial)
- **RequiredEvidence**: Event types that confirm this state
- **Transitions**: Next state ID and optional help guide
- **Terminal flag**: Marks end states

### Validation System

**WorkflowDefinition.Validate()** checks:
1. ✅ Required fields present (ID, name, rules)
2. ✅ At least one rule marked as required
3. ✅ Confidence threshold in range [0.0, 1.0]
4. ✅ Total rule weights reasonable (0.5-10.0)
5. ✅ No duplicate state IDs
6. ✅ State transitions reference valid states
7. ✅ Initial state exists (sequence 0)

Detailed error messages guide workflow authors on what to fix.

## JSON Schema

### Workflow File Structure

```json
{
  "workflowId": "unique-identifier",
  "name": "Human-Readable Name",
  "description": "What this workflow represents",
  "category": "domain-category",
  "version": "1.0",
  "status": "Active|Inactive|Draft|Archived|Deprecated",
  "tags": ["tag1", "tag2"],
  "minimumConfidenceThreshold": 0.6,
  "createdBy": "system",
  "createdDate": "2026-06-10T00:00:00Z",
  "lastModifiedDate": "2026-06-10T00:00:00Z",
  "metadata": {
    "domain": "Path Workspace",
    "complexity": "Medium|Simple|Complex",
    "estimatedDuration": "5-10 minutes"
  },
  "activitySignature": [...],
  "states": [...],
  "actions": [...],
  "helpGuideIds": ["guide-id-1", "guide-id-2"]
}
```

### Activity Signature Rule

```json
{
  "eventType": "SearchRecords|OpenWorkspace|SaveRecords|RecordCreated|UserInteraction|...",
  "recordKind": "Path|Node|Connector|Topology|Diagram|Carrier|Tag|...",
  "workspaceKind": "Path|Topology|Diagram|null",
  "required": true,
  "weight": 1.0,
  "description": "What this rule detects",
  "missingPenalty": 0.1,
  "maxAgeMinutes": 15
}
```

Rules with `required: true` must be present for the workflow to match.
Weights are normalized and summed to compute confidence scores.

### State Definition

```json
{
  "stateId": "state-identifier",
  "name": "State Display Name",
  "description": "What this state means",
  "requiredEvidence": ["event-type-1", "event-type-2"],
  "sequence": 0,
  "isTerminal": false,
  "nextStateId": "next-state-id",
  "helpGuideId": "optional-help-guide",
  "metadata": {}
}
```

Sequence determines order (0 is initial). Terminal states have no next state.

### Action Definition

```json
{
  "actionId": "action-identifier",
  "name": "Action Name",
  "description": "What this action does",
  "executionMode": "Automatic|RequiresApproval|Informational|Prompt",
  "availableInStateId": "state-id-where-available",
  "metadata": {
    "hotkey": "Ctrl+S",
    "optional": "true"
  }
}
```

## Example Workflows

### 1. Update Node in Path (`update-node-in-path.workflow.json`)

**Purpose**: User modifies an existing node's properties within a path

**Signature**:
- Search for Path (supporting, weight 0.15)
- Open Path workspace (required, weight 0.35)
- Interact with Node (required, weight 0.35)
- Save Records (supporting, weight 0.15)

**States**:
1. `path_opened` - Path workspace open
2. `node_selected` - User selected a node
3. `node_modified` - Node properties changed
4. `node_saved` - Changes persisted

**Actions**:
- Save Node Changes (Automatic)
- Discard Changes (Prompt)

### 2. Add Connector to Path (`add-connector-to-path.workflow.json`)

**Purpose**: User creates a new connector between two nodes in a path

**Signature** (6 rules):
- Search Path (optional, weight 0.12)
- Open Path workspace (required, weight 0.30)
- Select start node (required, weight 0.20)
- Select end node (required, weight 0.20, max age 15 min)
- Create Connector (required, weight 0.28)
- Save Records (optional, weight 0.10)

**States**:
1. `path_opened`
2. `start_node_selected`
3. `end_node_selected`
4. `connector_created`
5. `connector_configured`
6. `path_saved` (terminal)

**Actions**:
- Create Connector (Prompt)
- Configure Connector Properties (Prompt)
- Save Path Changes (Automatic)
- Cancel Connector Creation (Prompt)

## Usage

### Loading Workflows

```csharp
// Create library pointing to workflow directory
var library = new FileWorkflowLibrary("/path/to/workflows", logger);

// Load all workflows
await library.LoadAsync();

// Or use lazily (auto-loads on first access)
var workflow = library.GetById("update-node-in-path");
```

### Querying Workflows

```csharp
// Get all workflows
var allWorkflows = library.GetAll();

// Get specific workflow
var workflow = library.GetById("update-node-in-path");

// Get workflows by category
var pathWorkflows = library.GetByTag("path-operations");

// Get only active workflows
var active = library.GetActive();
```

### Validating Workflows

```csharp
var errors = library.GetValidationErrors("update-node-in-path");
if (errors.Count > 0)
{
    foreach (var error in errors)
    {
        Console.WriteLine($"Validation error: {error}");
    }
}
```

### Matching Workflows Against Activity

```csharp
var workflow = library.GetById("update-node-in-path");
var recentEvents = new List<ActivityEvent> { /* ... */ };

if (workflow.MatchesSignature(recentEvents))
{
    Console.WriteLine($"User is performing: {workflow.Name}");
}
```

## Best Practices

### For Workflow Authors

1. **Clear naming**: Use descriptive workflow IDs and names
   - ✅ `update-node-in-path` (action-object-context)
   - ❌ `workflow1`, `path-operation`

2. **Balanced weights**: Keep total weights in reasonable range
   - Rule of thumb: 5 rules × 1.0 weight = 5.0 total
   - Avoid: tiny weights (< 0.1) or huge weights (> 2.0)

3. **Required rules**: Mark critical activities as required
   - At least one must be `required: true`
   - Example: OpenWorkspace is usually required

4. **Age constraints**: Use `maxAgeMinutes` for ordering
   - Ensures start node selected before end node
   - Prevents false matches with old events

5. **Clear state progression**: Sequential, logical flow
   - Use `sequence` numbers consistently (0, 1, 2, ...)
   - One initial state, clear terminal states

6. **Helpful descriptions**: Document why each rule exists
   - Guides future maintainers
   - Assists with debugging false matches

### For System Maintainers

1. **Validate before deploying**: Check `IsValid()` before loading
2. **Monitor logs**: Watch for validation errors during load
3. **Version carefully**: Increment version when changing signature rules
4. **Tag consistently**: Use standard tags across all workflows
5. **Archive instead of delete**: Set status to `Archived` rather than removing

## Directory Structure

```
project-root/
├── workflows/
│   ├── update-node-in-path.workflow.json
│   ├── add-connector-to-path.workflow.json
│   └── [future workflows...]
├── Aktavara.WorkflowIntelligence.Core/
│   ├── Interfaces/
│   │   └── IWorkflowLibrary.cs
│   ├── Services/
│   │   └── FileWorkflowLibrary.cs
│   └── Models/
│       ├── WorkflowDefinition.cs
│       └── WorkflowStateDefinition.cs
└── Aktavara.WorkflowIntelligence.Tests/
    ├── FileWorkflowLibraryTests.cs (56 tests)
    └── WorkflowDefinitionTests.cs (36 tests)
```

## Test Coverage

### FileWorkflowLibraryTests (56 tests)
- **Loading**: Empty directory, non-existent directory, valid files, multiple files
- **Error handling**: Invalid JSON, duplicate IDs, skipped files
- **Querying**: GetById, GetByTag, GetActive
- **Validation**: Valid/invalid workflows, validation error reporting

### WorkflowDefinitionTests (36 tests)
- **Validation**: Required fields, confidence threshold, rule configuration
- **State management**: Duplicate IDs, invalid transitions, initial state
- **Signature matching**: Event matching, required rules, age constraints
- **Utilities**: GetInitialState, GetStateById, GetRequiredSignatures

**Total**: 92 workflow-specific tests, all passing ✅

## Future Extensions

### Short Term
- [ ] JSON schema (XSD/JSON Schema) for IDE validation
- [ ] Workflow designer UI
- [ ] Performance metrics per workflow

### Medium Term  
- [ ] Workflow inheritance/composition
- [ ] Dynamic rule generation from logs
- [ ] A/B testing workflow variants

### Long Term
- [ ] Machine learning for signature optimization
- [ ] Auto-tuning weights based on accuracy
- [ ] Workflow recommendation engine

## Configuration

### Registering with DI Container

```csharp
services.AddSingleton<IWorkflowLibrary>(sp =>
    new FileWorkflowLibrary(
        workflowsDirectory: "/path/to/workflows",
        logger: sp.GetRequiredService<ILogger<FileWorkflowLibrary>>()
    )
);
```

### Environment Variables

```bash
# Configuration via appsettings.json
{
  "WorkflowLibrary": {
    "WorkflowsPath": "${WORKFLOWS_DIR}/workflows"
  }
}
```

## Troubleshooting

### Workflow Not Loading
- Check file is named `*.workflow.json`
- Validate JSON syntax (use jsonlint.com)
- Check WorkflowId is not empty
- Review validation errors: `library.GetValidationErrors(id)`

### False Matches
- Reduce `minimumConfidenceThreshold`
- Mark unnecessary rules as `required: false`
- Adjust rule weights
- Add `maxAgeMinutes` constraints

### Performance
- Lazy loading (default): `LoadAsync()` called only once
- Workflows are cached in memory after loading
- Direct dictionary lookup by ID (O(1))

## References

- **WorkflowDefinition**: Models/WorkflowDefinition.cs
- **FileWorkflowLibrary**: Services/FileWorkflowLibrary.cs
- **Example workflows**: workflows/*.workflow.json
- **Tests**: Aktavara.WorkflowIntelligence.Tests/

## Support

For questions or issues:
1. Check workflow JSON against schema
2. Review validation errors
3. Check test examples in FileWorkflowLibraryTests.cs
4. Review real workflow examples in workflows/ directory
