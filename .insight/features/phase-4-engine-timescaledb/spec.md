# Specification: Phase 4 - Local Engine Kafka Tick Publishing & TimescaleDB Continuous Aggregates

## Requirements Matrix
* **[REQ-01]**: Implement startup database migration in `src/Services/Execution/Palantir.LocalEngineService/Program.cs` executing raw SQL DDL to initialize TimescaleDB extension (`CREATE EXTENSION IF NOT EXISTS timescaledb;`), convert `market_ticks` table into a hypertable (`create_hypertable`), and define continuous aggregates for timeframes `ohlc_1s`, `ohlc_1m`, `ohlc_5m`, `ohlc_1h`, `ohlc_1d`.
* **[REQ-02]**: In `src/Services/Execution/Palantir.LocalEngineService/Endpoints/LocalEngineEndpoints.cs`, update `MatchOrderAsync` to publish `TradeExecutedEvent` messages and stream execution ticks to Kafka topic `market.ticks.v1`.
* **[REQ-03]**: Implement query endpoints in `LocalEngineEndpoints.cs` to query materialized OHLC candlestick data from TimescaleDB continuous aggregates with timeframe filtering.
* **[REQ-04]**: Ensure lock-free, thread-safe order book matching in `OrderBook.cs` maintaining strict price-time priority.
