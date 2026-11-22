---
insight_schema: "1.0.0"
feature_id: "phase-3-orleans-oms"
standards_version: "1.0.0"
source: "docs/review.md"
spec_hash: "80978193a86defa65b12ef3501ae5e6e20ca7085f2205b406ac94b174656cb04"
status: "approved"
---

# Feature Implementation Plan: Phase 3 - Microsoft Orleans OMS Persistence & Execution Routing

## 1. Requirements Matrix
* **[REQ-01]**: Configure Orleans ADO.NET PostgreSQL grain persistence and clustering in `src/Services/Oms/Palantir.OmsService/Program.cs` utilizing `Orleans.Clustering.AdoNet` and `Orleans.Persistence.AdoNet`.
* **[REQ-02]**: Refactor `OrderGrain` in `src/Services/Oms/Palantir.OmsService/Grains/Grains.cs` to inject `[PersistentState("orders", "orders")] IPersistentState<OrderGrainState>` for transactional, persistent state transitions.
* **[REQ-03]**: Implement execution venue routing in `OrderGrain.SubmitOrderAsync` to dispatch orders via typed HTTP clients to `Palantir.LocalEngineService` (for `VenueType == "Local"`) or `Palantir.ExternalGatewayService` (for `VenueType == "External"`).
* **[REQ-04]**: Implement `IOrderExpiryGrain` with Orleans Reminders (`IRemindable`) to track and automatically transition expired Good-Til-Time (GTT) orders to `OrderStatus.Expired`.
* **[REQ-05]**: Register typed resilient HttpClient services in `Palantir.OmsService/Program.cs` for engine dispatch communication.

## 2. In-Scope Modules & File Targets
* `src/Services/Oms/Palantir.Oms.Contracts/Contracts.cs` (Modify: Add IOrderExpiryGrain interface and DTOs)
* `src/Services/Oms/Palantir.OmsService/Program.cs` (Modify: Configure ADO.NET grain storage, clustering, reminders, and typed HttpClients)
* `src/Services/Oms/Palantir.OmsService/Grains/Grains.cs` (Modify: Inject IPersistentState, implement execution dispatch, and implement IOrderExpiryGrain)
* `src/Services/Oms/Palantir.OmsService/Palantir.OmsService.csproj` (Modify: Ensure Orleans.Clustering.AdoNet, Orleans.Persistence.AdoNet, Orleans.Reminders.AdoNet packages are referenced)

## 3. Explicit Out-of-Scope (Scope Guard)
* TimescaleDB continuous aggregate schema generation (handled in Phase 4).
* Settlement saga state machine compensation logic (isolated in SettlementService).
* Frontend Blotter UI state management.

## 4. Architectural Invariants
* **[ARCH-01]**: Grain state transitions must be serialized by Orleans virtual actor concurrency guarantee; direct concurrent DB writes to grain tables outside Orleans are prohibited.
* **[ARCH-02]**: Execution dispatch must be fault-tolerant with Polly retry and circuit breaker policies applied to external venue invocations.
* **[ARCH-03]**: Order status transitions must be monotonic and validated against the trading state transition matrix.

## 5. Verification & Test Plan
* **Unit Tests**: `tests/Services/Palantir.OmsService.UnitTests/OrderStateTests.cs` verifying snapshot generation and state validation.
* **Orleans Lifecycle Verification**: Verify grain activation, persistent state read/write semantics, and reminder execution.
