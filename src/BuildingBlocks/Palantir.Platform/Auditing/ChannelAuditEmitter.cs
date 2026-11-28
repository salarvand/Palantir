using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Palantir.Platform.Observability;

namespace Palantir.Platform.Auditing;

/// <summary>
/// Bounded-channel implementation of <see cref="IAuditEmitter"/> guaranteeing O(1) non-blocking writes.
/// </summary>
public sealed class ChannelAuditEmitter(
    Channel<AuditEntry> channel,
    ITraceContextAccessor traceContextAccessor,
    ILogger<ChannelAuditEmitter> logger) : IAuditEmitter
{
    public void Emit(AuditEntry entry)
    {
        var trace = traceContextAccessor.Capture();
        var enriched = entry with
        {
            CorrelationId = entry.CorrelationId ?? trace.CorrelationId,
            TraceId = entry.TraceId ?? trace.TraceId,
            SpanId = entry.SpanId ?? trace.SpanId,
            TraceFlags = entry.TraceId is null ? trace.TraceFlags : entry.TraceFlags
        };

        if (!channel.Writer.TryWrite(enriched))
        {
            logger.LogWarning("Audit channel full; dropped audit entry {AuditId} ({EventType})", enriched.AuditId, enriched.EventType);
        }
    }
}
