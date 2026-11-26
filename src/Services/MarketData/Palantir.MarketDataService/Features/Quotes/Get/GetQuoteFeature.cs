using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Palantir.Constants;
using Palantir.Platform.Endpoints;
using ZiggyCreatures.Caching.Fusion;

namespace Palantir.MarketDataService.Features.Quotes.Get;

public sealed record MarketQuoteDto(
    string Symbol,
    decimal Bid,
    decimal Ask,
    decimal Last,
    DateTimeOffset TimestampUtc);

internal sealed class GetQuoteEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/market/quote/{symbol}", async (string symbol, IFusionCache cache, CancellationToken ct) =>
            {
                var upperSymbol = symbol.Trim().ToUpperInvariant();
                var cacheKey = MessagingConstants.CacheKeys.MarketQuote(upperSymbol);

                var quote = await cache.GetOrSetAsync(
                    cacheKey,
                    _ => Task.FromResult(new MarketQuoteDto(upperSymbol, 100.50m, 100.55m, 100.52m, DateTimeOffset.UtcNow)),
                    TimeSpan.FromSeconds(15),
                    ct);

                return TypedResults.Ok(quote);
            })
            .ProducesGet<MarketQuoteDto>()
            .WithTags("Market Data")
            .WithName(nameof(GetQuoteEndpoint))
            .MapToApiVersion(ApiVersioningExtensions.V1);
    }
}
