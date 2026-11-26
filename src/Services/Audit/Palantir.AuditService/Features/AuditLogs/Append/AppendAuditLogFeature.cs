using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Palantir.AuditService.Domain;
using Palantir.AuditService.Infrastructure;
using Palantir.Constants;
using Palantir.Platform.Endpoints;
using Palantir.Platform.Validation;

namespace Palantir.AuditService.Features.AuditLogs.Append;

public sealed class AppendAuditLogValidator : AbstractValidator<RegulatoryAuditRecord>
{
    public AppendAuditLogValidator()
    {
        RuleFor(x => x.EventType).NotEmpty();
        RuleFor(x => x.Symbol).NotEmpty();
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
        var persisted = await repository.AppendLogAsync(record, cancellationToken);
        return TypedResults.Created($"/api/v1/audit/logs/{persisted.AuditId}", persisted);
    }
}
