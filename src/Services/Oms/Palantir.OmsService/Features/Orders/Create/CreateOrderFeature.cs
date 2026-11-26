using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Orleans;
using Palantir.Constants;
using Palantir.OmsService.Grains;
using Palantir.Platform.Endpoints;
using Palantir.Platform.Validation;
using Palantir.SharedKernel;
using Wolverine;

namespace Palantir.OmsService.Features.Orders.Create;

public sealed record CreateOrderCommand(
    Guid OrderId,
    Guid ClientId,
    string Symbol,
    TradeSide Side,
    OrderType Type,
    decimal Quantity,
    decimal Price,
    TimeInForce TimeInForce,
    string VenueType,
    TimeSpan? ExpiryDuration = null
);

public sealed class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.Symbol).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Price)
            .GreaterThan(0)
            .When(x => x.Type is OrderType.Limit or OrderType.StopLimit);
    }
}

public sealed class CreateOrderHandler(IGrainFactory grainFactory)
{
    public async Task<OrderStateSnapshot> Handle(CreateOrderCommand command, CancellationToken ct)
    {
        var orderId = command.OrderId != Guid.Empty ? command.OrderId : Guid.NewGuid();
        var request = new SubmitOrderRequest(
            orderId,
            command.ClientId,
            command.Symbol.Trim().ToUpperInvariant(),
            command.Side,
            command.Type,
            command.Quantity,
            command.Price,
            command.TimeInForce,
            string.IsNullOrWhiteSpace(command.VenueType) ? "Local" : command.VenueType,
            command.ExpiryDuration);

        var grain = grainFactory.GetGrain<IOrderGrain>(orderId);
        return await grain.SubmitOrderAsync(request);
    }
}

internal sealed class CreateOrderEndpoint : IEndpoint<IResult, CreateOrderCommand, IMessageBus>
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/orders", async (
                [FromHeader(Name = ApiConstants.RequestIdHeader)] string? requestId,
                CreateOrderCommand command,
                IMessageBus bus,
                CancellationToken ct) => await HandleAsync(command, bus, ct))
            .ProducesPost<OrderStateSnapshot>()
            .WithValidation<CreateOrderCommand>()
            .WithIdempotency()
            .WithTags("OMS")
            .WithName(nameof(CreateOrderEndpoint))
            .WithSummary("Submit Order")
            .WithDescription("Submits an institutional order through Orleans + Marten Event Store + Wolverine Outbox.")
            .MapToApiVersion(ApiVersioningExtensions.V1)
            .RequireAuthorization(Permissions.AllowPlaceOrder);
    }

    public async Task<IResult> HandleAsync(
        CreateOrderCommand command,
        IMessageBus bus,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await bus.InvokeAsync<OrderStateSnapshot>(command, cancellationToken);
        return TypedResults.Created($"/api/v1/orders/{snapshot.OrderId}", snapshot);
    }
}
