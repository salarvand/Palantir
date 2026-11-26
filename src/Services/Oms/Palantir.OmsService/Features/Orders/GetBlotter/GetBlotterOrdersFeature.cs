using Marten;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Palantir.Constants;
using Palantir.OmsService.Projections;
using Palantir.Platform.Endpoints;
using Wolverine;

namespace Palantir.OmsService.Features.Orders.GetBlotter;

public sealed record GetBlotterOrdersQuery(string? Symbol, Guid? ClientId, int Limit);

public sealed class GetBlotterOrdersHandler(IQuerySession querySession)
{
    public async Task<IReadOnlyList<OrderBlotterView>> Handle(GetBlotterOrdersQuery query, CancellationToken ct)
    {
        var size = Math.Clamp(query.Limit, 1, 200);
        IQueryable<OrderBlotterView> q = querySession.Query<OrderBlotterView>();

        if (query.ClientId.HasValue && query.ClientId.Value != Guid.Empty)
        {
            q = q.Where(x => x.ClientId == query.ClientId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Symbol))
        {
            var upperSymbol = query.Symbol.Trim().ToUpperInvariant();
            q = q.Where(x => x.Symbol == upperSymbol);
        }

        return await q.OrderByDescending(x => x.CreatedAtUtc)
            .Take(size)
            .ToListAsync(ct);
    }
}

internal sealed class GetBlotterOrdersEndpoint : IEndpoint<IResult, GetBlotterOrdersQuery, IMessageBus>
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/orders/blotter", async (
                string? symbol,
                Guid? clientId,
                int? limit,
                IMessageBus bus,
                CancellationToken ct) => await HandleAsync(new GetBlotterOrdersQuery(symbol, clientId, limit ?? 50), bus, ct))
            .ProducesGet<IReadOnlyList<OrderBlotterView>>()
            .WithTags("OMS")
            .WithName(nameof(GetBlotterOrdersEndpoint))
            .WithSummary("Get Order Blotter")
            .MapToApiVersion(ApiVersioningExtensions.V1)
            .RequireAuthorization(Permissions.AllowViewBlotter);
    }

    public async Task<IResult> HandleAsync(
        GetBlotterOrdersQuery query,
        IMessageBus bus,
        CancellationToken cancellationToken = default)
    {
        var orders = await bus.InvokeAsync<IReadOnlyList<OrderBlotterView>>(query, cancellationToken);
        return TypedResults.Ok(orders);
    }
}
