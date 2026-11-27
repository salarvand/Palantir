using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Palantir.Platform.Auditing;

/// <summary>
/// Background worker draining the bounded audit channel in batches and dispatching to all active sinks.
/// </summary>
public sealed class AuditPipelineService(
    Channel<AuditEntry> channel,
    IEnumerable<IAuditSink> sinks,
    ILogger<AuditPipelineService> logger) : BackgroundService
{
    private const int MaxBatchSize = 100;
    private readonly IReadOnlyList<IAuditSink> _sinks = sinks.ToList();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var batch = new List<AuditEntry>(MaxBatchSize);

        await foreach (var entry in channel.Reader.ReadAllAsync(stoppingToken))
        {
            batch.Add(entry);
            while (batch.Count < MaxBatchSize && channel.Reader.TryRead(out var next))
            {
                batch.Add(next);
            }

            await DispatchBatchAsync(batch, stoppingToken);
            batch.Clear();
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        channel.Writer.TryComplete();
        await base.StopAsync(cancellationToken);

        var remaining = new List<AuditEntry>(MaxBatchSize);
        while (channel.Reader.TryRead(out var entry))
        {
            remaining.Add(entry);
            if (remaining.Count >= MaxBatchSize)
            {
                await DispatchBatchAsync(remaining, cancellationToken);
                remaining.Clear();
            }
        }

        if (remaining.Count > 0)
        {
            await DispatchBatchAsync(remaining, cancellationToken);
        }
    }

    private async Task DispatchBatchAsync(IReadOnlyList<AuditEntry> batch, CancellationToken ct)
    {
        if (_sinks.Count == 0 || batch.Count == 0)
        {
            return;
        }

        await Task.WhenAll(_sinks.Select(sink => FlushSinkSafeAsync(sink, batch, ct)));
    }

    private async Task FlushSinkSafeAsync(IAuditSink sink, IReadOnlyList<AuditEntry> batch, CancellationToken ct)
    {
        try
        {
            await sink.FlushAsync(batch, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Audit sink '{Sink}' failed to flush {Count} entries.", sink.Name, batch.Count);
        }
    }
}
