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
- Prompts 1-8 complete
- 128 tests passing, 0 errors, 0 warnings
- Parser: working, handles JSON and XML formats
- AktavaraSchemaTypes.cs: complete enum set from Swagger
- WorkflowLibrary: loads *.workflow.json from workflows/ folder
- WorkflowMatcher: 10-factor scoring, confidence levels High/Medium/Low
- Normalizer: fixed - produces 87 events from 180 log entries (was 7)

## Known issues being worked on
- SearchRecords and SaveRecords show Record Kind: Other (request payload not read)
- Matcher output not yet wired into CLI analyze command
- $ref resolution returning 0 nodes/connectors on most workspace opens

## Sample logs
- samples/logs/log20260608.txt (old XML format, 3 actions)
- samples/logs/log20260610.txt (new JSON format, 180 entries, 87 events)

## Workflow library
- workflows/update-node-in-path.workflow.json
- workflows/add-connector-to-path.workflow.json

## Next prompts
- Fix SearchRecords/SaveRecords payload extraction
- Wire matcher output into CLI
- Prompt 9: Activity context service
- Then prompts 10-12, then extended prompts 17-23

## Key design rule
LLM does not parse, match, or make safety decisions.
All deterministic work happens in Core.