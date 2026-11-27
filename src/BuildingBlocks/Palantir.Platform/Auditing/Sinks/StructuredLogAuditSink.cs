using Microsoft.Extensions.Logging;

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
            logger.LogInformation(
                "AUDIT {AuditId} {ServiceName} {EventType} {ResourceType}/{ResourceId} {Action} User={UserId} Correlation={CorrelationId}",
                entry.AuditId,
                entry.ServiceName,
                entry.EventType,
                entry.ResourceType,
                entry.ResourceId,
                entry.Action,
                entry.UserId ?? "anonymous",
                entry.CorrelationId ?? "none");
        }

        return Task.CompletedTask;
    }
}
