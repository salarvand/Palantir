using Palantir.AuditService.Domain;
using Palantir.AuditService.Features.AuditLogs.Append;
using Shouldly;
using Xunit;

namespace Palantir.AuditService.UnitTests;

public sealed class AppendAuditLogValidatorTests
{
    private readonly AppendAuditLogValidator _validator = new();

    [Fact]
    public void Validate_ValidAuditRecord_ShouldPass()
    {
        var record = new RegulatoryAuditRecord(
            Guid.NewGuid(),
            "OrderSubmitted",
            Guid.NewGuid(),
            null,
            Guid.NewGuid(),
            "PAL/USD",
            150m,
            100m,
            "{}",
            DateTimeOffset.UtcNow);

        _validator.Validate(record).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_MissingEventType_ShouldFail()
    {
        var record = new RegulatoryAuditRecord(
            Guid.NewGuid(),
            string.Empty,
            Guid.NewGuid(),
            null,
            Guid.NewGuid(),
            "PAL/USD",
            150m,
            100m,
            "{}",
            DateTimeOffset.UtcNow);

        _validator.Validate(record).IsValid.ShouldBeFalse();
    }
}
