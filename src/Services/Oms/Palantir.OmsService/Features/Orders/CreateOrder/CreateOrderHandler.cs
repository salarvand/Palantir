using System.Globalization;
using MassTransit;
using Palantir.LocalEngineService.Grpc;
using Palantir.Oms.Contracts;
using Palantir.OmsService.Repositories;
using Palantir.Platform.CQRS;
using Palantir.SharedKernel;

namespace Palantir.OmsService.Features.Orders.CreateOrder;

public class CreateOrderHandler : ICommandHandler<CreateOrderCommand, CreateOrderResponse>
{
    private readonly IOrderRepository _repository;
    private readonly MatchingEngineGrpc.MatchingEngineGrpcClient _matchingClient;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CreateOrderHandler> _logger;

    public CreateOrderHandler(
        IOrderRepository repository,
        MatchingEngineGrpc.MatchingEngineGrpcClient matchingClient,
        IPublishEndpoint publishEndpoint,
        ILogger<CreateOrderHandler> logger)
    {
        _repository = repository;
        _matchingClient = matchingClient;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<CreateOrderResponse> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var orderId = OrderId.From(Guid.NewGuid());
        var clientId = ClientId.From(request.ClientId);
        var symbolId = SymbolId.From(request.Symbol);
        var qty = new Quantity(request.Quantity);
        var price = new Price(request.Price);

        var order = Order.Create(
            orderId,
            clientId,
            symbolId,
            request.Side,
            request.Type,
            qty,
            price,
            request.TimeInForce
        );

        // Pre-trade risk approval (10% standard margin reservation)
        var margin = request.Quantity * (request.Price > 0 ? request.Price : 100m) * 0.1m;
        order.ApproveRisk(margin);

        await _repository.AppendEventsAsync(order, cancellationToken);

        // Submit to Matching Engine over HTTP/2 gRPC
        var grpcRequest = new GrpcSubmitOrderRequest
        {
            OrderId = order.Id.Value.ToString(),
            ClientId = order.ClientId.Value.ToString(),
            Symbol = order.Symbol.Value,
            Side = (int)order.Side,
            Type = (int)order.Type,
            Quantity = order.Quantity.Value.ToString(CultureInfo.InvariantCulture),
            Price = order.Price.Value.ToString(CultureInfo.InvariantCulture),
            TimeInForce = (int)order.TimeInForce
        };

        var executionReport = await _matchingClient.SubmitOrderAsync(grpcRequest, cancellationToken: cancellationToken);

        if (executionReport.Status is "Filled" or "PartiallyFilled")
        {
            if (decimal.TryParse(executionReport.FilledQuantity, NumberStyles.Any, CultureInfo.InvariantCulture, out var filledQty) &&
                decimal.TryParse(executionReport.AveragePrice, NumberStyles.Any, CultureInfo.InvariantCulture, out var avgPrice) &&
                Guid.TryParse(executionReport.TradeId, out var tradeId))
            {
                order.ApplyExecution(TradeId.From(tradeId), new Quantity(filledQty), new Price(avgPrice));
                await _repository.AppendEventsAsync(order, cancellationToken);
            }
        }

        // Dispatch settlement saga command through message bus
        await _publishEndpoint.Publish(new InitiateOrderSettlementCommand(
            order.Id.Value,
            order.ClientId.Value,
            order.Symbol.Value,
            order.Quantity.Value,
            order.Price.Value
        ), cancellationToken);

        _logger.LogInformation("Order {OrderId} successfully placed and routed to settlement saga.", order.Id.Value);

        return new CreateOrderResponse(
            order.Id.Value,
            order.Status.ToString(),
            order.CumulativeFilledQuantity.Value,
            order.AverageFilledPrice.Value,
            executionReport.Message
        );
    }
}
