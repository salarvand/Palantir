---
insight_schema: "1.0.0"
feature_id: "phase-2-aspire-gateway"
standards_version: "1.0.0"
source: "docs/review.md"
spec_hash: "cdb31952a441356fe75d12a6936557fdc08eb31c5dab93ac7d8b4229ce7b80bc"
status: "approved"
---

# Feature Implementation Plan: Phase 2 - Aspire AppHost Orchestration & Gateway Route Completion

## 1. Requirements Matrix
* **[REQ-01]**: Register all 12 microservice and edge projects in `src/Aspire/Palantir.AppHost/Program.cs` with references to Postgres, Redis, RabbitMQ, Kafka, and Keycloak resources.
* **[REQ-02]**: Add container resource definitions for TimescaleDB (`timescale/timescaledb-ha:pg16`), Elasticsearch (`docker.elastic.co/elasticsearch/elasticsearch:8.17.0`), and Qdrant (`qdrant/qdrant:latest`) to `Palantir.AppHost/Program.cs`.
* **[REQ-03]**: Populate complete reverse proxy routes and cluster destinations in `src/Gateway/Palantir.Gateway/appsettings.json` covering all backend domains: `/api/v1/engine/**` (`engine-cluster`), `/api/v1/external/**` (`external-cluster`), `/api/v1/risk/**` (`risk-cluster`), `/api/v1/settlements/**` (`settlement-cluster`), `/mcp/**` (`ai-cluster`), `/api/v1/iam/**`, `/api/v1/orders/**`, `/api/v1/market/**`, `/api/v1/portfolio/**`, and `/api/v1/audit/**`.
* **[REQ-04]**: Configure Aspire service discovery and environment bindings so YARP gateway proxies upstream requests to internal service endpoints seamlessly.

## 2. In-Scope Modules & File Targets
* `src/Aspire/Palantir.AppHost/Program.cs` (Modify: Register all 12 projects and additional container definitions for TimescaleDB, Elastic, Qdrant)
* `src/Gateway/Palantir.Gateway/appsettings.json` (Modify: Define full YARP routing tables and upstream cluster destinations)
* `src/Aspire/Palantir.AppHost/Palantir.AppHost.csproj` (Modify: Add package references if needed for community hosting extensions)

## 3. Explicit Out-of-Scope (Scope Guard)
* Direct business logic changes in microservice domain layers.
* Database entity schema migrations (handled in service-specific phases).
* Client micro-frontend remotes build setup.

## 4. Architectural Invariants
* **[ARCH-01]**: Gateway routing must rely strictly on Aspire service discovery DNS names rather than hardcoded IPs or host ports.
* **[ARCH-02]**: Infrastructure containers must use persistent named data volumes to preserve data between local runs.
* **[ARCH-03]**: Reverse proxy must propagate trace headers (`traceparent`, `tracestate`) and authentication bearer tokens downstream.

## 5. Verification & Test Plan
* **Configuration Validation**: Verify JSON schema validity of `Palantir.Gateway/appsettings.json`.
* **Orchestration Integrity**: Verify project dependencies in `Palantir.AppHost.csproj` match all registered projects in `Program.cs`.
