namespace Palantir.Platform.Auditing;

/// <summary>
/// Pluggable destination sink for batched audit entries.
/// </summary>
public interface IAuditSink
{
    string Name { get; }
    Task FlushAsync(IReadOnlyList<AuditEntry> batch, CancellationToken ct);
}
