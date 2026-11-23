using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Palantir.Platform.Endpoints;

namespace Palantir.OmsService.Features.Orders.GetActiveBlotter;

public class GetActiveBlotterEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/orders/blotter", async (Guid? clientId, string? symbol, int? limit, ISender sender, CancellationToken ct) =>
        {
            var query = new GetActiveBlotterQuery(clientId, symbol, limit ?? 50);
            var results = await sender.Send(query, ct);
            return Results.Ok(results);
        })
        .WithName("GetActiveBlotter")
        .WithTags("Orders")
        .RequireAuthorization();
    }
}
