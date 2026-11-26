using Palantir.AuditService.Domain;
using Palantir.AuditService.Infrastructure;
using Palantir.AuditService.IntegrationEvents.Events;

namespace Palantir.AuditService.Handlers;

public sealed class AuditTrailEventHandler
{
    private readonly IAuditLogRepository _repository;

    public AuditTrailEventHandler(IAuditLogRepository repository)
    {
        _repository = repository;
    }

    public Task Handle(AuditTrailEvent evt, CancellationToken ct) =>
        _repository.AppendLogAsync(new RegulatoryAuditRecord(
            Guid.NewGuid(),
            evt.EventType,
            evt.OrderId,
            evt.TradeId,
            evt.ClientId,
            evt.Symbol,
            evt.Price,
            evt.Quantity,
            evt.PayloadJson,
            evt.TimestampUtc), ct);
}
