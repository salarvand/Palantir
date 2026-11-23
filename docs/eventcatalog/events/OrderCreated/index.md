---
id: OrderCreated
name: OrderCreated
version: 1.0.0
summary: Emitted when an order passes invariant validation and is recorded in the Marten event store.
producers:
  - OmsService
consumers:
  - SettlementService
  - AuditService
---

# OrderCreated Event

```json
{
  "orderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "clientId": "7ca85f64-5717-4562-b3fc-2c963f66afb7",
  "symbol": "PAL/USD",
  "side": "Buy",
  "type": "Limit",
  "quantity": 100.0,
  "price": 150.25,
  "timeInForce": "GoodTilCancelled",
  "timestampUtc": "2026-11-23T10:00:00Z"
}
```
