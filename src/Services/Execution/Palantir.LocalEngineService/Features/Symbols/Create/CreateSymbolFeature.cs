using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Palantir.Constants;
using Palantir.LocalEngineService.Domain;
using Palantir.LocalEngineService.Matching;
using Palantir.LocalEngineService.Persistence;
using Palantir.Platform.Endpoints;
using Palantir.Platform.Validation;

namespace Palantir.LocalEngineService.Features.Symbols.Create;

public sealed class CreateSymbolValidator : AbstractValidator<TradingPair>
{
    public CreateSymbolValidator()
    {
        RuleFor(x => x.Symbol).NotEmpty().MaximumLength(20);
    }
}

internal sealed class CreateSymbolEndpoint : IEndpoint<IResult, TradingPair, MarketTickDbContext, IOrderBookRegistry>
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/engine/symbols", async (
                TradingPair pair,
                MarketTickDbContext db,
                IOrderBookRegistry bookRegistry,
                CancellationToken ct) => await HandleAsync(pair, db, bookRegistry, ct))
            .ProducesPost<TradingPair>()
            .WithValidation<TradingPair>()
            .WithTags("Local Matching Engine")
            .WithName(nameof(CreateSymbolEndpoint))
            .MapToApiVersion(ApiVersioningExtensions.V1)
            .RequireAuthorization(Permissions.AllowDefineSymbol);
    }

    public async Task<IResult> HandleAsync(
        TradingPair pair,
        MarketTickDbContext db,
        IOrderBookRegistry bookRegistry,
        CancellationToken cancellationToken = default)
    {
        pair.Id = pair.Symbol.ToUpperInvariant();
        db.Pairs.Add(pair);
        await db.SaveChangesAsync(cancellationToken);

        bookRegistry.GetOrCreate(pair.Id);
        return TypedResults.Created($"/api/v1/engine/symbols/{pair.Id}", pair);
    }
}
