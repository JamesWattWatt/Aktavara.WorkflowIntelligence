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
- Prompts 1-13a complete ✓
- 175 tests passing (128 existing + 12 ActivityContextBuilder + 11 AssistantContextPacketGenerator + 3 GuidedMode + 7 ApiIntegration + 14 HelpGuideStore), 0 errors
- CLI: 5 commands (parse, analyze, guided, list-workflows, validate)
- API: 10 endpoints (analyze, workflows, help-guides with section extraction)
- Help guides: 29 Aktavara documentation chapters with section parsing, workflow-step mapping
- Parser: working, handles JSON and XML formats
- AktavaraSchemaTypes.cs: complete enum set from Swagger
- WorkflowLibrary: loads *.workflow.json from workflows/ folder
- WorkflowMatcher: 10-factor scoring, confidence levels High/Medium/Low
- ActivityContextBuilder: determines current state, active entities, workflow hints
- Normalizer: fully working with WorkspaceKind extraction
- CLI analyze: now outputs activity context summary before workflow matching results

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

## Next prompts
- Prompt 14: React UI integration and frontend
- Prompt 15+: Extended features and optimizations

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