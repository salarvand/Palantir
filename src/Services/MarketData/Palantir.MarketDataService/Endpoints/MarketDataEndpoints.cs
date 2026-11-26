using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Palantir.Constants;
using Palantir.Platform.Caching;
using Palantir.Platform.Endpoints;

namespace Palantir.MarketDataService.Endpoints;

public class MarketDataEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/market")
            .WithTags("Market Data");

        group.MapGet("/quote/{symbol}", GetQuoteAsync);
        group.MapGet("/orderbook/{symbol}", GetCachedOrderBookAsync);
    }

    private static async Task<IResult> GetQuoteAsync(string symbol, IKeyValueStore cache, CancellationToken ct)
    {
        var upperSymbol = symbol.ToUpperInvariant();
        var json = await cache.GetAsync(MessagingConstants.CacheKeys.MarketQuote(upperSymbol), ct);
        if (!string.IsNullOrEmpty(json))
        {
            return Results.Content(json, "application/json");
        }

        return Results.Ok(new
        {
            Symbol = upperSymbol,
            Bid = 100.50m,
            Ask = 100.55m,
            Last = 100.52m,
            TimestampUtc = DateTimeOffset.UtcNow
        });
    }

    private static async Task<IResult> GetCachedOrderBookAsync(string symbol, IKeyValueStore cache, CancellationToken ct)
    {
        var upperSymbol = symbol.ToUpperInvariant();
        var json = await cache.GetAsync(MessagingConstants.CacheKeys.OrderBook(upperSymbol), ct);

        if (string.IsNullOrEmpty(json))
        {
            return Results.Ok(new
            {
                Symbol = upperSymbol,
                Bids = new[] { new { Price = 100.50m, Quantity = 1000m } },
                Asks = new[] { new { Price = 100.55m, Quantity = 850m } },
                TimestampUtc = DateTimeOffset.UtcNow
            });
        }

        return Results.Content(json, "application/json");
    }
}
