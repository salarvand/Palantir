using Microsoft.Extensions.AI;

namespace Palantir.AiAssistantService.Agents;

public record TradeAnalysisResult(
    string Symbol,
    string Momentum,
    decimal Spread,
    decimal ImbalanceRatio,
    string Recommendation,
    double Confidence,
    string Rationale
);

public class TradeAnalysisAgent
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<TradeAnalysisAgent> _logger;

    public TradeAnalysisAgent(IChatClient chatClient, ILogger<TradeAnalysisAgent> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
    }

    public async Task<TradeAnalysisResult> AnalyzeSymbolAsync(
        string symbol,
        decimal bestBid,
        decimal bestAsk,
        decimal bidVolume,
        decimal askVolume,
        CancellationToken ct = default)
    {
        _logger.LogInformation("TradeAnalysisAgent executing depth and momentum analysis for {Symbol}", symbol);

        var spread = bestAsk - bestBid;
        var totalVolume = bidVolume + askVolume;
        var imbalanceRatio = totalVolume > 0 ? (bidVolume - askVolume) / totalVolume : 0m;

        var prompt = $@"You are the TradeAnalysisAgent for Palantir Trading Platform.
Analyze the following market condition:
Symbol: {symbol}
Best Bid: {bestBid}
Best Ask: {bestAsk}
Spread: {spread}
Bid Volume: {bidVolume}
Ask Volume: {askVolume}
Order Book Imbalance: {imbalanceRatio:P2}

Provide concise market analysis and indicate direction (BULLISH, BEARISH, NEUTRAL).";

        var response = await _chatClient.GetResponseAsync(
            [new ChatMessage(ChatRole.User, prompt)],
            cancellationToken: ct
        );

        var momentum = imbalanceRatio > 0.15m ? "BULLISH" : imbalanceRatio < -0.15m ? "BEARISH" : "NEUTRAL";
        var recommendation = momentum == "BULLISH" ? "ACCUMULATE" : momentum == "BEARISH" ? "REDUCE" : "HOLD";
        var confidence = Math.Min(0.95, 0.5 + (double)Math.Abs(imbalanceRatio) * 0.4);

        return new TradeAnalysisResult(
            symbol,
            momentum,
            spread,
            imbalanceRatio,
            recommendation,
            confidence,
            response.Messages.FirstOrDefault()?.Text ?? "Automated momentum and depth evaluation completed."
        );
    }
}
