using System.Diagnostics;
using Palantir.Platform.Observability;

namespace Palantir.Platform.Auditing.Sinks;

/// <summary>
/// Emits audit entries as OpenTelemetry activities captured by the OTLP exporter.
/// </summary>
public sealed class OtlpAuditSink : IAuditSink
{
    private static readonly ActivitySource AuditActivitySource = new("Palantir.Auditing");

    public string Name => "OTLP";

    public Task FlushAsync(IReadOnlyList<AuditEntry> batch, CancellationToken ct)
    {
        foreach (var entry in batch)
        {
            var hasParent = TraceContextSnapshot.TryParseActivityContext(
                entry.TraceId,
                entry.SpanId,
                entry.TraceFlags,
                out var parentContext);

            using var activity = hasParent
                ? AuditActivitySource.StartActivity("AuditEntry", ActivityKind.Internal, parentContext)
                : AuditActivitySource.StartActivity("AuditEntry", ActivityKind.Internal);
            if (activity is null)
            {
                continue;
            }

            activity.SetTag("audit.id", entry.AuditId.ToString());
            activity.SetTag("audit.event_type", entry.EventType);
            activity.SetTag("audit.service", entry.ServiceName);
            activity.SetTag("audit.resource_type", entry.ResourceType);
            activity.SetTag("audit.resource_id", entry.ResourceId);
            activity.SetTag("audit.action", entry.Action);
            if (!string.IsNullOrWhiteSpace(entry.UserId))
            {
                activity.SetTag("audit.user_id", entry.UserId);
            }
            if (!string.IsNullOrWhiteSpace(entry.CorrelationId))
            {
                activity.SetTag("audit.correlation_id", entry.CorrelationId);
            }
            if (!string.IsNullOrWhiteSpace(entry.TraceId))
            {
                activity.SetTag("audit.trace_id", entry.TraceId);
            }
        }

        return Task.CompletedTask;
    }
}
