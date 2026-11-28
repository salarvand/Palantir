using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Extensions.Logging.Abstractions;
using Palantir.Platform.Auditing;
using Palantir.Platform.Observability;
using Shouldly;
using Xunit;

namespace Palantir.Platform.UnitTests;

public sealed class ChannelAuditEmitterTests
{
    [Fact]
    public async Task Emit_ShouldEnqueueEntryWithoutBlocking()
    {
        var channel = Channel.CreateBounded<AuditEntry>(10);
        var emitter = new ChannelAuditEmitter(
            channel,
            new ActivityTraceContextAccessor(),
            NullLogger<ChannelAuditEmitter>.Instance);
        var entry = AuditEntry.Create("OrderSubmitted", "OmsService", "Order", "123", "Create");

        emitter.Emit(entry);

        var read = await channel.Reader.ReadAsync();
        read.AuditId.ShouldBe(entry.AuditId);
        read.EventType.ShouldBe("OrderSubmitted");
    }

    [Fact]
    public async Task Emit_WithActiveActivity_ShouldCaptureTraceIdAndSpanId()
    {
        var channel = Channel.CreateBounded<AuditEntry>(10);
        var emitter = new ChannelAuditEmitter(
            channel,
            new ActivityTraceContextAccessor(),
            NullLogger<ChannelAuditEmitter>.Instance);

        using var activity = new Activity("TestOperation").Start();
        var entry = AuditEntry.Create("OrderSubmitted", "OmsService", "Order", "456", "Create");

        emitter.Emit(entry);

        var read = await channel.Reader.ReadAsync();
        read.TraceId.ShouldBe(activity.TraceId.ToHexString());
        read.SpanId.ShouldBe(activity.SpanId.ToHexString());
        read.TraceFlags.ShouldBe((byte)activity.ActivityTraceFlags);
    }
}
