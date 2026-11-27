using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Palantir.Platform.Auditing;

/// <summary>
/// Bounded-channel implementation of <see cref="IAuditEmitter"/> guaranteeing O(1) non-blocking writes.
/// </summary>
public sealed class ChannelAuditEmitter(
    Channel<AuditEntry> channel,
    ILogger<ChannelAuditEmitter> logger) : IAuditEmitter
{
    public void Emit(AuditEntry entry)
    {
        if (!channel.Writer.TryWrite(entry))
        {
            logger.LogWarning("Audit channel full; dropped audit entry {AuditId} ({EventType})", entry.AuditId, entry.EventType);
        }
    }
}
