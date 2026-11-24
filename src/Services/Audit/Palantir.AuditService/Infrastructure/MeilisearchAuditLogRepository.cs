using Meilisearch;
using Microsoft.Extensions.Logging;
using Palantir.AuditService.Domain;

namespace Palantir.AuditService.Infrastructure;

public class MeilisearchAuditLogRepository : IAuditLogRepository
{
    public const string IndexName = "audit-records-v1";
    private readonly MeilisearchClient _client;
    private readonly ILogger<MeilisearchAuditLogRepository> _logger;

    public MeilisearchAuditLogRepository(MeilisearchClient client, ILogger<MeilisearchAuditLogRepository> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RegulatoryAuditRecord>> QueryLogsAsync(string? symbol, int limit, CancellationToken ct = default)
    {
        var size = limit <= 0 ? 50 : Math.Min(limit, 500);
        var index = _client.Index(IndexName);

        var searchQuery = new SearchQuery
        {
            Limit = size,
            Sort = new[] { "timestampUtc:desc" }
        };

        if (!string.IsNullOrWhiteSpace(symbol))
        {
            searchQuery.Filter = $"symbol = \"{symbol.Trim().ToUpperInvariant()}\"";
        }

        try
        {
            var result = await index.SearchAsync<RegulatoryAuditRecord>(
                string.Empty,
                searchQuery,
                ct);

            return result.Hits.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query audit records from Meilisearch index '{IndexName}'.", IndexName);
            return Array.Empty<RegulatoryAuditRecord>();
        }
    }

    public async Task<RegulatoryAuditRecord> AppendLogAsync(RegulatoryAuditRecord record, CancellationToken ct = default)
    {
        var auditId = record.AuditId != Guid.Empty ? record.AuditId : Guid.NewGuid();
        var adjustedRecord = record with
        {
            AuditId = auditId,
            TimestampUtc = record.TimestampUtc == default ? DateTimeOffset.UtcNow : record.TimestampUtc
        };

        var index = _client.Index(IndexName);
        await index.AddDocumentsAsync(new[] { adjustedRecord }, primaryKey: "auditId", cancellationToken: ct);

        return adjustedRecord;
    }
}
