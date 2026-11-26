using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Palantir.LocalEngineService.Domain;
using Palantir.LocalEngineService.Persistence;
using Palantir.Platform.Endpoints;

namespace Palantir.LocalEngineService.Features.Symbols.List;

internal sealed class ListSymbolsEndpoint : IEndpoint<IResult, MarketTickDbContext>
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/engine/symbols", async (MarketTickDbContext db, CancellationToken ct) =>
                await HandleAsync(db, ct))
            .ProducesGet<List<TradingPair>>()
            .WithTags("Local Matching Engine")
            .WithName(nameof(ListSymbolsEndpoint))
            .MapToApiVersion(ApiVersioningExtensions.V1);
    }

    public async Task<IResult> HandleAsync(MarketTickDbContext db, CancellationToken cancellationToken = default)
    {
        var pairs = await db.Pairs.AsNoTracking().ToListAsync(cancellationToken);
        return TypedResults.Ok(pairs);
    }
}
