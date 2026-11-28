using Wolverine.Attributes;

namespace Palantir.AuditService.IntegrationEvents.Events;

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
