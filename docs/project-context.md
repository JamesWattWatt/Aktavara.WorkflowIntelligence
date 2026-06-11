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
- Prompts 1-11 complete ✓
- 154 tests passing (128 existing + 12 ActivityContextBuilder + 11 AssistantContextPacketGenerator + 3 GuidedMode), 0 errors, 0 warnings
- CLI: 5 commands (parse, analyze, guided, list-workflows, validate)
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

## Next prompts
- Prompt 12: API integration and deployment
- Prompt 13-23: Extended features and optimizations

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