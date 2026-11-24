using Meilisearch;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Palantir.AuditService.Infrastructure;

public class MeilisearchIndexInitializer : IHostedService
{
    private readonly MeilisearchClient _client;
    private readonly ILogger<MeilisearchIndexInitializer> _logger;

    public MeilisearchIndexInitializer(MeilisearchClient client, ILogger<MeilisearchIndexInitializer> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var index = _client.Index(MeilisearchAuditLogRepository.IndexName);
            try
            {
                await _client.GetIndexAsync(MeilisearchAuditLogRepository.IndexName, cancellationToken);
            }
            catch
            {
                await _client.CreateIndexAsync(MeilisearchAuditLogRepository.IndexName, primaryKey: "auditId", cancellationToken);
            }

            await index.UpdateFilterableAttributesAsync(
                new[] { "symbol", "auditId", "eventType", "clientId", "orderId" },
                cancellationToken);

            await index.UpdateSortableAttributesAsync(
                new[] { "timestampUtc" },
                cancellationToken);

            _logger.LogInformation("Meilisearch index '{IndexName}' initialized successfully.", MeilisearchAuditLogRepository.IndexName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not initialize Meilisearch index '{IndexName}'. It may be created on first write.", MeilisearchAuditLogRepository.IndexName);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
