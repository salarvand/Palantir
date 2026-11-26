using Palantir.SharedKernel;

namespace Palantir.ExternalGatewayService.Features.ExternalOrders.Route;

public sealed record ExternalOrderRequest(
    Guid OrderId,
    Guid ClientId,
    string Symbol,
    TradeSide Side,
    OrderType Type,
    decimal Quantity,
    decimal Price,
    TimeInForce TimeInForce,
    string VenueType);
