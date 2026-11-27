# Trading, Execution & Market Data Services

## 1. `Palantir.Gateway` — YARP API Edge

- **Files**: [Program.cs](../../../src/Gateway/Palantir.Gateway/Program.cs), [appsettings.json](../../../src/Gateway/Palantir.Gateway/appsettings.json), [AspireServiceDiscoveryDestinationResolver.cs](../../../src/Gateway/Palantir.Gateway/AspireServiceDiscoveryDestinationResolver.cs).
- **Capabilities**:
  - Validates Keycloak JWT tokens at the edge (`AddPalantirJwtAuthentication`).
  - Configures `"AllowMicroUI"` CORS policy for local microfrontend ports (`:3000`–`:3003`, `:3100`–`:3101`).
  - Routes `/api/v1/iam/*`, `/api/v1/orders/*`, `/api/v1/engine/*`, `/api/v1/external/*`, `/api/v1/risk/*`, `/api/v1/market/*`, `/hubs/marketdata/*`, `/api/v1/portfolio/*`, `/api/v1/settlements/*`, `/api/v1/audit/*`, `/mcp/*`, `/api/v1/ai/*`, `/api/v1/a2a/*`, and `/mock-exchange/*` via Aspire service discovery.

---

## 2. `Palantir.OmsService` — Order Management

- **Core Stack**: Orleans (`OrderGrain`, `ClientSessionGrain`, `OrderExpiryGrain`), Marten Event Store (`MartenOrderRepository`, `OrderBlotterProjection`), Wolverine Outbox, gRPC + HTTP dispatchers.
- **Startup** ([Program.cs](../../../src/Services/Oms/Palantir.OmsService/Program.cs)):
  1. Initializes Orleans ADO.NET PostgreSQL schema ([OrleansSchemaInitializer.cs](../../../src/Services/Oms/Palantir.OmsService/Infrastructure/OrleansSchemaInitializer.cs)).
  2. Configures Orleans silo clustering (localhost in Dev, ADO.NET in Prod), `"orders"` grain storage, and reminders.
  3. Registers Marten with async `OrderBlotterProjection`.
  4. Registers `LocalGrpcVenueDispatcher` (HTTP/2 gRPC with `AddResilientGrpcClient`) and `ExternalHttpVenueDispatcher` (with `AddTokenExchangeHandler`).
- **Vertical Slices (`Features/Orders/`)**:
  - `POST /api/v1/orders` ([CreateOrderFeature.cs](../../../src/Services/Oms/Palantir.OmsService/Features/Orders/Create/CreateOrderFeature.cs)): Validates `CreateOrderCommand` using shared `OrderSymbolSpecification`, `OrderQuantitySpecification`, and `LimitOrderPriceSpecification` (both in `CreateOrderValidator` and `CreateOrderHandler`), invokes `IOrderGrain.SubmitOrderAsync`, and emits a non-blocking `AuditEntry` via `IAuditEmitter`.
  - `DELETE /api/v1/orders/{id:guid}` ([CancelOrderFeature.cs](../../../src/Services/Oms/Palantir.OmsService/Features/Orders/Cancel/CancelOrderFeature.cs)): Cancels active grain order.
  - `GET /api/v1/orders/{id:guid}` ([GetOrderFeature.cs](../../../src/Services/Oms/Palantir.OmsService/Features/Orders/Get/GetOrderFeature.cs)), `GET /api/v1/orders/blotter` ([GetBlotterOrdersFeature.cs](../../../src/Services/Oms/Palantir.OmsService/Features/Orders/GetBlotter/GetBlotterOrdersFeature.cs)), and `GET /api/v1/orders/client/{clientId:guid}/active` ([GetClientActiveOrdersFeature.cs](../../../src/Services/Oms/Palantir.OmsService/Features/Orders/GetClientActive/GetClientActiveOrdersFeature.cs)).

---

## 3. `Palantir.LocalEngineService` — Matching Engine

- **Startup** ([Program.cs](../../../src/Services/Execution/Palantir.LocalEngineService/Program.cs)): Hosts `MatchingEngineGrpcService` + gRPC health checks, `InMemoryOrderBookRegistry`, `MarketTickDbContext` (`local_engine` schema), and RabbitMQ fanout publishers for `MarketTickRecordedEvent` and `OrderBookUpdatedEvent` (`market.data` -> `market.ticks.v1`) plus `TradeExecutedEvent` (`trading.settlement.saga`).
- **Vertical Slices**:
  - `POST /api/v1/engine/match` ([MatchOrderFeature.cs](../../../src/Services/Execution/Palantir.LocalEngineService/Features/Matching/MatchOrder/MatchOrderFeature.cs))
  - `GET /api/v1/engine/orderbook/{symbol}` ([GetOrderBookFeature.cs](../../../src/Services/Execution/Palantir.LocalEngineService/Features/OrderBook/Get/GetOrderBookFeature.cs))
  - `GET /api/v1/engine/candles/{symbol}` ([GetCandlesFeature.cs](../../../src/Services/Execution/Palantir.LocalEngineService/Features/Candles/Get/GetCandlesFeature.cs))
  - `POST /api/v1/engine/symbols` & `GET /api/v1/engine/symbols` ([CreateSymbolFeature.cs](../../../src/Services/Execution/Palantir.LocalEngineService/Features/Symbols/Create/CreateSymbolFeature.cs), [ListSymbolsFeature.cs](../../../src/Services/Execution/Palantir.LocalEngineService/Features/Symbols/List/ListSymbolsFeature.cs))

---

## 4. `Palantir.ExternalGatewayService` — Venue Adapter

- **Startup** ([Program.cs](../../../src/Services/Execution/Palantir.ExternalGatewayService/Program.cs)): Configures `RestWebSocketVenueConnector` implementing `IExchangeVenueConnector`.
- **Vertical Slices**:
  - `POST /api/v1/external/orders` ([RouteExternalOrderFeature.cs](../../../src/Services/Execution/Palantir.ExternalGatewayService/Features/ExternalOrders/Route/RouteExternalOrderFeature.cs)): Routes external venue orders (`Permissions.AllowPlaceOrder`).
  - `/mock-exchange/*` ([SimulateExchangeFeature.cs](../../../src/Services/Execution/Palantir.ExternalGatewayService/Features/Simulator/SimulateExchangeFeature.cs)): Built-in exchange execution simulator for local/integration testing.

---

## 5. `Palantir.MarketDataService` — Real-Time Data

- **Startup** ([Program.cs](../../../src/Services/MarketData/Palantir.MarketDataService/Program.cs)): Listens to RabbitMQ `market.ticks.v1` and maps SignalR hub `/hubs/marketdata` ([MarketDataHub.cs](../../../src/Services/MarketData/Palantir.MarketDataService/Hubs/MarketDataHub.cs)).
- **Event Handler** ([MarketDataTickHandler.cs](../../../src/Services/MarketData/Palantir.MarketDataService/Handlers/MarketDataTickHandler.cs)): Stores latest quotes and L2 snapshots in `IKeyValueStore` (`15 min` TTL) and broadcasts `MarketTick` / `OrderBookUpdated` to SignalR symbol groups.
- **Vertical Slices**: `GET /api/v1/market/quotes/{symbol}` ([GetQuoteFeature.cs](../../../src/Services/MarketData/Palantir.MarketDataService/Features/Quotes/Get/GetQuoteFeature.cs)) and `GET /api/v1/market/orderbook/{symbol}` ([GetCachedOrderBookFeature.cs](../../../src/Services/MarketData/Palantir.MarketDataService/Features/OrderBook/Get/GetCachedOrderBookFeature.cs)).
