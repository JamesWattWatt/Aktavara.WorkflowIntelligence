# Aktavara Workflow Intelligence MCP Server

A Model Context Protocol (MCP) server that exposes the Aktavara Workflow Intelligence pipeline to LLM clients (Claude, Cursor, VS Code Copilot, etc).

The MCP server is a **thin proxy** — it does NOT parse logs, match workflows, or call any LLM. All intelligence stays in the .NET API.

## Setup

### Prerequisites

- Node.js 18+
- TypeScript
- The Aktavara Workflow Intelligence API running (see main README)

### Installation

```bash
cd aktavara-workflow-mcp
npm install
npm run build
```

### Configuration

The server reads configuration from environment variables:

- `AKTAVARA_API_URL` — URL of the Workflow Intelligence API (default: `https://localhost:7200`)
- `AKTAVARA_API_KEY` — Optional API key for authentication (not yet implemented)
- `NODE_TLS_REJECT_UNAUTHORIZED=0` — For local development with self-signed certificates

### Running

```bash
# Development mode (with hot reload)
npm run dev

# Production mode
npm start
```

The server listens on stdio and outputs debug logs to stderr.

## Configuring in Claude Desktop

Edit `~/.claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "aktavara-workflow": {
      "command": "node",
      "args": ["/path/to/aktavara-workflow-mcp/dist/index.js"],
      "env": {
        "AKTAVARA_API_URL": "https://localhost:7200",
        "NODE_TLS_REJECT_UNAUTHORIZED": "0"
      }
    }
  }
}
```

## Configuring in VS Code

Edit `.vscode/settings.json`:

```json
{
  "modelcontextprotocol.servers": {
    "aktavara-workflow": {
      "command": "node",
      "args": ["./aktavara-workflow-mcp/dist/index.js"],
      "env": {
        "AKTAVARA_API_URL": "https://localhost:7200",
        "NODE_TLS_REJECT_UNAUTHORIZED": "0"
      }
    }
  }
}
```

## Tools

### 1. `analyze_activity`

Analyze recent Aktavara user activity from a log snippet to detect workflow patterns and provide guidance.

**Inputs:**
- `logContent` (string, required) — Raw activity log text
- `userName` (string, required) — The Aktavara user name
- `timeWindowMinutes` (number, optional, default 30) — Time window to consider
- `userQuestion` (string, optional) — What the user says they are trying to do

**Output:** Formatted markdown summary including current state, detected workflow, guidance level, and help documentation.

**Example:**

```
Claude: I've been working with paths in Aktavara. Can you analyze my recent activity?

Use analyze_activity with:
- logContent: [copy from log file]
- userName: "jsmith"
- userQuestion: "I want to add a connector between two nodes"
```

### 2. `detect_current_task`

Detect what workflow task the user is currently performing based on activity and optional description.

**Inputs:** Same as `analyze_activity`

**Output:** JSON object with:
- `currentState` — Current state (e.g., "PathOpened")
- `detectedWorkflow` — {id, name, confidence, level}
- `nextStep` — Recommended next step
- `activeEntities` — What the user is working with
- `ambiguity` — Ambiguity signal if present
- `semanticMatches` — Results from user's question (if provided)

**Example:**

```json
{
  "currentState": "NodeModified",
  "detectedWorkflow": {
    "id": "update-node-in-path",
    "name": "Update node in path",
    "confidence": 0.85,
    "level": "High"
  },
  "nextStep": "Save the modified node",
  "activeEntities": [
    { "kind": "Path", "name": "Main Path", "id": "path-001" },
    { "kind": "Node", "name": "Node A", "id": "node-123" }
  ],
  "ambiguity": null,
  "semanticMatches": []
}
```

### 3. `get_workflow_definition`

Get the full definition of a named workflow including its steps, states, and rules.

**Inputs:**
- `workflowId` (string, required) — E.g., "update-node-in-path"

**Output:** Formatted markdown with workflow name, description, activity signature, and state transitions.

### 4. `get_help_guide`

Get help guide content for a specific Aktavara workspace or feature.

**Inputs:**
- `helpGuideId` (string, required) — E.g., "Path_Workspace"
- `sectionId` (string, optional) — Specific section within the guide

**Output:** Markdown content. If no section is specified, returns table of contents.

**Available Guides:**
- `Path_Workspace` — Working with path workspaces
- `Topology_Workspace` — Working with topology workspaces
- `General` — General Aktavara help

### 5. `explain_next_step`

Get a complete explanation of what the user should do next, grounded in their activity and relevant help documentation.

**Inputs:** Same as `analyze_activity`

**Output:** Complete context block with:
- Current situation and active entities
- Detected task with confidence
- Recommended next step
- Relevant documentation sections
- Clarification questions (if ambiguity detected)

This output is designed to be injected directly into an LLM response.

## Resources

Resources expose Aktavara data as MCP resources:

### `akta://workflows`
List all available workflow definitions (JSON).

### `akta://workflows/{workflowId}`
Full workflow definition by ID (JSON).

### `akta://help-guides/{helpGuideId}`
Help guide content (Markdown).

### `akta://health`
API health status (JSON).

**Example:** Claude can read `akta://workflows/update-node-in-path` to get the full workflow definition.

## Testing

### Manual Test Suite

```bash
npm run test:manual
```

This runs 7 sequential tests:
1. API health check
2. Workflow list fetch
3. Workflow definition fetch
4. Activity analysis without question
5. Activity analysis with question (semantic search)
6. Help guide fetch
7. Help guide section fetch

Each test prints results to stdout.

## Troubleshooting

### "Connection refused" / API not reachable

Make sure the Aktavara Workflow Intelligence API is running:

```bash
# In the main solution directory
dotnet run --project Aktavara.WorkflowIntelligence.Api
```

### "self-signed certificate" error

For local development, set:

```bash
export NODE_TLS_REJECT_UNAUTHORIZED=0
npm run dev
```

Or in your MCP configuration:

```json
{
  "env": {
    "NODE_TLS_REJECT_UNAUTHORIZED": "0"
  }
}
```

### TypeScript compilation errors

Make sure all dependencies are installed:

```bash
npm install
npm run build
```

### MCP server won't start in Claude Desktop

1. Check that the path to `dist/index.js` is correct
2. Verify the API URL is accessible
3. Check logs in Claude Desktop (Help → Show Logs)

## Architecture

```
aktavara-workflow-mcp/
  src/
    index.ts              — MCP server entry point
    apiClient.ts          — Typed fetch wrapper for API
    tools/
      analyzeActivity.ts
      detectTask.ts
      getWorkflow.ts
      getHelpGuide.ts
      explainNextStep.ts
    resources/
      workflowResources.ts
      helpGuideResources.ts
    types/
      apiTypes.ts         — TypeScript interfaces
    test-manual.ts        — Manual test suite
  dist/                   — Compiled JavaScript (generated)
  package.json
  tsconfig.json
  README.md
```

## Design Principles

1. **Proxy, not processor** — All computation happens in the .NET API
2. **Type-safe** — Full TypeScript with Zod validation
3. **Streaming-friendly** — Tools return formatted markdown for LLM injection
4. **Error handling** — Descriptive errors with context
5. **Composable** — Tools can be used together for complex workflows
6. **Resource-based** — MCP resources expose data for LLM context

## Future Work

- Real embedding-based semantic search (Prompt 15+)
- Authentication with API key validation
- Streaming responses for large help guides
- Caching layer for frequently accessed resources
- Integration with Claude's memory system

## License

Same as Aktavara.WorkflowIntelligence
