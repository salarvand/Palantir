using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Palantir.Constants;
using Palantir.MarketDataService.Hubs;
using Palantir.MarketDataService.IntegrationEvents.Events;
using Palantir.Platform.Caching;

namespace Palantir.MarketDataService.Handlers;

public sealed class MarketDataTickHandler
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(15);
    private readonly IKeyValueStore _cache;
    private readonly IHubContext<MarketDataHub> _hubContext;

    public MarketDataTickHandler(IKeyValueStore cache, IHubContext<MarketDataHub> hubContext)
    {
        _cache = cache;
        _hubContext = hubContext;
    }

    public async Task Handle(MarketTickRecordedEvent tick, CancellationToken ct)
    {
        var symbol = tick.Symbol.ToUpperInvariant();
        var quotePayload = JsonSerializer.Serialize(new
        {
            Symbol = symbol,
            Bid = tick.Price,
            Ask = tick.Price,
            Last = tick.Price,
            tick.Quantity,
            tick.TimestampUtc
        });

        await _cache.SetAsync(MessagingConstants.CacheKeys.MarketQuote(symbol), quotePayload, CacheTtl, ct);
        await _hubContext.Clients.Group(symbol).SendAsync("MarketTick", tick, ct);
    }

    public async Task Handle(OrderBookUpdatedEvent snapshot, CancellationToken ct)
    {
        var symbol = snapshot.Symbol.ToUpperInvariant();
        var payload = JsonSerializer.Serialize(new
        {
            Symbol = symbol,
            snapshot.TimestampUtc,
            snapshot.Bids,
            snapshot.Asks
        });

        await _cache.SetAsync(MessagingConstants.CacheKeys.OrderBook(symbol), payload, CacheTtl, ct);
        await _hubContext.Clients.Group(symbol).SendAsync("OrderBookUpdated", snapshot, ct);
    }
}
