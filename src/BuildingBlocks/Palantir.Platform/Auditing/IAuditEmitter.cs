namespace Palantir.Platform.Auditing;

/// <summary>
/// Fire-and-forget audit producer contract. Implementations must never block caller threads.
/// </summary>
public interface IAuditEmitter
{
    void Emit(AuditEntry entry);
}
