using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Palantir.Constants;
using Palantir.Platform.Endpoints;
using StackExchange.Redis;

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

    private static IResult GetQuoteAsync(string symbol)
    {
        return Results.Ok(new
        {
            Symbol = symbol.ToUpperInvariant(),
            Bid = 100.50m,
            Ask = 100.55m,
            Last = 100.52m,
            TimestampUtc = DateTimeOffset.UtcNow
        });
    }

    private static async Task<IResult> GetCachedOrderBookAsync(string symbol, IConnectionMultiplexer redis)
    {
        var db = redis.GetDatabase();
        var key = MessagingConstants.RedisKeys.OrderBook(symbol);
        var json = await db.StringGetAsync(key);

        if (json.IsNullOrEmpty)
        {
            return Results.Ok(new
            {
                Symbol = symbol.ToUpperInvariant(),
                Bids = new[] { new { Price = 100.50m, Quantity = 1000m } },
                Asks = new[] { new { Price = 100.55m, Quantity = 850m } },
                TimestampUtc = DateTimeOffset.UtcNow
            });
        }

        return Results.Content((string)json!, "application/json");
    }
}
