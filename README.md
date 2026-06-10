# Aktavara Workflow Intelligence

A .NET 8 solution for parsing activity logs, matching workflows, and building assistant context packets for AI-driven workflow intelligence.

## Solution Structure

### Projects

1. **Aktavara.WorkflowIntelligence.Core**
   - Class library containing domain models, interfaces, and service implementations
   - Clean architecture: no ASP.NET dependencies
   
   **Enums:**
   - `EventType`: RequestInitiated, ResponseReceived, RecordCreated, RecordUpdated, RecordDeleted, StateChanged, RelationshipEstablished, RelationshipRemoved, ValidationPerformed, UserInteraction, ActionExecuted, ErrorOccurred, Unknown
   - `RecordKind`: Path, Node, Connector, Other
   - `WorkflowStatus`: Active, Inactive, Draft, Deprecated, Archived
   - `WorkflowActionExecutionMode`: Automatic, RequiresApproval, Informational, Prompt
   
   **Domain Models:**
   - `RawActivityLogEntry`: Raw log data with minimal processing
   - `ActivityEvent`: Processed event with extracted fields and context
   - `ChangedAttribute` / `ChangedAttributeRecord`: Attribute changes with before/after values
   - `ActiveEntity` / `ActiveEntityRecord`: Currently active records being worked on
   - `ActivityContext`: Aggregated user activity summary for a time window
   - `WorkflowSignatureRule`: Rules for detecting workflow occurrence
   - `WorkflowStateDefinition`: Workflow state with required evidence
   - `WorkflowDefinition`: Complete workflow with states, rules, actions, and guidance
   - `WorkflowMatchResult`: Result of matching activities to a workflow
   - `AssistantContextPacket`: Complete context for AI assistant with actions and guidance
   - `SafeAction`: Recommended action with execution mode and prerequisites
   - `HelpGuideReference`: Help guide reference with relevance score
   - `EvidenceReference`: Raw evidence snippet with source and relevance
   
   **Interfaces:**
   - `IActivityLogParser`: Parse log content and files
   - `IWorkflowMatcher`: Match activities against workflow definitions
   - `IAssistantContextBuilder`: Build context packets for AI assistant
   - `IWorkflowProvider`: Retrieve workflow definitions
   
   **Services:**
   - `ActivityLogParser`: Stub implementation for log parsing
   - `WorkflowMatcher`: Stub implementation for workflow matching
   - `AssistantContextBuilder`: Stub implementation for context building
   - `StaticWorkflowProvider`: Stub implementation with sample workflows

2. **Aktavara.WorkflowIntelligence.Api**
   - ASP.NET Core 8 Web API with minimal API/controller-based endpoints
   - Exposes REST endpoints for:
     - `POST /api/activitylog/parse` - Parse activity log content
     - `POST /api/workflowmatcher/match` - Match workflows
     - `POST /api/workflowmatcher/best-match` - Find best matching workflow
     - `POST /api/assistantcontext/build` - Build complete assistant context
   - Uses Microsoft.Extensions.DependencyInjection for service registration
   - Uses System.Text.Json for JSON serialization

3. **Aktavara.WorkflowIntelligence.Cli**
   - Console application for local testing
   - Commands:
     - `parse <logfile>` - Parse activity log file
     - `match <logfile>` - Match workflows from activity log
     - `context <workOrderId> <logfile>` - Build assistant context
   - Configured with dependency injection
   - Uses Microsoft.Extensions.Logging

4. **Aktavara.WorkflowIntelligence.Tests**
   - xUnit test project with Moq for mocking
   - Test classes:
     - `ActivityLogParserTests` - Unit tests for log parsing
     - `WorkflowMatcherTests` - Unit tests for workflow matching
     - `AssistantContextBuilderTests` - Unit tests for context building

## Prerequisites

- .NET 8 SDK or later (currently using .NET 10)
- Visual Studio, VS Code, or JetBrains Rider (optional)

## Building

```bash
cd Aktavara.WorkflowIntelligence
dotnet build
```

## Running

### API Server

```bash
dotnet run --project Aktavara.WorkflowIntelligence.Api
```

The API will be available at `https://localhost:7001` (HTTPS) or `http://localhost:5000` (HTTP).

### CLI

```bash
dotnet run --project Aktavara.WorkflowIntelligence.Cli -- <command> <args>
```

Examples:
```bash
dotnet run --project Aktavara.WorkflowIntelligence.Cli -- parse ./sample.log
dotnet run --project Aktavara.WorkflowIntelligence.Cli -- match ./sample.log
dotnet run --project Aktavara.WorkflowIntelligence.Cli -- context WO-123 ./sample.log
```

## Testing

```bash
dotnet test
```

## Completed Components

✅ **Activity Log Parser** (18 tests)
- Raw log text parsing with regex
- Timestamp, user, session, direction, action extraction
- XML payload preservation
- Sample flows: Search records, Open workspace, Save records

✅ **XML Extractor (Akta Records)** (27 tests)
- Record snapshot extraction with properties
- Namespace-tolerant parsing with System.Xml.Linq
- Path workspace structure extraction (nodes, connectors, edges)
- Pagination info extraction
- Boolean result extraction

✅ **Activity Event Normalizer** (15 new tests)
- Converts RawActivityLogEntry → ActivityEvent
- Action-specific processing (Search, Open, Save)
- XML extraction using IAktaXmlExtractor
- Request/response correlation with success detection
- Evidence tracking and metadata storage
- Deterministic (no LLM, reproducible)

✅ **Record Diff Service** (17 new tests)
- Computes attribute-level differences between snapshots
- Configurable ignored attributes via DiffOptions
- Case-sensitive/insensitive comparison options
- Integrated into ActivityEventNormalizer
- Auto-populates ChangedAttributes in SaveRecords events
- Tracks before/after values for precise change detection

**Test Summary**: 75/75 tests passing (100%)
- Parser tests: 18
- XML Extractor tests: 27
- Event Normalizer tests: 15
- Record Diff tests: 17

## TODO: Implementation Tasks

- [ ] Implement workflow matching and confidence scoring
- [ ] Implement context summarization
- [ ] Add database provider (replace `StaticWorkflowProvider`)
- [ ] Add OpenAPI/Swagger documentation to API
- [ ] Add validation and error handling for edge cases
- [ ] Implement streaming parser for large log files
- [ ] Add XML schema validation

## Dependencies

### Core Project
- Microsoft.Extensions.Logging.Abstractions
- System.Text.Json (included in .NET standard)

### API Project
- Microsoft.AspNetCore.OpenApi
- All dependencies from Core

### CLI Project
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Logging
- All dependencies from Core

### Tests Project
- xUnit
- Moq
- References to Core project

## Architecture

This solution follows clean architecture principles:
- **Core** is completely independent of frameworks (no ASP.NET dependencies)
- **Api** and **Cli** are presentation layers that depend on Core
- **Tests** depend on Core and use mocking for isolation
- Dependency injection is used for loose coupling between components
- Interfaces allow for easy testing and future implementation swaps

## License

(Add appropriate license information)
