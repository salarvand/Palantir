using Palantir.LocalEngineService.Grpc;
using Palantir.OmsService.Repositories;
using Palantir.Platform.CQRS;
using Palantir.SharedKernel;

namespace Palantir.OmsService.Features.Orders.CancelOrder;

public class CancelOrderHandler : ICommandHandler<CancelOrderCommand, CancelOrderResponse>
{
    private readonly IOrderRepository _repository;
    private readonly MatchingEngineGrpc.MatchingEngineGrpcClient _matchingClient;
    private readonly ILogger<CancelOrderHandler> _logger;

    public CancelOrderHandler(
        IOrderRepository repository,
        MatchingEngineGrpc.MatchingEngineGrpcClient matchingClient,
        ILogger<CancelOrderHandler> logger)
    {
        _repository = repository;
        _matchingClient = matchingClient;
        _logger = logger;
    }

    public async Task<CancelOrderResponse> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(OrderId.From(request.OrderId), cancellationToken);
        if (order == null)
        {
            return new CancelOrderResponse(request.OrderId, false, $"Order {request.OrderId} not found.");
        }

        if (order.IsTerminal)
        {
            return new CancelOrderResponse(request.OrderId, false, $"Order is already in terminal state '{order.Status}'.");
        }

        var grpcRequest = new GrpcCancelOrderRequest
        {
            OrderId = request.OrderId.ToString(),
            Symbol = order.Symbol.Value,
            Reason = request.Reason
        };

        var cancelReport = await _matchingClient.CancelOrderAsync(grpcRequest, cancellationToken: cancellationToken);

        order.Cancel(request.Reason);
        await _repository.AppendEventsAsync(order, cancellationToken);

        _logger.LogInformation("Order {OrderId} cancelled successfully. Engine report: {Message}", request.OrderId, cancelReport.Message);

        return new CancelOrderResponse(request.OrderId, cancelReport.IsCancelled, cancelReport.Message);
    }
}
