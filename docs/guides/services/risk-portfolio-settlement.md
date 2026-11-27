# Risk, Portfolio & Settlement Services

## 1. `Palantir.RiskService` — Pre-Trade Risk

- **Startup** ([Program.cs](../../../src/Services/Risk/Palantir.RiskService/Program.cs)): Registers singleton `RiskEngine`, listens to `trading.risk.checks`, and publishes `PreTradeRiskApprovedEvent` (fanout exchange `trading.sagas` -> `trading.settlement.saga` + `trading.portfolio.margin`), `OrderSettlementFailedEvent`, and `AuditTrailEvent`.
- **Risk Engine** ([RiskEngine.cs](../../../src/Services/Risk/Palantir.RiskService/Engine/RiskEngine.cs)): Enforces `MaxSingleOrderValue` (`1,000,000`) and `10%` initial margin requirement against `AvailableMargin`.
- **Vertical Slices & Handlers**:
  - `POST /api/v1/risk/validate` ([ValidateRiskFeature.cs](../../../src/Services/Risk/Palantir.RiskService/Features/Risk/Validate/ValidateRiskFeature.cs)): Validates requests using `SpecificationValidator<RiskEvaluationRequest>` (`RiskSymbolSpecification`, `RiskQuantitySpecification`, `RiskPriceSpecification`).
  - `GET /api/v1/risk/limits/{clientId}` ([GetClientLimitsFeature.cs](../../../src/Services/Risk/Palantir.RiskService/Features/Limits/Get/GetClientLimitsFeature.cs)): Exposes client risk limits.
  - [CheckPreTradeRiskHandler.cs](../../../src/Services/Risk/Palantir.RiskService/Handlers/CheckPreTradeRiskHandler.cs): Evaluates asynchronous `CheckPreTradeRiskCommand` from `OrderSettlementSaga` and emits approval/failure + `AuditTrailEvent`.

---

## 2. `Palantir.PortfolioService` — Accounts & Margin

- **Startup** ([Program.cs](../../../src/Services/Portfolio/Palantir.PortfolioService/Program.cs)): Uses `PortfolioDbContext` (`portfolio` schema), seeds demo account `11111111-1111-1111-1111-111111111111` in Development, and listens to `trading.portfolio.margin`.
- **Vertical Slices & Handlers**:
  - `GET /api/v1/portfolio/{clientId}` ([GetPortfolioFeature.cs](../../../src/Services/Portfolio/Palantir.PortfolioService/Features/Accounts/Get/GetPortfolioFeature.cs))
  - `POST /api/v1/portfolio/margin/reserve` ([ReserveMarginFeature.cs](../../../src/Services/Portfolio/Palantir.PortfolioService/Features/Margin/Reserve/ReserveMarginFeature.cs))
  - `POST /api/v1/portfolio/margin/release` ([ReleaseMarginFeature.cs](../../../src/Services/Portfolio/Palantir.PortfolioService/Features/Margin/Release/ReleaseMarginFeature.cs))
  - [PortfolioSettlementHandler.cs](../../../src/Services/Portfolio/Palantir.PortfolioService/Handlers/PortfolioSettlementHandler.cs): Handles `ReleaseReservedMarginCommand` (margin compensation on failed settlement), `ConfirmSettlementEvent`, and `PreTradeRiskApprovedEvent`.

---

## 3. `Palantir.SettlementService` — Distributed Saga

- **Startup** ([Program.cs](../../../src/Services/Settlement/Palantir.SettlementService/Program.cs)): Uses `SettlementDbContext` (`settlement` schema), listens to `trading.settlement.saga`, and routes outgoing saga commands/events to `trading.risk.checks`, `trading.portfolio.margin`, and `audit.trail.v1`.
- **Saga State Machine** ([OrderSettlementSaga.cs](../../../src/Services/Settlement/Palantir.SettlementService/Sagas/OrderSettlementSaga.cs)):
  1. `Start(InitiateOrderSettlementCommand)` -> sets state `"Submitted"` and emits `CheckPreTradeRiskCommand`.
  2. `Handle(PreTradeRiskApprovedEvent)` -> stores `ReservedMargin` and sets state `"RiskApproved"`.
  3. `Handle(TradeExecutedEvent)` -> sets state `"Settled"`, calls `MarkCompleted()`, and emits `ConfirmSettlementEvent` + `"SettlementCompleted"` `AuditTrailEvent`.
  4. `Handle(OrderSettlementFailedEvent)` -> sets state `"Failed"`, calls `MarkCompleted()`, and emits compensating `ReleaseReservedMarginCommand` + `"SettlementFailed"` `AuditTrailEvent`.
- **Vertical Slice**: `GET /api/v1/settlements/{orderId}` ([GetSettlementFeature.cs](../../../src/Services/Settlement/Palantir.SettlementService/Features/Settlements/Get/GetSettlementFeature.cs)).
