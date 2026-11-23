using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Palantir.Platform.Endpoints;

namespace Palantir.OmsService.Features.Orders.GetOrderById;

public class GetOrderByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/orders/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var query = new GetOrderByIdQuery(id);
            var result = await sender.Send(query, ct);
            return result != null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetOrderById")
        .WithTags("Orders")
        .RequireAuthorization();
    }
}
