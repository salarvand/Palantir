using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Orleans;
using Palantir.Constants;
using Palantir.OmsService.Grains;
using Palantir.Platform.Auditing;
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

public sealed class OrderSymbolSpecification : ValidationSpecification<CreateOrderCommand>
{
    public override string RuleName => nameof(CreateOrderCommand.Symbol);
    public override string ErrorMessage => "Symbol must be non-empty and at most 20 characters.";
    public override bool IsSatisfiedBy(CreateOrderCommand cmd) =>
        !string.IsNullOrWhiteSpace(cmd.Symbol) && cmd.Symbol.Trim().Length <= 20;
}

public sealed class OrderQuantitySpecification : ValidationSpecification<CreateOrderCommand>
{
    public override string RuleName => nameof(CreateOrderCommand.Quantity);
    public override string ErrorMessage => "Quantity must be greater than zero.";
    public override bool IsSatisfiedBy(CreateOrderCommand cmd) => cmd.Quantity > 0;
}

public sealed class LimitOrderPriceSpecification : ValidationSpecification<CreateOrderCommand>
{
    public override string RuleName => nameof(CreateOrderCommand.Price);
    public override string ErrorMessage => "Limit and StopLimit orders require a positive price.";
    public override bool IsSatisfiedBy(CreateOrderCommand cmd) =>
        cmd.Type is not (OrderType.Limit or OrderType.StopLimit) || cmd.Price > 0;
}

public sealed class CreateOrderValidator : SpecificationValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();
        UseSpecification(new OrderSymbolSpecification());
        UseSpecification(new OrderQuantitySpecification());
        UseSpecification(new LimitOrderPriceSpecification());
    }
}

public sealed class CreateOrderHandler(
    IGrainFactory grainFactory,
    IAuditEmitter auditEmitter)
{
    private static readonly IValidationSpecification<CreateOrderCommand>[] DomainSpecs =
    [
        new OrderSymbolSpecification(),
        new OrderQuantitySpecification(),
        new LimitOrderPriceSpecification()
    ];

    public async Task<OrderStateSnapshot> Handle(CreateOrderCommand command, CancellationToken ct)
    {
        command.EnsureSatisfies(DomainSpecs);

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
        var snapshot = await grain.SubmitOrderAsync(request);

        auditEmitter.Emit(AuditEntry.Create(
            "OrderSubmitted",
            "OmsService",
            "Order",
            snapshot.OrderId.ToString(),
            "Create",
            new Dictionary<string, object?>
            {
                ["Symbol"] = request.Symbol,
                ["Side"] = request.Side.ToString(),
                ["Quantity"] = request.Quantity,
                ["Price"] = request.Price
            },
            command.ClientId.ToString()));

        return snapshot;
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
