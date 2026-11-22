---
insight_schema: "1.0.0"
feature_id: "phase-5-audit-mcp"
standards_version: "1.0.0"
source: "docs/review.md"
spec_hash: "dbe0ec4c5f80a0f69f0d5c22152b54b12bce546a04856e69a58dcfa96c4a16b5"
status: "approved"
---

# Feature Implementation Plan: Phase 5 - Elasticsearch Compliance Audit & MCP AI Assistant

## 1. Requirements Matrix
* **[REQ-01]**: Replace in-memory `ConcurrentBag<RegulatoryAuditRecord>` in `src/Services/Audit/Palantir.AuditService/Endpoints/AuditEndpoints.cs` with an `Elastic.Clients.Elasticsearch` client querying and indexing records into an `audit-records-v1` index.
* **[REQ-02]**: Register Elasticsearch client singleton in `src/Services/Audit/Palantir.AuditService/Program.cs` with connection string resolving from Aspire discovery (`AspireResourceNames.Elasticsearch`).
* **[REQ-03]**: Replace hardcoded switch mocks in `src/Services/AiAssistant/Palantir.AiAssistantService/Mcp/McpTools.cs` with real tools delegating to Gateway / Microservices (fetching live blotter from OMS, quote from MarketData, risk limits from RiskService).
* **[REQ-04]**: Wire Qdrant vector client in `Palantir.AiAssistantService` for market sentiment semantic retrieval.
* **[REQ-05]**: Expose MCP server endpoints conforming to standard Model Context Protocol via ASP.NET Core endpoint routing.

## 2. In-Scope Modules & File Targets
* `src/Services/Audit/Palantir.AuditService/Program.cs` (Modify: Register Elasticsearch client and auto-create audit-records index)
* `src/Services/Audit/Palantir.AuditService/Endpoints/AuditEndpoints.cs` (Modify: Index and search regulatory audit records in Elasticsearch)
* `src/Services/Audit/Palantir.AuditService/Palantir.AuditService.csproj` (Modify: Add Elastic.Clients.Elasticsearch package reference)
* `src/Services/AiAssistant/Palantir.AiAssistantService/Program.cs` (Modify: Register Qdrant client and typed downstream service clients)
* `src/Services/AiAssistant/Palantir.AiAssistantService/Mcp/McpTools.cs` (Modify: Implement live MCP tool dispatchers against downstream services)
* `src/Services/AiAssistant/Palantir.AiAssistantService/Endpoints/AiEndpoints.cs` (Modify: Expose MCP JSON-RPC protocol endpoints)
* `src/Services/AiAssistant/Palantir.AiAssistantService/Palantir.AiAssistantService.csproj` (Modify: Add Qdrant.Client package reference)

## 3. Explicit Out-of-Scope (Scope Guard)
* Real-time financial news crawler or web scraping pipelines.
* Micro UI trading desk React views.
* Keycloak IAM admin portal frontend.

## 4. Architectural Invariants
* **[ARCH-01]**: Compliance audit logs must be strictly append-only (WORM - Write Once, Read Many); deletion or update APIs are forbidden.
* **[ARCH-02]**: MCP tools must not connect directly to relational database instances; all AI tool actions must be brokered through authenticated internal API endpoints.
* **[ARCH-03]**: Qdrant vector search queries must include cosine distance thresholds to discard low-confidence sentiment embeddings.

## 5. Verification & Test Plan
* **Audit Service Verification**: Test indexing and searching regulatory audit logs.
* **MCP Registry Validation**: Verify MCP tool registry schemas and tool call execution handlers.
