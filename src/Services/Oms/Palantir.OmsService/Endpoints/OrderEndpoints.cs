using Marten;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Orleans;
using Palantir.Constants;
using Palantir.Oms.Contracts;
using Palantir.OmsService.Projections;
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

        group.MapGet("/blotter", GetBlotterOrdersAsync);
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
        IQuerySession querySession,
        CancellationToken ct)
    {
        var grain = grainFactory.GetGrain<IOrderGrain>(id);
        var snapshot = await grain.GetSnapshotAsync();
        if (snapshot.OrderId != Guid.Empty)
        {
            return Results.Ok(snapshot);
        }

        var blotterView = await querySession.LoadAsync<OrderBlotterView>(id, ct);
        return blotterView != null ? Results.Ok(blotterView) : Results.NotFound();
    }

    private static async Task<IResult> CancelOrderAsync(
        Guid id,
        string? reason,
        IGrainFactory grainFactory,
        CancellationToken ct)
    {
        var grain = grainFactory.GetGrain<IOrderGrain>(id);
        var snapshot = await grain.CancelOrderAsync(reason ?? "User cancelled");
        return snapshot.OrderId != Guid.Empty ? Results.Ok(snapshot) : Results.NotFound();
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

    private static async Task<IResult> GetBlotterOrdersAsync(
        string? symbol,
        Guid? clientId,
        int? limit,
        IQuerySession querySession,
        CancellationToken ct)
    {
        var size = Math.Clamp(limit ?? 50, 1, 200);
        IQueryable<OrderBlotterView> query = querySession.Query<OrderBlotterView>();

        if (clientId.HasValue && clientId.Value != Guid.Empty)
        {
            query = query.Where(x => x.ClientId == clientId.Value);
        }

        if (!string.IsNullOrWhiteSpace(symbol))
        {
            var upperSymbol = symbol.Trim().ToUpperInvariant();
            query = query.Where(x => x.Symbol == upperSymbol);
        }

        var orders = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(size)
            .ToListAsync(ct);

        return Results.Ok(orders);
    }
}
