using Wolverine.Attributes;

namespace Palantir.OmsService.IntegrationEvents.Events;

[MessageIdentity("palantir.initiate-order-settlement")]
public sealed record InitiateOrderSettlementCommand(
    Guid OrderId,
    Guid ClientId,
    string Symbol,
    decimal Quantity,
    decimal Price);

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
