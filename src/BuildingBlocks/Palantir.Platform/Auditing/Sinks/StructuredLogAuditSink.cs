using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Palantir.Platform.Observability;

namespace Palantir.Platform.Auditing.Sinks;

/// <summary>
/// Emits audit entries as structured log events for file/stdout log collectors.
/// </summary>
public sealed class StructuredLogAuditSink(ILogger<StructuredLogAuditSink> logger) : IAuditSink
{
    public string Name => "StructuredLog";

    public Task FlushAsync(IReadOnlyList<AuditEntry> batch, CancellationToken ct)
    {
        foreach (var entry in batch)
        {
            using var traceScope = StartTraceScope(entry);

            logger.LogInformation(
                "AUDIT {AuditId} {ServiceName} {EventType} {ResourceType}/{ResourceId} {Action} User={UserId} Correlation={CorrelationId} TraceId={TraceId}",
                entry.AuditId,
                entry.ServiceName,
                entry.EventType,
                entry.ResourceType,
                entry.ResourceId,
                entry.Action,
                entry.UserId,
                entry.CorrelationId,
                entry.TraceId);
        }

        return Task.CompletedTask;
    }

    private static Activity? StartTraceScope(AuditEntry entry)
    {
        if (!TraceContextSnapshot.TryParseActivityContext(entry.TraceId, entry.SpanId, entry.TraceFlags, out var parentContext))
        {
            return null;
        }

        var activity = new Activity("AuditLogScope");
        activity.SetParentId(parentContext.TraceId, parentContext.SpanId, parentContext.TraceFlags);
        activity.Start();
        return activity;
    }
}
