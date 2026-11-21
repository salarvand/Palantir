# Palantir: Cloud-Native .NET 10 Trading Platform Reference Architecture

## Goal Description
Build **Palantir**, a production-grade, cloud-native .NET 10 reference architecture utilizing modern financial trading systems as the representative domain. The solution serves as an enterprise architectural blueprint synthesizing the strengths of both **BookWorm** (cutting-edge .NET, Aspire orchestration, zero-reflection source-generated CQRS, Vogen strongly-typed IDs, FusionCache, AI/MCP integration, and modern TUnit/PactNet/ArchUnit testing) and **DocumentManagement** (MassTransit Saga State Machines with compensation, granular PBAC with Redis-cached permission tokens, Elasticsearch compliance audit logging, traceable entities, and custom Roslyn source generators).

The architecture includes a stateful **Microsoft Orleans Order Management System (OMS)** silo, paired with a **Dual-Mode Trading Engine**: an internal local matching engine capable of defining custom trading pairs and generating real-time OHLC candlestick data via TimescaleDB Continuous Aggregates, alongside an external exchange gateway connecting to existing third-party trading venues via REST and WebSockets.

---

## User Review Required

> [!IMPORTANT]
> - **SDK Requirement**: The solution targets `.NET 10.0 Preview` (`net10.0`), utilizing C# 14 language features and `.slnx` solution format.
> - **Microsoft Orleans OMS**: Order state, client sessions, and time-in-force order lifecycle are managed by Orleans Virtual Actors (`IOrderGrain`, `IClientSessionGrain`) with clustering and grain persistence backed by PostgreSQL (`Orleans.Clustering.AdoNet`, `Orleans.Persistence.AdoNet`).
> - **Dual Execution Venues**:
>   - **Local Engine (`Palantir.LocalEngineService`)**: Handles custom symbol definitions, in-memory limit order book matching, and automated OHLC (1s, 1m, 5m, 1h, 1d) aggregation in TimescaleDB.
>   - **External Gateway (`Palantir.ExternalGatewayService`)**: Pluggable REST and WebSocket connector communicating with external venues, accompanied by a mock exchange simulator for local testing.
> - **Micro UI**: Webpack Module Federation with React 19 and TypeScript, exposing independent micro-frontend remotes (Trading Desk, Portfolio Management, Admin/Audit) loaded dynamically into a Host Shell.

---

## Architecture & System Overview

### High-Level Topology

```mermaid
flowchart TD
    subgraph Clients["Micro UI (Webpack Module Federation)"]
        Shell["Host Shell (Container App)"]
        DeskRemote["Trading Desk Remote (OrderBook, Blotter, OHLC Charts)"]
        PortfolioRemote["Portfolio & Risk Remote (Positions, PnL)"]
        AdminRemote["Compliance & Symbol Admin Remote (Custom Pairs, Trade Logs)"]
        Shell --> DeskRemote
        Shell --> PortfolioRemote
        Shell --> AdminRemote
    end

    subgraph GatewayLayer["Edge & Security Layer"]
        Gateway["YARP Reverse Proxy Gateway (Palantir.Gateway)"]
        Keycloak["Keycloak IAM (OAuth2 / OIDC + Token Exchange)"]
    end

    Shell -->|HTTPS / WSS| Gateway
    Gateway -->|Auth Validation| Keycloak

    subgraph AspireHost[".NET Aspire Orchestration (Palantir.AppHost)"]
        subgraph OmsLayer["Orleans OMS Silo"]
            OmsSvc["Orleans OMS Silo (Palantir.OmsService)\n- OrderGrain (State & Lifecycle)\n- ClientSessionGrain\n- OrderExpiryReminder"]
        end

        subgraph ExecutionEngines["Dual-Mode Execution Engines"]
            LocalEngine["Local Trading Engine (Palantir.LocalEngineService)\n- Custom Symbol Definition\n- In-Memory Matching Book\n- Real-Time Tick Publisher"]
            ExternalGateway["External Exchange Gateway (Palantir.ExternalGatewayService)\n- REST / WebSocket Pluggable Venue Connectors\n- Simulated Mock Exchange"]
        end

        subgraph CoreServices["Trading Microservices (.NET 10 VSA + DDD)"]
            MarketDataSvc["Market Data Service (Palantir.MarketDataService)"]
            RiskSvc["Pre-Trade Risk Service (Palantir.RiskService)"]
            PortfolioSvc["Portfolio & Ledger Service (Palantir.PortfolioService)"]
            SettlementSvc["Settlement & Clearing Service (Palantir.SettlementService)"]
            AuditSvc["Regulatory Audit Service (Palantir.AuditService)"]
            AiCopilotSvc["AI Copilot & MCP Tools Service (Palantir.AiAssistantService)"]
        end

        subgraph EventBackbone["Hybrid Event Backbone"]
            RabbitMQ["RabbitMQ (MassTransit Commands & Sagas)"]
            Kafka["Apache Kafka (Market Ticks & High-Throughput Streams)"]
        end

        subgraph StorageLayer["Polyglot Persistence Layer"]
            Postgres["PostgreSQL\n- EF Core (Accounts, Portfolios, Sagas)\n- Orleans Clustering & Grain Storage"]
            Timescale["TimescaleDB (Hypertables & Continuous Aggregates for OHLC)"]
            Redis["Redis + FusionCache (L1/L2 OrderBook & Quotes Cache)"]
            Elastic["Elasticsearch (MiFID II / CAT Compliance Audit Logs)"]
            Qdrant["Qdrant (Vector Embeddings for Market Sentiment)"]
        end
    end

    Gateway --> OmsSvc & LocalEngine & MarketDataSvc & PortfolioSvc & AiCopilotSvc & AuditSvc
    OmsSvc -->|Route Order Execution| LocalEngine & ExternalGateway
    OmsSvc -->|State Persistence| Postgres
    OmsSvc -->|Saga Triggers| RabbitMQ
    LocalEngine -->|Publish Ticks| Kafka & Timescale
    LocalEngine -->|Fills & Matches| RabbitMQ & Kafka
    ExternalGateway -->|Normalized Fills| RabbitMQ & Kafka
    SettlementSvc -->|Saga Orchestration| RabbitMQ
    MarketDataSvc -->|Stream Candles & OrderBook| Redis & Timescale
    AuditSvc --> Elastic
    AiCopilotSvc --> Qdrant
```

---

## Detailed Component Specifications

### 1. Solution Layout (`Palantir.slnx`)

```
Palantir/
├── .github/
│   └── workflows/                          # Multi-stage CI/CD pipelines
│       ├── backend-ci.yml                  # Build, Unit Tests (TUnit), ArchUnit
│       ├── contract-tests.yml              # PactNet contract tests
│       ├── frontend-ci.yml                 # Vitest, TypeCheck, Lint, Playwright
│       └── release.yml                     # Container build & SLSA provenance
├── deploy/                                 # K8s Helm charts & docker-compose fallbacks
├── docs/
│   └── palantir_implementation_plan.md     # Architecture documentation
├── src/
│   ├── Aspire/
│   │   ├── Palantir.AppHost/               # Aspire orchestrator
│   │   └── Palantir.ServiceDefaults/       # OTEL, health checks, Serilog, discovery
│   │
│   ├── BuildingBlocks/
│   │   ├── Palantir.SharedKernel/          # DDD primitives: Entity, AggregateRoot, DomainEvent
│   │   ├── Palantir.Chassis/               # Cross-cutting: CQRS, EF Core, Redis, Auth, Endpoints
│   │   ├── Palantir.Constants/             # Centralized system constants & Aspire names
│   │   ├── Palantir.Generators/            # Roslyn Source Generators (Fast Query Parameter Binder)
│   │   └── Palantir.Generators.Abstractions/
│   │
│   ├── Gateway/
│   │   └── Palantir.Gateway/               # YARP Gateway with rate-limiting & auth forwarding
│   │
│   ├── Services/
│   │   ├── Oms/                            # Microsoft Orleans OMS Silo
│   │   │   ├── Palantir.Oms.Contracts/     # Orleans Grain Interfaces (IOrderGrain, IClientSessionGrain)
│   │   │   └── Palantir.OmsService/        # Silo host with PostgreSQL clustering & persistence
│   │   │
│   │   ├── Execution/
│   │   │   ├── Palantir.LocalEngineService/# Custom symbol pairs, matching engine, TimescaleDB OHLC
│   │   │   └── Palantir.ExternalGatewayService/ # REST/WebSocket external exchange connectors + simulator
│   │   │
│   │   ├── MarketData/                     # Ticks, order books, candlestick streaming
│   │   │   └── Palantir.MarketDataService/
│   │   ├── Risk/                           # Pre-trade margin, limit checks, circuit breakers
│   │   │   └── Palantir.RiskService/
│   │   ├── Portfolio/                      # Cash ledger, open positions, real-time PnL
│   │   │   └── Palantir.PortfolioService/
│   │   ├── Settlement/                     # Post-trade clearing & MassTransit Saga workflow
│   │   │   └── Palantir.SettlementService/
│   │   ├── Audit/                          # Regulatory audit trail to Elasticsearch
│   │   │   └── Palantir.AuditService/
│   │   └── AiAssistant/                    # MCP server & AI market sentiment copilot
│   │       └── Palantir.AiAssistantService/
│   │
│   └── Clients/
│       ├── shell/                          # Module Federation Container Shell (Host)
│       ├── remote-trading-desk/            # Micro-frontend: Blotter, OrderBook, OHLC Charting
│       ├── remote-portfolio/               # Micro-frontend: Positions, Balances, Risk UI
│       ├── remote-admin/                   # Micro-frontend: Custom Symbol admin, Audit trails
│       └── shared-components/              # Shared Tailwind/Radix UI component library
│
└── tests/
    ├── Palantir.ArchTests/                 # ArchUnitNET architecture governance tests
    ├── Palantir.ContractTests/             # PactNet consumer-driven contract tests
    ├── Palantir.Testing.Shared/            # Testcontainers fixtures & Bogus fakers
    ├── Services/
    │   ├── Palantir.OmsService.UnitTests/
    │   ├── Palantir.LocalEngineService.UnitTests/
    │   ├── Palantir.ExternalGatewayService.UnitTests/
    │   ├── Palantir.RiskService.UnitTests/
    │   ├── Palantir.SettlementService.IntegrationTests/
    │   └── Palantir.MarketDataService.UnitTests/
    └── e2e/
        └── playwright/                     # Playwright Micro UI E2E test suite
```

---

### 2. Microsoft Orleans Order Management System (`Palantir.OmsService`)

- **Virtual Actor Contracts (`Palantir.Oms.Contracts`)**:
  - `IOrderGrain`: Keyed by `Guid orderId`. Encapsulates full order lifecycle, validation rules, state transitions (`New`, `Validated`, `PartiallyFilled`, `Filled`, `Cancelled`, `Rejected`), and execution routing.
  - `IClientSessionGrain`: Keyed by `string clientId`. Tracks client active orders, cumulative open exposure, and receives real-time execution callbacks.
  - `IOrderExpiryGrain`: Configured with Orleans Reminders to automatically cancel expired Good-Til-Time (GTT) orders.
- **Clustering & Storage**:
  - `Orleans.Clustering.AdoNet`: Cluster membership managed via PostgreSQL tables.
  - `Orleans.Persistence.AdoNet`: Grain state snapshots persisted directly into PostgreSQL tables with optimistic concurrency checks (`ETag`).
- **Execution Dispatch**:
  - The `OrderGrain` evaluates the destination venue from symbol metadata:
    - If `VenueType == Local`: Invokes `Palantir.LocalEngineService` via gRPC / high-speed client.
    - If `VenueType == External`: Dispatches via `Palantir.ExternalGatewayService`.

---

### 3. Dual-Mode Trading Engine

#### A. Local Trading Engine (`Palantir.LocalEngineService`)
- **Custom Trading Pair Management**:
  - Users can define custom pairs (e.g., `PAL/USD`, `ETH/PAL`, `GOLD/USDT`) with parameters: base currency, quote currency, tick size, lot size, min/max quantity, and price collar bands.
- **In-Memory Matching Engine**:
  - High-performance, lock-free price-time priority L2 limit order book (`OrderBook`).
  - Supports `Limit`, `Market`, and `Stop-Limit` order types with partial fills.
- **Internal OHLC (Open, High, Low, Close) Aggregation via TimescaleDB**:
  - Each trade match produces an execution tick written to a TimescaleDB hypertable (`market_ticks`).
  - TimescaleDB **Continuous Aggregates** automatically compute and materialize candlestick bars across multiple intervals:
    - `ohlc_1s`, `ohlc_1m`, `ohlc_5m`, `ohlc_1h`, `ohlc_1d`.
  - Active (in-flight) candles are held in Redis and updated in real-time before the window closes, then streamed to clients via SignalR.

#### B. External Exchange Gateway (`Palantir.ExternalGatewayService`)
- **REST & WebSocket Pluggable Venue Connectors**:
  - Modular connector architecture implementing `IExchangeVenueConnector`.
  - Normalizes external venue order formats into Palantir standard commands and maps external execution responses into standard `TradeExecutedEvent` messages.
- **Simulated Mock Exchange**:
  - Embedded container/service simulating an external venue with realistic order response latency, simulated order matching, and public quote feeds for deterministic testing.

---

### 4. Building Blocks & Cross-Cutting Chassis

#### `Palantir.SharedKernel`
- **Domain Primitives**:
  - Strongly-typed IDs via `Vogen` (`OrderId`, `TradeId`, `AccountId`, `SymbolId`, `ClientId`).
  - Domain Events base record (`EventId`, `TimestampUtc`, `CorrelationId`).
  - Value Objects: `Money`, `Price`, `Quantity`, `TradeSide` (Buy/Sell), `OrderStatus`, `TimeInForce`.
  - Auditing (`IAuditableEntity`) and Soft Deletes (`ISoftDeletable`).
- **Specifications**: Base `ISpecification<T>` and specification evaluator for domain constraints.

#### `Palantir.Chassis`
- **Zero-Reflection CQRS**: `Mediator.SourceGenerator` for compile-time generated `ICommand<T>` / `IQuery<T>` handlers with pipeline behaviors (validation, logging, tracing).
- **Minimal API Discovery**: Automatic scanning and registration via `IEndpoint` with `Asp.Versioning.Http`.
- **Distributed Caching**: `FusionCache` (L1 MemoryCache + L2 Redis) for ultra-low-latency quote and order book queries.
- **Security & Authorization**:
  - Keycloak JWT Bearer authentication with automatic JWKS retrieval.
  - Granular Policy-Based Access Control (PBAC): `[HasPermission(Permissions.AllowPlaceOrder)]`, `[HasPermission(Permissions.AllowDefineSymbol)]`.
  - Redis-backed Permission Token Cache evaluating user rights in `< 1ms`.
- **EF Core Interceptors**: `PublishDomainEventsInterceptor` and `AuditableEntityInterceptor`.

#### `Palantir.Generators`
- Roslyn source generator generating allocation-free query parameter binding for Minimal APIs.

---

### 5. Messaging & Saga Workflow Architecture

- **RabbitMQ (MassTransit)**:
  - Command dispatching, request/response, and post-trade settlement sagas.
  - Outbox pattern enabled across all services via `MassTransit.EntityFrameworkCoreIntegration`.
- **Apache Kafka (MassTransit Kafka Rider)**:
  - High-throughput tick and candle streaming: `market.ticks.v1`, `market.ohlc.v1`, `trades.executed.v1`.
  - Partitioned by `SymbolId` for strict order preservation.
- **Order Execution & Settlement Saga**:
  - Orchestrated in `Palantir.SettlementService` via MassTransit Automatonymous state machine with compensation, margin holds/releases, and timeout monitors.

---

### 6. Micro UI (Module Federation Architecture)

- **Host Shell**:
  - React 19 + TypeScript + Webpack 5 Module Federation container.
  - Top-level Keycloak PKCE authentication, navigation, and workspace context.
- **Trading Desk Remote**:
  - Real-time Depth Chart & L2 OrderBook widget.
  - Interactive Candlestick Chart (Lightweight Charts) connected to internal TimescaleDB OHLC feeds.
  - Order Entry Blotter with live status updates.
- **Portfolio & Risk Remote**:
  - Live positions grid, margin utilization gauge, real-time PnL.
- **Admin & Symbol Management Remote**:
  - Custom trading pair creator (define symbols, tick sizes, quote currency).
  - Regulatory trade audit log viewer (queries against Elasticsearch).

---

### 7. AI Assistant & Model Context Protocol (MCP)

Implemented in `Palantir.AiAssistantService`:
- Exposes an **MCP Server** (`ModelContextProtocol.AspNetCore`) enabling AI agents to query orders, verify margin, check OHLC history, and simulate trades.
- Market Sentiment Copilot with news sentiment vectorization stored in **Qdrant**.

---

### 8. Testing Strategy & Quality Assurance

1. **Unit Testing (`TUnit` / `Shouldly` / `Bogus`)**: Fast parallel unit tests for trading math, order state transitions, and matching logic.
2. **Orleans Testing (`Microsoft.Orleans.TestingHost`)**: Test cluster fixtures verifying `OrderGrain` lifecycle, re-entrancy, and state persistence.
3. **Architecture Governance (`ArchUnitNET`)**: Strict layer separation, VSA feature isolation, and strongly-typed ID enforcement.
4. **Contract Testing (`PactNet v5`)**: Verify contracts between OMS, Execution Services, Risk, and Portfolio.
5. **Integration Testing (`Testcontainers`)**: Real PostgreSQL, TimescaleDB, RabbitMQ, Kafka, and Redis containers.
6. **End-to-End Testing (`Playwright`)**: Micro UI flows: Keycloak login, custom symbol creation, order submission, and chart update verification.

---

## Verification Plan

### Automated Tests
- Unit & Architecture: `rtk dotnet test tests/Palantir.ArchTests/Palantir.ArchTests.csproj`
- Orleans OMS Tests: `rtk dotnet test tests/Services/Palantir.OmsService.UnitTests/Palantir.OmsService.UnitTests.csproj`
- Matching & OHLC Tests: `rtk dotnet test tests/Services/Palantir.LocalEngineService.UnitTests/Palantir.LocalEngineService.UnitTests.csproj`
- Contract Tests: `rtk dotnet test tests/Palantir.ContractTests/Palantir.ContractTests.csproj`
- Frontend E2E: `npx playwright test`

### Manual Verification
1. Launch solution via .NET Aspire: `dotnet run --project src/Aspire/Palantir.AppHost`
2. Open Aspire Dashboard: Verify all containers (Postgres, TimescaleDB, RabbitMQ, Kafka, Redis, Keycloak, Elasticsearch, Qdrant) and microservices report healthy.
3. Access Admin Remote: Create a custom symbol pair (e.g., `PAL/USD`).
4. Access Trading Desk Remote: Place matching buy and sell limit orders on `PAL/USD` via the Orleans OMS; observe trade execution and real-time OHLC candlestick bar generation on the chart.
