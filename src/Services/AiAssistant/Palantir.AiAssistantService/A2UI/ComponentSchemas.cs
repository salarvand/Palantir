namespace Palantir.AiAssistantService.A2UI;

public record TradeProposalCard(
    Guid ProposalId,
    string Symbol,
    string Side,
    decimal Quantity,
    decimal Price,
    string ExecutionStatus,
    double Confidence,
    string ReasoningNote
);

public record RiskGauge(
    decimal CurrentMarginPercent,
    decimal ProjectedMarginPercent,
    string RiskTier,
    bool WithinRiskTolerance
);

public record DepthImpactChart(
    string Symbol,
    decimal MidPrice,
    decimal EstimatedSlippageBps,
    IReadOnlyList<PriceLevelDepth> Levels
);

public record PriceLevelDepth(
    decimal Price,
    decimal CumulativeVolume,
    string LevelType
);
