using Palantir.Platform.CQRS;

namespace Palantir.OmsService.Features.Orders.CancelOrder;

public record CancelOrderCommand(
    Guid OrderId,
    string Reason
) : ICommand<CancelOrderResponse>;

public record CancelOrderResponse(
    Guid OrderId,
    bool IsCancelled,
    string Message
);
