# Aktavara Workflow Intelligence - Project Context

## Tech stack
- .NET 10, C#, clean architecture
- JSON API format with $type/$id/$ref referencing
- Legacy XML format also exists (lower priority)

## Solution structure
- Core (domain models, parser, extractor, normalizer, matcher, library)
- Api (minimal API)
- Cli (test harness)
- Tests (xUnit)

## Current status
- Prompts 1-18 complete ✓
- 197 tests passing (.NET), TypeScript compiles clean, 0 errors
- CLI: 5 commands with --question flag for semantic search (parse, analyze, guided, list-workflows, validate)
- API: 11 endpoints with semantic search support (analyze, workflows with status persistence, help-guides with section extraction)
- Help guides: 29 Aktavara documentation chapters with section parsing, workflow-step mapping
- Parser: working, handles JSON and XML formats
- AktavaraSchemaTypes.cs: complete enum set from Swagger
- WorkflowLibrary: loads *.workflow.json from workflows/ folder with hot reload support
- WorkflowMatcher: 10-factor scoring, confidence levels High/Medium/Low
- ActivityContextBuilder: determines current state, active entities, workflow hints
- Normalizer: fully working with WorkspaceKind extraction
- SemanticWorkflowSearch: keyword-based deterministic scoring with stop-word filtering
- AmbiguityDetection: 8 decision rules for activity vs semantic match scenarios
- MCP Server: TypeScript proxy to API, 5 tools, 4 resources, stdio transport
- Supports: Claude Desktop, VS Code, Cursor, any MCP client
- Upload endpoint: processes historical logs with time-window relative to last event

## Prompt 9 Completed (Activity Context Service)
- IActivityContextBuilder interface with single BuildContext method
- ActivityContext model enhanced with CurrentState enum, SessionId, WorkflowHints
- CurrentState determination from most recent event (8 possible states)
- ActiveEntities identification (Path, Node, Connector with names and metadata)
- WorkflowHints generation (rapid sequences, batch operations, open-save patterns)
- 12 comprehensive unit tests covering all scenarios
- CLI integration: context displayed before workflow matching

## Prompt 10 Completed (Assistant Context Packet Generator)
- GuidanceLevel enum (NoGuidance, Suggest, Confirm, Instruct)
- WorkflowMatchSummary model with detailed match information
- AssistantContextPacket model enhanced with:
  - CurrentState, WorkflowHints, ActiveEntities (serializable)
  - BestMatch, AllMatches (WorkflowMatchSummary list)
  - GuidanceLevel determination from confidence scores
  - RecommendedNextStep from workflow state definitions
  - ContextNarrative for LLM system prompts
  - ToJson() method for API integration
- IAssistantContextPacketGenerator interface
- AssistantContextPacketGenerator service:
  - Converts WorkflowMatchResult to summaries
  - Formats evidence as human-readable descriptions
  - Builds context narratives deterministically
  - 11 comprehensive unit tests
- CLI integration: packet displayed with guidance level and narrative

## Known issues being worked on
- $ref resolution returning 0 nodes/connectors on most workspace opens
- ActivityContext correlation with full workspace snapshots (future enhancement)

## Sample logs
- samples/logs/log20260608.txt (old XML format, 3 actions)
- samples/logs/log20260610.txt (new JSON format, 180 entries, 87 events)

## Workflow library
- workflows/update-node-in-path.workflow.json
- workflows/add-connector-to-path.workflow.json

## Prompt 11 Completed (CLI Completion)
- 5 CLI commands: parse, analyze, guided, list-workflows, validate
- parse: parses log file (no processing)
- analyze: parses, normalizes, matches workflows, generates context packet, outputs JSON with --verbose flag
- guided: time-window filtered analysis for active user guidance (defaults to 30 min window)
- list-workflows: enumerates loaded workflows with rule and state counts
- validate: validates all *.workflow.json files in directory
- Logging: per-event logs moved to debug level, only rule match/miss at info level
- 3 new tests for guided mode: time window filtering, user filtering, guidance generation

## Prompt 12 Completed (Minimal API)
- 6 REST endpoints for React UI and external tools
- File upload endpoint: POST /api/analyze/upload (multipart/form-data, max 10MB, .txt only)
- Text analysis endpoint: POST /api/analyze/text with time window filtering
- Workflow listing: GET /api/workflows (returns summaries with counts)
- Workflow detail: GET /api/workflows/{id} (full definition)
- Workflow status update: PATCH /api/workflows/{id}/status
- Health check: GET /api/health
- Response models: AnalyzeResponse, WorkflowCandidateResult, WorkflowSummary
- Full DI container with all Core services
- CORS policy for localhost:3000, localhost:5173 (React dev ports)
- OpenAPI/Swagger support in development
- 7 API integration tests
- Configuration in appsettings.json (workflows path, time window, max file size)

## Prompt 14 Completed (Semantic Workflow Search & Ambiguity Detection)
- KeywordSemanticWorkflowSearch service (deterministic, no LLM required):
  - Scoring algorithm: Name match 0.5 exact phrase/0.4 all words/0.1 per word (max 0.3), Description 0.05 per word (max 0.2), Tags 0.15 per tag (max 0.3), Guide titles 0.05 per word (max 0.2)
  - Stop-word filtering: ignores 14 common words (i, am, trying, to, a, the, is, in, and, or, my, for, of, with, this, that, want, need, how, do, can, please, help)
  - Only returns matches with score > 0.1, ordered by relevance
  - IsAvailable property false (keyword) vs true for future embeddings
- AmbiguityDetector service with 8 decision rules:
  - Analyzes activity-based vs semantic matches
  - Recommends: UseActivity, UseSemantic, AskClarification, or NoMatch
  - Generates clarification questions for ambiguous cases
- AssistantContextPacket enhanced:
  - SemanticMatches: list of SemanticWorkflowMatch (WorkflowId, WorkflowName, Score, MatchedTerms, MatchedFields, Reason)
  - Ambiguity: AmbiguitySignal (IsAmbiguous, ActivityMatchId, SemanticMatchId, ActivityConfidence, SemanticScore, RecommendedAction, ClarificationQuestion)
  - UserText: original user search text
- AssistantContextPacketGenerator updated:
  - Accepts optional ISemanticWorkflowSearch dependency
  - Accepts optional userText parameter to GeneratePacket
  - Calls SearchAsync if userText provided, then detects ambiguity
- CLI analyze command: new --question flag
  - Format: dotnet run -- analyze --log samples/logs/log20260610.txt --question "I am trying to connect two nodes"
  - Displays semantic matches and ambiguity signal
- API endpoints updated:
  - POST /api/analyze/upload: userQuestion form field
  - POST /api/analyze/text: userQuestion body field (added to AnalyzeTextRequest)
  - AnalyzeResponse: SemanticMatches and Ambiguity properties
- Registered in DI as singleton (both CLI and API)
- 22 new tests (10 KeywordSemanticWorkflowSearch + 9 AmbiguityDetector + 3 integration)
- 197 total tests passing

## Prompt 13a Completed (Help Guide System Refactor)
**MAJOR REFACTOR:** Switched from per-step custom guides to working with existing 29-chapter Aktavara documentation
- Refactored HelpGuide model: now contains Sections list with parsed content
- New HelpGuideSection model: SectionId, Heading, Level, Content, ParentSectionId, RelevantStepIds
- FileHelpGuideStore completely rewritten:
  - Loads all 30 markdown files (skips index.md)
  - Extracts title from first # heading
  - Infers WorkspaceType from filename (Path_Workspace.md → "Path", files without _Workspace → "General")
  - Parses ## and ### headings into sections
  - Preserves all markdown: callouts (> ⚠️, > 💡), images, lists, formatting
  - Proper section hierarchy (### linked to parent ##)
  - SectionId slugification with collision handling (lowercase, spaces→hyphens)
- Created workflow-guide-mapping.json with 8 mappings (update-node-in-path, add-connector-to-path steps to guide sections)
- Updated IHelpGuideStore: GetByFileName, GetByWorkflowAndStep, GetWorkspaceTypes
- AssistantContextPacket: RelevantGuideSections property (up to 2 sections per workflow step)
- Updated API: 4 help-guide endpoints for summaries, detail, sections by workflow, by workspace
- 14 comprehensive new tests for section parsing, hierarchy, workspace types, workflow mapping
- 175 total tests passing

## Prompt 13 Completed (Help Guide Store)
- HelpGuide model: Full content with metadata
- IHelpGuideStore interface: Query by ID, workflow, or step
- FileHelpGuideStore service: Loads markdown files with YAML front matter
  - Lazy initialization with in-memory caching
  - Graceful error handling for invalid files
  - Extracts: helpGuideId, workflowId, stepId, title, tags, isAutoGenerated
- 40+ markdown help guide files in help-guides/ folder
- 4 workflow-specific guides with detailed markdown content:
  - update-node-in-path/save-modified-node.md
  - update-node-in-path/open-path-workspace.md
  - add-connector-to-path/select-nodes-to-connect.md
  - add-connector-to-path/save-connector.md
- AssistantContextPacket integration: Includes up to 3 relevant help guides
- 3 new API endpoints for help guides:
  - GET /api/help-guides (list with references)
  - GET /api/help-guides/{helpGuideId} (full content)
  - GET /api/help-guides/workflow/{workflowId} (workflow-specific)
- 9 comprehensive unit tests for help guide store
- Configuration in appsettings.json (help guides path)

## Prompt 15 Completed (MCP Server)
**TypeScript MCP Server** — Exposes workflow intelligence to LLM clients via Model Context Protocol
- **Architecture:** Thin proxy to .NET API (no processing, no LLM calls)
- **Transport:** stdio (standard MCP protocol)
- **Configuration:** Environment variables (AKTAVARA_API_URL, NODE_TLS_REJECT_UNAUTHORIZED)

**5 Tools:**
1. `analyze_activity` — Parse logs and return formatted markdown summary
2. `detect_current_task` — Return JSON with detected workflow, state, next step
3. `get_workflow_definition` — Return workflow definition as markdown
4. `get_help_guide` — Return help guide content or table of contents
5. `explain_next_step` — Complete LLM-ready context block with documentation

**4 Resources:**
1. `akta://workflows` — List all workflows (JSON)
2. `akta://workflows/{id}` — Individual workflow definition (JSON)
3. `akta://help-guides/{id}` — Help guide content (Markdown)
4. `akta://health` — API health status (JSON)

**Project Structure:**
- src/index.ts — MCP server entry point
- src/apiClient.ts — Typed fetch wrapper
- src/tools/ — 5 tool implementations
- src/resources/ — Resource providers
- src/types/apiTypes.ts — TypeScript interfaces matching API responses
- src/test-manual.ts — Manual test suite (7 sequential tests)
- tsconfig.json, package.json — TypeScript configuration
- README.md — Setup and configuration guide

**Build:** `npm run build` (clean TypeScript, no errors)
**Dev:** `npm run dev` (hot reload)
**Prod:** `npm start` (stdio transport)
**Test:** `npm run test:manual` (7 sequential tests)

**Claude Desktop Config:**
```json
{
  "mcpServers": {
    "aktavara-workflow": {
      "command": "node",
      "args": ["/path/to/dist/index.js"],
      "env": {
        "AKTAVARA_API_URL": "https://localhost:7200",
        "NODE_TLS_REJECT_UNAUTHORIZED": "0"
      }
    }
  }
}
```

**Dependencies:**
- @modelcontextprotocol/sdk — MCP protocol implementation
- zod — Input validation for tool arguments
- TypeScript, tsx, @types/node — Development tooling

**Design Principles:**
- Proxy-only (no intelligence in server)
- Type-safe (full TypeScript + Zod validation)
- Streaming-friendly (markdown output for LLM injection)
- Resource-based (expose data via MCP resources)
- Composable (tools work together)

**Supports:** Claude Desktop, VS Code Copilot, Cursor, any MCP-compatible client

## Prompt 17 Completed (API Endpoint Audit)
**API Audit and Completion** — Verified all endpoints are production-ready for React UI integration
- Audited 10 endpoints: 2 analyze endpoints + 8 workflow/help-guide endpoints
- All endpoints verified working with correct validation and error handling
- Added WorkshopQuestions field to WorkflowCandidateResult (state-specific guidance)
- Updated workflow JSON files: added workshopQuestions to all states
  - update-node-in-path: 2 states × 3 questions each
  - add-connector-to-path: 3 states × 3 questions each
- API handlers now populate WorkshopQuestions from workflow metadata
- Enhanced CORS: added support for Vite's alternate port (localhost:5174)
- All 197 .NET tests passing, 0 errors
- Full AnalyzeResponse includes: session ID, timing, state, guidance, matches, ambiguity, active entities, hints, workflow candidates with workshop questions
- Fixed upload endpoint time window: now calculates relative to last event in historical logs (not current time)

## Prompt 18 Completed (Workflow Endpoints Audit)
**Workflow Management Endpoints** — Verified and enhanced all 3 workflow endpoints + added reload
- **GET /api/workflows** — Returns List<WorkflowSummary>
  - Status: reads from workflow.Status enum (displays as "Active", "Draft", etc.)
  - Version: reads from workflow.Version definition
  - Tags: reads from workflow.Tags (populated from workflow JSON)
  - ConfidenceThreshold: reads from workflow.MinimumConfidenceThreshold (0.6, 0.65)
  - RiskLevel: reads from workflow.Metadata["riskLevel"] with fallback to "Medium"
  - All 9 required fields present and populated from actual workflow data
- **GET /api/workflows/{id}** — Returns full WorkflowDefinition with all metadata
  - Complete workflow definition including states, actions, activitySignature
  - All nested objects and collections properly serialized
- **PATCH /api/workflows/{id}/status** — Updates and persists workflow status
  - Accepts UI statuses: "Approved", "Candidate", "Deprecated"
  - Maps to JSON values: "Active" (Approved), "Draft" (Candidate), "Deprecated"
  - Persists changes to workflow JSON files on disk
  - Updates lastModifiedDate timestamp automatically
  - Returns updated WorkflowSummary confirming change
- **POST /api/workflows/reload** — Development-only endpoint (new)
  - Forces reload of all workflow definitions from disk
  - Returns ReloadResponse: { reloadedCount, timestamp, loadedWorkflowIds }
  - Only available when environment = Development
- All endpoints tested and verified production-ready
- All 197 .NET tests passing
- UI-to-storage mapping enables React components to use "Approved/Candidate" terminology

## Next prompts
- Prompt 19: React UI (frontend/dashboard)
- Prompt 20+: Real embedding-based semantic search (swap KeywordSemanticWorkflowSearch for EmbeddingSemanticWorkflowSearch)
- Extended features and optimizations

## Key design rule
LLM does not parse, match, or make safety decisions.
All deterministic work happens in Core.

## Future: OpenTelemetry integration
Aktavara has existing OTEL telemetry instrumentation.
Long-term: replace ActivityLogParser with OtelTraceParser reading 
from the OTEL backend directly. Same Core pipeline from normalizer 
onwards. Benefits: better session correlation, timing data, 
validation messages, failure events — all without activity log changes.
Defer until after initial demo is complete.