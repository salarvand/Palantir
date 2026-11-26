using Palantir.LocalEngineService.IntegrationEvents.Events;
using Palantir.LocalEngineService.Persistence;
using Palantir.SharedKernel;
using Wolverine;

namespace Palantir.LocalEngineService.Matching;

public interface IOrderExecutionPublisher
{
    Task PublishMatchResultsAsync(
        string symbol,
        TradeSide aggressorSide,
        OrderBook book,
        MatchResult matchResult,
        CancellationToken ct = default);
}

public sealed class OrderExecutionPublisher : IOrderExecutionPublisher
{
    private readonly MarketTickDbContext _db;
    private readonly IMessageBus _messageBus;

    public OrderExecutionPublisher(MarketTickDbContext db, IMessageBus messageBus)
    {
        _db = db;
        _messageBus = messageBus;
    }

    public async Task PublishMatchResultsAsync(
        string symbol,
        TradeSide aggressorSide,
        OrderBook book,
        MatchResult matchResult,
        CancellationToken ct = default)
    {
        if (matchResult.Executions.Count > 0)
        {
            var ticks = matchResult.Executions.Select(e => new MarketTick
            {
                Symbol = e.Symbol,
                Price = e.Price,
                Quantity = e.Quantity,
                TimestampUtc = e.TimestampUtc,
                TradeId = e.TradeId,
                Side = aggressorSide
            }).ToList();

            _db.Ticks.AddRange(ticks);
            await _db.SaveChangesAsync(ct);

            foreach (var execution in matchResult.Executions)
            {
                await _messageBus.PublishAsync(new TradeExecutedEvent(
                    execution.BuyOrderId,
                    execution.TradeId,
                    execution.Quantity,
                    execution.Price));

                await _messageBus.PublishAsync(new TradeExecutedEvent(
                    execution.SellOrderId,
                    execution.TradeId,
                    execution.Quantity,
                    execution.Price));

                await _messageBus.PublishAsync(new MarketTickRecordedEvent(
                    execution.TradeId,
                    execution.Symbol,
                    execution.Price,
                    execution.Quantity,
                    aggressorSide,
                    execution.TimestampUtc));
            }
        }

        var (bids, asks) = book.GetDepth(10);
        await _messageBus.PublishAsync(new OrderBookUpdatedEvent(
            symbol.ToUpperInvariant(),
            bids.Select(b => new OrderBookLevelDto(b.Price, b.Quantity)).ToList(),
            asks.Select(a => new OrderBookLevelDto(a.Price, a.Quantity)).ToList(),
            DateTimeOffset.UtcNow));
    }
}
