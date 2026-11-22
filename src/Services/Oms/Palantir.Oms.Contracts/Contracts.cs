using Palantir.SharedKernel;

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
    Guid OrderId,
    Guid TradeId,
    decimal FilledQuantity,
    decimal ExecutionPrice
);
