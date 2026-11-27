namespace Palantir.Platform.Auditing;

/// <summary>
/// Immutable audit record emitted across service boundaries via the non-blocking audit channel.
/// </summary>
public sealed record AuditEntry(
    Guid AuditId,
    string EventType,
    string ServiceName,
    string? UserId,
    string? CorrelationId,
    string ResourceType,
    string ResourceId,
    string Action,
    IReadOnlyDictionary<string, object?> Payload,
    DateTimeOffset TimestampUtc)
{
    public static AuditEntry Create(
        string eventType,
        string serviceName,
        string resourceType,
        string resourceId,
        string action,
        IReadOnlyDictionary<string, object?>? payload = null,
        string? userId = null,
        string? correlationId = null) =>
        new(
            Guid.NewGuid(),
            eventType,
            serviceName,
            userId,
            correlationId,
            resourceType,
            resourceId,
            action,
            payload ?? new Dictionary<string, object?>(),
            DateTimeOffset.UtcNow);
}
