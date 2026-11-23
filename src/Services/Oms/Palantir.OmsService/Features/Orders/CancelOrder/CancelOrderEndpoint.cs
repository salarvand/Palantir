using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Palantir.Constants;
using Palantir.Platform.Endpoints;

namespace Palantir.OmsService.Features.Orders.CancelOrder;

public class CancelOrderEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/v1/orders/{id:guid}", async (Guid id, string? reason, ISender sender, CancellationToken ct) =>
        {
            var command = new CancelOrderCommand(id, reason ?? "User requested cancellation");
            var result = await sender.Send(command, ct);
            return result.IsCancelled ? Results.Ok(result) : Results.BadRequest(result);
        })
        .WithName("CancelOrder")
        .WithTags("Orders")
        .RequireAuthorization(Permissions.AllowCancelOrder);
    }
}
