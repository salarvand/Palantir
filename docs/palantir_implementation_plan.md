# Palantir: Cloud-Native .NET 10 Trading Platform Reference Architecture & Implementation Plan

## Goal Description
Build **Palantir**, a production-grade, cloud-native .NET 10 reference architecture utilizing modern financial trading systems as the representative domain. The solution serves as an enterprise architectural blueprint synthesizing the strengths of **BookWorm** (cutting-edge .NET 10, Aspire orchestration, vertical slices, CQRS pipeline behaviors, Vogen strongly-typed IDs, FusionCache, AI/MCP integration, and modern TUnit/PactNet/ArchUnit testing) and **DocumentManagement** (MassTransit Saga State Machines with compensation, granular PBAC with Redis-cached permission tokens, Elasticsearch compliance audit logging, and traceable entities).

All 24 required capabilities are comprehensively planned and implemented, with the **Order Lifecycle & Execution (OMS + LocalEngine + Settlement Saga + AI Trade Copilot)** designated as the flagship slice exhibiting all patterns end-to-end.

---

## Architectural Directives & Alignments
1. **Microservices Chassis**: `Palantir.Platform` is retained and expanded as the comprehensive, reusable building block chassis across all services.
2. **Client Architecture**: Standardized on **Webpack 5 Module Federation** with distributed microfrontends: Shell (`:3000`), Trading Desk (`:3001`), Portfolio & Risk (`:3002`), and Admin & IAM (`:3003`), conforming to WCAG 2.1 AA standards.
3. **Identity Provider**: Exclusively **Keycloak** (OIDC + PKCE + RFC 8693 Token Exchange; strictly zero Azure/Entra ID).
4. **AI Architecture**: Self-hosted **Ollama** container orchestrated via Aspire with `Microsoft.Extensions.AI`, Microsoft Agent Framework, Model Context Protocol (MCP), A2A, AG-UI, and A2UI.
5. **Flagship Domain Slice**: **Order Lifecycle & Execution (OMS + LocalEngine + Settlement Saga + AI Trade Copilot)** implemented end-to-end with production-grade schemas, zero fallback or mock data, and full event-sourced persistence.

---

## Complete Multi-Phase Implementation Roadmap
For the comprehensive, step-by-step technical execution instructions, protobuf contracts, class structures, and verification procedures, refer to:
[palantir_multi_phase_implementation_guide.md](file:///d:/01/01/07/PalantirCode/docs/palantir_multi_phase_implementation_guide.md)

### Summary of Phases:
- **Phase 1: Microservices Chassis Expansion (`Palantir.Platform`)**: MediatR CQRS pipeline behaviors (`Validation`, `Logging`, `Activity`, `Transaction`), Wolverine PostgreSQL durable outbox and inbox, Marten Event Store registration, `Asp.Versioning.Http`, `Microsoft.FeatureManagement`, Keycloak RFC 8693 Token Exchange, and gRPC client resilience.
- **Phase 2: Domain-Driven Design & Marten Event Sourcing**: Rich `Order` and `Trade` aggregates with invariant encapsulation, immutable domain events, and Marten append-only PostgreSQL streams with async daemon read model projections (`OrderBlotterView`).
- **Phase 3: Vertical Slice Architecture & gRPC in Flagship Slice**: Re-architect `Palantir.OmsService` into vertical slices (`Features/Orders/CreateOrder`, `CancelOrder`, `GetOrderById`, `GetActiveBlotter`) and implement `MatchingEngineGrpc` service in `Palantir.LocalEngineService` for sub-millisecond execution.
- **Phase 4: Saga Orchestration & Distributed Compensation**: MassTransit Automatonymous state machine saga in `Palantir.SettlementService` coordinating two-phase settlement between OMS, Risk, Portfolio, and Clearing with compensation on failure.
- **Phase 5: Self-Hosted AI Multi-Agent Subsystem**: Aspire Ollama container, `Microsoft.Extensions.AI`, Microsoft Agent Framework multi-agent workflow (Trade Analysis Agent + Risk Guard Agent), standardized MCP tools, A2A protocol, AG-UI streaming, interactive A2UI component schemas, and Presidio PII governance.
- **Phase 6: Centralized Architecture & Event Documentation**: OpenAPI 3.1 with Scalar UI, AsyncAPI 3.0 event contracts for Kafka/RabbitMQ topics, and EventCatalog centralized graph documentation in `docs/eventcatalog/`.
- **Phase 7: Webpack 5 Module Federation Microfrontends**: Host Shell (`:3000`), Trading Desk remote (`:3001`), Portfolio remote (`:3002`), Admin remote (`:3003`), and WCAG 2.1 AA accessibility compliance (`docs/ACCESSIBILITY.md`).
- **Phase 8: Comprehensive Enterprise Testing Strategy**: Service unit tests (xUnit + Shouldly + Bogus), ArchUnitNET architecture governance tests, PactNet v5 consumer/provider contract tests, Vitest/RTL frontend tests, and Playwright BDD Gherkin `.feature` tests.
- **Phase 9: CI/CD Automation with GitHub Actions**: End-to-end GitHub Actions workflows covering backend restore/build/arch-tests/unit-tests/pacts and frontend lint/test/build/e2e.
