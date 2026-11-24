using Palantir.AuditService.Domain;

namespace Palantir.AuditService.Infrastructure;

public interface IAuditLogRepository
{
    Task<IReadOnlyList<RegulatoryAuditRecord>> QueryLogsAsync(string? symbol, int limit, CancellationToken ct = default);
    Task<RegulatoryAuditRecord> AppendLogAsync(RegulatoryAuditRecord record, CancellationToken ct = default);
}
