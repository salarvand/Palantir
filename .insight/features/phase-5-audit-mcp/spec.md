# Specification: Phase 5 - Elasticsearch Compliance Audit & MCP AI Assistant

## Requirements Matrix
* **[REQ-01]**: Replace in-memory `ConcurrentBag<RegulatoryAuditRecord>` in `src/Services/Audit/Palantir.AuditService/Endpoints/AuditEndpoints.cs` with an `Elastic.Clients.Elasticsearch` client querying and indexing records into an `audit-records-v1` index.
* **[REQ-02]**: Register Elasticsearch client singleton in `src/Services/Audit/Palantir.AuditService/Program.cs` with connection string resolving from Aspire discovery (`AspireResourceNames.Elasticsearch`).
* **[REQ-03]**: Replace hardcoded switch mocks in `src/Services/AiAssistant/Palantir.AiAssistantService/Mcp/McpTools.cs` with real tools delegating to Gateway / Microservices (fetching live blotter from OMS, quote from MarketData, risk limits from RiskService).
* **[REQ-04]**: Wire Qdrant vector client in `Palantir.AiAssistantService` for market sentiment semantic retrieval.
* **[REQ-05]**: Expose MCP server endpoints conforming to standard Model Context Protocol via ASP.NET Core endpoint routing.
