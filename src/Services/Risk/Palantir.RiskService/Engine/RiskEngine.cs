using Palantir.SharedKernel;

namespace Palantir.RiskService.Engine;

public record RiskEvaluationRequest(
    Guid ClientId,
    string Symbol,
    TradeSide Side,
    decimal Quantity,
    decimal Price,
    decimal AvailableMargin
);

public record RiskEvaluationResult(
    bool Approved,
    string? RejectionReason,
    decimal RequiredMargin
);

public class RiskEngine
{
    private const decimal MaxSingleOrderValue = 1000000m;
    private const decimal MarginRequirementRate = 0.10m; // 10% initial margin

    public RiskEvaluationResult EvaluatePreTradeRisk(RiskEvaluationRequest request)
    {
        var notionalValue = request.Quantity * request.Price;

        if (notionalValue > MaxSingleOrderValue)
        {
            return new RiskEvaluationResult(false, $"Single order notional value (${notionalValue:N2}) exceeds max threshold (${MaxSingleOrderValue:N2}).", 0m);
        }

        var requiredMargin = notionalValue * MarginRequirementRate;
        if (requiredMargin > request.AvailableMargin)
        {
            return new RiskEvaluationResult(false, $"Insufficient available margin (${request.AvailableMargin:N2}). Required: ${requiredMargin:N2}.", requiredMargin);
        }

        return new RiskEvaluationResult(true, null, requiredMargin);
    }
}
