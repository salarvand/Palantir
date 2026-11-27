using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Palantir.ExternalGatewayService.Connectors;
using Palantir.Platform.Endpoints;
using Palantir.Platform.FeatureFlags;

namespace Palantir.ExternalGatewayService.Features.Simulator;

public sealed record SimulateOrderRequest(string Symbol, decimal Quantity, decimal Price);

internal sealed class SimulateExchangeEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/external/mock/orders", (SimulateOrderRequest request) =>
                TypedResults.Ok(SimulatedVenueConnector.CreateSimulatedReport(
                    request.Symbol,
                    request.Quantity,
                    request.Price)))
            .WithTags("Mock Exchange Simulator")
            .WithName("SimulateExternalOrder")
            .MapToApiVersion(ApiVersioningExtensions.V1)
            .RequireFeature(SimulatedVenueConnector.FeatureName);

        app.MapGet("/external/mock/quotes/{symbol}", (string symbol) =>
                TypedResults.Ok(new
                {
                    Symbol = symbol.Trim().ToUpperInvariant(),
                    Bid = 185.45m,
                    Ask = 185.55m,
                    Last = 185.50m,
                    TimestampUtc = DateTimeOffset.UtcNow
                }))
            .WithTags("Mock Exchange Simulator")
            .WithName("SimulateExternalQuote")
            .MapToApiVersion(ApiVersioningExtensions.V1)
            .RequireFeature(SimulatedVenueConnector.FeatureName);
    }
}
