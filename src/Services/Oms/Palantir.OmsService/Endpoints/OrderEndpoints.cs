using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Orleans;
using Palantir.Constants;
using Palantir.Oms.Contracts;
using Palantir.Platform.Endpoints;

namespace Palantir.OmsService.Endpoints;

public class OrderEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/orders")
            .WithTags("OMS");

        group.MapPost("/", SubmitOrderAsync)
            .RequireAuthorization(Permissions.AllowPlaceOrder);

        group.MapGet("/{id:guid}", GetOrderByIdAsync);

        group.MapDelete("/{id:guid}", CancelOrderAsync)
            .RequireAuthorization(Permissions.AllowCancelOrder);

        group.MapGet("/client/{clientId:guid}", GetClientActiveOrdersAsync);
    }

    private static async Task<IResult> SubmitOrderAsync(
        SubmitOrderRequest request,
        IGrainFactory grainFactory,
        CancellationToken ct)
    {
        var orderId = request.OrderId != Guid.Empty ? request.OrderId : Guid.NewGuid();
        var adjustedRequest = request with { OrderId = orderId };

        var grain = grainFactory.GetGrain<IOrderGrain>(orderId);
        var snapshot = await grain.SubmitOrderAsync(adjustedRequest);

        return Results.Created($"/api/v1/orders/{orderId}", snapshot);
    }

    private static async Task<IResult> GetOrderByIdAsync(
        Guid id,
        IGrainFactory grainFactory,
        CancellationToken ct)
    {
        var grain = grainFactory.GetGrain<IOrderGrain>(id);
        var snapshot = await grain.GetSnapshotAsync();
        return Results.Ok(snapshot);
    }

    private static async Task<IResult> CancelOrderAsync(
        Guid id,
        string? reason,
        IGrainFactory grainFactory,
        CancellationToken ct)
    {
        var grain = grainFactory.GetGrain<IOrderGrain>(id);
        var snapshot = await grain.CancelOrderAsync(reason ?? "User cancelled");
        return Results.Ok(snapshot);
    }

    private static async Task<IResult> GetClientActiveOrdersAsync(
        Guid clientId,
        IGrainFactory grainFactory,
        CancellationToken ct)
    {
        var session = grainFactory.GetGrain<IClientSessionGrain>(clientId.ToString("N"));
        var activeOrderIds = await session.GetActiveOrdersAsync();
        return Results.Ok(activeOrderIds);
    }
}
