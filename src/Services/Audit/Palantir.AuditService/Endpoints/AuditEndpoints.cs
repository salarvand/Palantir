using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Palantir.AuditService.Domain;
using Palantir.AuditService.Infrastructure;
using Palantir.Constants;
using Palantir.Platform.Endpoints;

namespace Palantir.AuditService.Endpoints;

public class AuditEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/audit")
            .WithTags("Regulatory Audit");

        group.MapGet("/logs", GetLogsAsync)
            .RequireAuthorization(Permissions.AllowViewAuditLogs);

        group.MapPost("/logs", AppendLogAsync);
    }

    private static async Task<IResult> GetLogsAsync(
        string? symbol,
        int limit,
        IAuditLogRepository repository,
        CancellationToken ct)
    {
        var logs = await repository.QueryLogsAsync(symbol, limit, ct);
        return Results.Ok(logs);
    }

    private static async Task<IResult> AppendLogAsync(
        RegulatoryAuditRecord record,
        IAuditLogRepository repository,
        CancellationToken ct)
    {
        var persistedRecord = await repository.AppendLogAsync(record, ct);
        return Results.Created($"/api/v1/audit/logs/{persistedRecord.AuditId}", persistedRecord);
    }
}
