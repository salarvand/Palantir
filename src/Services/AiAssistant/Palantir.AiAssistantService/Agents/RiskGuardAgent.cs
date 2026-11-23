using Microsoft.Extensions.AI;

namespace Palantir.AiAssistantService.Agents;

public record RiskGuardVerdict(
    bool IsApproved,
    decimal CurrentMarginUtilization,
    decimal ProjectedMarginUtilization,
    string RiskTier,
    IReadOnlyList<string> InvariantViolations,
    string Notes
);

public class RiskGuardAgent
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<RiskGuardAgent> _logger;

    public RiskGuardAgent(IChatClient chatClient, ILogger<RiskGuardAgent> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
    }

    public async Task<RiskGuardVerdict> EvaluateRiskAsync(
        Guid clientId,
        string symbol,
        string side,
        decimal quantity,
        decimal price,
        decimal currentMarginUsed,
        decimal totalAccountEquity,
        CancellationToken ct = default)
    {
        _logger.LogInformation("RiskGuardAgent evaluating pre-trade risk for Client {ClientId}, Symbol {Symbol}", clientId, symbol);

        var orderNotional = quantity * price;
        var requiredOrderMargin = orderNotional * 0.1m; // 10% leverage requirement
        var projectedMarginUsed = currentMarginUsed + requiredOrderMargin;
        var currentUtilization = totalAccountEquity > 0 ? (currentMarginUsed / totalAccountEquity) * 100m : 100m;
        var projectedUtilization = totalAccountEquity > 0 ? (projectedMarginUsed / totalAccountEquity) * 100m : 100m;

        var violations = new List<string>();
        if (projectedUtilization > 85.0m)
        {
            violations.Add($"Projected margin utilization ({projectedUtilization:F2}%) exceeds maximum safe threshold of 85.0%.");
        }

        if (orderNotional > totalAccountEquity * 0.5m)
        {
            violations.Add($"Order notional value (${orderNotional:N2}) exceeds single-order concentration limit of 50% equity.");
        }

        var isApproved = violations.Count == 0;
        var riskTier = projectedUtilization > 80m ? "HIGH" : projectedUtilization > 50m ? "MEDIUM" : "LOW";

        var prompt = $@"You are RiskGuardAgent enforcing trading governance.
Order: {side} {quantity} {symbol} @ {price}
Current Margin: {currentUtilization:F1}%
Projected Margin: {projectedUtilization:F1}%
Status: {(isApproved ? "PASS" : "REJECT")}";

        var response = await _chatClient.GetResponseAsync(
            [new ChatMessage(ChatRole.User, prompt)],
            cancellationToken: ct
        );

        return new RiskGuardVerdict(
            isApproved,
            currentUtilization,
            projectedUtilization,
            riskTier,
            violations,
            response.Messages.FirstOrDefault()?.Text ?? (isApproved ? "Risk bounds approved." : "Pre-trade risk invariant violated.")
        );
    }
}
