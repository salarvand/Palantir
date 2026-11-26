using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Orleans;
using Palantir.Constants;
using Palantir.OmsService.Grains;
using Palantir.Platform.Endpoints;
using Palantir.Platform.Validation;
using Wolverine;

namespace Palantir.OmsService.Features.Orders.Cancel;

public sealed record CancelOrderCommand(Guid OrderId, string Reason);

public sealed class CancelOrderValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(250);
    }
}

public sealed class CancelOrderHandler(IGrainFactory grainFactory)
{
    public async Task<OrderStateSnapshot> Handle(CancelOrderCommand command, CancellationToken ct)
    {
        var grain = grainFactory.GetGrain<IOrderGrain>(command.OrderId);
        return await grain.CancelOrderAsync(command.Reason);
    }
}

internal sealed class CancelOrderEndpoint : IEndpoint<IResult, CancelOrderCommand, IMessageBus>
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/orders/{id:guid}", async (Guid id, string? reason, IMessageBus bus, CancellationToken ct) =>
                await HandleAsync(new CancelOrderCommand(id, reason ?? "User cancelled"), bus, ct))
            .ProducesGet<OrderStateSnapshot>(hasValidation: true, hasNotFound: true)
            .WithValidation<CancelOrderCommand>()
            .WithTags("OMS")
            .WithName(nameof(CancelOrderEndpoint))
            .WithSummary("Cancel Order")
            .MapToApiVersion(ApiVersioningExtensions.V1)
            .RequireAuthorization(Permissions.AllowCancelOrder);
    }

    public async Task<IResult> HandleAsync(
        CancelOrderCommand command,
        IMessageBus bus,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await bus.InvokeAsync<OrderStateSnapshot>(command, cancellationToken);
        return snapshot.OrderId != Guid.Empty ? TypedResults.Ok(snapshot) : TypedResults.NotFound();
    }
}
