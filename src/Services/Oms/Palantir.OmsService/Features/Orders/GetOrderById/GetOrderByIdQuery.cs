using Palantir.Platform.CQRS;

namespace Palantir.OmsService.Features.Orders.GetOrderById;

public record OrderDto(
    Guid OrderId,
    Guid ClientId,
    string Symbol,
    string Side,
    string Type,
    decimal Quantity,
    decimal Price,
    string Status,
    decimal CumulativeFilledQuantity,
    decimal AverageFilledPrice,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc
);

public record GetOrderByIdQuery(Guid OrderId) : IQuery<OrderDto?>;
