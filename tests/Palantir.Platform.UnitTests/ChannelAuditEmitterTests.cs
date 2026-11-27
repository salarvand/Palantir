using System.Threading.Channels;
using Microsoft.Extensions.Logging.Abstractions;
using Palantir.Platform.Auditing;
using Shouldly;
using Xunit;

namespace Palantir.Platform.UnitTests;

public sealed class ChannelAuditEmitterTests
{
    [Fact]
    public async Task Emit_ShouldEnqueueEntryWithoutBlocking()
    {
        var channel = Channel.CreateBounded<AuditEntry>(10);
        var emitter = new ChannelAuditEmitter(channel, NullLogger<ChannelAuditEmitter>.Instance);
        var entry = AuditEntry.Create("OrderSubmitted", "OmsService", "Order", "123", "Create");

        emitter.Emit(entry);

        var read = await channel.Reader.ReadAsync();
        read.AuditId.ShouldBe(entry.AuditId);
        read.EventType.ShouldBe("OrderSubmitted");
    }
}
