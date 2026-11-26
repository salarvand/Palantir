using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Palantir.ExternalGatewayService.Connectors;
using Palantir.Platform.Endpoints;
using Palantir.Platform.FeatureFlags;
using Palantir.SharedKernel;

namespace Palantir.ExternalGatewayService.Features.Simulator;

internal sealed class SimulateExchangeEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/external/mock/orders", (object payload) =>
            {
                var report = new ExternalExecutionReport(
                    Guid.NewGuid().ToString("N"),
                    "AAPL",
                    OrderStatus.Filled,
                    100m,
                    185.50m,
                    DateTimeOffset.UtcNow);
                return TypedResults.Ok(report);
            })
            .WithTags("Mock Exchange Simulator")
            .WithName("SimulateExternalOrder")
            .MapToApiVersion(ApiVersioningExtensions.V1)
            .RequireFeature("ExternalExchangeSimulator");

        app.MapGet("/external/mock/quotes/{symbol}", (string symbol) =>
                TypedResults.Ok(new
                {
                    Symbol = symbol.ToUpperInvariant(),
                    Bid = 185.45m,
                    Ask = 185.55m,
                    Last = 185.50m,
                    TimestampUtc = DateTimeOffset.UtcNow
                }))
            .WithTags("Mock Exchange Simulator")
            .WithName("SimulateExternalQuote")
            .MapToApiVersion(ApiVersioningExtensions.V1)
            .RequireFeature("ExternalExchangeSimulator");
    }
}
