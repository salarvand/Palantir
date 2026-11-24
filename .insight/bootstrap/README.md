# Palantir Repository Architecture Bootstrap Dossier

## Repository Identification
* **Solution**: `Palantir.slnx`
* **Target Runtime**: .NET 10.0 (C# 14 preview)
* **Architecture Pattern**: Cloud-Native Microservices, Domain-Driven Design (DDD), Vertical Slice Architecture (VSA), CQRS, Event Sourcing, Saga Orchestration, Microservices Chassis, Module Federation Microfrontends.
* **Orchestration**: .NET Aspire AppHost

## Top-Level Architecture Domains

### 1. Aspire Orchestration Layer
* `src/Aspire/Palantir.AppHost`: Central Aspire orchestrator coordinating PostgreSQL, Redis, RabbitMQ, Kafka, Keycloak, TimescaleDB, Meilisearch, Qdrant, Ollama, and 10 microservices.
* `src/Aspire/Palantir.ServiceDefaults`: Shared resilience, OpenTelemetry metrics and tracing, health checks, and service discovery.

### 2. Microservices Chassis & Shared Building Blocks
* `src/BuildingBlocks/Palantir.Platform`: Reusable chassis containing CQRS pipeline behaviors (validation, logging, telemetry, transaction), PostgreSQL durable outbox/inbox with Wolverine, Marten event store setups, FusionCache distributed caching, Keycloak PKCE/RFC 8693 token exchange, API versioning, and feature flags.
* `src/BuildingBlocks/Palantir.SharedKernel`: Core DDD primitives (`Entity<T>`, `AggregateRoot<T>`, `DomainEvent`), Vogen strongly-typed value objects (`OrderId`, `TradeId`, `SymbolId`, `UserId`, `AccountId`), and domain value types (`Money`, `Price`, `Quantity`).
* `src/BuildingBlocks/Palantir.Constants`: System-wide identifiers, permission constants, messaging topics, and Aspire resource names.
* `src/BuildingBlocks/Palantir.Generators`: Roslyn source generator for zero-allocation query parameter binding.

### 3. Flagship Domain Slice: Order Lifecycle & Execution
* `src/Services/Oms/Palantir.OmsService`: Order Management System utilizing Vertical Slice Architecture, CQRS handlers, Marten Event Store on PostgreSQL, and Wolverine outbox.
* `src/Services/Execution/Palantir.LocalEngineService`: High-performance matching engine with in-memory order book, HTTP/2 gRPC server (`matching_engine.proto`), and TimescaleDB hypertable for real-time OHLC continuous aggregates.
* `src/Services/Settlement/Palantir.SettlementService`: MassTransit Automatonymous state machine saga coordinating two-phase settlement between OMS, Risk, Portfolio, and Clearing with compensation on failure.
* `src/Services/AiAssistant/Palantir.AiAssistantService`: Self-hosted AI multi-agent workflow (Trade Analysis Agent + Risk Guard Agent) with Aspire-managed Ollama, Standardized MCP tools, A2A protocol, AG-UI streaming, and interactive A2UI component schemas.

### 4. Supporting Microservices
* `src/Services/Iam/Palantir.IamService`: Keycloak Admin REST API integration, trader KYC profiles, and Redis PBAC token management.
* `src/Services/Execution/Palantir.ExternalGatewayService`: Pluggable REST/WebSocket connectors for external liquidity venues.
* `src/Services/MarketData/Palantir.MarketDataService`: SignalR market hubs streaming real-time Kafka ticks.
* `src/Services/Risk/Palantir.RiskService`: Pre-trade margin calculations, position concentration checks, and risk limits.
* `src/Services/Portfolio/Palantir.PortfolioService`: Ledger accounts, cash balances, reserved margin, and real-time positions.
* `src/Services/Audit/Palantir.AuditService`: Regulatory compliance audit trail indexing to Meilisearch.
* `src/Gateway/Palantir.Gateway`: YARP Reverse Proxy with dynamic service discovery and Keycloak bearer validation.

### 5. Client Microfrontends (Webpack 5 Module Federation)
* `src/Clients/shell`: Host container (port 3000) with Keycloak PKCE auth, routing, and remote container loader.
* `src/Clients/trading-desk`: Remote 1 (port 3001) - L2 OrderBook, Timescale candlestick chart, live blotter, and A2UI AI Copilot drawer.
* `src/Clients/portfolio-mfe`: Remote 2 (port 3002) - Margin utilization gauge and real-time PnL.
* `src/Clients/admin-mfe`: Remote 3 (port 3003) - IAM permission matrix, custom symbols, and regulatory audit viewer.
* Compliance: WCAG 2.1 AA accessibility standards across all components.
