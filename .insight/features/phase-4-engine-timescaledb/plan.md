---
insight_schema: "1.0.0"
feature_id: "phase-4-engine-timescaledb"
standards_version: "1.0.0"
source: "docs/review.md"
spec_hash: "bc42a39d7e23218155c883020e333db377d9b4415e700501e116257ea604ff34"
status: "approved"
---

# Feature Implementation Plan: Phase 4 - Local Engine Kafka Tick Publishing & TimescaleDB Continuous Aggregates

## 1. Requirements Matrix
* **[REQ-01]**: Implement startup database migration in `src/Services/Execution/Palantir.LocalEngineService/Program.cs` executing raw SQL DDL to initialize TimescaleDB extension (`CREATE EXTENSION IF NOT EXISTS timescaledb;`), convert `market_ticks` table into a hypertable (`create_hypertable`), and define continuous aggregates for timeframes `ohlc_1s`, `ohlc_1m`, `ohlc_5m`, `ohlc_1h`, `ohlc_1d`.
* **[REQ-02]**: In `src/Services/Execution/Palantir.LocalEngineService/Endpoints/LocalEngineEndpoints.cs`, update `MatchOrderAsync` to publish `TradeExecutedEvent` messages and stream execution ticks to Kafka topic `market.ticks.v1`.
* **[REQ-03]**: Implement query endpoints in `LocalEngineEndpoints.cs` to query materialized OHLC candlestick data from TimescaleDB continuous aggregates with timeframe filtering.
* **[REQ-04]**: Ensure lock-free, thread-safe order book matching in `OrderBook.cs` maintaining strict price-time priority.

## 2. In-Scope Modules & File Targets
* `src/Services/Execution/Palantir.LocalEngineService/Program.cs` (Modify: Add startup TimescaleDB hypertable and continuous aggregate migration DDL)
* `src/Services/Execution/Palantir.LocalEngineService/Endpoints/LocalEngineEndpoints.cs` (Modify: Publish ticks to Kafka and query continuous aggregate views)
* `src/Services/Execution/Palantir.LocalEngineService/Timescale/TimescaleEntities.cs` (Modify: Map continuous aggregate view entities)
* `src/Services/Execution/Palantir.LocalEngineService/Palantir.LocalEngineService.csproj` (Modify: Ensure MassTransit Kafka Rider or Wolverine Kafka dependency is present)

## 3. Explicit Out-of-Scope (Scope Guard)
* Post-trade settlement saga processing (handled in SettlementService).
* Regulatory audit log indexing to Elasticsearch (handled in Phase 5).
* External broker FIX/WebSocket adapter integrations.

## 4. Architectural Invariants
* **[ARCH-01]**: OHLC candlestick calculation must run inside TimescaleDB continuous aggregates; application layer must never recalculate historical OHLC bars from raw ticks.
* **[ARCH-02]**: High-frequency ticks published to Kafka must use `SymbolId` partition keys to guarantee strict per-symbol ordering.
* **[ARCH-03]**: Order book state matching in memory must be deterministic and thread-safe.

## 5. Verification & Test Plan
* **Unit Tests**: `tests/Services/Palantir.LocalEngineService.UnitTests/OrderBookTests.cs` verifying price-time priority matching and partial fill behavior.
* **OHLC Query Tests**: Verify candlestick query endpoint returns continuous aggregate data.
