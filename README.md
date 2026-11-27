# CMDB Infrastructure Assistant

ASP.NET Core 10 solution for a conversational Cloud CMDB assistant. The solution follows the five-project architecture from the shared design:

```text
CmdbAssistant.sln
|-- CmdbAssistant.Api       controllers, SignalR hubs, composition root
|-- CmdbAssistant.Core      models and graph/chat/insight contracts
|-- CmdbAssistant.Graph     Neo4j driver, Cypher catalog, read-only guard
|-- CmdbAssistant.AI        Semantic Kernel plugin, prompt, chat orchestration
|-- CmdbAssistant.Insights  scheduled analyzers and SignalR publishing
```

## Run the demo

Requires the .NET 10 SDK. Without external configuration, the API uses an in-memory sample graph and the deterministic `GraphAssistant(IKnowledgeGraph graph)` implementation.

```powershell
dotnet restore .\CmdbAssistant.sln
dotnet build .\CmdbAssistant.sln
dotnet run --project .\CmdbAssistant.Api\CmdbAssistant.Api.csproj
```

The API listens at `http://localhost:5072`. OpenAPI is available at `http://localhost:5072/openapi/v1.json` in Development. Example requests are in [CmdbAssistant.Api.http](CmdbAssistant.Api/CmdbAssistant.Api.http).

In VS Code, run the `CMDB Assistant: Run API` task from **Terminal > Run Task**. The OpenAPI description includes the developer attribution “Developed by Ramesh Thirupathi”; it is intentionally limited to API documentation metadata.

### Company-managed device execution policy

If Windows Application Control, AppLocker, or WDAC blocks the generated API assembly, the solution is still buildable but the local runtime is not authorized by the current device policy. Do not disable or work around that policy. Use a company-approved development directory/runtime, or ask IT to approve the .NET SDK and this signed/approved application build. A signed release can be produced with the company’s approved signing process; never place signing keys or credentials in this repository.

## Enable Neo4j and Semantic Kernel

The Neo4j path requires a graph using the labels, properties, and relationship directions represented in [CypherCatalog.cs](CmdbAssistant.Graph/Queries/CypherCatalog.cs). Configure a dedicated read-only Neo4j account. Queries use Neo4j read sessions/transactions; free-form fallback Cypher is separately guarded and bounded to 200 rows.

PowerShell environment variables use double underscores for nested configuration keys:

```powershell
$env:Neo4j__Uri = "bolt://localhost:7687"
$env:Neo4j__Username = "<read-only-user>"
$env:Neo4j__Password = "<read-only-password>"
$env:AI__Provider = "AzureOpenAI"
$env:AI__Endpoint = "<Azure OpenAI endpoint>"
$env:AI__ApiKey = "<API key>"
$env:AI__DeploymentName = "<deployment name>"
dotnet run --project .\CmdbAssistant.Api\CmdbAssistant.Api.csproj
```

Alternatively set `AI__Provider=OpenAI`, `AI__Model`, and `AI__ApiKey`. The Semantic Kernel chat assistant is activated only when both Neo4j and AI configuration are complete. It offers 14 curated graph tools and a guarded read-only Cypher fallback. No credentials are committed to this repository.

## Endpoints

- `POST /api/chat` with `{ "question": "..." }` returns an answer, intent, confidence, evidence, and recommendations.
- `GET /api/insights` runs the configured demo or Neo4j insight analyzer.
- `GET /api/graph/summary` reports the in-memory sample graph counts.
- `GET /healthz` is the liveness check.
- `/hubs/chat` exposes the chat assistant through SignalR `Ask`.
- `/hubs/insights` broadcasts `insightsUpdated` when the insight snapshot changes.

## Current limits

The sample inventory is illustrative and must not be treated as operational CMDB data. The Neo4j query catalog assumes the schema described in the shared design and may need mapping to your actual CMDB. Predictive scoring is not implemented; the background analyzer currently provides graph-rule findings for redundancy, unhealthy dependencies, orphaned applications, OS end-of-support metadata, and missing support-group assignments. Add authentication, authorization/tenant filtering, auditing, and production monitoring before exposing this API outside a trusted environment.