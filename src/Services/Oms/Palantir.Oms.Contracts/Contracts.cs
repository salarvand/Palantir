using Palantir.SharedKernel;
using Wolverine.Persistence.Sagas;

namespace Palantir.Oms.Contracts;

public interface IOrderGrain : IGrainWithGuidKey
{
    Task<OrderStateSnapshot> SubmitOrderAsync(SubmitOrderRequest request);
    Task<OrderStateSnapshot> CancelOrderAsync(string reason);
    Task<OrderStateSnapshot> ExpireOrderAsync();
    Task<OrderStateSnapshot> ApplyExecutionReportAsync(decimal fillQuantity, decimal fillPrice, string tradeId);
    Task<OrderStateSnapshot> GetSnapshotAsync();
}

public interface IOrderExpiryGrain : IGrainWithGuidKey
{
    Task TrackOrderExpiryAsync(Guid orderId, TimeSpan duration);
    Task CancelOrderExpiryAsync();
}

public interface IClientSessionGrain : IGrainWithStringKey
{
    Task RegisterOrderAsync(Guid orderId);
    Task UnregisterOrderAsync(Guid orderId);
    Task<IReadOnlyList<Guid>> GetActiveOrdersAsync();
}

[GenerateSerializer]
public record SubmitOrderRequest(
    Guid OrderId,
    Guid ClientId,
    string Symbol,
    TradeSide Side,
    OrderType Type,
    decimal Quantity,
    decimal Price,
    TimeInForce TimeInForce,
    string VenueType,
    TimeSpan? ExpiryDuration = null
);

[GenerateSerializer]
public record OrderStateSnapshot(
    Guid OrderId,
    Guid ClientId,
    string Symbol,
    TradeSide Side,
    OrderType Type,
    decimal Quantity,
    decimal Price,
    decimal CumulativeFilledQuantity,
    decimal AverageFilledPrice,
    OrderStatus Status,
    TimeInForce TimeInForce,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastUpdatedAtUtc,
    string? RejectionReason
);

[GenerateSerializer]
public record TradeExecutedEvent(
    [property: SagaIdentity] Guid OrderId,
    Guid TradeId,
    decimal FilledQuantity,
    decimal ExecutionPrice
);

public record OrderBookLevelDto(decimal Price, decimal Quantity);

public record MarketTickRecordedEvent(
    Guid TradeId,
    string Symbol,
    decimal Price,
    decimal Quantity,
    TradeSide Side,
    DateTimeOffset TimestampUtc
);

public record OrderBookUpdatedEvent(
    string Symbol,
    IReadOnlyList<OrderBookLevelDto> Bids,
    IReadOnlyList<OrderBookLevelDto> Asks,
    DateTimeOffset TimestampUtc
);

public record InitiateOrderSettlementCommand(
    [property: SagaIdentity] Guid OrderId,
    Guid ClientId,
    string Symbol,
    decimal Quantity,
    decimal Price
);

public record PreTradeRiskApprovedEvent(
    [property: SagaIdentity] Guid OrderId,
    decimal RequiredMargin
);

public record ReleaseReservedMarginCommand(
    Guid OrderId,
    Guid ClientId,
    decimal Amount,
    string Reason
);

public record CheckPreTradeRiskCommand(
    Guid OrderId,
    Guid ClientId,
    string Symbol,
    decimal Quantity,
    decimal Price
);

public record ConfirmSettlementEvent(
    Guid OrderId,
    DateTimeOffset SettledAtUtc
);

public record OrderSettlementFailedEvent(
    [property: SagaIdentity] Guid OrderId,
    string Reason
);

public record AuditTrailEvent(
    string EventType,
    Guid OrderId,
    Guid? TradeId,
    Guid ClientId,
    string Symbol,
    decimal Price,
    decimal Quantity,
    string PayloadJson,
    DateTimeOffset TimestampUtc
);
