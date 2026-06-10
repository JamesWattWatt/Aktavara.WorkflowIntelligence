\# Aktavara Workflow Intelligence - Project Context



\## Tech stack

\- .NET 10, C#, clean architecture

\- JSON API format with $type/$id/$ref referencing (Akta.Json.StringObject etc.)

\- Legacy XML format also exists (older API version, lower priority)



\## Solution structure

\- Core (domain models, parser, extractor, matcher, assistant packet)

\- Api (minimal API)

\- Cli (test harness)

\- Tests (xUnit)



\## Current status

\- Prompts 1-6 complete and building (0 errors, 5 warnings)

\- Parser working against sample logs

\- JSON extractor needs $ref resolution

\- Swagger doc provided - use for complete TypeKind/action enums



\## Key workspace types found in logs

\- Path (GetPathWorkspaceDataRequest/Response)

\- Topology (GetWsTopologyRequest/Response)  

\- Diagram (GetWsDiagramRequest/Response)



\## Sample logs

\- samples/logs/log20260608.txt (old XML format)

\- samples/logs/log20260610.txt (new JSON format, primary)



\## Next step

\- Prompt 7: Workflow library JSON schema/loader

\- Generate TypeKind enums from Swagger before proceeding

