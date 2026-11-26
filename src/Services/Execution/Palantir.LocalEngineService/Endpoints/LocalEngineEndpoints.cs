using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Palantir.Constants;
using Palantir.LocalEngineService.Domain;
using Palantir.LocalEngineService.Matching;
using Palantir.LocalEngineService.Persistence;
using Palantir.Platform.Endpoints;

namespace Palantir.LocalEngineService.Endpoints;

public class LocalEngineEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/engine")
            .WithTags("Local Matching Engine");

        group.MapPost("/symbols", CreateSymbolAsync)
            .RequireAuthorization(Permissions.AllowDefineSymbol);

        group.MapGet("/symbols", GetSymbolsAsync);

        group.MapPost("/match", MatchOrderAsync);

        group.MapGet("/orderbook/{symbol}", GetOrderBookAsync);

        group.MapGet("/candles/{symbol}", GetCandlesAsync);
    }

    private static async Task<IResult> CreateSymbolAsync(
        TradingPair pair,
        MarketTickDbContext db,
        IOrderBookRegistry bookRegistry,
        CancellationToken ct)
    {
        pair.Id = pair.Symbol.ToUpperInvariant();
        db.Pairs.Add(pair);
        await db.SaveChangesAsync(ct);

        bookRegistry.GetOrCreate(pair.Id);

        return Results.Created($"/api/v1/engine/symbols/{pair.Id}", pair);
    }

    private static async Task<IResult> GetSymbolsAsync(MarketTickDbContext db, CancellationToken ct)
    {
        var pairs = await db.Pairs.AsNoTracking().ToListAsync(ct);
        return Results.Ok(pairs);
    }

    private static async Task<IResult> MatchOrderAsync(
        OrderBookEntry entry,
        IOrderBookRegistry bookRegistry,
        IOrderExecutionPublisher executionPublisher,
        CancellationToken ct)
    {
        var book = bookRegistry.GetOrCreate(entry.Symbol);
        var matchResult = book.Match(entry);

        await executionPublisher.PublishMatchResultsAsync(entry.Symbol, entry.Side, book, matchResult, ct);

        return Results.Ok(matchResult);
    }

    private static IResult GetOrderBookAsync(string symbol, IOrderBookRegistry bookRegistry, int depth = 10)
    {
        if (!bookRegistry.TryGet(symbol, out var book) || book == null)
        {
            return Results.NotFound($"Symbol '{symbol}' not found in active order books.");
        }

        var (bids, asks) = book.GetDepth(depth);
        return Results.Ok(new
        {
            Symbol = symbol.ToUpperInvariant(),
            TimestampUtc = DateTimeOffset.UtcNow,
            Bids = bids.Select(b => new { Price = b.Price, Quantity = b.Quantity }),
            Asks = asks.Select(a => new { Price = a.Price, Quantity = a.Quantity })
        });
    }

    private static async Task<IResult> GetCandlesAsync(
        string symbol,
        string? timeframe,
        MarketTickDbContext db,
        CancellationToken ct)
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

        return Results.Ok(candles);
    }
}
