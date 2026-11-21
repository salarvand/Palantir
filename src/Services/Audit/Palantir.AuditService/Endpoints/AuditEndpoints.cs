using System.Collections.Concurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Palantir.AuditService.Domain;
using Palantir.Constants;
using Palantir.Platform.Endpoints;

namespace Palantir.AuditService.Endpoints;

public class AuditEndpoints : IEndpoint
{
    private static readonly ConcurrentBag<RegulatoryAuditRecord> AuditLogStore =
    [
        new RegulatoryAuditRecord(
            Guid.NewGuid(),
            "OrderPlaced",
            Guid.NewGuid(),
            null,
            Guid.NewGuid(),
            "PAL/USD",
            100.50m,
            500m,
            "{\"Side\":\"Buy\",\"Type\":\"Limit\"}",
            DateTimeOffset.UtcNow.AddMinutes(-5)
        )
    ];

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/audit")
            .WithTags("Regulatory Audit");

        group.MapGet("/logs", GetLogsAsync)
            .RequireAuthorization(Permissions.AllowViewAuditLogs);

        group.MapPost("/logs", AppendLogAsync);
    }

    private static IResult GetLogsAsync(string? symbol, int limit = 50)
    {
        var query = AuditLogStore.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(symbol))
        {
            query = query.Where(l => string.Equals(l.Symbol, symbol, StringComparison.OrdinalIgnoreCase));
        }

        var results = query.OrderByDescending(l => l.TimestampUtc).Take(limit).ToList();
        return Results.Ok(results);
    }

    private static IResult AppendLogAsync(RegulatoryAuditRecord record)
    {
        AuditLogStore.Add(record);
        return Results.Created($"/api/v1/audit/logs/{record.AuditId}", record);
    }
}
