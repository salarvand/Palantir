using Microsoft.EntityFrameworkCore;
using Palantir.SharedKernel;

namespace Palantir.LocalEngineService.Timescale;

public class MarketTick
{
    public long Id { get; set; }
    public DateTimeOffset TimestampUtc { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public TradeSide Side { get; set; }
    public Guid TradeId { get; set; }
}

public class CandlestickBar
{
    public DateTimeOffset TimestampUtc { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Timeframe { get; set; } = "1m"; // 1s, 1m, 5m, 1h, 1d
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
    public int TradeCount { get; set; }
}

public class TimescaleDbContext : DbContext
{
    public DbSet<MarketTick> Ticks => Set<MarketTick>();
    public DbSet<CandlestickBar> Candles => Set<CandlestickBar>();
    public DbSet<Domain.TradingPair> Pairs => Set<Domain.TradingPair>();

    public TimescaleDbContext(DbContextOptions<TimescaleDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MarketTick>(b =>
        {
            b.ToTable("market_ticks");
            b.HasKey(t => new { t.Id, t.TimestampUtc });
            b.Property(t => t.Id).ValueGeneratedOnAdd();
            b.Property(t => t.Symbol).IsRequired().HasMaxLength(20);
            b.Property(t => t.Price).HasPrecision(18, 8);
            b.Property(t => t.Quantity).HasPrecision(18, 8);
            b.HasIndex(t => new { t.Symbol, t.TimestampUtc });
        });

        modelBuilder.Entity<CandlestickBar>(b =>
        {
            b.HasKey(c => new { c.Symbol, c.Timeframe, c.TimestampUtc });
            b.Property(c => c.Symbol).IsRequired().HasMaxLength(20);
            b.Property(c => c.Timeframe).IsRequired().HasMaxLength(10);
            b.Property(c => c.Open).HasPrecision(18, 8);
            b.Property(c => c.High).HasPrecision(18, 8);
            b.Property(c => c.Low).HasPrecision(18, 8);
            b.Property(c => c.Close).HasPrecision(18, 8);
            b.Property(c => c.Volume).HasPrecision(18, 8);
        });

        modelBuilder.Entity<Domain.TradingPair>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.TickSize).HasPrecision(18, 8);
            b.Property(p => p.LotSize).HasPrecision(18, 8);
        });
    }
}
