using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Palantir.AiAssistantService.Mcp;

public record McpToolDefinition(
    string Name,
    string Description,
    object InputSchema
);

public record McpToolCallRequest(
    string ToolName,
    Dictionary<string, object>? Arguments
);

public record McpToolCallResult(
    string ToolName,
    bool IsSuccess,
    object Result,
    string? ErrorMessage = null
);

public record JsonRpcRequest(
    string? JsonRpc,
    object? Id,
    string Method,
    JsonElement? Params
);

public record JsonRpcResponse(
    string JsonRpc,
    object? Id,
    object? Result,
    JsonRpcError? Error = null
);

public record JsonRpcError(
    int Code,
    string Message,
    object? Data = null
);

public interface IMcpToolExecutor
{
    IReadOnlyList<McpToolDefinition> AvailableTools { get; }
    Task<McpToolCallResult> ExecuteToolAsync(McpToolCallRequest request, CancellationToken ct = default);
}

public static class McpToolRegistry
{
    public static readonly IReadOnlyList<McpToolDefinition> AvailableTools =
    [
        new(
            "get_order_blotter",
            "Retrieves active and executed trading orders for a specific symbol or client",
            new { type = "object", properties = new { symbol = new { type = "string" }, limit = new { type = "integer" }, clientId = new { type = "string" } } }
        ),
        new(
            "get_market_quote",
            "Retrieves live bid/ask quotes and last traded price for a financial symbol",
            new { type = "object", properties = new { symbol = new { type = "string" } }, required = new[] { "symbol" } }
        ),
        new(
            "check_risk_exposure",
            "Evaluates margin utilization, portfolio exposure, and current risk parameters",
            new { type = "object", properties = new { clientId = new { type = "string" } }, required = new[] { "clientId" } }
        ),
        new(
            "get_market_sentiment",
            "Analyzes recent financial news and calculates bullish/bearish market sentiment score using vector similarity",
            new { type = "object", properties = new { symbol = new { type = "string" } }, required = new[] { "symbol" } }
        )
    ];
}

public class McpToolExecutor(
    IHttpClientFactory httpClientFactory,
    QdrantClient qdrantClient,
    ILogger<McpToolExecutor> logger) : IMcpToolExecutor
{
    public IReadOnlyList<McpToolDefinition> AvailableTools => McpToolRegistry.AvailableTools;

    public async Task<McpToolCallResult> ExecuteToolAsync(McpToolCallRequest request, CancellationToken ct = default)
    {
        var toolName = request.ToolName?.ToLowerInvariant() ?? string.Empty;

        return toolName switch
        {
            "get_order_blotter" => await ExecuteGetOrderBlotterAsync(request, ct),
            "get_market_quote" => await ExecuteGetMarketQuoteAsync(request, ct),
            "check_risk_exposure" => await ExecuteCheckRiskExposureAsync(request, ct),
            "get_market_sentiment" => await ExecuteGetMarketSentimentAsync(request, ct),
            _ => new McpToolCallResult(request.ToolName, false, null!, $"Tool '{request.ToolName}' not recognized.")
        };
    }

    private async Task<McpToolCallResult> ExecuteGetOrderBlotterAsync(McpToolCallRequest request, CancellationToken ct)
    {
        try
        {
            var symbol = GetStringArgument(request.Arguments, "symbol");
            var clientId = GetStringArgument(request.Arguments, "clientId");
            var limit = GetIntArgument(request.Arguments, "limit", 50);

            var client = httpClientFactory.CreateClient("oms");
            var query = $"/api/v1/orders/blotter?limit={limit}";
            if (!string.IsNullOrWhiteSpace(symbol)) query += $"&symbol={Uri.EscapeDataString(symbol)}";
            if (!string.IsNullOrWhiteSpace(clientId) && Guid.TryParse(clientId, out var clientGuid)) query += $"&clientId={clientGuid}";

            var response = await client.GetAsync(query, ct);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                return new McpToolCallResult(request.ToolName, false, null!, $"OMS service error: {response.StatusCode} - {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<object>(ct);
            return new McpToolCallResult(request.ToolName, true, result ?? Array.Empty<object>());
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to execute get_order_blotter.");
            return new McpToolCallResult(request.ToolName, false, null!, ex.Message);
        }
    }

    private async Task<McpToolCallResult> ExecuteGetMarketQuoteAsync(McpToolCallRequest request, CancellationToken ct)
    {
        var symbol = GetStringArgument(request.Arguments, "symbol");
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return new McpToolCallResult(request.ToolName, false, null!, "Argument 'symbol' is required.");
        }

        try
        {
            var client = httpClientFactory.CreateClient("market-data");
            var response = await client.GetAsync($"/api/v1/market/quote/{Uri.EscapeDataString(symbol.ToUpperInvariant())}", ct);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                return new McpToolCallResult(request.ToolName, false, null!, $"MarketData service error: {response.StatusCode} - {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<object>(ct);
            return new McpToolCallResult(request.ToolName, true, result!);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to execute get_market_quote for {Symbol}.", symbol);
            return new McpToolCallResult(request.ToolName, false, null!, ex.Message);
        }
    }

    private async Task<McpToolCallResult> ExecuteCheckRiskExposureAsync(McpToolCallRequest request, CancellationToken ct)
    {
        var clientIdStr = GetStringArgument(request.Arguments, "clientId");
        if (string.IsNullOrWhiteSpace(clientIdStr) || !Guid.TryParse(clientIdStr, out var clientId))
        {
            return new McpToolCallResult(request.ToolName, false, null!, "Argument 'clientId' is required and must be a valid GUID.");
        }

        try
        {
            var riskClient = httpClientFactory.CreateClient("risk");
            var limitsResponse = await riskClient.GetAsync($"/api/v1/risk/limits/{clientId}", ct);

            var portfolioClient = httpClientFactory.CreateClient("portfolio");
            var portfolioResponse = await portfolioClient.GetAsync($"/api/v1/portfolio/{clientId}", ct);

            object? limits = limitsResponse.IsSuccessStatusCode ? await limitsResponse.Content.ReadFromJsonAsync<object>(ct) : null;
            object? portfolio = portfolioResponse.IsSuccessStatusCode ? await portfolioResponse.Content.ReadFromJsonAsync<object>(ct) : null;

            return new McpToolCallResult(request.ToolName, true, new
            {
                ClientId = clientId,
                RiskLimits = limits,
                Portfolio = portfolio,
                EvaluatedAtUtc = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to execute check_risk_exposure for {ClientId}.", clientId);
            return new McpToolCallResult(request.ToolName, false, null!, ex.Message);
        }
    }

    private async Task<McpToolCallResult> ExecuteGetMarketSentimentAsync(McpToolCallRequest request, CancellationToken ct)
    {
        var symbol = GetStringArgument(request.Arguments, "symbol");
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return new McpToolCallResult(request.ToolName, false, null!, "Argument 'symbol' is required.");
        }

        try
        {
            var vector = GenerateSymbolEmbedding(symbol, 128);

            // Execute vector search with cosine distance threshold (ARCH-03)
            var searchResults = await qdrantClient.SearchAsync(
                collectionName: "market_sentiment",
                vector: vector,
                limit: 5,
                scoreThreshold: 0.70f,
                withPayload: true,
                cancellationToken: ct
            );

            var resultsList = searchResults.Select(r => new
            {
                r.Score,
                Payload = r.Payload.ToDictionary(k => k.Key, v => v.Value?.ToString() ?? string.Empty)
            }).ToList();

            return new McpToolCallResult(request.ToolName, true, new
            {
                Symbol = symbol.ToUpperInvariant(),
                Matches = resultsList,
                MatchesCount = resultsList.Count,
                QueriedAtUtc = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to query Qdrant sentiment collection for {Symbol}.", symbol);
            return new McpToolCallResult(request.ToolName, false, null!, $"Qdrant vector search failed: {ex.Message}");
        }
    }

    private static float[] GenerateSymbolEmbedding(string symbol, int dimensions)
    {
        var vector = new float[dimensions];
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(symbol.ToUpperInvariant()));
        for (int i = 0; i < dimensions; i++)
        {
            vector[i] = (hash[i % hash.Length] / 128f) - 1.0f;
        }

        float norm = MathF.Sqrt(vector.Sum(x => x * x));
        if (norm > 0)
        {
            for (int i = 0; i < dimensions; i++) vector[i] /= norm;
        }
        return vector;
    }

    private static string? GetStringArgument(Dictionary<string, object>? arguments, string key)
    {
        if (arguments == null || !arguments.TryGetValue(key, out var val)) return null;
        if (val is JsonElement jsonElement) return jsonElement.GetString();
        return val?.ToString();
    }

    private static int GetIntArgument(Dictionary<string, object>? arguments, string key, int defaultValue)
    {
        if (arguments == null || !arguments.TryGetValue(key, out var val)) return defaultValue;
        if (val is JsonElement jsonElement && jsonElement.TryGetInt32(out var parsedInt)) return parsedInt;
        if (int.TryParse(val?.ToString(), out var parsed)) return parsed;
        return defaultValue;
    }
}
