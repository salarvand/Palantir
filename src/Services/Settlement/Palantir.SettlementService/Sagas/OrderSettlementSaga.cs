using MassTransit;
using Palantir.Oms.Contracts;

namespace Palantir.SettlementService.Sagas;

public class OrderSettlementState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
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
    public byte[]? RowVersion { get; set; }
}

public record OrderSettlementFailedEvent(
    Guid OrderId,
    string Reason
) : CorrelatedBy<Guid>
{
    public Guid CorrelationId => OrderId;
}

public class OrderSettlementStateMachine : MassTransitStateMachine<OrderSettlementState>
{
    public State Submitted { get; private set; } = null!;
    public State RiskApproved { get; private set; } = null!;
    public State ExecutionMatched { get; private set; } = null!;
    public State Settled { get; private set; } = null!;
    public State Compensating { get; private set; } = null!;
    public State Failed { get; private set; } = null!;

    public Event<InitiateOrderSettlementCommand> OrderInitiated { get; private set; } = null!;
    public Event<PreTradeRiskApprovedEvent> RiskApprovedEvent { get; private set; } = null!;
    public Event<TradeExecutedEvent> TradeExecuted { get; private set; } = null!;
    public Event<OrderSettlementFailedEvent> OrderFailedOrRejected { get; private set; } = null!;

    public OrderSettlementStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => OrderInitiated, x => x.CorrelateById(c => c.Message.OrderId));
        Event(() => RiskApprovedEvent, x => x.CorrelateById(c => c.Message.OrderId));
        Event(() => TradeExecuted, x => x.CorrelateById(c => c.Message.OrderId));
        Event(() => OrderFailedOrRejected, x => x.CorrelateById(c => c.Message.OrderId));

        Initially(
            When(OrderInitiated)
                .Then(context =>
                {
                    context.Saga.OrderId = context.Message.OrderId;
                    context.Saga.ClientId = context.Message.ClientId;
                    context.Saga.Symbol = context.Message.Symbol;
                    context.Saga.Quantity = context.Message.Quantity;
                    context.Saga.Price = context.Message.Price;
                    context.Saga.CreatedAtUtc = DateTimeOffset.UtcNow;
                })
                .Publish(context => new CheckPreTradeRiskCommand(
                    context.Saga.OrderId,
                    context.Saga.ClientId,
                    context.Saga.Symbol,
                    context.Saga.Quantity,
                    context.Saga.Price
                ))
                .TransitionTo(Submitted)
        );

        During(Submitted,
            When(RiskApprovedEvent)
                .Then(context =>
                {
                    context.Saga.ReservedMargin = context.Message.RequiredMargin;
                })
                .TransitionTo(RiskApproved)
        );

        During(RiskApproved,
            When(TradeExecuted)
                .Then(context =>
                {
                    context.Saga.SettledAtUtc = DateTimeOffset.UtcNow;
                })
                .Publish(context => new ConfirmSettlementEvent(
                    context.Saga.OrderId,
                    DateTimeOffset.UtcNow
                ))
                .TransitionTo(Settled)
                .Finalize()
        );

        DuringAny(
            When(OrderFailedOrRejected)
                .Then(context =>
                {
                    context.Saga.FailureReason = context.Message.Reason;
                })
                .TransitionTo(Compensating)
                .Publish(context => new ReleaseReservedMarginCommand(
                    context.Saga.OrderId,
                    context.Saga.ClientId,
                    context.Saga.ReservedMargin,
                    context.Message.Reason
                ))
                .TransitionTo(Failed)
        );

        SetCompletedWhenFinalized();
    }
}
