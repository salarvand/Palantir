using Wolverine.Attributes;
using Wolverine.Persistence.Sagas;

namespace Palantir.SettlementService.IntegrationEvents.Events;

[MessageIdentity("palantir.initiate-order-settlement")]
public sealed record InitiateOrderSettlementCommand(
    [property: SagaIdentity] Guid OrderId,
    Guid ClientId,
    string Symbol,
    decimal Quantity,
    decimal Price);

[MessageIdentity("palantir.check-pre-trade-risk")]
public sealed record CheckPreTradeRiskCommand(
    Guid OrderId,
    Guid ClientId,
    string Symbol,
    decimal Quantity,
    decimal Price);

[MessageIdentity("palantir.pre-trade-risk-approved")]
public sealed record PreTradeRiskApprovedEvent(
    [property: SagaIdentity] Guid OrderId,
    decimal RequiredMargin);

[MessageIdentity("palantir.trade-executed")]
public sealed record TradeExecutedEvent(
    [property: SagaIdentity] Guid OrderId,
    Guid TradeId,
    decimal FilledQuantity,
    decimal ExecutionPrice);

[MessageIdentity("palantir.confirm-settlement")]
public sealed record ConfirmSettlementEvent(
    Guid OrderId,
    DateTimeOffset SettledAtUtc);

[MessageIdentity("palantir.order-settlement-failed")]
public sealed record OrderSettlementFailedEvent(
    [property: SagaIdentity] Guid OrderId,
    string Reason);

[MessageIdentity("palantir.release-reserved-margin")]
public sealed record ReleaseReservedMarginCommand(
    Guid OrderId,
    Guid ClientId,
    decimal Amount,
    string Reason);

[MessageIdentity("palantir.audit-trail")]
public sealed record AuditTrailEvent(
    string EventType,
    Guid OrderId,
    Guid? TradeId,
    Guid ClientId,
    string Symbol,
    decimal Price,
    decimal Quantity,
    string PayloadJson,
    DateTimeOffset TimestampUtc,
    string? TraceId = null);
