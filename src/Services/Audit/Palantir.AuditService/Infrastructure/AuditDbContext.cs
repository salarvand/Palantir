using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace Palantir.AuditService.Infrastructure;

public sealed class AuditRecordEntity
{
    public Guid AuditId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public Guid? TradeId { get; set; }
    public Guid ClientId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
    public DateTimeOffset TimestampUtc { get; set; }
    public string? TraceId { get; set; }
    public NpgsqlTsVector? SearchVector { get; set; }
}

public class AuditDbContext : DbContext
{
    public DbSet<AuditRecordEntity> AuditRecords => Set<AuditRecordEntity>();

    public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("audit");

        modelBuilder.Entity<AuditRecordEntity>(entity =>
        {
            entity.ToTable("audit_records");
            entity.HasKey(x => x.AuditId);
            entity.Property(x => x.EventType).IsRequired().HasMaxLength(100);
            entity.Property(x => x.Symbol).IsRequired().HasMaxLength(32);
            entity.Property(x => x.Price).HasPrecision(18, 8);
            entity.Property(x => x.Quantity).HasPrecision(18, 8);
            entity.Property(x => x.PayloadJson).HasColumnType("jsonb");
            entity.Property(x => x.TraceId).HasMaxLength(32);

            entity.HasIndex(x => new { x.Symbol, x.TimestampUtc });
            entity.HasIndex(x => x.TraceId);

            entity.HasGeneratedTsVectorColumn(
                    x => x.SearchVector!,
                    "english",
                    x => new { x.EventType, x.Symbol })
                .HasIndex(x => x.SearchVector)
                .HasMethod("GIN");
        });
    }
}
