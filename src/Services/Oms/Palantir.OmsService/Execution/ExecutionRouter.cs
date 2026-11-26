using System.Globalization;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Palantir.LocalEngineService.Grpc;
using Palantir.Oms.Contracts;

namespace Palantir.OmsService.Execution;

public record ExecutionDispatchResult(
    bool IsSuccess,
    string Status = "Placed",
    decimal FilledQuantity = 0m,
    decimal AveragePrice = 0m,
    Guid? TradeId = null,
    string? ErrorMessage = null);

public interface IVenueDispatcher
{
    string VenueType { get; }
    Task<ExecutionDispatchResult> DispatchOrderAsync(SubmitOrderRequest request, CancellationToken ct = default);
    Task<bool> CancelOrderAsync(Guid orderId, string symbol, string reason, CancellationToken ct = default);
}

public sealed class LocalGrpcVenueDispatcher : IVenueDispatcher
{
    private readonly MatchingEngineGrpc.MatchingEngineGrpcClient _grpcClient;
    private readonly ILogger<LocalGrpcVenueDispatcher> _logger;

    public LocalGrpcVenueDispatcher(
        MatchingEngineGrpc.MatchingEngineGrpcClient grpcClient,
        ILogger<LocalGrpcVenueDispatcher> logger)
    {
        _grpcClient = grpcClient;
        _logger = logger;
    }

    public string VenueType => "Local";

    public async Task<ExecutionDispatchResult> DispatchOrderAsync(SubmitOrderRequest request, CancellationToken ct = default)
    {
        var grpcRequest = new GrpcSubmitOrderRequest
        {
            OrderId = request.OrderId.ToString(),
            ClientId = request.ClientId.ToString(),
            Symbol = request.Symbol,
            Side = (int)request.Side,
            Type = (int)request.Type,
            Quantity = request.Quantity.ToString(CultureInfo.InvariantCulture),
            Price = request.Price.ToString(CultureInfo.InvariantCulture),
            TimeInForce = (int)request.TimeInForce
        };

        var report = await _grpcClient.SubmitOrderAsync(grpcRequest, cancellationToken: ct);

        decimal.TryParse(report.FilledQuantity, NumberStyles.Any, CultureInfo.InvariantCulture, out var filledQty);
        decimal.TryParse(report.AveragePrice, NumberStyles.Any, CultureInfo.InvariantCulture, out var avgPrice);
        Guid? tradeId = Guid.TryParse(report.TradeId, out var parsedTradeId) ? parsedTradeId : null;

        var isSuccess = !string.Equals(report.Status, "Rejected", StringComparison.OrdinalIgnoreCase);
        if (!isSuccess)
        {
            _logger.LogWarning("LocalEngine rejected order {OrderId}: {Message}", request.OrderId, report.Message);
        }

        return new ExecutionDispatchResult(isSuccess, report.Status, filledQty, avgPrice, tradeId, isSuccess ? null : report.Message);
    }

    public async Task<bool> CancelOrderAsync(Guid orderId, string symbol, string reason, CancellationToken ct = default)
    {
        var report = await _grpcClient.CancelOrderAsync(new GrpcCancelOrderRequest
        {
            OrderId = orderId.ToString(),
            Symbol = symbol,
            Reason = reason
        }, cancellationToken: ct);

        return report.IsCancelled;
    }
}

public sealed class ExternalHttpVenueDispatcher : IVenueDispatcher
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExternalHttpVenueDispatcher> _logger;

    public ExternalHttpVenueDispatcher(HttpClient httpClient, ILogger<ExternalHttpVenueDispatcher> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public string VenueType => "External";

    public async Task<ExecutionDispatchResult> DispatchOrderAsync(SubmitOrderRequest request, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/v1/external/orders", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to dispatch order {OrderId} to ExternalGateway. StatusCode: {StatusCode}", request.OrderId, response.StatusCode);
            return new ExecutionDispatchResult(false, "Rejected", ErrorMessage: $"External venue error: {response.StatusCode}");
        }

        return new ExecutionDispatchResult(true, "Placed");
    }

    public Task<bool> CancelOrderAsync(Guid orderId, string symbol, string reason, CancellationToken ct = default) =>
        Task.FromResult(true);
}

public interface IExecutionRouter
{
    Task<ExecutionDispatchResult> DispatchOrderAsync(SubmitOrderRequest request, CancellationToken ct = default);
    Task<bool> CancelOrderAsync(string venueType, Guid orderId, string symbol, string reason, CancellationToken ct = default);
}

public sealed class ExecutionRouter : IExecutionRouter
{
    private readonly IReadOnlyDictionary<string, IVenueDispatcher> _dispatchers;
    private readonly ILogger<ExecutionRouter> _logger;

    public ExecutionRouter(IEnumerable<IVenueDispatcher> dispatchers, ILogger<ExecutionRouter> logger)
    {
        _dispatchers = dispatchers.ToDictionary(d => d.VenueType, StringComparer.OrdinalIgnoreCase);
        _logger = logger;
    }

    public async Task<ExecutionDispatchResult> DispatchOrderAsync(SubmitOrderRequest request, CancellationToken ct = default)
    {
        try
        {
            var dispatcher = ResolveDispatcher(request.VenueType);
            return await dispatcher.DispatchOrderAsync(request, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching order {OrderId} to venue {VenueType}", request.OrderId, request.VenueType);
            return new ExecutionDispatchResult(false, "Rejected", ErrorMessage: ex.Message);
        }
    }

    public async Task<bool> CancelOrderAsync(string venueType, Guid orderId, string symbol, string reason, CancellationToken ct = default)
    {
        try
        {
            var dispatcher = ResolveDispatcher(venueType);
            return await dispatcher.CancelOrderAsync(orderId, symbol, reason, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order {OrderId} on venue {VenueType}", orderId, venueType);
            return false;
        }
    }

    private IVenueDispatcher ResolveDispatcher(string venueType)
    {
        if (_dispatchers.TryGetValue(venueType, out var dispatcher))
        {
            return dispatcher;
        }

        return _dispatchers["Local"];
    }
}
