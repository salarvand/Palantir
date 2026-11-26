using System.ComponentModel;
using ModelContextProtocol.Server;
using Palantir.AiAssistantService.Governance;

namespace Palantir.AiAssistantService.Mcp;

[McpServerToolType]
public sealed class TradingMcpTools(IMcpToolExecutor executor, AgentGovernanceManager governance)
{
    private const string DefaultAgentId = "trade-copilot-coordinator";

    [McpServerTool(Name = "get_order_blotter"), Description("Retrieves active and executed trading orders for a symbol or client.")]
    public Task<McpToolCallResult> GetOrderBlotterAsync(string? symbol = null, string? clientId = null, int limit = 50, CancellationToken ct = default) =>
        ExecuteGovernedAsync("get_order_blotter", new Dictionary<string, object>
        {
            ["symbol"] = symbol ?? string.Empty,
            ["clientId"] = clientId ?? string.Empty,
            ["limit"] = limit
        }, ct);

    [McpServerTool(Name = "get_market_quote"), Description("Retrieves live bid/ask quotes and last traded price for a financial symbol.")]
    public Task<McpToolCallResult> GetMarketQuoteAsync(string symbol, CancellationToken ct = default) =>
        ExecuteGovernedAsync("get_market_quote", new Dictionary<string, object> { ["symbol"] = symbol }, ct);

    [McpServerTool(Name = "check_risk_exposure"), Description("Evaluates margin utilization, portfolio exposure, and risk limits.")]
    public Task<McpToolCallResult> CheckRiskExposureAsync(string clientId, CancellationToken ct = default) =>
        ExecuteGovernedAsync("check_risk_exposure", new Dictionary<string, object> { ["clientId"] = clientId }, ct);

    [McpServerTool(Name = "get_market_sentiment"), Description("Analyzes financial news sentiment using PostgreSQL pgvector similarity.")]
    public Task<McpToolCallResult> GetMarketSentimentAsync(string symbol, CancellationToken ct = default) =>
        ExecuteGovernedAsync("get_market_sentiment", new Dictionary<string, object> { ["symbol"] = symbol }, ct);

    private async Task<McpToolCallResult> ExecuteGovernedAsync(string toolName, Dictionary<string, object> args, CancellationToken ct)
    {
        if (!governance.IsToolAllowed(DefaultAgentId, toolName, out var policyError))
        {
            return new McpToolCallResult(toolName, false, null!, policyError);
        }

        if (!governance.TryAcquireExecutionBudget(out var budgetError))
        {
            return new McpToolCallResult(toolName, false, null!, budgetError);
        }

        return await executor.ExecuteToolAsync(new McpToolCallRequest(toolName, args), ct);
    }
}
