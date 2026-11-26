using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Palantir.LocalEngineService.Persistence;
using Palantir.Platform.Endpoints;

namespace Palantir.LocalEngineService.Features.Candles.Get;

internal sealed class GetCandlesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/engine/candles/{symbol}", async (
                string symbol,
                string? timeframe,
                MarketTickDbContext db,
                CancellationToken ct) =>
            {
                var tf = string.IsNullOrWhiteSpace(timeframe) ? "1m" : timeframe.ToLowerInvariant();
                var (truncUnit, bucketSeconds) = tf switch
                {
                    "1s" => ("second", 1),
                    "1m" => ("minute", 60),
                    "5m" => ("minute", 300),
                    "1h" => ("hour", 3600),
                    "1d" => ("day", 86400),
                    _ => ("minute", 60)
                };

                var upperSymbol = symbol.ToUpperInvariant();
                var symbolParam = new NpgsqlParameter("symbol", upperSymbol);
                var timeframeParam = new NpgsqlParameter("tf", tf);

                var sql = bucketSeconds == 300
                    ? """
                      SELECT
                          to_timestamp(floor(extract(epoch FROM "TimestampUtc") / 300) * 300) AT TIME ZONE 'UTC' AS "TimestampUtc",
                          "Symbol",
                          @tf AS "Timeframe",
                          (array_agg("Price" ORDER BY "TimestampUtc" ASC))[1] AS "Open",
                          max("Price") AS "High",
                          min("Price") AS "Low",
                          (array_agg("Price" ORDER BY "TimestampUtc" DESC))[1] AS "Close",
                          sum("Quantity") AS "Volume",
                          count(*)::int AS "TradeCount"
                      FROM local_engine.market_ticks
                      WHERE "Symbol" = @symbol
                      GROUP BY 1, "Symbol"
                      ORDER BY 1 DESC
                      LIMIT 100
                      """
                    : $"""
                      SELECT
                          date_trunc('{truncUnit}', "TimestampUtc") AS "TimestampUtc",
                          "Symbol",
                          @tf AS "Timeframe",
                          (array_agg("Price" ORDER BY "TimestampUtc" ASC))[1] AS "Open",
                          max("Price") AS "High",
                          min("Price") AS "Low",
                          (array_agg("Price" ORDER BY "TimestampUtc" DESC))[1] AS "Close",
                          sum("Quantity") AS "Volume",
                          count(*)::int AS "TradeCount"
                      FROM local_engine.market_ticks
                      WHERE "Symbol" = @symbol
                      GROUP BY 1, "Symbol"
                      ORDER BY 1 DESC
                      LIMIT 100
                      """;

                var candles = await db.Candles
                    .FromSqlRaw(sql, symbolParam, timeframeParam)
                    .AsNoTracking()
                    .ToListAsync(ct);

                return TypedResults.Ok(candles);
            })
            .ProducesGet<List<CandlestickBar>>()
            .WithTags("Local Matching Engine")
            .WithName(nameof(GetCandlesEndpoint))
            .MapToApiVersion(ApiVersioningExtensions.V1);
    }
}
