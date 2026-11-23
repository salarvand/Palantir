using Palantir.AiAssistantService.A2UI;

namespace Palantir.AiAssistantService.Agents;

public record CopilotProposal(
    string Summary,
    TradeAnalysisResult Analysis,
    RiskGuardVerdict Risk,
    TradeProposalCard ProposalCard,
    RiskGauge RiskGauge
);

public class TradeCopilotCoordinator
{
    private readonly TradeAnalysisAgent _analysisAgent;
    private readonly RiskGuardAgent _riskAgent;
    private readonly ILogger<TradeCopilotCoordinator> _logger;

    public TradeCopilotCoordinator(
        TradeAnalysisAgent analysisAgent,
        RiskGuardAgent riskAgent,
        ILogger<TradeCopilotCoordinator> logger)
    {
        _analysisAgent = analysisAgent;
        _riskAgent = riskAgent;
        _logger = logger;
    }

    public async Task<CopilotProposal> GenerateProposalAsync(
        Guid clientId,
        string symbol,
        string side,
        decimal quantity,
        decimal price,
        decimal bestBid,
        decimal bestAsk,
        decimal bidVolume,
        decimal askVolume,
        decimal currentMarginUsed,
        decimal totalAccountEquity,
        CancellationToken ct = default)
    {
        _logger.LogInformation("TradeCopilotCoordinator orchestrating multi-agent workflow for {Symbol}", symbol);

        var analysisTask = _analysisAgent.AnalyzeSymbolAsync(symbol, bestBid, bestAsk, bidVolume, askVolume, ct);
        var riskTask = _riskAgent.EvaluateRiskAsync(clientId, symbol, side, quantity, price, currentMarginUsed, totalAccountEquity, ct);

        await Task.WhenAll(analysisTask, riskTask);

        var analysis = await analysisTask;
        var risk = await riskTask;

        var proposalCard = new TradeProposalCard(
            Guid.NewGuid(),
            symbol,
            side,
            quantity,
            price,
            risk.IsApproved ? "READY_TO_EXECUTE" : "RISK_BLOCKED",
            analysis.Confidence,
            analysis.Rationale
        );

        var riskGauge = new RiskGauge(
            risk.CurrentMarginUtilization,
            risk.ProjectedMarginUtilization,
            risk.RiskTier,
            risk.IsApproved
        );

        var summary = $"Copilot synthesized proposal for {symbol}: Momentum={analysis.Momentum}, Risk={risk.RiskTier}, Approved={risk.IsApproved}.";

        return new CopilotProposal(summary, analysis, risk, proposalCard, riskGauge);
    }
}
