using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Palantir.Constants;
using Palantir.LocalEngineService.Matching;
using Palantir.Platform.Endpoints;
using Palantir.Platform.Validation;

namespace Palantir.LocalEngineService.Features.Matching.MatchOrder;

public sealed class MatchOrderValidator : AbstractValidator<OrderBookEntry>
{
    public MatchOrderValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Symbol).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

internal sealed class MatchOrderEndpoint : IEndpoint<IResult, OrderBookEntry, IOrderBookRegistry, IOrderExecutionPublisher>
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/engine/match", async (
                OrderBookEntry entry,
                IOrderBookRegistry bookRegistry,
                IOrderExecutionPublisher executionPublisher,
                CancellationToken ct) => await HandleAsync(entry, bookRegistry, executionPublisher, ct))
            .ProducesPostWithoutLocation<MatchResult>()
            .WithValidation<OrderBookEntry>()
            .WithTags("Local Matching Engine")
            .WithName(nameof(MatchOrderEndpoint))
            .MapToApiVersion(ApiVersioningExtensions.V1)
            .RequireAuthorization(Permissions.AllowPlaceOrder);
    }

    public async Task<IResult> HandleAsync(
        OrderBookEntry entry,
        IOrderBookRegistry bookRegistry,
        IOrderExecutionPublisher executionPublisher,
        CancellationToken cancellationToken = default)
    {
        var book = bookRegistry.GetOrCreate(entry.Symbol);
        var matchResult = book.Match(entry);

        await executionPublisher.PublishMatchResultsAsync(entry.Symbol, entry.Side, book, matchResult, cancellationToken);
        return TypedResults.Ok(matchResult);
    }
}
