using Palantir.SharedKernel;

namespace Palantir.LocalEngineService.Matching;

public record OrderBookEntry(
    Guid OrderId,
    Guid ClientId,
    string Symbol,
    TradeSide Side,
    OrderType Type,
    decimal Price,
    decimal Quantity,
    DateTimeOffset TimestampUtc
)
{
    public decimal RemainingQuantity { get; set; } = Quantity;
}

public record TradeExecution(
    Guid TradeId,
    Guid BuyOrderId,
    Guid SellOrderId,
    string Symbol,
    decimal Price,
    decimal Quantity,
    DateTimeOffset TimestampUtc
);

public record MatchResult(
    List<TradeExecution> Executions,
    decimal FilledQuantity,
    bool IsFullyFilled
);

public class OrderBook
{
    private readonly object _lock = new();
    private readonly SortedDictionary<decimal, Queue<OrderBookEntry>> _bids = new(Comparer<decimal>.Create((a, b) => b.CompareTo(a))); // Descending
    private readonly SortedDictionary<decimal, Queue<OrderBookEntry>> _asks = new(); // Ascending

    public string Symbol { get; }

    public OrderBook(string symbol)
    {
        Symbol = symbol.ToUpperInvariant();
    }

    public MatchResult Match(OrderBookEntry incoming)
    {
        lock (_lock)
        {
            var executions = new List<TradeExecution>();
            var oppositeBook = incoming.Side == TradeSide.Buy ? _asks : _bids;

            while (incoming.RemainingQuantity > 0 && oppositeBook.Count > 0)
            {
                var bestPriceLevel = oppositeBook.Keys.First();

                // Check price crossing condition
                if (incoming.Side == TradeSide.Buy && incoming.Type == OrderType.Limit && incoming.Price < bestPriceLevel)
                    break;
                if (incoming.Side == TradeSide.Sell && incoming.Type == OrderType.Limit && incoming.Price > bestPriceLevel)
                    break;

                var levelQueue = oppositeBook[bestPriceLevel];

                while (levelQueue.Count > 0 && incoming.RemainingQuantity > 0)
                {
                    var resting = levelQueue.Peek();
                    var matchQty = Math.Min(incoming.RemainingQuantity, resting.RemainingQuantity);
                    var matchPrice = resting.Price;

                    executions.Add(new TradeExecution(
                        Guid.NewGuid(),
                        incoming.Side == TradeSide.Buy ? incoming.OrderId : resting.OrderId,
                        incoming.Side == TradeSide.Sell ? incoming.OrderId : resting.OrderId,
                        Symbol,
                        matchPrice,
                        matchQty,
                        DateTimeOffset.UtcNow
                    ));

                    incoming.RemainingQuantity -= matchQty;
                    resting.RemainingQuantity -= matchQty;

                    if (resting.RemainingQuantity <= 0)
                    {
                        levelQueue.Dequeue();
                    }
                }

                if (levelQueue.Count == 0)
                {
                    oppositeBook.Remove(bestPriceLevel);
                }
            }

            var isFullyFilled = incoming.RemainingQuantity <= 0;

            // If not filled and limit order, place remaining in order book
            if (!isFullyFilled && incoming.Type == OrderType.Limit)
            {
                var targetBook = incoming.Side == TradeSide.Buy ? _bids : _asks;
                if (!targetBook.TryGetValue(incoming.Price, out var queue))
                {
                    queue = new Queue<OrderBookEntry>();
                    targetBook[incoming.Price] = queue;
                }
                queue.Enqueue(incoming);
            }

            return new MatchResult(executions, incoming.Quantity - incoming.RemainingQuantity, isFullyFilled);
        }
    }

    public (IReadOnlyList<(decimal Price, decimal Quantity)> Bids, IReadOnlyList<(decimal Price, decimal Quantity)> Asks) GetDepth(int depth = 10)
    {
        lock (_lock)
        {
            var bids = _bids.Take(depth).Select(kv => (kv.Key, kv.Value.Sum(e => e.RemainingQuantity))).ToList();
            var asks = _asks.Take(depth).Select(kv => (kv.Key, kv.Value.Sum(e => e.RemainingQuantity))).ToList();
            return (bids, asks);
        }
    }
}
