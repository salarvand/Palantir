using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Palantir.AuditService.Domain;
using Palantir.Constants;
using Palantir.Platform.Endpoints;

namespace Palantir.AuditService.Endpoints;

public class AuditEndpoints : IEndpoint
{
    private const string IndexName = "audit-records-v1";

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
        ElasticsearchClient esClient,
        CancellationToken ct)
    {
        var size = limit <= 0 ? 50 : Math.Min(limit, 500);

        var searchResponse = await esClient.SearchAsync<RegulatoryAuditRecord>(s => s
            .Index(IndexName)
            .Size(size)
            .Query(q =>
            {
                if (!string.IsNullOrWhiteSpace(symbol))
                {
                    q.Match(m => m.Field(f => f.Symbol).Query(symbol.ToUpperInvariant()));
                }
                else
                {
                    q.MatchAll(_ => { });
                }
            })
            .Sort(so => so.Field(f => f.TimestampUtc, new FieldSort { Order = SortOrder.Desc })),
            ct);

        if (!searchResponse.IsValidResponse)
        {
            return Results.Ok(Array.Empty<RegulatoryAuditRecord>());
        }

        return Results.Ok(searchResponse.Documents.ToList());
    }

    private static async Task<IResult> AppendLogAsync(
        RegulatoryAuditRecord record,
        ElasticsearchClient esClient,
        CancellationToken ct)
    {
        var auditId = record.AuditId != Guid.Empty ? record.AuditId : Guid.NewGuid();
        var adjustedRecord = record with
        {
            AuditId = auditId,
            TimestampUtc = record.TimestampUtc == default ? DateTimeOffset.UtcNow : record.TimestampUtc
        };

        var response = await esClient.IndexAsync(
            adjustedRecord,
            idx => idx.Index(IndexName).Id(auditId.ToString()),
            ct);

        if (!response.IsValidResponse)
        {
            return Results.Problem(
                detail: response.ElasticsearchServerError?.Error.Reason ?? "Failed to index audit record into Elasticsearch.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        return Results.Created($"/api/v1/audit/logs/{auditId}", adjustedRecord);
    }
}
