# IAM, Audit & AI Assistant Services

## 1. `Palantir.IamService` — Identity & Permissions

- **Startup** ([Program.cs](../../../src/Services/Iam/Palantir.IamService/Program.cs)): Configures `IamDbContext` (`iam` schema), `KeycloakAdminClient`, and RabbitMQ fanout exchange `iam.events` (`UserCreatedIntegrationEvent`, `PermissionsUpdatedIntegrationEvent`).
- **Vertical Slices**:
  - `POST /api/v1/iam/users`, `GET /api/v1/iam/users`, `GET /api/v1/iam/users/{id:guid}` ([CreateUserFeature.cs](../../../src/Services/Iam/Palantir.IamService/Features/Users/Create/CreateUserFeature.cs), [ListUsersFeature.cs](../../../src/Services/Iam/Palantir.IamService/Features/Users/List/ListUsersFeature.cs), [GetUserByIdFeature.cs](../../../src/Services/Iam/Palantir.IamService/Features/Users/Get/GetUserByIdFeature.cs)).
  - `PUT /api/v1/iam/users/{id:guid}/permissions` ([AssignPermissionsFeature.cs](../../../src/Services/Iam/Palantir.IamService/Features/UserPermissions/Assign/AssignPermissionsFeature.cs)): Updates `TradingProfile.AssignedPermissions`, caches `PermissionToken` in `IPermissionTokenManager` (`30 min` TTL), and publishes `PermissionsUpdatedIntegrationEvent`.
  - `POST /api/v1/iam/token/exchange` ([ExchangeTokenFeature.cs](../../../src/Services/Iam/Palantir.IamService/Features/Tokens/Exchange/ExchangeTokenFeature.cs)): Hydrates and caches `PermissionToken` from `IamDbContext` if absent from `IKeyValueStore`.

---

## 2. `Palantir.AuditService` — Regulatory Audit

- **Startup** ([Program.cs](../../../src/Services/Audit/Palantir.AuditService/Program.cs)): Configures `AuditDbContext` (`audit` schema, `audit.audit_records` with PostgreSQL generated `tsvector` + `GIN` index on `EventType` and `Symbol`), `PostgresAuditLogRepository`, and listens to `audit.trail.v1`.
- **Event Handler** ([AuditTrailEventHandler.cs](../../../src/Services/Audit/Palantir.AuditService/Handlers/AuditTrailEventHandler.cs)): Emits an `AuditEntry` to the non-blocking `IAuditEmitter` channel (persisted to `platform_audit.audit_entries` + OTLP + structured logs) and appends a `RegulatoryAuditRecord` via `IAuditLogRepository`.
- **Vertical Slices**:
  - `POST /api/v1/audit/logs` ([AppendAuditLogFeature.cs](../../../src/Services/Audit/Palantir.AuditService/Features/AuditLogs/Append/AppendAuditLogFeature.cs)): Validates `RegulatoryAuditRecord` using `SpecificationValidator<RegulatoryAuditRecord>` (`AuditEventTypeSpecification`, `AuditSymbolSpecification`).
  - `GET /api/v1/audit/logs` ([ListAuditLogsFeature.cs](../../../src/Services/Audit/Palantir.AuditService/Features/AuditLogs/List/ListAuditLogsFeature.cs)): Supports full-text symbol/event querying via PostgreSQL `PlainToTsQuery`.

---

## 3. `Palantir.AiAssistantService` — Multi-Agent AI

- **Startup** ([Program.cs](../../../src/Services/AiAssistant/Palantir.AiAssistantService/Program.cs)): Configures `SentimentDbContext` (`ai` schema + `pgvector` extension), OpenAI-compatible `IChatClient` / `IEmbeddingGenerator`, `TradeCopilotCoordinator`, `TradeAnalysisAgent`, `RiskGuardAgent`, `AgentGovernanceManager`, and downstream HTTP clients (`oms`, `market-data`, `risk`, `portfolio`) with `AddTokenExchangeHandler`.
- **Governance** ([AgentGovernanceMiddleware.cs](../../../src/Services/AiAssistant/Palantir.AiAssistantService/Governance/AgentGovernanceMiddleware.cs), [ai-agents.yaml](../../../src/Services/AiAssistant/Palantir.AiAssistantService/Policies/ai-agents.yaml)):
  - `PresidioPiiRedactor` redacts emails, phone numbers, SSNs, and card numbers before LLM/prompt processing.
  - `AgentGovernanceManager` enforces daily execution budgets, blocked prompt regexes, and per-agent tool allow-lists from YAML policy.
- **Protocols & Endpoints**:
  - **MCP (`/mcp`)**: `ModelContextProtocol.AspNetCore` HTTP transport exposing `TradingMcpTools` + `/api/v1/mcp/tools` discovery ([McpDiscoveryFeature.cs](../../../src/Services/AiAssistant/Palantir.AiAssistantService/Features/Mcp/McpDiscoveryFeature.cs)).
  - **A2A (`/api/v1/a2a/card`, `/api/v1/a2a/messages`)**: Agent card capability discovery and message dispatch ([A2AFeature.cs](../../../src/Services/AiAssistant/Palantir.AiAssistantService/Features/A2A/A2AFeature.cs)).
  - **AG-UI & A2UI (`POST /api/v1/ai/stream`)**: Streams SSE events (`RUN_STARTED`, `thought_start`, `reasoning_token`, `a2ui_component` for `TradeProposalCard` & `RiskGauge`, `RUN_FINISHED`) ([AgUiStreamFeature.cs](../../../src/Services/AiAssistant/Palantir.AiAssistantService/Features/AgUi/AgUiStreamFeature.cs), [ComponentSchemas.cs](../../../src/Services/AiAssistant/Palantir.AiAssistantService/A2UI/ComponentSchemas.cs)).
