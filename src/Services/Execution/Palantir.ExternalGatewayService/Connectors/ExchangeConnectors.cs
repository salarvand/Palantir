using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Palantir.Oms.Contracts;
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
    Task<ExternalExecutionReport> SubmitOrderAsync(SubmitOrderRequest order, CancellationToken ct = default);
    Task<bool> CancelOrderAsync(string externalOrderId, CancellationToken ct = default);
}

public class RestWebSocketVenueConnector : IExchangeVenueConnector
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RestWebSocketVenueConnector> _logger;

    public string VenueName => "SimulatedExternalExchange";

    public RestWebSocketVenueConnector(HttpClient httpClient, ILogger<RestWebSocketVenueConnector> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ExternalExecutionReport> SubmitOrderAsync(SubmitOrderRequest order, CancellationToken ct = default)
    {
        _logger.LogInformation("Routing order {OrderId} for {Symbol} to venue {Venue}", order.OrderId, order.Symbol, VenueName);

        var payload = new
        {
            clientOrderId = order.OrderId,
            symbol = order.Symbol,
            side = order.Side.ToString(),
            type = order.Type.ToString(),
            quantity = order.Quantity,
            price = order.Price
        };

        var response = await _httpClient.PostAsJsonAsync("/mock-exchange/orders", payload, ct);
        if (response.IsSuccessStatusCode)
        {
            var report = await response.Content.ReadFromJsonAsync<ExternalExecutionReport>(cancellationToken: ct);
            return report ?? CreateDefaultReport(order);
        }

        return CreateDefaultReport(order);
    }

    public async Task<bool> CancelOrderAsync(string externalOrderId, CancellationToken ct = default)
    {
        var response = await _httpClient.DeleteAsync($"/mock-exchange/orders/{externalOrderId}", ct);
        return response.IsSuccessStatusCode;
    }

    private static ExternalExecutionReport CreateDefaultReport(SubmitOrderRequest order) =>
        new(
            Guid.NewGuid().ToString("N"),
            order.Symbol,
            OrderStatus.Filled,
            order.Quantity,
            order.Price,
            DateTimeOffset.UtcNow
        );
}
