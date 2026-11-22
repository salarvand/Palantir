using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Palantir.Oms.Contracts;

namespace Palantir.OmsService.Execution;

public record ExecutionDispatchResult(bool IsSuccess, string? ErrorMessage = null);

public interface ILocalEngineClient
{
    Task<bool> DispatchOrderAsync(SubmitOrderRequest request, CancellationToken ct = default);
}

public class LocalEngineClient : ILocalEngineClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LocalEngineClient> _logger;

    public LocalEngineClient(HttpClient httpClient, ILogger<LocalEngineClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> DispatchOrderAsync(SubmitOrderRequest request, CancellationToken ct = default)
    {
        var entry = new
        {
            request.OrderId,
            request.ClientId,
            request.Symbol,
            request.Side,
            request.Type,
            request.Price,
            request.Quantity,
            TimestampUtc = DateTimeOffset.UtcNow
        };

        var response = await _httpClient.PostAsJsonAsync("/api/v1/engine/match", entry, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to dispatch order {OrderId} to LocalEngine. StatusCode: {StatusCode}", request.OrderId, response.StatusCode);
            return false;
        }

        return true;
    }
}

public interface IExternalGatewayClient
{
    Task<bool> DispatchOrderAsync(SubmitOrderRequest request, CancellationToken ct = default);
}

public class ExternalGatewayClient : IExternalGatewayClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExternalGatewayClient> _logger;

    public ExternalGatewayClient(HttpClient httpClient, ILogger<ExternalGatewayClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> DispatchOrderAsync(SubmitOrderRequest request, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/v1/external/orders", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to dispatch order {OrderId} to ExternalGateway. StatusCode: {StatusCode}", request.OrderId, response.StatusCode);
            return false;
        }

        return true;
    }
}

public interface IExecutionRouter
{
    Task<ExecutionDispatchResult> DispatchOrderAsync(SubmitOrderRequest request, CancellationToken ct = default);
}

public class ExecutionRouter : IExecutionRouter
{
    private readonly ILocalEngineClient _localEngineClient;
    private readonly IExternalGatewayClient _externalGatewayClient;
    private readonly ILogger<ExecutionRouter> _logger;

    public ExecutionRouter(
        ILocalEngineClient localEngineClient,
        IExternalGatewayClient externalGatewayClient,
        ILogger<ExecutionRouter> logger)
    {
        _localEngineClient = localEngineClient;
        _externalGatewayClient = externalGatewayClient;
        _logger = logger;
    }

    public async Task<ExecutionDispatchResult> DispatchOrderAsync(SubmitOrderRequest request, CancellationToken ct = default)
    {
        try
        {
            bool success;
            if (string.Equals(request.VenueType, "External", StringComparison.OrdinalIgnoreCase))
            {
                success = await _externalGatewayClient.DispatchOrderAsync(request, ct);
            }
            else
            {
                success = await _localEngineClient.DispatchOrderAsync(request, ct);
            }

            return new ExecutionDispatchResult(success, success ? null : "Execution venue rejected or failed to process order.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching order {OrderId} to venue {VenueType}", request.OrderId, request.VenueType);
            return new ExecutionDispatchResult(false, ex.Message);
        }
    }
}
