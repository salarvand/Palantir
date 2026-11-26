using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Palantir.AuditService.Domain;
using Palantir.AuditService.Infrastructure;
using Palantir.Constants;
using Palantir.Platform.Endpoints;

namespace Palantir.AuditService.Features.AuditLogs.List;

internal sealed class ListAuditLogsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/audit/logs", async (
                string? symbol,
                int? limit,
                IAuditLogRepository repository,
                CancellationToken ct) =>
            {
                var logs = await repository.QueryLogsAsync(symbol, Math.Clamp(limit ?? 50, 1, 500), ct);
                return TypedResults.Ok(logs);
            })
            .ProducesGet<IReadOnlyList<RegulatoryAuditRecord>>()
            .WithTags("Regulatory Audit")
            .WithName(nameof(ListAuditLogsEndpoint))
            .MapToApiVersion(ApiVersioningExtensions.V1)
            .RequireAuthorization(Permissions.AllowViewAuditLogs);
    }
}
