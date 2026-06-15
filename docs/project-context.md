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
- Prompts 1-22a complete
- 200 tests passing, 0 errors
- API running on http://localhost:5112
- React UI (Vite) running on http://localhost:5173 with full functionality
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

## React UI
Location: workflow-ui/
Framework: React 18 + TypeScript + Vite + Tailwind CSS
Dev server: http://localhost:5173

Architecture:
- src/types/api.ts — Complete TypeScript interfaces matching all API models
- src/services/apiClient.ts — Typed fetch wrapper with 6 methods
- src/components/ — 6 components (Prompts 19-22 complete)
  - LogDropZone: ✓ Full file upload with drag/drop, validation, loading, error handling
  - WorkflowList: ✓ Candidate cards with confidence bars (color-coded), evidence count
  - WorkflowDetail: ✓ Rules, evidence tags, score table, hints, workshop preview
  - FlowVisualiser: ✓ Matched/missing step flow, evidence tags, confidence explanation, next hints
  - WorkshopPanel: ✓ Workflow name editor, questions, execution decisions, status controls, session export
  - AnalysisSummary: ✓ Collapsible summary with guidance badge, next step, context
- src/App.tsx — Two-column layout with tabbed interface
  - Left (280px): drop zone, summary, candidate list with selection
  - Right: tabs for Details and Workshop, flow visualization
  - Empty states for no file, no candidates, no selection
  - Dark/light theme with Tailwind classes

Features (Prompts 20-22):
- LogDropZone: drag-over visual state, file preview, spinner, validation, errors, clear button
- WorkflowList: colored confidence bars (green/amber/red), evidence count, selected indicator
- WorkflowDetail: evidence tags, missing rules in red, score breakdown table, hints, workshop preview
- FlowVisualiser: matched steps (green), missing steps (amber), color-coded dots, evidence linking, confidence explanation
- WorkshopPanel: ✓ Name editor, qualification questions with notes, execution decision (3 options), status controls (Approve/Candidate/Deprecate), session export (JSON), help guide preview
- AnalysisSummary: guidance badge (color-coded), recommended next step, collapsible
- Error handling: friendly API errors, file validation (type, size), empty states
- Layout: Fixed header, independently scrollable panels, natural page scroll at 100% zoom
- State management: React useState for all data, no localStorage persistence

API Proxy: vite.config.ts routes /api to http://localhost:5112

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
- Prompt 23: Library management UI (workflow CRUD, bulk operations, status updates, validation)
- Prompt 24: E2E testing (Playwright, critical user paths, accessibility)
- Prompt 25: Deployment & hosting (Docker, CI/CD, cloud setup)

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


## Future: Database migration (required for horizontal scaling)

Current FileWorkflowLibrary and FileHelpGuideStore use 
local JSON/markdown files — not suitable for multi-instance 
deployment.

Migration path:
- Add DatabaseWorkflowLibrary implementing IWorkflowLibrary
- Add DatabaseHelpGuideStore implementing IHelpGuideStore  
- Swap in Program.cs DI registration
- Help guide markdown → blob storage (Azure/S3)
- Help guide metadata/sections → SQL DB
- Suggested: EF Core + PostgreSQL or Azure SQL
- Schema design: see docs/DATABASE_SCHEMA.md (to be created)

Trigger: before first hosted/cloud deployment