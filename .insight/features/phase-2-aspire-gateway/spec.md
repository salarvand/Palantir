# Specification: Phase 2 - Aspire AppHost Orchestration & Gateway Route Completion

## Requirements Matrix
* **[REQ-01]**: Register all 12 microservice and edge projects in `src/Aspire/Palantir.AppHost/Program.cs` with references to Postgres, Redis, RabbitMQ, Kafka, and Keycloak resources.
* **[REQ-02]**: Add container resource definitions for TimescaleDB (`timescale/timescaledb-ha:pg16`), Elasticsearch (`docker.elastic.co/elasticsearch/elasticsearch:8.17.0`), and Qdrant (`qdrant/qdrant:latest`) to `Palantir.AppHost/Program.cs`.
* **[REQ-03]**: Populate complete reverse proxy routes and cluster destinations in `src/Gateway/Palantir.Gateway/appsettings.json` covering all backend domains: `/api/v1/engine/**` (`engine-cluster`), `/api/v1/external/**` (`external-cluster`), `/api/v1/risk/**` (`risk-cluster`), `/api/v1/settlements/**` (`settlement-cluster`), `/mcp/**` (`ai-cluster`), `/api/v1/iam/**`, `/api/v1/orders/**`, `/api/v1/market/**`, `/api/v1/portfolio/**`, and `/api/v1/audit/**`.
* **[REQ-04]**: Configure Aspire service discovery and environment bindings so YARP gateway proxies upstream requests to internal service endpoints seamlessly.
