namespace Palantir.SharedKernel;

public record OrderCreatedDomainEvent(
    OrderId OrderId,
    ClientId ClientId,
    SymbolId Symbol,
    TradeSide Side,
    OrderType Type,
    Quantity Quantity,
    Price Price,
    TimeInForce TimeInForce,
    DateTimeOffset TimestampUtc
) : DomainEvent(Guid.NewGuid(), TimestampUtc, OrderId.Value.ToString("N"));

public record OrderRiskApprovedDomainEvent(
    OrderId OrderId,
    decimal ReservedMargin,
    DateTimeOffset TimestampUtc
) : DomainEvent(Guid.NewGuid(), TimestampUtc, OrderId.Value.ToString("N"));

public record OrderExecutionAppliedDomainEvent(
    OrderId OrderId,
    TradeId TradeId,
    Quantity FillQuantity,
    Price FillPrice,
    Quantity RemainingQuantity,
    OrderStatus Status,
    DateTimeOffset TimestampUtc
) : DomainEvent(Guid.NewGuid(), TimestampUtc, OrderId.Value.ToString("N"));

public record OrderCancelledDomainEvent(
    OrderId OrderId,
    string Reason,
    DateTimeOffset TimestampUtc
) : DomainEvent(Guid.NewGuid(), TimestampUtc, OrderId.Value.ToString("N"));

public record OrderRejectedDomainEvent(
    OrderId OrderId,
    string Reason,
    DateTimeOffset TimestampUtc
) : DomainEvent(Guid.NewGuid(), TimestampUtc, OrderId.Value.ToString("N"));

public record OrderExpiredDomainEvent(
    OrderId OrderId,
    DateTimeOffset TimestampUtc
) : DomainEvent(Guid.NewGuid(), TimestampUtc, OrderId.Value.ToString("N"));
