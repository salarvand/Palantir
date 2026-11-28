using Palantir.AuditService.Domain;
using Palantir.AuditService.Handlers;
using Palantir.AuditService.Infrastructure;
using Palantir.AuditService.IntegrationEvents.Events;
using Palantir.Platform.Auditing;
using Shouldly;
using Xunit;

namespace Palantir.AuditService.UnitTests;

public sealed class AuditTrailEventHandlerTests
{
    [Fact]
    public async Task Handle_ShouldPropagateTraceIdToAuditEntryAndRegulatoryRecord()
    {
        var repository = new StubAuditLogRepository();
        var emitter = new StubAuditEmitter();
        var handler = new AuditTrailEventHandler(repository, emitter);

        const string expectedTraceId = "4bf92f3577b34da6a3ce929d0e0e4736";
        var evt = new AuditTrailEvent(
            "OrderSubmitted",
            Guid.NewGuid(),
            null,
            Guid.NewGuid(),
            "PAL/USD",
            150m,
            10m,
            "{}",
            DateTimeOffset.UtcNow,
            expectedTraceId);

        await handler.Handle(evt, CancellationToken.None);

        emitter.Emitted.Count.ShouldBe(1);
        emitter.Emitted[0].TraceId.ShouldBe(expectedTraceId);
        repository.Appended.Count.ShouldBe(1);
        repository.Appended[0].TraceId.ShouldBe(expectedTraceId);
    }

    private sealed class StubAuditEmitter : IAuditEmitter
    {
        public List<AuditEntry> Emitted { get; } = [];
        public void Emit(AuditEntry entry) => Emitted.Add(entry);
    }

    private sealed class StubAuditLogRepository : IAuditLogRepository
    {
        public List<RegulatoryAuditRecord> Appended { get; } = [];

        public Task<IReadOnlyList<RegulatoryAuditRecord>> QueryLogsAsync(
            string? symbol,
            int limit,
            CancellationToken ct = default,
            string? traceId = null) =>
            Task.FromResult<IReadOnlyList<RegulatoryAuditRecord>>(Appended);

        public Task<RegulatoryAuditRecord> AppendLogAsync(RegulatoryAuditRecord record, CancellationToken ct = default)
        {
            Appended.Add(record);
            return Task.FromResult(record);
        }
    }
}
