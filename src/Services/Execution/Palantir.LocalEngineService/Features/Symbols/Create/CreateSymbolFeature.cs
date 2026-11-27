using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Npgsql;
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
            .ProducesProblem(StatusCodes.Status409Conflict)
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
        var symbolId = pair.Symbol.Trim().ToUpperInvariant();
        if (await db.Pairs.AsNoTracking().AnyAsync(p => p.Id == symbolId, cancellationToken))
        {
            return DuplicateSymbolConflict(symbolId);
        }

        pair.Id = symbolId;
        pair.BaseCurrency = pair.BaseCurrency.Trim().ToUpperInvariant();
        pair.QuoteCurrency = pair.QuoteCurrency.Trim().ToUpperInvariant();

        db.Pairs.Add(pair);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return DuplicateSymbolConflict(symbolId);
        }

        bookRegistry.GetOrCreate(pair.Id);
        return TypedResults.Created($"/api/v1/engine/symbols/{pair.Id}", pair);
    }

    private static IResult DuplicateSymbolConflict(string symbolId) =>
        TypedResults.Conflict(new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Duplicate trading symbol.",
            Detail = $"Trading symbol '{symbolId}' already exists."
        });
}
