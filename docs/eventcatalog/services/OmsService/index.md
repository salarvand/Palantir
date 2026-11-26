---
id: OmsService
name: Palantir.OmsService
version: 1.0.0
summary: Microservice implementing Microsoft Orleans Grains, Marten Event Sourcing on PostgreSQL, and Wolverine RabbitMQ messaging.
---

# Palantir.OmsService

## Endpoints
- `POST /api/v1/orders` (Submit order via `IOrderGrain`)
- `DELETE /api/v1/orders/{id}` (Cancel order via `IOrderGrain`)
- `GET /api/v1/orders/{id}` (Get order snapshot or Marten blotter projection)
- `GET /api/v1/orders/client/{clientId}` (Get active client order IDs)
- `GET /api/v1/orders/blotter` (Query Marten `OrderBlotterView` in PostgreSQL)

## Protocols
- HTTP/2 gRPC client to `Palantir.LocalEngineService`
- Wolverine RabbitMQ outbox publisher to `Palantir.SettlementService` and `Palantir.AuditService`
