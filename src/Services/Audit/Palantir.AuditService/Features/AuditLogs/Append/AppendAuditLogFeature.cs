using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Palantir.AuditService.Domain;
using Palantir.AuditService.Infrastructure;
using Palantir.Constants;
using Palantir.Platform.Endpoints;
using Palantir.Platform.Observability;
using Palantir.Platform.Validation;
using Palantir.SharedKernel;

namespace Palantir.AuditService.Features.AuditLogs.Append;

public sealed class AuditEventTypeSpecification : ValidationSpecification<RegulatoryAuditRecord>
{
    public override string RuleName => nameof(RegulatoryAuditRecord.EventType);
    public override string ErrorMessage => "EventType is required.";
    public override bool IsSatisfiedBy(RegulatoryAuditRecord record) => !string.IsNullOrWhiteSpace(record.EventType);
}

public sealed class AuditSymbolSpecification : ValidationSpecification<RegulatoryAuditRecord>
{
    public override string RuleName => nameof(RegulatoryAuditRecord.Symbol);
    public override string ErrorMessage => "Symbol is required.";
    public override bool IsSatisfiedBy(RegulatoryAuditRecord record) => !string.IsNullOrWhiteSpace(record.Symbol);
}

public sealed class AppendAuditLogValidator : SpecificationValidator<RegulatoryAuditRecord>
{
    public AppendAuditLogValidator()
    {
        UseSpecification(new AuditEventTypeSpecification());
        UseSpecification(new AuditSymbolSpecification());
    }
}

internal sealed class AppendAuditLogEndpoint : IEndpoint<IResult, RegulatoryAuditRecord, IAuditLogRepository>
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/audit/logs", async (
                RegulatoryAuditRecord record,
                IAuditLogRepository repository,
                CancellationToken ct) => await HandleAsync(record, repository, ct))
            .ProducesPost<RegulatoryAuditRecord>()
            .WithValidation<RegulatoryAuditRecord>()
            .WithTags("Regulatory Audit")
            .WithName(nameof(AppendAuditLogEndpoint))
            .MapToApiVersion(ApiVersioningExtensions.V1)
            .RequireAuthorization(Permissions.AllowViewAuditLogs);
    }

    public async Task<IResult> HandleAsync(
        RegulatoryAuditRecord record,
        IAuditLogRepository repository,
        CancellationToken cancellationToken = default)
    {
        var enriched = string.IsNullOrWhiteSpace(record.TraceId)
            ? record with { TraceId = TraceContextSnapshot.GetCurrentTraceId() }
            : record;

        var persisted = await repository.AppendLogAsync(enriched, cancellationToken);
        return TypedResults.Created($"/api/v1/audit/logs/{persisted.AuditId}", persisted);
    }
}
