using System.Collections.Concurrent;
using System.Globalization;
using Grpc.Core;
using MassTransit;
using Palantir.Constants;
using Palantir.LocalEngineService.Matching;
using Palantir.LocalEngineService.Timescale;
using Palantir.SharedKernel;

namespace Palantir.LocalEngineService.Grpc;

public class MatchingEngineGrpcService : MatchingEngineGrpc.MatchingEngineGrpcBase
{
    private static readonly ConcurrentDictionary<string, OrderBook> OrderBooks = new(StringComparer.OrdinalIgnoreCase);
    private readonly ITopicProducer<string, MarketTick> _tickProducer;
    private readonly ILogger<MatchingEngineGrpcService> _logger;

    public MatchingEngineGrpcService(
        ITopicProducer<string, MarketTick> tickProducer,
        ILogger<MatchingEngineGrpcService> logger)
    {
        _tickProducer = tickProducer;
        _logger = logger;
    }

    public static OrderBook GetOrCreateBook(string symbol) =>
        OrderBooks.GetOrAdd(symbol.ToUpperInvariant(), s => new OrderBook(s));

    public override async Task<GrpcExecutionReport> SubmitOrder(GrpcSubmitOrderRequest request, ServerCallContext context)
    {
        _logger.LogInformation("gRPC SubmitOrder received for Order {OrderId}, Symbol {Symbol}", request.OrderId, request.Symbol);

        if (!Guid.TryParse(request.OrderId, out var orderId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid order_id"));
        if (!Guid.TryParse(request.ClientId, out var clientId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid client_id"));

        if (!decimal.TryParse(request.Quantity, NumberStyles.Any, CultureInfo.InvariantCulture, out var quantity) || quantity <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid quantity"));

        decimal price = 0;
        if (!string.IsNullOrEmpty(request.Price) &&
            !decimal.TryParse(request.Price, NumberStyles.Any, CultureInfo.InvariantCulture, out price))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid price format"));
        }

        var side = (TradeSide)request.Side;
        var type = (OrderType)request.Type;

        var book = GetOrCreateBook(request.Symbol);
        var entry = new OrderBookEntry(
            orderId,
            clientId,
            request.Symbol,
            side,
            type,
            price,
            quantity,
            DateTimeOffset.UtcNow
        );

        var matchResult = book.Match(entry);

        var tradeId = Guid.Empty;
        decimal averagePrice = price;
        if (matchResult.Executions.Count > 0)
        {
            tradeId = matchResult.Executions[^1].TradeId;
            var totalVal = matchResult.Executions.Sum(e => e.Quantity * e.Price);
            averagePrice = totalVal / matchResult.FilledQuantity;

            // Stream market ticks to Kafka
            foreach (var execution in matchResult.Executions)
            {
                var tick = new MarketTick
                {
                    TradeId = execution.TradeId,
                    TimestampUtc = execution.TimestampUtc,
                    Symbol = request.Symbol,
                    Price = execution.Price,
                    Quantity = execution.Quantity,
                    Side = side
                };
                await _tickProducer.Produce(request.Symbol, tick, context.CancellationToken);
            }
        }

        string status = matchResult.IsFullyFilled
            ? "Filled"
            : matchResult.FilledQuantity > 0 ? "PartiallyFilled" : "Placed";

        return new GrpcExecutionReport
        {
            OrderId = request.OrderId,
            Status = status,
            FilledQuantity = matchResult.FilledQuantity.ToString("F8", CultureInfo.InvariantCulture),
            AveragePrice = averagePrice.ToString("F8", CultureInfo.InvariantCulture),
            TradeId = tradeId != Guid.Empty ? tradeId.ToString() : string.Empty,
            Message = $"Order processed by in-memory engine. Status: {status}.",
            TimestampUtcTicks = DateTimeOffset.UtcNow.Ticks
        };
    }

    public override Task<GrpcCancelReport> CancelOrder(GrpcCancelOrderRequest request, ServerCallContext context)
    {
        _logger.LogInformation("gRPC CancelOrder received for Order {OrderId}, Symbol {Symbol}", request.OrderId, request.Symbol);

        if (!Guid.TryParse(request.OrderId, out var orderId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid order_id"));

        var book = GetOrCreateBook(request.Symbol);
        var cancelled = book.Cancel(orderId);

        return Task.FromResult(new GrpcCancelReport
        {
            OrderId = request.OrderId,
            IsCancelled = cancelled,
            Message = cancelled ? "Order cancelled from book." : "Order not found in resting book."
        });
    }

    public override Task<GrpcOrderBookSnapshot> GetOrderBook(GrpcGetOrderBookRequest request, ServerCallContext context)
    {
        var book = GetOrCreateBook(request.Symbol);
        var depth = request.Depth > 0 ? request.Depth : 10;
        var (bids, asks) = book.GetDepth(depth);

        var snapshot = new GrpcOrderBookSnapshot
        {
            Symbol = request.Symbol,
            TimestampUtcTicks = DateTimeOffset.UtcNow.Ticks
        };

        foreach (var (p, q) in bids)
        {
            snapshot.Bids.Add(new GrpcPriceLevel
            {
                Price = p.ToString("F8", CultureInfo.InvariantCulture),
                Quantity = q.ToString("F8", CultureInfo.InvariantCulture),
                OrderCount = 1
            });
        }

        foreach (var (p, q) in asks)
        {
            snapshot.Asks.Add(new GrpcPriceLevel
            {
                Price = p.ToString("F8", CultureInfo.InvariantCulture),
                Quantity = q.ToString("F8", CultureInfo.InvariantCulture),
                OrderCount = 1
            });
        }

        return Task.FromResult(snapshot);
    }
}
