using Palantir.RiskService.Engine;
using Palantir.SharedKernel;
using Shouldly;
using Xunit;

namespace Palantir.RiskService.UnitTests;

public sealed class RiskEngineTests
{
    private readonly RiskEngine _engine = new();

    [Fact]
    public void EvaluatePreTradeRisk_WithinLimits_ShouldApprove()
    {
        var request = new RiskEvaluationRequest(
            Guid.NewGuid(),
            "PAL/USD",
            TradeSide.Buy,
            100m,
            150m,
            50_000m);

        var result = _engine.EvaluatePreTradeRisk(request);

        result.Approved.ShouldBeTrue();
        result.RequiredMargin.ShouldBe(1_500m);
        result.RejectionReason.ShouldBeNull();
    }

    [Fact]
    public void EvaluatePreTradeRisk_ExceedingMargin_ShouldReject()
    {
        var request = new RiskEvaluationRequest(
            Guid.NewGuid(),
            "PAL/USD",
            TradeSide.Buy,
            1_000m,
            150m,
            1_000m);

        var result = _engine.EvaluatePreTradeRisk(request);

        result.Approved.ShouldBeFalse();
        result.RejectionReason.ShouldNotBeNullOrWhiteSpace();
    }
}
