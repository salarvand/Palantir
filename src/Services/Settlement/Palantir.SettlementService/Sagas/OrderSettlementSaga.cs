using System.Text.Json;
using Palantir.Platform.Observability;
using Palantir.SettlementService.IntegrationEvents.Events;
using Wolverine;

namespace Palantir.SettlementService.Sagas;

public class OrderSettlementSaga : Saga
{
    public Guid Id { get; set; }
    public string CurrentState { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public Guid ClientId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal ReservedMargin { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? SettledAtUtc { get; set; }

    public static (OrderSettlementSaga, CheckPreTradeRiskCommand) Start(InitiateOrderSettlementCommand command)
    {
        var saga = new OrderSettlementSaga
        {
            Id = command.OrderId,
            OrderId = command.OrderId,
            ClientId = command.ClientId,
            Symbol = command.Symbol,
            Quantity = command.Quantity,
            Price = command.Price,
            CurrentState = "Submitted",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var riskCheck = new CheckPreTradeRiskCommand(
            command.OrderId,
            command.ClientId,
            command.Symbol,
            command.Quantity,
            command.Price);

        return (saga, riskCheck);
    }

    public void Handle(PreTradeRiskApprovedEvent evt)
    {
        if (CurrentState is "Settled" or "Failed") return;

        ReservedMargin = evt.RequiredMargin;
        CurrentState = "RiskApproved";
    }

    public (ConfirmSettlementEvent?, AuditTrailEvent?) Handle(TradeExecutedEvent evt)
    {
        if (CurrentState is "Settled" or "Failed")
        {
            return (null, null);
        }

        var now = DateTimeOffset.UtcNow;
        SettledAtUtc = now;
        CurrentState = "Settled";
        MarkCompleted();

        var confirm = new ConfirmSettlementEvent(OrderId, now);
        var audit = CreateAuditEvent("SettlementCompleted", evt.TradeId, evt.ExecutionPrice, evt.FilledQuantity);
        return (confirm, audit);
    }

    public (ReleaseReservedMarginCommand?, AuditTrailEvent?) Handle(OrderSettlementFailedEvent evt)
    {
        if (CurrentState is "Settled" or "Failed")
        {
            return (null, null);
        }

        FailureReason = evt.Reason;
        CurrentState = "Failed";
        MarkCompleted();

        var release = new ReleaseReservedMarginCommand(OrderId, ClientId, ReservedMargin, evt.Reason);
        var audit = CreateAuditEvent("SettlementFailed", null, Price, Quantity);
        return (release, audit);
    }

    private AuditTrailEvent CreateAuditEvent(string eventType, Guid? tradeId, decimal price, decimal quantity) =>
        new(
            eventType,
            OrderId,
            tradeId,
            ClientId,
            Symbol,
            price,
            quantity,
            JsonSerializer.Serialize(new { OrderId, CurrentState, ReservedMargin, FailureReason }),
            DateTimeOffset.UtcNow,
            TraceContextSnapshot.GetCurrentTraceId());
}
