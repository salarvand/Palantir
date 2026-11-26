using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Palantir.Constants;
using Palantir.Platform.Endpoints;
using Palantir.SettlementService.Infrastructure;
using Palantir.SettlementService.Sagas;

namespace Palantir.SettlementService.Features.Settlements.Get;

internal sealed class GetSettlementEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/settlements/{orderId:guid}", async (Guid orderId, SettlementDbContext db, CancellationToken ct) =>
            {
                var state = await db.SettlementStates.AsNoTracking().FirstOrDefaultAsync(s => s.Id == orderId, ct);
                return state is not null ? Results.Ok(state) : Results.NotFound();
            })
            .ProducesGet<OrderSettlementSaga>(hasNotFound: true)
            .WithTags("Settlement")
            .WithName(nameof(GetSettlementEndpoint))
            .MapToApiVersion(ApiVersioningExtensions.V1)
            .RequireAuthorization(Permissions.AllowViewBlotter);
    }
}
