using Microsoft.EntityFrameworkCore;
using Pgvector;

namespace Palantir.AiAssistantService.Persistence;

public sealed class MarketSentimentEntry
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Headline { get; set; } = string.Empty;
    public string SentimentLabel { get; set; } = string.Empty;
    public float SentimentScore { get; set; }
    public DateTimeOffset PublishedAtUtc { get; set; }
    public Vector Embedding { get; set; } = null!;
}

public class SentimentDbContext : DbContext
{
    public DbSet<MarketSentimentEntry> Sentiments => Set<MarketSentimentEntry>();

    public SentimentDbContext(DbContextOptions<SentimentDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.HasDefaultSchema("ai");

        modelBuilder.Entity<MarketSentimentEntry>(entity =>
        {
            entity.ToTable("market_sentiment");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Symbol).IsRequired().HasMaxLength(32);
            entity.Property(x => x.Headline).IsRequired().HasMaxLength(512);
            entity.Property(x => x.SentimentLabel).IsRequired().HasMaxLength(32);
            entity.Property(x => x.Embedding).HasColumnType("vector(768)");

            entity.HasIndex(x => x.Symbol);
            entity.HasIndex(x => x.Embedding)
                .HasMethod("hnsw")
                .HasOperators("vector_cosine_ops");
        });
    }
}
