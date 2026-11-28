using Palantir.AuditService.Domain;

namespace Palantir.AuditService.Infrastructure;

public interface IAuditLogRepository
{
    Task<IReadOnlyList<RegulatoryAuditRecord>> QueryLogsAsync(
        string? symbol,
        int limit,
        CancellationToken ct = default,
        string? traceId = null);
    Task<RegulatoryAuditRecord> AppendLogAsync(RegulatoryAuditRecord record, CancellationToken ct = default);
}
