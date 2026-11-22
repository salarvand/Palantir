using System.Collections.Concurrent;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Palantir.Constants;
using Palantir.LocalEngineService.Domain;
using Palantir.LocalEngineService.Matching;
using Palantir.LocalEngineService.Timescale;
using Palantir.Oms.Contracts;
using Palantir.Platform.Endpoints;
using Palantir.SharedKernel;

namespace Palantir.LocalEngineService.Endpoints;

public class LocalEngineEndpoints : IEndpoint
{
    private static readonly ConcurrentDictionary<string, OrderBook> OrderBooks = new();

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
        TimescaleDbContext db,
        CancellationToken ct)
    {
        pair.Id = pair.Symbol.ToUpperInvariant();
        db.Pairs.Add(pair);
        await db.SaveChangesAsync(ct);

        OrderBooks.TryAdd(pair.Id, new OrderBook(pair.Id));

        return Results.Created($"/api/v1/engine/symbols/{pair.Id}", pair);
    }

    private static async Task<IResult> GetSymbolsAsync(TimescaleDbContext db, CancellationToken ct)
    {
        var pairs = await db.Pairs.AsNoTracking().ToListAsync(ct);
        return Results.Ok(pairs);
    }

    private static async Task<IResult> MatchOrderAsync(
        OrderBookEntry entry,
        TimescaleDbContext db,
        IPublishEndpoint publishEndpoint,
        ITopicProducer<string, MarketTick> topicProducer,
        CancellationToken ct)
    {
        var book = OrderBooks.GetOrAdd(entry.Symbol.ToUpperInvariant(), s => new OrderBook(s));
        var matchResult = book.Match(entry);

        if (matchResult.Executions.Count > 0)
        {
            var ticks = matchResult.Executions.Select(e => new MarketTick
            {
                Symbol = e.Symbol,
                Price = e.Price,
                Quantity = e.Quantity,
                TimestampUtc = e.TimestampUtc,
                TradeId = e.TradeId,
                Side = entry.Side
            }).ToList();

            db.Ticks.AddRange(ticks);
            await db.SaveChangesAsync(ct);

            foreach (var execution in matchResult.Executions)
            {
                await publishEndpoint.Publish(new TradeExecutedEvent(
                    execution.BuyOrderId,
                    execution.TradeId,
                    execution.Quantity,
                    execution.Price
                ), ct);

                await publishEndpoint.Publish(new TradeExecutedEvent(
                    execution.SellOrderId,
                    execution.TradeId,
                    execution.Quantity,
                    execution.Price
                ), ct);
            }

            foreach (var tick in ticks)
            {
                await topicProducer.Produce(tick.Symbol, tick, ct);
            }
        }

        return Results.Ok(matchResult);
    }

    private static IResult GetOrderBookAsync(string symbol, int depth = 10)
    {
        if (!OrderBooks.TryGetValue(symbol.ToUpperInvariant(), out var book))
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
        string timeframe,
        TimescaleDbContext db,
        CancellationToken ct)
    {
        var tf = string.IsNullOrWhiteSpace(timeframe) ? "1m" : timeframe.ToLowerInvariant();
        var viewName = tf switch
        {
            "1s" => "ohlc_1s",
            "1m" => "ohlc_1m",
            "5m" => "ohlc_5m",
            "1h" => "ohlc_1h",
            "1d" => "ohlc_1d",
            _ => "ohlc_1m"
        };

        var upperSymbol = symbol.ToUpperInvariant();
        var symbolParam = new NpgsqlParameter("symbol", upperSymbol);

        var sql = $"""
            SELECT "TimestampUtc", "Symbol", '{tf}' AS "Timeframe", "Open", "High", "Low", "Close", "Volume", "TradeCount"
            FROM {viewName}
            WHERE "Symbol" = @symbol
            ORDER BY "TimestampUtc" DESC
            LIMIT 100
        """;

        var candles = await db.Candles
            .FromSqlRaw(sql, symbolParam)
            .AsNoTracking()
            .ToListAsync(ct);

        return Results.Ok(candles);
    }
}
