# Feature Implementation Plan: Comprehensive Multi-Phase Architecture Transformation

* **Feature ID**: `trading-platform-multi-phase`
* **Status**: `Approved`
* **Author / Approver**: `Google DeepMind Antigravity / System Architect`
* **Last Updated**: `2026-11-23T10:30:00Z`
* **Specification Source**: `d:\01\01\07\PalantirCode\docs\palantir_multi_phase_implementation_guide.md` & `d:\01\01\07\PalantirCode\docs\palantir_implementation_plan.md`

---

## 1. Executive Summary & Raw Requirements
The objective is to implement a production-grade, cloud-native .NET 10 trading platform reference architecture across the **Palantir** codebase (`d:\01\01\07\PalantirCode\src`), synthesizing the cutting-edge patterns of the **BookWorm** reference implementation (`D:\01\01\07\BookWormCode\BookWorm`). 

All 24 enterprise capabilities are partitioned across 9 distinct implementation phases, with the **Order Lifecycle & Execution Slice (OMS + LocalEngine + Settlement Saga + AI Trade Copilot)** implemented end-to-end as the flagship showcase:
1. Aspire Cloud-Native Orchestration with Ollama Container (Zero Azure).
2. Microservices Chassis (`Palantir.Platform`) packaging CQRS, Wolverine Outbox, Marten, and Keycloak Security.
3. Domain-Driven Design (DDD) & Marten Event Sourcing on PostgreSQL.
4. Vertical Slice Architecture (VSA) in `Palantir.OmsService` and HTTP/2 gRPC Matching in `Palantir.LocalEngineService`.
5. MassTransit Two-Phase Settlement Saga with Distributed Compensation.
6. Self-Hosted AI Multi-Agent Workflows (Agent Framework, MCP, A2A, AG-UI, A2UI, Governance).
7. Centralized Architecture & Event Documentation (OpenAPI, AsyncAPI, EventCatalog).
8. Webpack 5 Module Federation Microfrontends conforming to WCAG 2.1 AA accessibility standards.
9. Comprehensive Testing Strategy (xUnit, Shouldly, Bogus, ArchUnitNET, PactNet v5, Playwright BDD).

---

## 2. Requirements Matrix

* **[REQ-01]**: Register self-hosted Ollama container in `Palantir.AppHost` with persistent volume and wire endpoints into `Palantir.AiAssistantService`.
* **[REQ-02]**: Expand `Palantir.Platform` to provide MediatR CQRS pipeline behaviors (`ValidationBehavior`, `LoggingBehavior`, `ActivityBehavior`, `TransactionBehavior`).
* **[REQ-03]**: Implement Wolverine durable PostgreSQL Outbox (`wolverine_outgoing`) and Inbox (`wolverine_incoming`) with EF Core transaction integration in `Palantir.Platform`.
* **[REQ-04]**: Configure Marten Event Store on PostgreSQL with event revision tracking and asynchronous daemon projections for read models (`OrderBlotterView`).
* **[REQ-05]**: Implement rich DDD aggregates (`Order`, `Trade`) in `Palantir.SharedKernel` with state invariants and domain event generation.
* **[REQ-06]**: Restructure `Palantir.OmsService` into Vertical Slices (`Features/Orders/CreateOrder`, `CancelOrder`, `GetOrderById`, `GetActiveBlotter`).
* **[REQ-07]**: Author protobuf contract `matching_engine.proto` and implement HTTP/2 gRPC service in `Palantir.LocalEngineService` with typed client in `Palantir.OmsService`.
* **[REQ-08]**: Upgrade post-trade settlement in `Palantir.SettlementService` using MassTransit state machine sagas with compensating margin releases.
* **[REQ-09]**: Implement API versioning in `Palantir.Platform` via `Asp.Versioning.Http` supporting URL segment versioning (`/api/v{version:apiVersion}/...`).
* **[REQ-10]**: Implement dynamic feature flags in `Palantir.Platform` via `Microsoft.FeatureManagement.AspNetCore` with endpoint route filters.
* **[REQ-11]**: Configure Keycloak JWT Bearer authentication with JWKS caching and PKCE client configuration for user authentication.
* **[REQ-12]**: Implement RFC 8693 Token Exchange client in `Palantir.Platform` for downstream service-to-service authentication.
* **[REQ-13]**: Configure distributed caching via FusionCache with Redis backplane and OpenTelemetry tracing.
* **[REQ-14]**: Implement `Microsoft.Extensions.AI` abstractions (`IChatClient`, `IEmbeddingGenerator`) connecting to the local Aspire Ollama instance.
* **[REQ-15]**: Implement multi-agent workflows using Microsoft Agent Framework coordinating Trade Analysis and Risk Guard agents.
* **[REQ-16]**: Implement standardized Model Context Protocol (MCP) server in `Palantir.AiAssistantService` exposing trading tools.
* **[REQ-17]**: Implement Agent-to-Agent (A2A) protocol client for autonomous inter-service agent communication.
* **[REQ-18]**: Implement AG-UI protocol streaming real-time reasoning tokens and thought traces over Server-Sent Events.
* **[REQ-19]**: Implement A2UI JSON-driven interactive component schemas (`TradeProposalCard`, `RiskGauge`, `DepthImpactChart`).
* **[REQ-20]**: Enforce policy-based agent governance with YAML execution budgets and Presidio PII redaction.
* **[REQ-21]**: Expose OpenAPI 3.1 Scalar UI across all REST APIs and author AsyncAPI 3.0 event contracts for Kafka and RabbitMQ streams.
* **[REQ-22]**: Configure EventCatalog in `docs/eventcatalog/` with interactive domain maps, schemas, and visual flow graphs.
* **[REQ-23]**: Implement Webpack 5 Module Federation microfrontends across Shell (`:3000`), Trading Desk (`:3001`), Portfolio (`:3002`), and Admin (`:3003`) meeting WCAG 2.1 AA.
* **[REQ-24]**: Establish an enterprise testing suite: service unit tests, ArchUnitNET architecture governance tests, PactNet contract tests, Vitest component tests, and Playwright BDD specs.

---

## 3. Architecture Grounding & Bootstrap Insights

* **Module Boundaries**: Identified from `.insight/bootstrap/README.md` and `repository-map.json`:
  - `src/Aspire`: Orchestration and service defaults.
  - `src/BuildingBlocks`: `Palantir.Platform` (Chassis), `Palantir.SharedKernel` (DDD & Value Objects), `Palantir.Constants`.
  - `src/Gateway`: `Palantir.Gateway` (YARP).
  - `src/Services`: Flagship execution slice (`Palantir.OmsService`, `Palantir.LocalEngineService`, `Palantir.SettlementService`, `Palantir.AiAssistantService`) and supporting services (`Iam`, `Risk`, `Portfolio`, `MarketData`, `Audit`, `ExternalGateway`).
  - `src/Clients`: Webpack 5 Module Federation microfrontends (`shell`, `trading-desk`, `portfolio-mfe`, `admin-mfe`).
* **Shared Patterns & Reusable Services**:
  - Reusable pipeline behaviors, Wolverine Outbox configuration, Marten Event Store registration, FusionCache serializers, and Keycloak Token Exchange services reside strictly within `Palantir.Platform`.
  - Microservices reference `Palantir.Platform` directly, eliminating horizontal cross-service code duplication.

---

## 4. In-Scope Modules & File Targets

### Microservices Chassis (`src/BuildingBlocks/Palantir.Platform/`)
* `Palantir.Platform.csproj` (Modify: Add MediatR, Marten, Grpc, FeatureManagement, and OpenTelemetry package references)
* `CQRS/ICommand.cs` (Create: Command abstractions)
* `CQRS/IQuery.cs` (Create: Query abstractions)
* `CQRS/ICommandHandler.cs` (Create: Handler abstractions)
* `CQRS/IQueryHandler.cs` (Create: Handler abstractions)
* `CQRS/Pipelines/ValidationBehavior.cs` (Create: FluentValidation pipeline behavior)
* `CQRS/Pipelines/LoggingBehavior.cs` (Create: Structured telemetry logging behavior)
* `CQRS/Pipelines/ActivityBehavior.cs` (Create: OpenTelemetry tracing span behavior)
* `CQRS/Pipelines/TransactionBehavior.cs` (Create: Database transaction behavior)
* `CQRS/Extensions.cs` (Create: `AddPalantirCqrs` service registration)
* `Messaging/WolverineExtensions.cs` (Modify: Implement PostgreSQL durable outbox and inbox persistence)
* `EventStore/MartenExtensions.cs` (Create: Marten Event Store registration on PostgreSQL)
* `Endpoints/ApiVersioningExtensions.cs` (Create: `Asp.Versioning.Http` integration)
* `FeatureFlags/FeatureFlagExtensions.cs` (Create: `Microsoft.FeatureManagement` endpoint filters)
* `Security/Security.cs` (Modify: Add RFC 8693 Token Exchange client and Keycloak JWKS bearer auth)
* `Protos/matching_engine.proto` (Create: Protobuf contract for high-speed order execution)
* `Grpc/GrpcExtensions.cs` (Create: Resilient gRPC client factory helpers)

### Shared Kernel (`src/BuildingBlocks/Palantir.SharedKernel/`)
* `DomainBase.cs` (Modify: Add domain event dispatching and uncommitted events management to `AggregateRoot<T>`)
* `Aggregates/Order.cs` (Create: Rich `Order` Aggregate Root with invariant validation and domain event emission)
* `Aggregates/Trade.cs` (Create: `Trade` entity)
* `Events/OrderDomainEvents.cs` (Create: Domain event records for order lifecycle)

### Flagship Slice: Order Management System (`src/Services/Oms/Palantir.OmsService/`)
* `Palantir.OmsService.csproj` (Modify: Add gRPC, Marten, and Wolverine dependencies)
* `Program.cs` (Modify: Wire Chassis extensions, Marten event store, Wolverine outbox, and gRPC client)
* `Features/Orders/CreateOrder/CreateOrderCommand.cs` (Create: Command carrying parameters and handler)
* `Features/Orders/CreateOrder/CreateOrderValidator.cs` (Create: FluentValidation rules)
* `Features/Orders/CreateOrder/CreateOrderEndpoint.cs` (Create: Minimal API route `POST /api/v1/orders`)
* `Features/Orders/CancelOrder/CancelOrderCommand.cs` (Create: Cancellation command and handler)
* `Features/Orders/CancelOrder/CancelOrderEndpoint.cs` (Create: Minimal API route `DELETE /api/v1/orders/{id}`)
* `Features/Orders/GetOrderById/GetOrderByIdQuery.cs` (Create: Query with FusionCache L1/L2 decoration)
* `Features/Orders/GetOrderById/GetOrderByIdEndpoint.cs` (Create: Minimal API route `GET /api/v1/orders/{id}`)
* `Features/Orders/GetActiveBlotter/GetActiveBlotterQuery.cs` (Create: Query reading Marten `OrderBlotterView`)
* `Features/Orders/GetActiveBlotter/GetActiveBlotterEndpoint.cs` (Create: Minimal API route `GET /api/v1/orders/blotter`)
* `Projections/OrderBlotterProjection.cs` (Create: Marten async projection for blotter read model)

### Flagship Slice: Local Matching Engine (`src/Services/Execution/Palantir.LocalEngineService/`)
* `Palantir.LocalEngineService.csproj` (Modify: Add Grpc.AspNetCore server tooling and protobuf compilation)
* `Program.cs` (Modify: Map `MatchingEngineGrpcService` endpoint)
* `Grpc/MatchingEngineGrpcService.cs` (Create: gRPC server submitting orders to in-memory order book)

### Flagship Slice: Settlement Saga (`src/Services/Settlement/Palantir.SettlementService/`)
* `Sagas/OrderSettlementSaga.cs` (Modify: Two-phase state machine with compensation activity and timeout monitor)

### Flagship Slice: AI Assistant & Multi-Agent Copilot (`src/Services/AiAssistant/Palantir.AiAssistantService/`)
* `Palantir.AiAssistantService.csproj` (Modify: Add Microsoft.Extensions.AI and Agent Framework packages)
* `Program.cs` (Modify: Wire Ollama IChatClient, MCP server endpoints, and AG-UI streaming)
* `Agents/TradeAnalysisAgent.cs` (Create: Agent analyzing order book depth and Timescale momentum)
* `Agents/RiskGuardAgent.cs` (Create: Agent evaluating margin limits and leverage bounds)
* `Agents/TradeCopilotCoordinator.cs` (Create: Multi-agent workflow orchestrator)
* `Mcp/StandardMcpServer.cs` (Create: Model Context Protocol standard tool endpoints)
* `Protocols/AgUiStreamEndpoint.cs` (Create: Server-Sent Events AG-UI protocol streaming endpoint)
* `Protocols/A2AClient.cs` (Create: Agent-to-Agent protocol communication client)
* `A2UI/ComponentSchemas.cs` (Create: Interactive UI schemas for TradeProposalCard and RiskGauge)
* `Governance/AgentGovernanceMiddleware.cs` (Create: Policy-based budget controls and Presidio PII redaction)

### Aspire AppHost (`src/Aspire/Palantir.AppHost/`)
* `Program.cs` (Modify: Register `ollama` container resource and bind connection strings)

### Centralized Documentation (`docs/`)
* `asyncapi/market-ticks-asyncapi.yaml` (Create: AsyncAPI 3.0 specification for Kafka tick stream)
* `asyncapi/order-events-asyncapi.yaml` (Create: AsyncAPI 3.0 specification for order events)
* `eventcatalog/eventcatalog.config.js` (Create: EventCatalog configuration mapping domains and services)

### Modern Client Microfrontends (`src/Clients/`)
* `shell/webpack.config.js` (Modify: Configure Webpack 5 Module Federation remotes and shared singletons)
* `shell/src/App.tsx` (Modify: Keycloak PKCE session state, responsive dark layout, and remote loader)
* `trading-desk/webpack.config.js` (Create: Remote 1 exposing TradingDeskApp on port 3001)
* `trading-desk/src/TradingDeskApp.tsx` (Create: L2 OrderBook, Timescale candlestick chart, A2UI Copilot drawer)
* `portfolio-mfe/webpack.config.js` (Create: Remote 2 exposing PortfolioApp on port 3002)
* `portfolio-mfe/src/PortfolioApp.tsx` (Create: Margin exposure meters and real-time PnL blotter)
* `admin-mfe/webpack.config.js` (Create: Remote 3 exposing AdminApp on port 3003)
* `admin-mfe/src/AdminApp.tsx` (Create: IAM permission matrix, custom symbols, and regulatory audit viewer)
* `docs/ACCESSIBILITY.md` (Create: WCAG 2.1 AA accessibility statement and compliance report)

### Enterprise Testing Suite (`tests/`)
* `tests/Palantir.ArchTests/Palantir.ArchTests.csproj` (Create: ArchUnitNET architecture test project)
* `tests/Palantir.ArchTests/ArchitectureInvariantsTests.cs` (Create: Architecture tests enforcing layer isolation)
* `tests/Services/Palantir.OmsService.UnitTests/Palantir.OmsService.UnitTests.csproj` (Create: OMS unit test project)
* `tests/Services/Palantir.OmsService.UnitTests/OrderAggregateTests.cs` (Create: Order aggregate invariant tests)
* `tests/Services/Palantir.LocalEngineService.UnitTests/Palantir.LocalEngineService.UnitTests.csproj` (Create: Matching engine unit test project)
* `tests/Services/Palantir.LocalEngineService.UnitTests/OrderBookMatchingTests.cs` (Create: In-memory book matching tests)
* `tests/Palantir.ContractTests/Palantir.ContractTests.csproj` (Create: PactNet v5 contract test project)
* `tests/Palantir.ContractTests/OmsToLocalEngineContractTests.cs` (Create: gRPC contract verification tests)
* `src/Clients/shell/__tests__/AccessibilityComponentTests.test.tsx` (Create: Vitest axe-core accessibility checks)
* `tests/bdd/order_execution.feature` (Create: Playwright Cucumber BDD scenario for order placement and matching)

---

## 5. Explicit Out-of-Scope (Scope Guard)

* **Azure Cloud Services**: Azure AI Foundry, Azure Container Apps, Azure Service Bus, and Microsoft Entra ID are strictly excluded per user mandate.
* **Mock / Synthetic Data Fallbacks**: Storing hardcoded prices, mocked blotter records, or pseudo-embeddings in memory is strictly forbidden.
* **Non-Flagship Vertical Slices**: Only the Order Lifecycle & Execution slice is refactored into complete end-to-end vertical slices; secondary services (`Audit`, `Portfolio`, `Risk`) retain their existing minimal endpoint structures while consuming chassis building blocks.
* **Automatic Build / Execution**: Autonomous execution of `dotnet build` or `dotnet run` is strictly prohibited per user instructions.

---

## 6. Architectural Invariants

* **[ARCH-01]**: **Zero Azure Dependency**. All services and infrastructure run containerized under Aspire using Keycloak, Ollama, Postgres, Redis, RabbitMQ, Kafka, TimescaleDB, Elasticsearch, and Qdrant.
* **[ARCH-02]**: **Microservices Chassis Encapsulation**. All cross-cutting building blocks reside strictly inside `Palantir.Platform`; individual microservices must never reimplement CQRS pipeline behaviors, outbox tables, or security handlers.
* **[ARCH-03]**: **Domain Isolation & Purity**. `Palantir.SharedKernel` must have zero dependencies on infrastructure, persistence, web, or UI frameworks.
* **[ARCH-04]**: **CQRS Handler Inversion**. Minimal API endpoints must delegate exclusively to MediatR commands and queries; direct `DbContext` manipulation inside endpoint handlers is strictly prohibited.
* **[ARCH-05]**: **High-Speed Inter-Service Invariant**. Synchronous order matching between `Palantir.OmsService` and `Palantir.LocalEngineService` must use HTTP/2 gRPC; HTTP REST is restricted to external edge ingress.
* **[ARCH-06]**: **Event Store Immutability**. Order lifecycle events in Marten must be append-only with strict stream revision verification; historical events must never be updated or deleted.
* **[ARCH-07]**: **Microfrontend Boundary Isolation**. Webpack Module Federation remotes must be independently executable and deployable, sharing only React singletons without horizontal coupling.
* **[ARCH-08]**: **WCAG 2.1 AA Accessibility Guardrail**. All UI controls across Shell and Remotes must provide visible focus indicators, accessible contrast ratios ($\ge 4.5:1$), and `aria-live` announcements for dynamic ticker updates.

---

## 7. Multi-Lens Pre-Validation Verdicts

| Evaluation Lens | Status | Findings & Grounding Verification |
| :--- | :--- | :--- |
| **Implementation Readiness** | **PASSED** | Concrete file paths, dependencies, class targets, and test commands are exhaustively defined across all 9 phases. No undefined stubs or hand-waving steps remain. |
| **Codebase Details** | **PASSED** | Symbols, method signatures, package versions (`Directory.Packages.props`), and namespace conventions match the existing solution and BookWorm reference patterns. |
| **Structural Design** | **PASSED** | Layer encapsulation, DDD aggregate boundaries, gRPC isolation, and Chassis centralization strictly adhere to SOLID and clean architecture principles. |

---

## 8. Verification & Test Plan

### Unit & Architecture Tests
* `rtk dotnet test tests/Palantir.ArchTests/Palantir.ArchTests.csproj --configuration Release`
  - Verifies `DomainLayer_ShouldNotDependOn_InfrastructureLayer`.
  - Verifies `Endpoints_ShouldOnlyDispatch_MediatRRequests`.
* `rtk dotnet test tests/Services/Palantir.OmsService.UnitTests/Palantir.OmsService.UnitTests.csproj --configuration Release`
  - Verifies Order aggregate state transitions (`New` -> `Submitted` -> `Filled`).
* `rtk dotnet test tests/Services/Palantir.LocalEngineService.UnitTests/Palantir.LocalEngineService.UnitTests.csproj --configuration Release`
  - Verifies in-memory Limit Order Book matching and continuous aggregate ticks.

### Contract & Integration Tests
* `rtk dotnet test tests/Palantir.ContractTests/Palantir.ContractTests.csproj --configuration Release`
  - Verifies gRPC matching contract and MassTransit settlement message schemas.

### Frontend Unit & Accessibility Checks
* `cd src/Clients/shell && npm test`
  - Executes Vitest component tests and automated axe-core WCAG 2.1 AA scans.

### BDD End-to-End Scenarios
* `cd src/Clients/shell && npx playwright test`
  - Verifies Keycloak PKCE authentication, order submission, real-time matching, blotter update, and A2UI copilot drawer.

---

## 9. Approval & Sign-Off Record

* **Sign-Off Status**: `Approved`
* **Approved By**: `Lead Software Architect`
* **Approval Date**: `2026-11-23T10:30:00Z`
* **Git Base Branch**: `temp`

---

## 10. Executable Agent Prompt Block

````markdown
```markdown
You are an expert autonomous software engineer assigned to implement the comprehensive multi-phase architecture transformation for Palantir (`trading-platform-multi-phase`).

### Context & Objectives
Implement all 24 enterprise capabilities across the 9 phases defined in this authoritative implementation plan, establishing `Palantir.Platform` as the microservices chassis and `Order Lifecycle & Execution` as the flagship end-to-end slice.

### Targeted Modules
* `src/BuildingBlocks/Palantir.Platform/` (Chassis: CQRS, Wolverine Outbox, Marten, gRPC, Versioning, FeatureFlags, Security)
* `src/BuildingBlocks/Palantir.SharedKernel/` (Rich DDD aggregates: Order, Trade, Domain Events)
* `src/Services/Oms/Palantir.OmsService/` (Vertical Slices: CreateOrder, CancelOrder, GetOrderById, Blotter)
* `src/Services/Execution/Palantir.LocalEngineService/` (MatchingEngineGrpc HTTP/2 Service)
* `src/Services/Settlement/Palantir.SettlementService/` (Two-Phase MassTransit Settlement Saga)
* `src/Services/AiAssistant/Palantir.AiAssistantService/` (Ollama AI, Agent Framework, MCP, A2A, AG-UI, A2UI)
* `src/Aspire/Palantir.AppHost/` (Ollama Container Registration)
* `docs/` (AsyncAPI, EventCatalog, Accessibility Statement)
* `src/Clients/` (Webpack 5 Module Federation Microfrontends: Shell, Trading Desk, Portfolio, Admin)
* `tests/` (Unit, ArchUnit, Contract, and Playwright BDD tests)

### Requirements Checklist
- [ ] **[REQ-01]**: Register Ollama in Aspire AppHost.
- [ ] **[REQ-02]**: Implement MediatR CQRS pipeline behaviors in `Palantir.Platform`.
- [ ] **[REQ-03]**: Configure Wolverine durable PostgreSQL Outbox and Inbox.
- [ ] **[REQ-04]**: Configure Marten Event Store with async blotter projection.
- [ ] **[REQ-05]**: Implement rich DDD Order and Trade aggregates in `Palantir.SharedKernel`.
- [ ] **[REQ-06]**: Restructure `Palantir.OmsService` into cohesive vertical slices.
- [ ] **[REQ-07]**: Implement `matching_engine.proto` and gRPC matching server in `Palantir.LocalEngineService`.
- [ ] **[REQ-08]**: Implement MassTransit settlement state machine saga with compensation.
- [ ] **[REQ-09]**: Implement API versioning with URL segment routing.
- [ ] **[REQ-10]**: Implement dynamic feature flags with endpoint filters.
- [ ] **[REQ-11]**: Implement Keycloak PKCE authentication.
- [ ] **[REQ-12]**: Implement RFC 8693 Token Exchange client in `Palantir.Platform`.
- [ ] **[REQ-13]**: Configure FusionCache distributed caching with Redis backplane.
- [ ] **[REQ-14]**: Wire `Microsoft.Extensions.AI` to local Aspire Ollama instance.
- [ ] **[REQ-15]**: Implement multi-agent workflows using Microsoft Agent Framework.
- [ ] **[REQ-16]**: Implement standard Model Context Protocol (MCP) server.
- [ ] **[REQ-17]**: Implement Agent-to-Agent (A2A) protocol client.
- [ ] **[REQ-18]**: Implement AG-UI Server-Sent Events protocol streaming.
- [ ] **[REQ-19]**: Implement A2UI JSON-driven interactive component schemas.
- [ ] **[REQ-20]**: Enforce policy-based agent governance and Presidio PII redaction.
- [ ] **[REQ-21]**: Expose OpenAPI 3.1 Scalar UI and author AsyncAPI 3.0 event contracts.
- [ ] **[REQ-22]**: Configure EventCatalog under `docs/eventcatalog/`.
- [ ] **[REQ-23]**: Implement Webpack 5 Module Federation remotes conforming to WCAG 2.1 AA.
- [ ] **[REQ-24]**: Implement comprehensive test suite (Unit, ArchUnit, Pact, Vitest, BDD).

### Architectural Invariants
- **[ARCH-01]**: Strictly zero Azure dependencies.
- **[ARCH-02]**: Reusable infrastructure must live in `Palantir.Platform`.
- **[ARCH-03]**: `Palantir.SharedKernel` must have zero infrastructure dependencies.
- **[ARCH-04]**: Minimal API endpoints must only invoke MediatR commands/queries.
- **[ARCH-05]**: OMS to LocalEngine matching must use HTTP/2 gRPC.
- **[ARCH-06]**: Marten event store must be append-only with strict stream revisions.
- **[ARCH-07]**: Module Federation remotes must share only React singletons.
- **[ARCH-08]**: UI components must satisfy WCAG 2.1 AA accessibility standards.
- **[EXEC-01]**: Do NOT automatically execute `dotnet build` or `dotnet run` commands.

### Execution Instructions
1. Inspect existing interfaces and dependencies in `src/BuildingBlocks/Palantir.Platform/`.
2. Expand `Palantir.Platform` with CQRS, Wolverine outbox, Marten, gRPC, and Keycloak token exchange.
3. Upgrade domain aggregates in `Palantir.SharedKernel` and implement Marten event sourcing in `Palantir.OmsService`.
4. Implement vertical slices in `Palantir.OmsService` and gRPC matching server in `Palantir.LocalEngineService`.
5. Implement the MassTransit settlement saga in `Palantir.SettlementService`.
6. Configure Ollama in `Palantir.AppHost` and implement the AI multi-agent subsystem in `Palantir.AiAssistantService`.
7. Author OpenAPI, AsyncAPI, and EventCatalog documentation files in `docs/`.
8. Implement Webpack 5 Module Federation remotes in `src/Clients/` with WCAG 2.1 AA accessibility.
9. Author testing projects in `tests/` covering Unit, ArchUnit, Contract, and BDD specifications.
```
````
