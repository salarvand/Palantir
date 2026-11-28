using Microsoft.EntityFrameworkCore;
using Palantir.AuditService.Domain;

namespace Palantir.AuditService.Infrastructure;

public sealed class PostgresAuditLogRepository : IAuditLogRepository
{
    private readonly AuditDbContext _db;

    public PostgresAuditLogRepository(AuditDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<RegulatoryAuditRecord>> QueryLogsAsync(
        string? symbol,
        int limit,
        CancellationToken ct = default,
        string? traceId = null)
    {
        var size = Math.Clamp(limit <= 0 ? 50 : limit, 1, 500);
        IQueryable<AuditRecordEntity> query = _db.AuditRecords.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(symbol))
        {
            var term = symbol.Trim().ToUpperInvariant();
            query = query.Where(x =>
                x.Symbol == term ||
                (x.SearchVector != null && x.SearchVector.Matches(EF.Functions.PlainToTsQuery("english", term))));
        }

        if (!string.IsNullOrWhiteSpace(traceId))
        {
            var normalizedTraceId = traceId.Trim().ToLowerInvariant();
            query = query.Where(x => x.TraceId == normalizedTraceId);
        }

        return await query
            .OrderByDescending(x => x.TimestampUtc)
            .Take(size)
            .Select(x => new RegulatoryAuditRecord(
                x.AuditId,
                x.EventType,
                x.OrderId,
                x.TradeId,
                x.ClientId,
                x.Symbol,
                x.Price,
                x.Quantity,
                x.PayloadJson,
                x.TimestampUtc,
                x.TraceId))
            .ToListAsync(ct);
    }

    public async Task<RegulatoryAuditRecord> AppendLogAsync(RegulatoryAuditRecord record, CancellationToken ct = default)
    {
        var auditId = record.AuditId != Guid.Empty ? record.AuditId : Guid.NewGuid();
        var timestamp = record.TimestampUtc == default ? DateTimeOffset.UtcNow : record.TimestampUtc;
        var normalizedTraceId = string.IsNullOrWhiteSpace(record.TraceId) ? null : record.TraceId.Trim().ToLowerInvariant();

        var entity = new AuditRecordEntity
        {
            AuditId = auditId,
            EventType = record.EventType,
            OrderId = record.OrderId,
            TradeId = record.TradeId,
            ClientId = record.ClientId,
            Symbol = record.Symbol.ToUpperInvariant(),
            Price = record.Price,
            Quantity = record.Quantity,
            PayloadJson = string.IsNullOrWhiteSpace(record.PayloadJson) ? "{}" : record.PayloadJson,
            TimestampUtc = timestamp,
            TraceId = normalizedTraceId
        };

        _db.AuditRecords.Add(entity);
        await _db.SaveChangesAsync(ct);

        return record with
        {
            AuditId = auditId,
            Symbol = entity.Symbol,
            TimestampUtc = timestamp,
            TraceId = normalizedTraceId
        };
    }
}
