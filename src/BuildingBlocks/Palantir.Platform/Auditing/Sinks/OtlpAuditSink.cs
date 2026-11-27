using System.Diagnostics;

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
            using var activity = AuditActivitySource.StartActivity("AuditEntry", ActivityKind.Internal);
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
            activity.SetTag("audit.user_id", entry.UserId);
            activity.SetTag("audit.correlation_id", entry.CorrelationId);
        }

        return Task.CompletedTask;
    }
}
