using Palantir.Platform.CQRS;
using Palantir.SharedKernel;

namespace Palantir.OmsService.Features.Orders.CreateOrder;

[Transactional]
public record CreateOrderCommand(
    Guid ClientId,
    string Symbol,
    TradeSide Side,
    OrderType Type,
    decimal Quantity,
    decimal Price,
    TimeInForce TimeInForce
) : ICommand<CreateOrderResponse>;

public record CreateOrderResponse(
    Guid OrderId,
    string Status,
    decimal FilledQuantity,
    decimal AveragePrice,
    string Message
);
