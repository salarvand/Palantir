using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Palantir.LocalEngineService.Timescale;

public static class TimescaleMigration
{
    public static async Task InitializeTimescaleAsync(TimescaleDbContext db, ILogger logger, CancellationToken ct = default)
    {
        await db.Database.EnsureCreatedAsync(ct);

        try
        {
            // 1. Initialize TimescaleDB extension
            await db.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS timescaledb CASCADE;", ct);

            // 2. Convert market_ticks table to TimescaleDB hypertable
            await db.Database.ExecuteSqlRawAsync(
                "SELECT create_hypertable('market_ticks', 'TimestampUtc', if_not_exists => TRUE);", ct);

            // 3. Define continuous aggregates for 1s, 1m, 5m, 1h, and 1d
            var timeframes = new[]
            {
                ("ohlc_1s", "1 second"),
                ("ohlc_1m", "1 minute"),
                ("ohlc_5m", "5 minutes"),
                ("ohlc_1h", "1 hour"),
                ("ohlc_1d", "1 day")
            };

            foreach (var (viewName, bucketInterval) in timeframes)
            {
                var ddl = $"""
                    CREATE MATERIALIZED VIEW IF NOT EXISTS {viewName}
                    WITH (timescaledb.continuous) AS
                    SELECT
                        time_bucket('{bucketInterval}', "TimestampUtc") AS "TimestampUtc",
                        "Symbol",
                        first("Price", "TimestampUtc") AS "Open",
                        max("Price") AS "High",
                        min("Price") AS "Low",
                        last("Price", "TimestampUtc") AS "Close",
                        sum("Quantity") AS "Volume",
                        count(*)::int AS "TradeCount"
                    FROM market_ticks
                    GROUP BY time_bucket('{bucketInterval}', "TimestampUtc"), "Symbol"
                    WITH NO DATA;
                """;

                await db.Database.ExecuteSqlRawAsync(ddl, ct);
            }

            logger.LogInformation("TimescaleDB extension, hypertable, and continuous aggregates initialized successfully.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "TimescaleDB initialization skipped or failed (extension or Docker environment may not support TimescaleDB).");
        }
    }
}
