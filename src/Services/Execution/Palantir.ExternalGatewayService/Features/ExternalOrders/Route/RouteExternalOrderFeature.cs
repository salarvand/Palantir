using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Palantir.Constants;
using Palantir.ExternalGatewayService.Connectors;
using Palantir.Platform.Endpoints;
using Palantir.Platform.Validation;

namespace Palantir.ExternalGatewayService.Features.ExternalOrders.Route;

public sealed class RouteExternalOrderValidator : AbstractValidator<ExternalOrderRequest>
{
    public RouteExternalOrderValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Symbol).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

internal sealed class RouteExternalOrderEndpoint : IEndpoint<IResult, ExternalOrderRequest, IExchangeVenueConnector>
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/external/orders", async (
                ExternalOrderRequest request,
                IExchangeVenueConnector connector,
                CancellationToken ct) => await HandleAsync(request, connector, ct))
            .ProducesPostWithoutLocation<ExternalExecutionReport>()
            .WithValidation<ExternalOrderRequest>()
            .WithTags("External Exchange Gateway")
            .WithName(nameof(RouteExternalOrderEndpoint))
            .MapToApiVersion(ApiVersioningExtensions.V1)
            .RequireAuthorization(Permissions.AllowPlaceOrder);
    }

    public async Task<IResult> HandleAsync(
        ExternalOrderRequest request,
        IExchangeVenueConnector connector,
        CancellationToken cancellationToken = default)
    {
        var report = await connector.SubmitOrderAsync(request, cancellationToken);
        return TypedResults.Ok(report);
    }
}
