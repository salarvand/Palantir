using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Palantir.ExternalGatewayService.Features.ExternalOrders.Route;
using Palantir.SharedKernel;

namespace Palantir.ExternalGatewayService.Connectors;

public record ExternalExecutionReport(
    string ExternalOrderId,
    string Symbol,
    OrderStatus Status,
    decimal ExecutedQuantity,
    decimal ExecutedPrice,
    DateTimeOffset TimestampUtc
);

public interface IExchangeVenueConnector
{
    string VenueName { get; }
    Task<ExternalExecutionReport> SubmitOrderAsync(ExternalOrderRequest order, CancellationToken ct = default);
    Task<bool> CancelOrderAsync(string externalOrderId, CancellationToken ct = default);
}

public sealed class RestWebSocketVenueConnector(
    HttpClient httpClient,
    ILogger<RestWebSocketVenueConnector> logger) : IExchangeVenueConnector
{
    public string VenueName => "ExternalExchangeHttp";

    public async Task<ExternalExecutionReport> SubmitOrderAsync(ExternalOrderRequest order, CancellationToken ct = default)
    {
        logger.LogInformation("Routing order {OrderId} for {Symbol} to venue {Venue}", order.OrderId, order.Symbol, VenueName);

        var payload = new
        {
            clientOrderId = order.OrderId,
            symbol = order.Symbol,
            side = order.Side.ToString(),
            type = order.Type.ToString(),
            quantity = order.Quantity,
            price = order.Price
        };

        var response = await httpClient.PostAsJsonAsync("/mock-exchange/orders", payload, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ExternalExecutionReport>(cancellationToken: ct)
               ?? throw new InvalidOperationException($"Venue '{VenueName}' returned an empty execution report.");
    }

    public async Task<bool> CancelOrderAsync(string externalOrderId, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync($"/mock-exchange/orders/{externalOrderId}", ct);
        return response.IsSuccessStatusCode;
    }
}

public sealed class SimulatedVenueConnector(
    IFeatureManager featureManager,
    ILogger<SimulatedVenueConnector> logger) : IExchangeVenueConnector
{
    public const string FeatureName = "ExternalExchangeSimulator";

    public string VenueName => "SimulatedExternalExchange";

    public async Task<ExternalExecutionReport> SubmitOrderAsync(ExternalOrderRequest order, CancellationToken ct = default)
    {
        if (!await featureManager.IsEnabledAsync(FeatureName))
        {
            throw new InvalidOperationException(
                $"No external venue configured: set 'ExternalVenues:MockExchangeUrl' or enable feature '{FeatureName}'.");
        }

        logger.LogInformation("Simulating external execution for order {OrderId} ({Symbol})", order.OrderId, order.Symbol);
        return CreateSimulatedReport(order.Symbol, order.Quantity, order.Price);
    }

    public async Task<bool> CancelOrderAsync(string externalOrderId, CancellationToken ct = default) =>
        await featureManager.IsEnabledAsync(FeatureName);

    public static ExternalExecutionReport CreateSimulatedReport(string symbol, decimal quantity, decimal price) =>
        new(
            Guid.NewGuid().ToString("N"),
            symbol.Trim().ToUpperInvariant(),
            OrderStatus.Filled,
            quantity,
            price,
            DateTimeOffset.UtcNow);
}
