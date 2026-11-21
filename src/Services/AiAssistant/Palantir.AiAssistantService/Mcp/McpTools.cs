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

public static class McpToolRegistry
{
    public static readonly IReadOnlyList<McpToolDefinition> AvailableTools =
    [
        new(
            "get_order_blotter",
            "Retrieves active and executed trading orders for a specific symbol or client",
            new { type = "object", properties = new { symbol = new { type = "string" }, limit = new { type = "integer" } } }
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
            "Analyzes recent financial news and calculates bullish/bearish market sentiment score",
            new { type = "object", properties = new { symbol = new { type = "string" } }, required = new[] { "symbol" } }
        )
    ];

    public static McpToolCallResult ExecuteTool(McpToolCallRequest request)
    {
        return request.ToolName.ToLowerInvariant() switch
        {
            "get_market_quote" => new McpToolCallResult(
                request.ToolName,
                true,
                new { Symbol = "PAL/USD", Bid = 100.50m, Ask = 100.55m, Last = 100.52m, Spread = 0.05m }
            ),
            "check_risk_exposure" => new McpToolCallResult(
                request.ToolName,
                true,
                new { CashBalance = 100000m, ReservedMargin = 15000m, MarginUtilization = "15.0%", Status = "HEALTHY" }
            ),
            "get_market_sentiment" => new McpToolCallResult(
                request.ToolName,
                true,
                new { Symbol = "PAL/USD", SentimentScore = 0.82, Sentiment = "Bullish", Confidence = 0.91, SourceCount = 14 }
            ),
            "get_order_blotter" => new McpToolCallResult(
                request.ToolName,
                true,
                new[] { new { OrderId = Guid.NewGuid(), Symbol = "PAL/USD", Side = "Buy", Quantity = 500m, Status = "Filled" } }
            ),
            _ => new McpToolCallResult(request.ToolName, false, null!, $"Tool '{request.ToolName}' not recognized.")
        };
    }
}
