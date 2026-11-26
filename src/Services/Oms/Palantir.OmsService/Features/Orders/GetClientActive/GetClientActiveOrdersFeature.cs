using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Orleans;
using Palantir.Constants;
using Palantir.OmsService.Grains;
using Palantir.Platform.Endpoints;
using Wolverine;

namespace Palantir.OmsService.Features.Orders.GetClientActive;

public sealed record GetClientActiveOrdersQuery(Guid ClientId);

public sealed class GetClientActiveOrdersHandler(IGrainFactory grainFactory)
{
    public Task<IReadOnlyList<Guid>> Handle(GetClientActiveOrdersQuery query, CancellationToken ct)
    {
        var session = grainFactory.GetGrain<IClientSessionGrain>(query.ClientId.ToString("N"));
        return session.GetActiveOrdersAsync();
    }
}

internal sealed class GetClientActiveOrdersEndpoint : IEndpoint<IResult, Guid, IMessageBus>
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/orders/client/{clientId:guid}", async (Guid clientId, IMessageBus bus, CancellationToken ct) =>
                await HandleAsync(clientId, bus, ct))
            .ProducesGet<IReadOnlyList<Guid>>()
            .WithTags("OMS")
            .WithName(nameof(GetClientActiveOrdersEndpoint))
            .WithSummary("Get Client Active Orders")
            .MapToApiVersion(ApiVersioningExtensions.V1)
            .RequireAuthorization(Permissions.AllowViewBlotter);
    }

    public async Task<IResult> HandleAsync(Guid clientId, IMessageBus bus, CancellationToken cancellationToken = default)
    {
        var activeIds = await bus.InvokeAsync<IReadOnlyList<Guid>>(new GetClientActiveOrdersQuery(clientId), cancellationToken);
        return TypedResults.Ok(activeIds);
    }
}
