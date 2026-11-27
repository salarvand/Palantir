# Microservices & API Gateway Index

Palantir comprises **1 API Gateway** (`YARP`) and **10 deployable backend service projects** across 9 service domains. Each service owns an isolated PostgreSQL schema (or Marten event store) and communicates synchronously via HTTP/gRPC or asynchronously via RabbitMQ/Wolverine.

| Service | Aspire ID | Schema / Store | Key Protocols & Tech | Deep-Dive Guide |
| --- | --- | --- | --- | --- |
| **Palantir.Gateway** | `gateway` | Stateless | YARP Reverse Proxy, Aspire service discovery, JWT AuthN, CORS | [Trading & Execution](trading-and-execution.md#1-palantirgateway--yarp-api-edge) |
| **Palantir.OmsService** | `oms-service` | `event_store` + Orleans ADO.NET | Orleans `OrderGrain`, Marten projections, gRPC + HTTP dispatch, `IAuditEmitter` | [Trading & Execution](trading-and-execution.md#2-palantiromsservice--order-management) |
| **Palantir.LocalEngineService** | `local-engine-service` | `local_engine` (`MarketTickDbContext`) | HTTP/2 gRPC (`MatchingEngineGrpcService`), L2 in-memory order book, RabbitMQ ticks | [Trading & Execution](trading-and-execution.md#3-palantirlocalengineservice--matching-engine) |
| **Palantir.ExternalGatewayService** | `external-gateway-service` | Stateless | `IExchangeVenueConnector`, mock exchange simulator | [Trading & Execution](trading-and-execution.md#4-palantirexternalgatewayservice--venue-adapter) |
| **Palantir.MarketDataService** | `market-data-service` | `platform_cache` | SignalR (`MarketDataHub`), `IKeyValueStore` quote/book caching | [Trading & Execution](trading-and-execution.md#5-palantirmarketdataservice--real-time-data) |
| **Palantir.RiskService** | `risk-service` | Stateless | `RiskEngine`, specification-validated pre-trade risk, RabbitMQ handlers | [Risk, Portfolio & Settlement](risk-portfolio-settlement.md#1-palantirriskservice--pre-trade-risk) |
| **Palantir.PortfolioService** | `portfolio-service` | `portfolio` (`PortfolioDbContext`) | `Account`/`Position` margin reserve & release, settlement handlers | [Risk, Portfolio & Settlement](risk-portfolio-settlement.md#2-palantirportfolioservice--accounts--margin) |
| **Palantir.SettlementService** | `settlement-service` | `settlement` (`SettlementDbContext`) | Wolverine `OrderSettlementSaga` orchestration & compensation | [Risk, Portfolio & Settlement](risk-portfolio-settlement.md#3-palantirsettlementservice--distributed-saga) |
| **Palantir.IamService** | `iam-service` | `iam` (`IamDbContext`) | Keycloak Admin sync, `PermissionToken` caching, `iam.events` fanout | [Platform & AI Services](platform-and-ai-services.md#1-palantiriamservice--identity--permissions) |
| **Palantir.AuditService** | `audit-service` | `audit` (`AuditDbContext`) + `platform_audit` | `RegulatoryAuditRecord` (`GIN` full-text index), RabbitMQ `audit.trail.v1`, `IAuditEmitter` | [Platform & AI Services](platform-and-ai-services.md#2-palantirauditservice--regulatory-audit) |
| **Palantir.AiAssistantService** | `ai-assistant-service` | `ai` (`SentimentDbContext` + `pgvector`) | MCP `/mcp`, A2A `/api/v1/a2a/*`, AG-UI SSE `/api/v1/ai/stream`, A2UI, YAML governance | [Platform & AI Services](platform-and-ai-services.md#3-palantiraiassistantservice--multi-agent-ai) |
