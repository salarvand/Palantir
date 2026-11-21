using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Palantir.ExternalGatewayService.Connectors;
using Palantir.Oms.Contracts;
using Palantir.Platform.Endpoints;
using Palantir.SharedKernel;

namespace Palantir.ExternalGatewayService.Endpoints;

public class ExternalGatewayEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/external")
            .WithTags("External Exchange Gateway");

        group.MapPost("/orders", RouteExternalOrderAsync);

        // Built-in Mock Exchange Simulator endpoints for local testing & CI
        var mock = app.MapGroup("/mock-exchange")
            .WithTags("Mock Exchange Simulator");

        mock.MapPost("/orders", SimulateOrderAsync);
        mock.MapGet("/quotes/{symbol}", SimulateQuoteAsync);
    }

    private static async Task<IResult> RouteExternalOrderAsync(
        SubmitOrderRequest request,
        IExchangeVenueConnector connector,
        CancellationToken ct)
    {
        var report = await connector.SubmitOrderAsync(request, ct);
        return Results.Ok(report);
    }

    private static IResult SimulateOrderAsync(object payload)
    {
        var report = new ExternalExecutionReport(
            Guid.NewGuid().ToString("N"),
            "AAPL",
            OrderStatus.Filled,
            100m,
            185.50m,
            DateTimeOffset.UtcNow
        );
        return Results.Ok(report);
    }

    private static IResult SimulateQuoteAsync(string symbol)
    {
        return Results.Ok(new
        {
            Symbol = symbol.ToUpperInvariant(),
            Bid = 185.45m,
            Ask = 185.55m,
            Last = 185.50m,
            TimestampUtc = DateTimeOffset.UtcNow
        });
    }
}
