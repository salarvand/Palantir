using Palantir.SharedKernel;
using Wolverine.Attributes;

namespace Palantir.LocalEngineService.IntegrationEvents.Events;

public sealed record OrderBookLevelDto(decimal Price, decimal Quantity);

[MessageIdentity("palantir.trade-executed")]
public sealed record TradeExecutedEvent(
    Guid OrderId,
    Guid TradeId,
    decimal FilledQuantity,
    decimal ExecutionPrice);

[MessageIdentity("palantir.market-tick-recorded")]
public sealed record MarketTickRecordedEvent(
    Guid TradeId,
    string Symbol,
    decimal Price,
    decimal Quantity,
    TradeSide Side,
    DateTimeOffset TimestampUtc);

[MessageIdentity("palantir.order-book-updated")]
public sealed record OrderBookUpdatedEvent(
    string Symbol,
    IReadOnlyList<OrderBookLevelDto> Bids,
    IReadOnlyList<OrderBookLevelDto> Asks,
    DateTimeOffset TimestampUtc);
