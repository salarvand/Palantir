# Palantir Cloud-Native Trading Platform

An enterprise .NET 10 + Aspire reference architecture implementing Vertical Slice Architecture (`Features/`), Domain-Driven Design, CQRS, Event Sourcing, Distributed Sagas, Zero-Trust Keycloak AuthN/AuthZ, Multi-Agent AI Protocols, Module Federation + Next.js Multi-Zones, and multi-layered testing.

## Reference Documentation
- [Comprehensive Architectural Reference & Implementation Guide](file:///f:/Lab/Trade/04/Palantir/docs/architecture-reference-guide.md)
- [WCAG 2.1 AA Accessibility Statement](file:///f:/Lab/Trade/04/Palantir/docs/ACCESSIBILITY.md)
- [EventCatalog Configuration](file:///f:/Lab/Trade/04/Palantir/docs/eventcatalog/eventcatalog.config.js)

## Project Goals

- [x] Developed a cloud-native application using Aspire (`Palantir.AppHost` with all backend services & UIs)
- [x] Implemented Vertical Slice Architecture with Domain-Driven Design & CQRS (`Features/` slices across all 9 services; `Endpoints/` retained only in `Palantir.Platform`)
- [x] Enabled service-to-service communication with gRPC (`matching_engine.proto` + resilient HTTP/2 gRPC + health checks)
- [x] Incorporated microservices patterns:
  - [x] Outbox and inbox patterns (`WolverineFx` PostgreSQL durable outbox/inbox)
  - [x] Saga patterns for orchestration (`OrderSettlementSaga`) and choreography (`PreTradeRiskApprovedEvent`, `TradeExecutedEvent`)
  - [x] Event sourcing (`Marten` PostgreSQL event streams + async `OrderBlotterProjection`)
  - [x] Microservices chassis (`Palantir.Platform`) for cross-cutting concerns
- [x] Implemented API versioning (`Asp.Versioning.Http`) and feature flags (`Microsoft.FeatureManagement.AspNetCore`)
- [x] Set up AuthN/AuthZ with Keycloak:
  - [x] Authorization Code Flow with PKCE (`palantir-shell` client + Next.js Multi-Zones)
  - [x] RFC 8693 Token Exchange (`KeycloakTokenExchangeService` + `TokenExchangeDelegatingHandler`)
- [x] Implemented caching (`FusionCache` with PostgreSQL L2 `IDistributedCache` + fail-safe)
- [x] Incorporated AI components:
  - [x] Multi-agent workflows (`TradeCopilotCoordinator`, `TradeAnalysisAgent`, `RiskGuardAgent`)
  - [x] Standardized AI tooling with Model Context Protocol (`ModelContextProtocol.AspNetCore` `/mcp`)
  - [x] Agent-to-agent communication via A2A Protocol (`/api/v1/a2a/card` & `/api/v1/a2a/messages`)
  - [x] Agent interactions via AG-UI Protocol (`/api/v1/ai/stream` SSE)
  - [x] Policy-based agent governance (`Policies/ai-agents.yaml` + PII redaction + budget controls)
  - [x] Rich interactive UIs using A2UI (`TradeProposalCard`, `RiskGauge`, `DepthImpactChart` + frontend `A2UIRenderer`)
- [x] Configured CI/CD with GitHub Actions (`.github/workflows/backend-ci.yml`, `.github/workflows/frontend-ci.yml`)
- [x] Created comprehensive documentation (OpenAPI + Scalar, Saunter AsyncAPI, EventCatalog)
- [x] Built modern client applications accessible via the Aspire Dashboard:
  - [x] Webpack 5 Module Federation (`shell` `:3000`, `trading-desk` `:3001`, `portfolio-mfe` `:3002`, `admin-mfe` `:3003`)
  - [x] Microfrontend (MFE) architecture using Next.js Multi-Zones (`storefront` `:3100`, `backoffice` `:3101`)
  - [x] Targeted WCAG 2.1 AA accessibility standards
- [x] Established a testing strategy:
  - [x] Service unit tests (`xUnit` + `Shouldly` + `Bogus` across all services)
  - [x] Pact-based contract testing (`PactNet` HTTP & message contracts)
  - [x] Architecture testing (`ArchUnitNET` enforcing `Features/`, no service `Endpoints/`, DDD/Saga rules)
  - [x] Frontend unit and component tests (`Vitest` + `@testing-library/react`)
  - [x] End-to-end testing with BDD (`playwright-bdd` + `@axe-core/playwright` for `tests/bdd/*.feature`)
