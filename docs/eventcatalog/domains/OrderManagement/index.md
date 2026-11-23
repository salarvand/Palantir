---
id: OrderManagement
name: Order Management
version: 1.0.0
summary: Core order processing domain enforcing aggregate state invariants and Marten event streams.
---

# Order Management Domain

The Order Management Domain handles order inception, validation, risk checks, routing to matching venues, and blotter persistence.

## Services
- `Palantir.OmsService`

## Key Aggregate
- `Order` (Marten event-sourced stream)

## Outgoing Events
- `OrderCreated`
- `OrderExecutionApplied`
- `OrderCancelled`
- `OrderExpired`
