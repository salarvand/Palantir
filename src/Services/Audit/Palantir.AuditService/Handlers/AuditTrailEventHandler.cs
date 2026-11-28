using Palantir.AuditService.Domain;
using Palantir.AuditService.Infrastructure;
using Palantir.AuditService.IntegrationEvents.Events;
using Palantir.Platform.Auditing;
using Palantir.Platform.Observability;

namespace Palantir.AuditService.Handlers;

public sealed class AuditTrailEventHandler(
    IAuditLogRepository repository,
    IAuditEmitter auditEmitter)
{
    public async Task Handle(AuditTrailEvent evt, CancellationToken ct)
    {
        var traceId = !string.IsNullOrWhiteSpace(evt.TraceId)
            ? evt.TraceId
            : TraceContextSnapshot.GetCurrentTraceId();

        auditEmitter.Emit(AuditEntry.Create(
            evt.EventType,
            "AuditService",
            "Order",
            evt.OrderId.ToString(),
            evt.EventType,
            new Dictionary<string, object?>
            {
                ["Symbol"] = evt.Symbol,
                ["Price"] = evt.Price,
                ["Quantity"] = evt.Quantity,
                ["TradeId"] = evt.TradeId
            },
            evt.ClientId.ToString(),
            traceId: traceId));

        await repository.AppendLogAsync(new RegulatoryAuditRecord(
            Guid.NewGuid(),
            evt.EventType,
            evt.OrderId,
            evt.TradeId,
            evt.ClientId,
            evt.Symbol,
            evt.Price,
            evt.Quantity,
            evt.PayloadJson,
            evt.TimestampUtc,
            traceId), ct);
    }
}
