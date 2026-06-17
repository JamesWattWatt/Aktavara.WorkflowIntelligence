# Aktavara Workflow Intelligence - Project Context

## Tech stack
- .NET 10, C#, clean architecture
- JSON API format with $type/$id/$ref referencing (Akta.Json.StringObject etc.)
- Legacy XML format also exists (lower priority)
- TypeScript MCP server (aktavara-workflow-mcp/)
- React + TypeScript front-end (workflow-ui/) (React UI complete through Prompt 22b)

## Solution structure
- Core (domain models, parser, extractor, normalizer, 
  matcher, library, context builder, packet generator,
  help guide store, semantic search)
- Api (minimal API, 10 endpoints)
- Cli (5 commands: parse, analyze, guided, 
  list-workflows, validate)
- Tests (xUnit, 200 tests)
- aktavara-workflow-mcp/ (TypeScript MCP server)
- workflow-ui/ (React UI complete through Prompt 22b)

## Current status
- Prompts 1-25a complete
- 215 tests passing, 0 errors
- API running on http://localhost:5112 with all endpoints functional (including PUT/POST/DELETE workflows)
- React UI (Vite) running on http://localhost:5173 with full functionality
- MCP server: aktavara-workflow-mcp/
- Help guides: 29 chapters in help-guides/
- Workflow library: 2 workflows in workflows/
- Intelligent help guide matcher: LLM-driven guide discovery with human approval
- Offline discovery service: fully implemented with 10 inference methods, working inference endpoints
- Library management UI: full CRUD with inference modal, import/export (Prompt 23b)
- Layout: two-column discovery (280px candidates | flex content), horizontal steps, collapsible evidence (Prompt 24d)
- Contextual help system: sliding panel with markdown rendering, 7 help topics (Prompt 24b)

## API endpoints (all working)
POST /api/analyze/upload — file upload, full pipeline
POST /api/analyze/text — raw text, full pipeline (fixed null reference issue)
GET  /api/workflows — list all workflows
GET  /api/workflows/{id} — full workflow definition
GET  /api/workflows/library — list workflows with metadata (Prompt 23a)
POST /api/workflows/infer — infer workflow from activity logs, returns InferredWorkflowSuggestion (Prompt 23a)
POST /api/workflows/infer/name — get LLM-suggested workflow name via Claude API (Prompt 23a)
PATCH /api/workflows/{id}/status — update status
POST /api/workflows/reload — force reload (dev only)
GET  /api/health — health check
GET  /api/help-guides — list all guides
GET  /api/help-guides/{helpGuideId} — full guide
GET  /api/help-guides/section?workflowId=X&stepId=Y
POST /api/help-guides/suggest — LLM suggest guide for step (Prompt 22b)
POST /api/help-guides/mapping — save approved guide mapping (Prompt 22b)

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

Features (Prompts 20-22b):
- LogDropZone: drag-over visual state, file preview, spinner, validation, errors, clear button
- WorkflowList: colored confidence bars (green/amber/red), evidence count, selected indicator
- WorkflowDetail: evidence tags, missing rules in red, score breakdown table, hints, workshop preview
- FlowVisualiser: matched steps (green), missing steps (amber), color-coded dots, evidence linking, confidence explanation
- WorkshopPanel: ✓ Name editor, questions with notes, execution decision (3 options), status controls, session export (JSON), help guide preview with LLM suggestion UI (Prompt 22b)
- AnalysisSummary: guidance badge (color-coded), recommended next step, collapsible
- Error handling: friendly API errors, file validation (type, size), empty states
- Layout: Fixed header, independently scrollable panels, natural page scroll at 100% zoom
- State management: React useState for all data, no localStorage persistence
- Guide suggestion (22b): "Suggest guide" button when no mapping exists, shows LLM suggestion with approval/dismiss buttons

API Proxy: vite.config.ts routes /api to http://localhost:5112

## Intelligent Help Guide Matcher (Prompt 22b)

IntelligentHelpGuideMatcher service (Api/Services/):
- Calls Claude Sonnet 4.6 API to suggest best guide section for a workflow step
- Input: workflowId, workflowName, stepId, currentStateName, matchedRules, matchedEvidence
- Builds available sections list from IHelpGuideStore (max 100)
- Constructs LLM prompt with workflow context and available guides
- Parses JSON response: { guideFile, sectionId, sectionHeading, reason }
- Returns GuideSuggestion with parsed result or null on failure
- Graceful degradation if API key not configured

HelpGuideMappingWriter service (Api/Services/):
- Reads/writes workflow-guide-mapping.json with atomic locking
- Normalizes stepId (spaces/hyphens to underscores, lowercase)
- Removes old mapping, adds new one, writes file with pretty-print JSON
- Calls IHelpGuideStore.Reload() to cache-invalidate after write
- Used by WorkshopPanel to persist user-approved suggestions

API Endpoints (Prompt 22b):
- POST /api/help-guides/suggest — LLM suggests guide for workflow step
- POST /api/help-guides/mapping — save approved guide mapping to file

React Integration (Prompt 22b):
- WorkshopPanel: suggestGuideMapping(), approveSuggestion(), dismissSuggestion()
- UI state: guideSuggestion, suggestingGuide, showSuggestionUI
- Shows suggestion with approval/dismiss when no existing mapping

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

## Prompt 23a: OfflineDiscoveryService (COMPLETE)
Core inference service for workflow discovery from activity logs:
- **IOfflineDiscoveryService**: Main interface for inference
- **OfflineDiscoveryService**: Implements 10 internal methods:
  - ClusterBySessions: group events by user/session with 60-min time breaks
  - ExtractActionSequences: convert events to (EventType, RecordKind) pairs
  - FindCommonSequences: identify patterns appearing in multiple sessions
  - CalculateActionFrequencies: frequency-based rule weighting
  - DeriveSignatureRules: generate WorkflowSignatureRule objects with required/optional flags
  - BuildStateModel: create WorkflowStateDefinition from sequences
  - DetectVariants: find workflow variations across sessions
  - InferRiskLevel: classify as High/Medium/Low based on operations
  - SuggestTags: extract from RecordKind and WorkspaceKind
  - GenerateWorkshopQuestions: context-aware questions for refinement
- **API Endpoints** (3 new):
  - POST /api/workflows/infer: accepts RawLogContent, returns InferredWorkflowSuggestion with rules, states, variants, evidence counts
  - POST /api/workflows/infer/name: calls Claude Sonnet to suggest business-friendly workflow names
  - GET /api/workflows/library: lists all workflows with metadata for library view
- **Models**:
  - InferredWorkflowSuggestion: rules, states, variants, risk level, evidence counts, inference notes
  - WorkflowVariant: detected variations with occurrence %, different steps, description
- **Enum Fixes**: Changed from integer tuples (int, int) to proper enum tuples (EventType, RecordKind?)
- **Bug Fix**: Fixed POST /api/analyze/text null reference in FileHelpGuideStore.GetByWorkflowAndStep
- **Tests**: 15 unit tests covering basic inference, risk detection, session clustering, variant detection, tag extraction, question generation, threshold calculation, weight normalization
- Status: 215 tests passing, 0 errors, all endpoints functional and tested

## Prompt 24a: Layout Fixes (COMPLETE)

Three layout improvements for the Discovery interface:

**FIX 1 - Full width layout (1200px minimum)**
- Added `min-w-[1200px]` to outer container
- Removed max-width centering constraints
- Three-column layout fills available width without horizontal scroll:
  - Left: 280px fixed (LogDropZone, AnalysisSummary, WorkflowList)
  - Middle: flexible width (FlowVisualiser with "Detected Steps" header)
  - Right: 380px fixed (Workflow Details/Workshop with sticky header)

**FIX 2 - Tab bar alignment**
- Moved Details/Workshop tabs above right column only (not both columns)
- Added "Detected Steps" header to middle column
- Clarifies visual hierarchy: tabs control right panel, not middle column
- Renamed full tab names to "Details"/"Workshop" to save space

**FIX 3 - Sticky workflow header**
- Added sticky header to top of right column showing:
  - Workflow name (bold, large)
  - Confidence score percentage (bold, green)
  - Confidence level badge (color-coded: green/yellow/red)
  - Current state name
- Header remains visible when scrolling Details/Workshop content
- z-index set to stay above tab bar during scroll
- Updates when different candidate selected

## Prompt 24b: Contextual Help System (COMPLETE)

Help system providing contextual guidance via sliding drawer panel:

**Components:**
- HelpPanel.tsx: Right-side sliding drawer (380px wide)
  - Smooth CSS transform transition (translateX)
  - Semi-transparent dark overlay (can click to close)
  - Sticky header with close button (×)
  - Markdown rendering: headings (## ###), bold (**text**), lists (- items), paragraphs
  - Closes on Escape key or overlay click
  - Dark theme support

- HelpIcon.tsx: Small (?) circle button (18px)
  - Grey border/text, blue on hover
  - Cursor: pointer, title: "Help"
  - Opens HelpPanel with content for specified key

**Help Content Library (7 topics):**
1. discovery-concept: About Workflow Discovery (upload, parse, match, confidence)
2. discovery-analysis: Analysis Panel (metrics, guidance level, context narrative)
3. discovery-workflow-details: Workflow Details (score, breakdown, evidence, rules, hints)
4. discovery-workshop: Workshop Qualification (name, questions, execution decision, approval)
5. library-concept: About Workflow Library (statuses, definitions, creation methods)
6. library-edit: Editing Workflows (5 editor tabs: Overview, Rules, States, Guides, JSON)

**Integration Points:**
- Header: Help icon next to Discovery/Library subtitle
- Discovery: Help icons next to Details and Workshop tab labels
- Analysis panel: Help icon next to ANALYSIS label
- Library: Help icon in Actions table column header
- State management: openHelp(key) callback passed to components

**Features:**
- Context-aware: Help key changes based on current tab/location
- Non-intrusive: Overlay allows dismissal, doesn't block interaction
- Accessible: Escape key closes panel
- Responsive: Works at all viewport sizes

## Prompt 24c: Column Proportions Fix (COMPLETE)

Fixed the three-column layout proportions for optimal content display:

**Layout Before:**
- Left: 280px fixed (LogDropZone, candidates list)
- Middle: flex-1 (fills remaining space)
- Right: 380px fixed (Workflow details/workshop)

**Layout After (Corrected):**
- Left: 280px fixed (drop zone, candidates)
- Middle: 320px fixed (Flow Visualiser/steps diagram)
- Right: flex-1 (fills all remaining width)

**Rationale:**
The right column contains the most critical content:
- Score breakdown table
- Matched rules and evidence
- Workshop questions and notes
- Requires maximum width for readability

The middle column (FlowVisualiser) is a vertical flow diagram
that works well at 320px width.

**Implementation:**
- Changed middle column from `flex-1` to `w-[320px] flex-shrink-0`
- Changed right column from `w-[380px] flex-shrink-0` to `flex-1`
- WorkflowDetail and WorkshopPanel use `w-full` on tables/content
- All padding and spacing maintained

**Proportions at 1920px viewport:**
- Left: 280px (14.6%)
- Middle: 320px (16.7%)
- Right: ~1300px (67.7%)

Gives content area maximum width for detailed views while
maintaining compact workflow list and step diagram visibility.

## Prompt 24d: Two-Column Layout Redesign (COMPLETE)

Major UI restructuring of Discovery tab from three-column to two-column layout:

**Layout Changes:**
- Left column: 280px fixed (LogDropZone, AnalysisSummary, WorkflowList)
- Right column: flex-1 (fills remaining space) - three stacked sections

**Section 1 - Workflow Info + Horizontal Detected Steps:**
- Workflow name (bold, 16px text)
- Subtitle: confidence % · level badge · current state
- "DETECTED STEPS" label
- Horizontal scrolling row of step cards with → arrows
- Step cards show:
  - Color dot (grey=search, blue=open, green=save, amber=modify)
  - Action name
  - "✓ Matched" or "✗ Missing" badge
  - Evidence snippet (record kind + timestamp)
- Matched steps: solid border, blue tint background
- Missing steps: dashed border, muted/dimmed
- Smooth horizontal scroll with thin scrollbar

**Section 2 - Evidence & Score (Collapsible):**
- Header: "Evidence & Score" with collapse/expand toggle
- Default: expanded (useState(true))
- Four horizontally scrollable cards (200px each):
  1. Matched evidence: green pill tags
  2. Matched rules: with green checkmarks
  3. Score breakdown: table with green/red values
  4. Next step: amber card with action hint
- Cards use overflow-x: auto with flex-shrink: 0
- Click header to toggle collapse state

**Section 3 - Workflow Details / Workshop Tabs:**
- Tab bar: "Workflow details" | "Workshop"
- Full-width content area below tabs
- Details tab shows:
  - Confidence explanation (info box)
  - WorkflowDetail component (rules, questions)
- Workshop tab shows: full WorkshopPanel
- Tabs control section 3 content only

**Components:**
- FlowVisualiser: Redesigned horizontal layout with color dots and arrows
- EvidenceSection: New collapsible component with 4 evidence cards
- App.tsx: New two-column discovery structure

**Styling:**
- Section cards: full width, proper spacing
- Evidence cards: fixed 200px width, flex-shrink-0
- Step cards: color-coded with visual flow
- Scrollbars: thin (4px), theme-aware
- All sections maintain dark theme support

**Result:**
- More focus on details (right column gets full width)
- Cleaner, more scannable layout
- Evidence and score data more accessible and readable
- Horizontal flow more intuitive for step sequences

## Prompt 25a: Auto-generate Workshop Questions (COMPLETE)

LLM-driven auto-generation of workshop questions for workflow states during creation, update, and inference.

**Core Service: WorkshopQuestionGenerator**
- Interface: `IWorkshopQuestionGenerator` (Core/Interfaces)
- Implementation: `WorkshopQuestionGenerator` (Api/Services)
- Calls Claude Sonnet 4.6 API with workflow context
- Generates exactly 3 questions per state
- Fallback questions on API failure or missing key
- Questions specific to workflow step, capture edge cases, plain business language

**API Updates:**
- POST /api/workflows/{id}/generate-questions: on-demand generation
- Updated InferWorkflowSuggestionAsync: generates questions for inferred states
- Updated CreateWorkflow/UpdateWorkflow: auto-generate for states with empty workshopQuestions
- POST /api/workflows/backfill-questions (dev only): backfill existing workflows

**OfflineDiscoveryService:**
- Made InferWorkflowSuggestionAsync (was synchronous)
- Injects IWorkshopQuestionGenerator
- Calls GenerateQuestionsForStatesAsync for each inferred state
- Questions populated in SuggestedStates before returning suggestion

**React UI (WorkflowEditor.tsx - States Tab):**
- "Generate all questions" button at top of States tab
- Per-state "Generate" button with loading spinner
- Display workshop questions below each state
- "Edit" button opens textarea for manual editing
- Questions appear as numbered list
- All changes saved via existing workflow save flow

**React UI (WorkshopPanel.tsx):**
- Auto-generates questions once per session when empty
- Shows loading state "Generating workshop questions..."
- Uses ref to track if generation attempted for current workflow
- No explicit user trigger needed
- Graceful fallback if generation fails

**Test Results:**
- 215 tests passing (all OfflineDiscoveryServiceTests updated to async)
- Backfill endpoint tested: { updated: 2, states: 6 }
- All compilation errors resolved

## Prompt 25b: Auto-suggest Guide Mappings (COMPLETE - 100%)

Comprehensive system for auto-suggesting guide mappings and managing their approval status.

**Part A - Workflow Auto-Suggest (COMPLETE)**
- POST /api/workflows (create) auto-suggests for each state
- PUT /api/workflows/{id} (update) auto-suggests for new states
- Calls IntelligentHelpGuideMatcher.SuggestAsync()
- Saves with isAutoGenerated: true

**Part B - Guides Tab Visual Distinction (COMPLETE)**
- Added GET /api/workflows/{id}/guide-mappings endpoint
- GuidesTab component redesigned with visual states:
  - **Green dot** - Approved mapping (isAutoGenerated: false)
  - **Amber dot** - Suggested mapping (isAutoGenerated: true)
  - **Grey dot** - No mapping
- For suggested mappings: card shows file, section, reason + Approve/Dismiss buttons
- Approve: POST /api/help-guides/mapping (sets isAutoGenerated: false)
- Dismiss: removes from display (doesn't persist)
- TypeScript types: GuideMapping, WorkflowGuideMappings

**Part C - Backfill Endpoint (COMPLETE)**
- POST /api/workflows/backfill-guide-mappings (dev only)
- Iterates all workflows and states
- Returns: { workflows: N, mappingsAdded: N }
- Tested: Creates 6 mappings across 2 workflows

**Part D - HelpGuideMappingWriter Flags (COMPLETE)**
- Fields: IsAutoGenerated, ApprovedAt, ApprovedBy
- Auto-generated: isAutoGenerated=true, approvedAt=null
- Approved: isAutoGenerated=false, approvedAt=UtcNow
- Saved to workflow-guide-mapping.json

**Part E - WorkshopPanel Badge Display (COMPLETE)**
- Loads current mapping on component mount
- Displays three states:
  - **Approved**: Green badge + guide content
  - **Suggested**: Amber badge + suggestion card (file, section, reason)
  - **None**: "Suggest Guide" button
- Approve button: reloads mappings, shows guide content
- Dismiss: hides suggestion
- Reload mappings after approval

**Status:**
- ✅ All 215 tests passing
- ✅ 0 TypeScript errors
- ✅ Full visual distinction in Guides tab
- ✅ Full badge/UI in WorkshopPanel
- ✅ Approve/Dismiss/Suggest workflow complete
- ✅ Prompt 25b 100% COMPLETE

## Prompt 25c: Pass Detected User Context Through UI (COMPLETE)

User context tracking for correct API calls in subsequent operations:

**API Changes:**
- Added `DetectedUser: string` field to AnalyzeResponse model
- HandleAnalyzeUpload: extracts detected user from events, sets `response.DetectedUser = detectedUser ?? ""`
- HandleAnalyzeText: sets `response.DetectedUser = userName ?? ""` after auto-detection
- Both endpoints now return the actual user that was used for analysis

**React UI Changes:**
- Updated TypeScript AnalyzeResponse interface to include `detectedUser: string`
- App.tsx: added state `const [detectedUser, setDetectedUser] = useState<string | null>(null)`
- handleAnalyzeResult: extracts `response.detectedUser` and stores in state
- Passes `detectedUser` prop to WorkshopPanel component

**WorkshopPanel Updates:**
- Added `detectedUser?: string | null` to props interface
- Component receives detected user for future analyze endpoint calls
- Currently used only for status updates, ready for future enhancement

**Verification:**
- API returns: `"detectedUser":"XAdmin"` from test log (log20260610.txt)
- React TypeScript: 0 errors after build
- Tests: 215 passing, 0 errors
- User context now flows from initial upload through to workshop panel

**Result:**
Any future analyze calls from WorkshopPanel can now pass the correct user context:
```
{
  "logContent": "...",
  "userName": detectedUser ?? "",
  "autoDetectUser": true,
  "timeWindowMinutes": 30
}
```

## Prompt 26a: Chat API Endpoint (COMPLETE)

Conversational chat API for providing contextual workflow guidance with LLM integration and session management.

**Architecture:**
- Strategy pattern for LLM providers (Anthropic/Mock implementations)
- Thread-safe in-memory session store with automatic expiration
- Full message history support for multi-turn conversations
- Configurable model, max tokens, and system prompt templates

**Core Models:**
- ChatMessage: role (user/assistant), content, timestamp, toolsUsed
- ChatSession: sessionId, createdAt, lastActivityAt, messages[], logFileName, analyzeResponse
- ChatRequest: sessionId?, message, logContent?, userName?, userQuestion?
- ChatResponse: sessionId, reply, toolsUsed[], workflowContext, sources[]

**LLM Provider Interface:**
- IChatLlmProvider: CompleteAsync(messages, systemPrompt, cancellationToken)
- AnthropicChatProvider: calls /v1/messages with full message history
  - Model: claude-sonnet-4-6 (configurable)
  - Max tokens: 1000 (configurable)
  - Auto-extracts text content from Anthropic response
- MockChatProvider: canned responses for testing without API keys
  - Keyword-based response routing (next, help, error)

**ChatSessionStore Service:**
- Singleton with thread-safe locking (lock _lockObject)
- Max 50 concurrent sessions
- 2-hour session expiration (auto-cleanup when at capacity)
- Methods: GetOrCreateSession, GetSession, AddMessage, RemoveSession, GetAllSessions, GetSessionCount

**API Endpoints (5 total):**
1. POST /api/chat
   - Creates new session (or uses existing sessionId if provided)
   - Accepts: message, logContent?, userName?, userQuestion?
   - Calls AnthropicChatProvider.CompleteAsync with full message history
   - Returns: ChatResponse { sessionId, reply, toolsUsed, workflowContext, sources }
   - Message history auto-persisted in ChatSessionStore

2. GET /api/chat/{sessionId}
   - Retrieves session with complete message history
   - Returns: ChatSession with all messages, timestamps, analysis

3. POST /api/chat/{sessionId}/save
   - Saves session to disk (chat-sessions/{sessionId}-{timestamp}.json)
   - Returns: { path: "chat-sessions/{sessionId}-{timestamp}.json" }

4. DELETE /api/chat/{sessionId}
   - Removes session from in-memory store
   - Returns: 204 No Content

5. GET /api/chat/sessions/list
   - Lists all active sessions with message counts
   - Returns: ChatSession[] with basic metadata

**Configuration (appsettings.json):**
```json
"ChatLlm": {
  "Provider": "Anthropic",
  "Model": "claude-sonnet-4-6",
  "MaxTokens": 1000,
  "SystemPromptTemplate": "You are a workflow guidance assistant for Aktavara..."
}
```

**System Prompt Template Variables:**
- {contextNarrative}: activity summary from AnalyzeResponse
- {workflowName}: detected workflow name
- {confidence}: confidence percentage
- {currentState}: current workflow state

**Service Registration (Program.cs):**
```csharp
builder.Services.AddSingleton<ChatSessionStore>();
builder.Services.AddScoped<IChatLlmProvider>(sp => 
    provider == "Mock" ? new MockChatProvider(...) : new AnthropicChatProvider(...));
```

**Testing:**
- All 5 endpoints tested with PowerShell
- Message history verified: 4 messages after 2 API calls
- Session persistence verified: saves to disk with proper JSON format
- Session deletion verified: 404 on GET after DELETE
- 215 unit tests passing, 0 errors

**Status:**
- ✅ LLM provider strategy pattern implemented
- ✅ Thread-safe session management with expiration
- ✅ Full message history support
- ✅ All 5 API endpoints working
- ✅ Anthropic integration functional
- ✅ Configuration-driven provider selection
- ✅ Prompt 26a 100% COMPLETE

## Prompt 26b: Chat UI Panel (COMPLETE)

Conversational chat interface for Discovery tab as collapsible right panel.

**Layout:**
- 3-column when chat open: left 280px (candidates) | center flex-1 (workflow) | right 360px (chat)
- 2-column when chat closed: left 280px | right flex-1
- Chat toggle button (💬 Chat) positioned on middle column, relative to content
- Smooth slide-in/out transition via conditional rendering

**ChatPanel Component (src/components/ChatPanel.tsx - 256 lines):**

Props:
- sessionId: string | null (current session)
- analyzeResponse: AnalyzeResponse | null (workflow context)
- logFileName: string | null (for context indicator)
- onSessionCreated: (sessionId: string) => void (session callback)

Features:
1. **Message Thread**
   - User messages: right-aligned blue bubbles
   - Assistant messages: left-aligned grey bubbles
   - Auto-scrolling to latest message
   - Message timestamps (HH:MM format)
   - Tool badges showing MCP tools used (e.g., [detect_current_task])
   - Loading indicator (typing...) during API call

2. **Suggested Questions**
   - Context-aware: based on workflow currentState
   - Generic fallback if no workflow detected
   - Clickable chips that send message on click
   - 4 suggested questions per context
   - Workflow-specific examples: PathSaved, NodeModified, ConnectorCreated

3. **Input Area**
   - Auto-expanding textarea (max 4 lines / 96px)
   - Send button (disabled when loading or empty)
   - Enter key to send (Shift+Enter for newline)
   - Placeholder: "Type your question..."

4. **Session Controls**
   - New conversation: deletes current session, clears UI
   - Save session: downloads session JSON to browser
   - Load session: file picker to load saved .json
   - Message counter: "5 messages · 2 tools used"

5. **Context Indicator**
   - Green dot + filename if log loaded: "Context: log20260610.txt"
   - Amber dot + text if no log: "No log loaded — drop a file for context-aware responses"

**App.tsx Integration:**
- New state: chatSessionId (string | null), chatPanelOpen (boolean)
- handleAnalyzeResult: opens chat panel, clears session on new upload
- onSessionCreated callback: stores sessionId for subsequent messages
- Pass analyzeResponse and fileName to ChatPanel
- Pass logFileName from analyzeResponse.fileName

**API Client (src/services/apiClient.ts):**
- sendChatMessage(request: ChatRequest): Promise<ChatResponse>
- getChatSession(sessionId: string): Promise<ChatSession>
- saveChatSession(sessionId: string): Promise<{ path: string }>
- deleteChatSession(sessionId: string): Promise<void>

**TypeScript Types (src/types/api.ts):**
- ChatMessage: role, content, timestamp, toolsUsed[]
- ChatSession: sessionId, createdAt, lastActivityAt, messages[], logFileName, analyzeResponse
- ChatRequest: sessionId?, message, logContent?, userName?, userQuestion?
- ChatResponse: sessionId, reply, toolsUsed[], workflowContext, sources[]

**Styling:**
- Dark theme (slate-800 panel, blue-600 buttons)
- Message bubbles with padding and rounded corners
- Responsive textarea with overflow handling
- Sticky session controls at top
- Scrollable message area with flex layout
- Minimal, clean design consistent with discovery UI

**Testing:**
- ✅ 0 TypeScript errors (npx tsc --noEmit)
- ✅ React build successful (vite build)
- ✅ Chat API integration verified
- ✅ Message history tested with workflow context
- ✅ Session management tested (create, retrieve, save, delete)
- ✅ Suggested questions display correctly
- ✅ Context indicator shows/hides based on log state

**Status:**
- ✅ ChatPanel component implemented (256 lines)
- ✅ App.tsx wired for chat panel state and layout
- ✅ API client methods fully typed
- ✅ Chat panel opens on log upload
- ✅ Session persistence working
- ✅ Suggested questions context-aware
- ✅ Full integration with backend chat endpoints
- ✅ Prompt 26b 100% COMPLETE

## Prompt 26c: SSE Streaming Chat (COMPLETE)

Token-by-token response streaming for real-time chat feedback:

**Backend Changes:**
- Added StreamAsync method to IChatLlmProvider interface
- AnthropicChatProvider.StreamAsync: calls Anthropic /v1/messages with stream=true
  - Reads response as HTTPCompletionOption.ResponseHeadersRead
  - Uses StreamReader to parse SSE line-by-line
  - Extracts text from content_block_delta events
  - Calls onDelta callback for each token
- MockChatProvider.StreamAsync: simulates streaming with 50ms delays per word
- POST /api/chat/stream endpoint: SSE handler that streams responses directly to response body
  - Sets Content-Type: text/event-stream
  - Writes data: {"delta": "token", "done": false}\n\n for each token
  - Final event: data: {"delta": "", "done": true, "sessionId": "..."}\n\n
  - **CRITICAL FIX**: Changed from Task<IResult> to Task (no result wrapping)
    - Direct response writing (SSE) cannot be mixed with IResult objects
    - Write directly to httpContext.Response via WriteAsync
    - Flush after each event with httpContext.Response.Body.FlushAsync

**Frontend Changes:**
- ChatPanel: ReadableStream + TextDecoder for SSE parsing
- Streaming state tracks ongoing response
- Cancel button (✕) appears during streaming, sends abort signal
- Input disabled during streaming
- Messages auto-scroll to show new tokens as they arrive
- scrollIntoView uses block: 'nearest' to prevent page-level scroll

**Scroll Fixes (related - Prompt 26c part 2):**
- Double requestAnimationFrame in toggleChatPanel for layout reflow timing
- overflow-anchor: none on main content container to prevent auto-scroll anchoring
- overflow-anchor: none on chat messages container
- Chat panel scrolls independently without affecting page scroll

**Status:**
- ✅ Streaming endpoint fixed (no more crash with Ok() result)
- ✅ Token-by-token display working in UI
- ✅ Scroll behavior isolated (chat ↔ page independent)
- ✅ 0 TypeScript errors, build successful
- ✅ Prompt 26c 100% COMPLETE

## Prompt 27c: Header Nav Icons and Underline Fix (COMPLETE)

Navigation refinements with Tabler icons and proper text underlines:

**Updated Navigation Labels:**
- "Discovery" → "Workflow Discovery"
- "Library" → "Workflow Library"

**Nav Item Icons (Tabler):**
- Workflow Discovery: ti-radar-2 icon (search/discovery)
- Workflow Library: ti-books icon (book/library collection)
- Icon size: 16px
- Icon color: matches text color (white when active, 75% white when inactive)
- Gap between icon and text: 6px (gap-1.5)

**Underline Styling Fix:**
- Changed from border-bottom to text-decoration-based underline
- Active state:
  * text-decoration: underline
  * text-decoration-color: white
  * text-decoration-thickness: 2px
  * text-underline-offset: 4px
  * Underline sits directly below text, not at bottom of header bar
- Inactive state: no underline

**Nav Item Spacing:**
- Horizontal padding: 16px (px-4)
- Icon-to-text gap: 6px (gap-1.5)
- Vertical centering: h-12 (48px) with flex items-center
- Hover state: rgba(255,255,255,0.1) background with rounded corners
- Border radius: 4px (rounded)

**Text Color States:**
- Active: white (#ffffff) with text-decoration underline
- Inactive: rgba(255,255,255,0.75)
- Hover: transitions to white

**Implementation Details:**
- Added Tabler icons CSS from CDN (cdn.jsdelivr.net)
- Used Tailwind classes for text-decoration utilities
- Flex layout for icon + text alignment
- Aria-hidden on icons for accessibility

**Verification:**
- ✅ "Workflow Discovery" with radar/search icon visible
- ✅ "Workflow Library" with books icon visible
- ✅ Active item shows white text with underline below text
- ✅ Inactive items show muted white text without underline
- ✅ Icons match text color at all times
- ✅ Hover background highlight works on both items
- ✅ 0 TypeScript errors
- ✅ Build successful

**Status:**
- ✅ Nav labels updated (user-visible only)
- ✅ Tabler icons integrated
- ✅ Underline styling fixed (text-decoration instead of border)
- ✅ Spacing and hover states refined
- ✅ Prompt 27c 100% COMPLETE

## Prompt 27b: Aktavara NRM Design System (COMPLETE)

Complete brand design system implementation with Noto Sans typography and official Aktavara NRM colors:

**Font System:**
- Google Fonts: Noto Sans (weights 400, 700)
- Applied throughout entire UI
- CSS font stacks: body { font-family: 'Noto Sans', sans-serif; }

**Color System (CSS Variables):**
- Brand: #2E75D1 (primary), #8CB3E6 (secondary)
- Icons: #535E6D (primary), #798799 (secondary), #82878C (disabled), #A21515 (error)
- Alerts: #B20000 (fatal), #E22A11 (critical), #FB8C00 (major), #FDD835 (minor), #00ACC1 (degraded), #417ABB (info), #43A047 (normal), #8E24AA (unknown)

**Dark Mode:**
- Brand colors inverted: primary becomes secondary, vice versa
- Icon colors adjusted for contrast (lighter tones on dark background)

**Typography Tokens:**
- h1-h4: weight 400, specific sizes and line heights
- h5-h6: weight 700
- body1-body3: weight 400, specific sizes (1em, 0.875em, 0.8125em)

**Applied Color Updates:**
- Header: Active nav items use brand secondary (#8CB3E6) bottom border
- Confidence bars: High ≥85% (#43A047), Medium 55-84% (#FB8C00), Low <55% (#E22A11)
- Confidence badges: Colored backgrounds at 10% opacity matching bars
- Step dots in FlowVisualiser:
  * Search: #798799 (grey)
  * Open/read: #2E75D1 (blue)
  * Save/write: #43A047 (green)
  * Modified: #FB8C00 (amber)
- Matched steps: Blue border and background using brand primary

**Tailwind Integration:**
- Created tailwind.config.js with extended color palette
- Brand colors available as Tailwind classes: bg-brand-primary, text-brand-secondary
- Alert colors: bg-alert-normal, bg-alert-critical, etc.
- Icon colors: bg-icon-primary, text-icon-secondary, etc.

**Verification:**
- ✅ Noto Sans font loaded from Google Fonts
- ✅ CSS custom properties defined in index.css
- ✅ Dark mode CSS variables configured
- ✅ Header uses brand secondary for active tabs
- ✅ Confidence bars use correct alert colors
- ✅ Step dots use brand colors (grey/blue/green/amber)
- ✅ Tailwind extended with brand palette
- ✅ 0 TypeScript errors
- ✅ Build successful

**Status:**
- ✅ Noto Sans typography system implemented
- ✅ Brand and alert colors applied throughout
- ✅ Confidence levels styled with correct colors
- ✅ Step visualization uses NRM color scheme
- ✅ Dark mode colors properly adjusted
- ✅ Prompt 27b 100% COMPLETE

## Prompt 27a: NRM-Style Header Redesign (COMPLETE)

Full header redesign matching Aktavara NRM application style with Enghouse branding:

**Header Design:**
- Background: #1a3a5c (dark navy blue)
- Height: 48px
- Sticky position, full width
- No rounded corners (full edge-to-edge)

**Layout (left to right):**
1. Enghouse logo (28px height, white via brightness filter)
2. Vertical divider (thin, semi-transparent)
3. "Workflow Intelligence" app name (14px, 500 weight, white)
4. Flexible spacer
5. Navigation items: Discovery | Library
   - Font: 13px, white
   - Inactive: rgba(255,255,255,0.75)
   - Active: white with 2px white bottom border
   - Hover: rgba(255,255,255,0.1) background
6. Right-side controls:
   - (?) Help icon (20px, white)
   - 💬 Chat button (Discovery tab only)

**Removed Elements:**
- Old white header with h1 "Aktavara Workflow Intelligence"
- Subtitle "Discovery & Workshop Interface"
- Tab bar below header (moved into header)
- Standalone chat toggle button
- Top padding/whitespace from old header

**Content Area:**
- Starts immediately below 48px header
- No additional top padding
- calc(100vh - 48px) for full viewport coverage
- Main content flex layout begins at header base

**Assets:**
- workflow-ui/src/assets/enghouse-logo.svg: Geometric circle logo
  - Uses #05539d (dark blue) and #6bb8d4 (light blue)
  - CSS filter brightness(0) invert(1) makes it white

**Integration:**
- Help icon calls openHelp with context-specific key
  - Discovery tab: 'discovery-concept'
  - Library tab: 'library-concept'
- Chat button wired to toggleChatPanel (Discovery only)
- Navigation items fully functional (setTopLevelTab)

**Verification:**
- ✅ Header is dark navy, full width, 48px tall
- ✅ Logo visible in white at top left
- ✅ App name displays next to logo
- ✅ Navigation items in header with underlines
- ✅ Help and Chat icons on right side
- ✅ No whitespace gap below header
- ✅ Content area starts immediately below
- ✅ 0 TypeScript errors
- ✅ Build successful

**Status:**
- ✅ Enghouse logo created and deployed
- ✅ Header restructured with NRM styling
- ✅ Navigation moved from tab bar to header
- ✅ Old elements removed
- ✅ Layout adjusted for header-relative sizing
- ✅ Prompt 27a 100% COMPLETE

## Prompt 28a: AI Assistant Demo Tab & Debug Panel (COMPLETE)

Comprehensive redesign moving chat functionality to a dedicated third top-level tab with advanced debugging capabilities.

**Navigation Changes:**
- Added third nav item "AI Assistant" with ti-brain icon between Discovery and Library
- Updated topLevelTab state to handle 3 options: 'discovery' | 'library' | 'ai-assistant'
- Removed chat toggle button from header (chat now lives in AI Assistant tab)
- Help icon context-aware for all three tabs

**AI Assistant Tab Layout:**
- Left column (380px fixed): LogDropZone + ChatPanel
- Right column (flex-1): New DebugPanel component
- Auto-opens when file uploaded from any tab
- ChatPanel captures log context automatically

**DebugPanel Component (5 Collapsible Sections):**
1. Activity context (expanded) - detected user, total events, current state, time window, top 3 active entities
2. Workflow matching (expanded) - matched workflow name, confidence %, level badge, score breakdown table, matched/missing rules
3. System prompt preview (collapsed) - full system prompt in monospace, copy-to-clipboard button
4. LLM call details (expanded) - provider/model, input tokens, output tokens, total tokens, response time (ms)
5. Guide references (collapsed) - list of help sections included with approval status

**Backend API Changes:**
- Extended ChatResponse model with debug fields:
  - SystemPrompt: string
  - InputTokens: int
  - OutputTokens: int
  - ResponseTimeMs: long
  - GuideReferences: List<string>
  - DetectedWorkflow: WorkflowCandidateResult
- Updated IChatLlmProvider interface with new methods:
  - CompleteWithDebugAsync(): returns (response, LlmDebugInfo)
  - StreamWithDebugAsync(): returns LlmDebugInfo after streaming
  - LlmDebugInfo class holds token counts and response time
- Updated AnthropicChatProvider to:
  - Parse usage info from Anthropic API response
  - Capture input_tokens, output_tokens from streaming events
  - Measure response time for all requests
- Updated MockChatProvider to implement debug methods (100 input, 50 output tokens)
- StreamChat endpoint now emits debug info in final SSE event with format:
  ```json
  { "done": true, "sessionId": "...", "debug": { "inputTokens": 100, "outputTokens": 50, "responseTimeMs": 1234, "systemPrompt": "..." } }
  ```

**Frontend Integration:**
- ChatPanel onDebugInfoCaptured callback passes debug data to App state
- DebugPanel receives analyzeResponse and selectedCandidate for activity/workflow sections
- Debug data persists across messages in same session
- System prompt section shows exact prompt sent to LLM

**User Experience:**
- Drop file → auto-navigates to AI Assistant tab
- Chat and debugging context visible side-by-side
- No interruption to Discovery tab workflow
- All debug data optional (graceful degradation if missing)

**Verification:**
- ✅ Third nav item "AI Assistant" visible with ti-brain icon
- ✅ Clicking opens two-column layout
- ✅ Drop log → chat session created with context
- ✅ Chat messages → debug panel updates with workflow match, tokens, response time
- ✅ Discovery tab no longer has chat panel or toggle button
- ✅ System prompt visible in collapsed section with copy button
- ✅ All 215 tests passing
- ✅ 0 TypeScript errors
- ✅ Build successful
- ✅ Prompt 28a 100% COMPLETE

## Completed Prompts
- ✅ Prompt 25a: Auto-generate Workshop Questions
- ✅ Prompt 25b: Auto-suggest Guide Mappings (Parts A-E complete)
- ✅ Prompt 25c: Pass Detected User Context Through UI
- ✅ Prompt 26a: Chat API endpoint, LLM provider strategy, session management
- ✅ Prompt 26b: Chat UI panel, message thread, suggested questions
- ✅ Prompt 26c: Streaming responses (SSE), scroll isolation fixes
- ✅ Prompt 27a: NRM-style header redesign, Enghouse logo, left-aligned nav
- ✅ Prompt 27b: Aktavara NRM design system, brand colors, Noto Sans typography
- ✅ Prompt 27c: Header nav icons, text-decoration underline, updated labels
- ✅ Prompt 28a: AI Assistant tab, debug panel, move chat out of Discovery

## Prompt 28b: Dynamic Follow-up Question Suggestions (COMPLETE)

LLM-generated contextual follow-up questions after each response in AI Assistant chat.

**Approach: Option B - LLM Includes Suggestions in Response**

**System Prompt Enhancement:**
- Updated ChatLlm:SystemPromptTemplate in appsettings.json
- Instructs LLM to append follow-ups in structured format
- Format: `FOLLOW_UPS: ["question 1", "question 2", "question 3"]`
- Constraints: max 8 words per question, directly relevant to response
- Purpose: help user go deeper or take next action

**ChatPanel Implementation:**
- Added `followUps` state to track current suggestions
- Streaming parser extracts FOLLOW_UPS line using regex: `/FOLLOW_UPS:\s*(\[.*?\])/s`
- Parses JSON array from final SSE event
- Removes FOLLOW_UPS line from displayed message (clean UI)
- Error handling: gracefully falls back to full content if parsing fails
- Follow-ups cleared when:
  - New message sent (prevent stale suggestions)
  - "New conversation" button clicked (reset to static suggestions)

**UI Rendering:**
- **Empty chat**: Static suggestions shown (4 questions)
  - "What am I doing right now?"
  - "What should I do next?"
  - "How do I add a connector?"
  - "Explain this workflow step"
- **After LLM response**: Follow-up chips (3 questions)
  - Blue border pill-shaped buttons
  - Hover state: blue background
  - Dark mode: blue-700 border, blue-400 text
  - Clickable: sends chip text as new message
  - Disappear on new message, reappear with next response

**User Flow:**
1. Drop log → chat ready with 4 static questions
2. Click static question or type custom message
3. LLM response streams in with FOLLOW_UPS appended
4. ChatPanel parses and removes FOLLOW_UPS line
5. Follow-up chips appear below last message
6. Click chip → sent as new message, chips clear
7. New response arrives with new follow-ups
8. Questions are contextually relevant to each response

**Fallback & Graceful Degradation:**
- If follow-up parsing fails: full message shown (no chips)
- If LLM omits follow-ups: no chips displayed (still works)
- Static questions always available when chat empty
- Never breaks functionality due to parsing issues

**Testing:**
- Follow-up parsing with real Claude responses
- Contextual relevance of suggestions to response content
- Fallback when FOLLOW_UPS malformed or missing
- Static questions shown on empty chat
- Chips clear on new message
- New conversation resets to static questions

**Status:**
✅ System prompt updated with follow-up instructions
✅ Streaming parser extracts and removes FOLLOW_UPS line
✅ UI renders follow-up chips contextually
✅ Static suggestions shown on empty chat
✅ Follow-ups cleared on new message
✅ All 215 tests passing
✅ 0 TypeScript errors
✅ Build successful
✅ Prompt 28b 100% COMPLETE

## Next prompts
- Prompt 28c: Additional refinements or new features

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