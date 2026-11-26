using System.Collections.Concurrent;

namespace Palantir.LocalEngineService.Matching;

public interface IOrderBookRegistry
{
    OrderBook GetOrCreate(string symbol);
    bool TryGet(string symbol, out OrderBook? book);
}

public sealed class InMemoryOrderBookRegistry : IOrderBookRegistry
{
    private readonly ConcurrentDictionary<string, OrderBook> _books = new(StringComparer.OrdinalIgnoreCase);

    public OrderBook GetOrCreate(string symbol) =>
        _books.GetOrAdd(symbol.ToUpperInvariant(), s => new OrderBook(s));

    public bool TryGet(string symbol, out OrderBook? book) =>
        _books.TryGetValue(symbol.ToUpperInvariant(), out book);
}
