---
id: OmsService
name: Palantir.OmsService
version: 1.0.0
summary: Microservice implementing Vertical Slice Architecture for high-throughput order processing.
---

# Palantir.OmsService

## Features
- `POST /api/v1/orders` (CreateOrder)
- `DELETE /api/v1/orders/{id}` (CancelOrder)
- `GET /api/v1/orders/{id}` (GetOrderById)
- `GET /api/v1/orders/blotter` (GetActiveBlotter)

## Protocols
- HTTP/2 gRPC client to `Palantir.LocalEngineService`
- MassTransit RabbitMQ saga publisher to `Palantir.SettlementService`
