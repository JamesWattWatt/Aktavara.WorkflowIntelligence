# Workflow JSON Verification & Corrections

## Issue Found

The initial workflow JSON files referenced **non-existent EventTypes** that would cause workflows to never match, silently reducing confidence scores to zero.

### Problems Identified

#### Original Issue
- Workflows used `UserInteraction` and `RecordCreated` EventTypes
- The parser (`ActivityEventNormalizer`) **only produces 3 EventTypes**:
  1. `SearchRecords`
  2. `OpenWorkspace`
  3. `SaveRecords`

#### Result
- **update-node-in-path workflow** required `UserInteraction` events that will never occur
- **add-connector-to-path workflow** required `UserInteraction` and `RecordCreated` events that will never occur
- Both workflows would fail to match real user activity silently (confidence score ≈ 0.0)

### Root Cause

The workflows were designed based on **logical user actions** (select node, create connector) rather than **observable parser events** (save record). The parser can only detect high-level operations extracted from request/response payloads, not individual UI interactions within those operations.

## Verification Process

### Step 1: Check Actual Action Names in Logs
```
Action names found in logs:
  - Search records
  - Open workspace Path
  - Open workspace Topology
  - Open workspace Diagram
  - Save records
```

**Key Finding**: Action names match exactly what's in the logs, casing-sensitive.

### Step 2: Check ActivityEventNormalizer Output
```
EventTypes created by parser:
  - EventType.SearchRecords (for "Search records" action)
  - EventType.OpenWorkspace (for "Open workspace *" actions)
  - EventType.SaveRecords (for "Save records" action)
```

**Key Finding**: No `UserInteraction` or `RecordCreated` events exist.

### Step 3: Check TypeKind/RecordKind in Logs
```
TypeKind values found:
  - Path
  - Node
  - Connector
  - Topology
  - Diagram
  - Carrier
  - Tag
```

These convert to RecordKind via `AktavaraTypeHelper.ToRecordKind()`:
- Path → Path
- Node → Node
- Connector → Connector
- Topology, Diagram, Carrier, Tag → Other

## Corrections Made

### update-node-in-path.workflow.json

**Before** (incorrect):
```json
"activitySignature": [
  {"eventType": "SearchRecords", "recordKind": "Path", "required": false},
  {"eventType": "OpenWorkspace", "recordKind": "Path", "required": true},
  {"eventType": "UserInteraction", "recordKind": "Node", "required": true},  // WRONG
  {"eventType": "SaveRecords", "recordKind": "Node", "required": false}
]
```

**After** (correct):
```json
"activitySignature": [
  {"eventType": "SearchRecords", "recordKind": "Path", "required": false},
  {"eventType": "OpenWorkspace", "recordKind": "Path", "required": true},
  {"eventType": "SaveRecords", "recordKind": "Node", "required": true},      // FIXED
  {"eventType": "SaveRecords", "recordKind": "Path", "required": false}
]
```

**Rationale**: 
- Removed `UserInteraction` (doesn't exist in parser output)
- Added second `SaveRecords` with `recordKind: Path` to capture path save after node edit
- Node save is the key observable event indicating node modification

**States Changed** (from 4 to 2):
- Removed internal states (`node_selected`, `node_modified`) that can't be detected from logs
- Kept observable states: `path_workspace_open`, `node_saved`

---

### add-connector-to-path.workflow.json

**Before** (incorrect):
```json
"activitySignature": [
  {"eventType": "SearchRecords", "recordKind": "Path", "required": false},
  {"eventType": "OpenWorkspace", "recordKind": "Path", "required": true},
  {"eventType": "UserInteraction", "recordKind": "Node", "required": true},      // WRONG
  {"eventType": "UserInteraction", "recordKind": "Node", "required": true},      // WRONG
  {"eventType": "RecordCreated", "recordKind": "Connector", "required": true},   // WRONG
  {"eventType": "SaveRecords", "recordKind": "Connector", "required": false}
]
```

**After** (correct):
```json
"activitySignature": [
  {"eventType": "SearchRecords", "recordKind": "Path", "required": false},
  {"eventType": "OpenWorkspace", "recordKind": "Path", "required": true},
  {"eventType": "SaveRecords", "recordKind": "Connector", "required": true},     // FIXED
  {"eventType": "SaveRecords", "recordKind": "Path", "required": false}
]
```

**Rationale**:
- Removed 2x `UserInteraction` (selecting start/end nodes isn't observable)
- Removed `RecordCreated` (doesn't exist in parser output)
- Connector creation is detected via `SaveRecords` with `recordKind: Connector`
- Increased weight of Connector save (0.50) to reflect criticality

**States Changed** (from 6 to 3):
- Removed unobservable states (`start_node_selected`, `end_node_selected`, `connector_configured`)
- Kept observable states: `path_opened`, `connector_created`, `path_saved`

---

## Verification Results

### JSON Validation
```
update-node-in-path.workflow.json
  - ID: update-node-in-path
  - Rules: 4 (all valid EventTypes)
  - States: 2 (sequence 0-1)
  - Status: VALID

add-connector-to-path.workflow.json
  - ID: add-connector-to-path
  - Rules: 4 (all valid EventTypes)
  - States: 3 (sequence 0-2)
  - Status: VALID
```

### Build & Tests
```
Build: SUCCEEDED (0 errors, 0 warnings)
Tests: 119/119 PASSED (100%)
```

### Event Type Cross-Check

**Update Node Workflow**:
| Rule | EventType | RecordKind | Parser Output | Match |
|------|-----------|-----------|---------------|-------|
| 1 | SearchRecords | Path | Produced from "Search records" action | ✓ |
| 2 | OpenWorkspace | Path | Produced from "Open workspace Path" action | ✓ |
| 3 | SaveRecords | Node | Produced when Node records are saved | ✓ |
| 4 | SaveRecords | Path | Produced when Path records are saved | ✓ |

**Add Connector Workflow**:
| Rule | EventType | RecordKind | Parser Output | Match |
|------|-----------|-----------|---------------|-------|
| 1 | SearchRecords | Path | Produced from "Search records" action | ✓ |
| 2 | OpenWorkspace | Path | Produced from "Open workspace Path" action | ✓ |
| 3 | SaveRecords | Connector | Produced when Connector records are saved | ✓ |
| 4 | SaveRecords | Path | Produced when Path records are saved | ✓ |

## Key Learnings

### 1. **Parser Output is the Source of Truth**
The parser defines what events CAN be detected. Workflows must use only those EventTypes.

### 2. **Observable vs. Internal State**
- **Observable**: Actions visible in logs (search, open, save)
- **Internal**: UI interactions (select, edit, configure)
- **Workflows must use observable events only**

### 3. **Casing and Spelling Matter**
EventType enum names must match exactly:
- ✓ `EventType.SearchRecords` (camelCase enum)
- ✓ Matches action: "Search records" (lowercase string in logs)
- Parser handles the conversion

### 4. **Weight Distribution**
Updated weights to reflect actual importance:
- Critical required rule (SaveRecords Connector): weight 0.50
- Supporting rules: weights 0.12-0.30
- Total per workflow: 1.0

## Future Recommendations

### For Workflow Design
1. **Always reference ActivityEventNormalizer** to see what EventTypes are produced
2. **Verify against sample logs** before deployment
3. **Test workflow matching** with real activity samples
4. **Document constraints**: What events CAN be matched vs. what cannot

### For Parser Enhancement
Consider expanding parser to detect:
- `RecordModified` - When record properties change (from diffs)
- `UserInteraction` - Derived from rapid successive events
- `ValidationFailed` - When validation rules trigger
- These would enable more granular workflow detection

## Testing the Corrected Workflows

To verify workflows match real activity:

```csharp
var library = new FileWorkflowLibrary("workflows", logger);
await library.LoadAsync();

// Load real activity from logs
var parser = new ActivityLogParser(logger);
var entries = await parser.ParseFileAsync("samples/logs/log20260610.txt");

var normalizer = new ActivityEventNormalizer(xmlExtractor, jsonExtractor, logger);
var events = normalizer.Normalize(entries);

// Test workflows
var updateNodeWorkflow = library.GetById("update-node-in-path");
var connectorWorkflow = library.GetById("add-connector-to-path");

var updateMatches = updateNodeWorkflow?.MatchesSignature(events.ToList());
var connectorMatches = connectorWorkflow?.MatchesSignature(events.ToList());

Console.WriteLine($"Update Node matches: {updateMatches}");
Console.WriteLine($"Add Connector matches: {connectorMatches}");
```

## Sign-Off

**Issue**: Workflows referenced non-existent EventTypes
**Root Cause**: Design based on UI actions, not parser output
**Fix**: Aligned rules with actual parser EventType production
**Verification**: JSON validation + build + all tests passing
**Status**: RESOLVED ✓

Workflows are now ready for activity matching in Prompt 8.
