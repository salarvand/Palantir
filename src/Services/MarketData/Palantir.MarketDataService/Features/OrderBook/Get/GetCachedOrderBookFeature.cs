using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Palantir.Constants;
using Palantir.Platform.Endpoints;
using ZiggyCreatures.Caching.Fusion;

namespace Palantir.MarketDataService.Features.OrderBook.Get;

public sealed record OrderBookLevelSnapshot(decimal Price, decimal Quantity);

public sealed record CachedOrderBookSnapshot(
    string Symbol,
    IReadOnlyList<OrderBookLevelSnapshot> Bids,
    IReadOnlyList<OrderBookLevelSnapshot> Asks,
    DateTimeOffset TimestampUtc);

internal sealed class GetCachedOrderBookEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/market/orderbook/{symbol}", async (string symbol, IFusionCache cache, CancellationToken ct) =>
            {
                var upperSymbol = symbol.Trim().ToUpperInvariant();
                var cacheKey = MessagingConstants.CacheKeys.OrderBook(upperSymbol);

                var book = await cache.GetOrSetAsync(
                    cacheKey,
                    _ => Task.FromResult(new CachedOrderBookSnapshot(
                        upperSymbol,
                        [new OrderBookLevelSnapshot(100.50m, 1000m)],
                        [new OrderBookLevelSnapshot(100.55m, 850m)],
                        DateTimeOffset.UtcNow)),
                    TimeSpan.FromSeconds(5),
                    ct);

                return TypedResults.Ok(book);
            })
            .ProducesGet<CachedOrderBookSnapshot>()
            .WithTags("Market Data")
            .WithName(nameof(GetCachedOrderBookEndpoint))
            .MapToApiVersion(ApiVersioningExtensions.V1);
    }
}
