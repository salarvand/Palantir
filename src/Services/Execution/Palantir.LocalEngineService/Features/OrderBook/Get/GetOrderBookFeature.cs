using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Palantir.LocalEngineService.Matching;
using Palantir.Platform.Endpoints;

namespace Palantir.LocalEngineService.Features.OrderBook.Get;

internal sealed class GetOrderBookEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/engine/orderbook/{symbol}", (string symbol, IOrderBookRegistry bookRegistry, int depth = 10) =>
            {
                if (!bookRegistry.TryGet(symbol, out var book) || book is null)
                {
                    return Results.NotFound($"Symbol '{symbol}' not found in active order books.");
                }

                var (bids, asks) = book.GetDepth(depth);
                return TypedResults.Ok(new
                {
                    Symbol = symbol.ToUpperInvariant(),
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Bids = bids.Select(b => new { b.Price, b.Quantity }),
                    Asks = asks.Select(a => new { a.Price, a.Quantity })
                });
            })
            .ProducesGet<object>(hasNotFound: true)
            .WithTags("Local Matching Engine")
            .WithName(nameof(GetOrderBookEndpoint))
            .MapToApiVersion(ApiVersioningExtensions.V1);
    }
}
