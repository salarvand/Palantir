using Marten;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Orleans;
using Palantir.Constants;
using Palantir.OmsService.Grains;
using Palantir.OmsService.Projections;
using Palantir.Platform.Endpoints;
using Wolverine;

namespace Palantir.OmsService.Features.Orders.Get;

public sealed record GetOrderQuery(Guid OrderId);

public sealed class GetOrderHandler(IGrainFactory grainFactory, IQuerySession querySession)
{
    public async Task<object?> Handle(GetOrderQuery query, CancellationToken ct)
    {
        var grain = grainFactory.GetGrain<IOrderGrain>(query.OrderId);
        var snapshot = await grain.GetSnapshotAsync();
        if (snapshot.OrderId != Guid.Empty)
        {
            return snapshot;
        }

        return await querySession.LoadAsync<OrderBlotterView>(query.OrderId, ct);
    }
}

internal sealed class GetOrderEndpoint : IEndpoint<IResult, Guid, IMessageBus>
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/orders/{id:guid}", async (Guid id, IMessageBus bus, CancellationToken ct) =>
                await HandleAsync(id, bus, ct))
            .ProducesGet<object>(hasNotFound: true)
            .WithTags("OMS")
            .WithName(nameof(GetOrderEndpoint))
            .WithSummary("Get Order By Id")
            .MapToApiVersion(ApiVersioningExtensions.V1)
            .RequireAuthorization(Permissions.AllowViewBlotter);
    }

    public async Task<IResult> HandleAsync(Guid id, IMessageBus bus, CancellationToken cancellationToken = default)
    {
        var result = await bus.InvokeAsync<object?>(new GetOrderQuery(id), cancellationToken);
        return result is not null ? TypedResults.Ok(result) : TypedResults.NotFound();
    }
}
