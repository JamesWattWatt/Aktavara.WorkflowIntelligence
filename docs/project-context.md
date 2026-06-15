# Aktavara Workflow Intelligence - Project Context

## Tech stack
- .NET 10, C#, clean architecture
- JSON API format with $type/$id/$ref referencing (Akta.Json.StringObject etc.)
- Legacy XML format also exists (lower priority)
- TypeScript MCP server (aktavara-workflow-mcp/)
- React + TypeScript front-end (workflow-ui/) — in progress

## Solution structure
- Core (domain models, parser, extractor, normalizer, 
  matcher, library, context builder, packet generator,
  help guide store, semantic search)
- Api (minimal API, 10 endpoints)
- Cli (5 commands: parse, analyze, guided, 
  list-workflows, validate)
- Tests (xUnit, 197 tests)
- aktavara-workflow-mcp/ (TypeScript MCP server)
- workflow-ui/ (React UI — Prompt 19 in progress)

## Current status
- Prompts 1-18 complete
- 197 tests passing, 0 errors
- API running on http://localhost:5112
- MCP server: aktavara-workflow-mcp/
- Help guides: 29 chapters in help-guides/
- Workflow library: 2 workflows in workflows/

## API endpoints (all working)
POST /api/analyze/upload — file upload, full pipeline
POST /api/analyze/text — raw text, full pipeline
GET  /api/workflows — list all workflows
GET  /api/workflows/{id} — full workflow definition
PATCH /api/workflows/{id}/status — update status
POST /api/workflows/reload — force reload (dev only)
GET  /api/health — health check
GET  /api/help-guides — list all guides
GET  /api/help-guides/{helpGuideId} — full guide
GET  /api/help-guides/section?workflowId=X&stepId=Y

## Important: workflow status mapping
Storage values → UI values:
- "Active" → "Approved"
- "Draft" → "Candidate"  
- "Deprecated" → "Deprecated"
PATCH /api/workflows/{id}/status accepts:
"Approved", "Candidate", or "Deprecated"

## Important: upload time window
POST /api/analyze/upload calculates time window 
relative to LAST EVENT in the log file, not system 
clock. This ensures historical logs always produce 
meaningful results.

## Known issues / warnings
- 29 nullable reference warnings in Core/Api 
  (CS8602, CS8604, CS8625) — non-blocking
- ASPDEPR002 warnings for WithOpenApi — .NET 10 
  deprecation, non-blocking
- AktaXmlExtractor.ExtractBooleanResult throws on 
  JSON payloads — logged at debug level, non-blocking
- $ref resolution returns 0 nodes/connectors on some
  workspace opens — Issue 3, deferred

## Sample logs
- samples/logs/log20260608.txt (old XML format, 3 actions)
- samples/logs/log20260610.txt (new JSON format, 
  180 entries, 196 events, primary test file)

## Workflow library
- workflows/update-node-in-path.workflow.json
- workflows/add-connector-to-path.workflow.json
Both have workshopQuestions on each state.

## Help guides
29 markdown chapters in help-guides/
Mapping file: help-guides/workflow-guide-mapping.json
Key files: Path_Workspace.md, Connections_Workspace.md,
Network_Explorer.md, Topology_Workspace.md

## MCP server
Location: aktavara-workflow-mcp/
5 tools: analyze_activity, detect_current_task,
  get_workflow_definition, get_help_guide, 
  explain_next_step
4 resources: akta://workflows, akta://workflows/{id},
  akta://help-guides/{id}, akta://health
Config for Claude Desktop:
{
  "mcpServers": {
    "aktavara-workflow": {
      "command": "node",
      "args": ["aktavara-workflow-mcp/dist/index.js"],
      "env": {
        "AKTAVARA_API_URL": "http://localhost:5112"
      }
    }
  }
}

## Next prompts
- Prompt 19: React scaffold (Vite + TypeScript)
- Prompt 20: Log drop zone + candidate list
- Prompt 21: Flow visualiser
- Prompt 22: Workshop qualification panel
- Prompt 23: Library management UI + inference

## Key design rules
- LLM does not parse, match, or make safety decisions
- All deterministic work happens in Core
- React UI calls API only, never Core directly
- MCP server is a thin proxy, no intelligence

## Future considerations
- OpenTelemetry integration (replace activity log parser)
- Real embedding provider for semantic search
- Temporal for governed workflow execution
- More log samples needed for inference pipeline
