# Specification: Phase 3 - Microsoft Orleans OMS Persistence & Execution Routing

## Requirements Matrix
* **[REQ-01]**: Configure Orleans ADO.NET PostgreSQL grain persistence and clustering in `src/Services/Oms/Palantir.OmsService/Program.cs` utilizing `Orleans.Clustering.AdoNet` and `Orleans.Persistence.AdoNet`.
* **[REQ-02]**: Refactor `OrderGrain` in `src/Services/Oms/Palantir.OmsService/Grains/Grains.cs` to inject `[PersistentState("orders", "orders")] IPersistentState<OrderGrainState>` for transactional, persistent state transitions.
* **[REQ-03]**: Implement execution venue routing in `OrderGrain.SubmitOrderAsync` to dispatch orders via typed HTTP clients to `Palantir.LocalEngineService` (for `VenueType == "Local"`) or `Palantir.ExternalGatewayService` (for `VenueType == "External"`).
* **[REQ-04]**: Implement `IOrderExpiryGrain` with Orleans Reminders (`IRemindable`) to track and automatically transition expired Good-Til-Time (GTT) orders to `OrderStatus.Expired`.
* **[REQ-05]**: Register typed resilient HttpClient services in `Palantir.OmsService/Program.cs` for engine dispatch communication.
