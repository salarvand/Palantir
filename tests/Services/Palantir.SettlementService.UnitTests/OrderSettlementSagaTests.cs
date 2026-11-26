using Palantir.SettlementService.IntegrationEvents.Events;
using Palantir.SettlementService.Sagas;
using Shouldly;
using Xunit;

namespace Palantir.SettlementService.UnitTests;

public sealed class OrderSettlementSagaTests
{
    [Fact]
    public void Saga_HappyPath_ShouldTransitionToSettledAndEmitAuditEvent()
    {
        var orderId = Guid.NewGuid();
        var clientId = Guid.NewGuid();

        var (saga, riskCheck) = OrderSettlementSaga.Start(
            new InitiateOrderSettlementCommand(orderId, clientId, "PAL/USD", 100m, 150m));

        saga.CurrentState.ShouldBe("Submitted");
        riskCheck.OrderId.ShouldBe(orderId);

        saga.Handle(new PreTradeRiskApprovedEvent(orderId, 1500m));
        saga.CurrentState.ShouldBe("RiskApproved");

        var (confirm, audit) = saga.Handle(new TradeExecutedEvent(orderId, Guid.NewGuid(), 100m, 150m));
        saga.CurrentState.ShouldBe("Settled");
        confirm.ShouldNotBeNull();
        audit.ShouldNotBeNull();
        audit.EventType.ShouldBe("SettlementCompleted");
    }

    [Fact]
    public void Saga_Failure_ShouldCompensateReservedMargin()
    {
        var orderId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var (saga, _) = OrderSettlementSaga.Start(
            new InitiateOrderSettlementCommand(orderId, clientId, "PAL/USD", 100m, 150m));
        saga.Handle(new PreTradeRiskApprovedEvent(orderId, 1500m));

        var (release, audit) = saga.Handle(new OrderSettlementFailedEvent(orderId, "Clearing rejected"));

        saga.CurrentState.ShouldBe("Failed");
        release.ShouldNotBeNull();
        release.Amount.ShouldBe(1500m);
        audit.ShouldNotBeNull();
        audit.EventType.ShouldBe("SettlementFailed");
    }
}
