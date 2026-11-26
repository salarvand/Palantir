using Wolverine.Attributes;

namespace Palantir.RiskService.IntegrationEvents.Events;

[MessageIdentity("palantir.check-pre-trade-risk")]
public sealed record CheckPreTradeRiskCommand(
    Guid OrderId,
    Guid ClientId,
    string Symbol,
    decimal Quantity,
    decimal Price);

[MessageIdentity("palantir.pre-trade-risk-approved")]
public sealed record PreTradeRiskApprovedEvent(
    Guid OrderId,
    decimal RequiredMargin);

[MessageIdentity("palantir.order-settlement-failed")]
public sealed record OrderSettlementFailedEvent(
    Guid OrderId,
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
    DateTimeOffset TimestampUtc);
