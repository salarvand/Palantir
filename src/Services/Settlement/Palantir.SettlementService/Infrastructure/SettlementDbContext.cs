using Microsoft.EntityFrameworkCore;
using Palantir.SettlementService.Sagas;

namespace Palantir.SettlementService.Infrastructure;

public class SettlementDbContext : DbContext
{
    public DbSet<OrderSettlementSaga> SettlementStates => Set<OrderSettlementSaga>();

    public SettlementDbContext(DbContextOptions<SettlementDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("settlement");

        modelBuilder.Entity<OrderSettlementSaga>(entity =>
        {
            entity.ToTable("order_settlement_sagas");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CurrentState).IsRequired().HasMaxLength(64);
            entity.Property(x => x.Symbol).IsRequired().HasMaxLength(32);
            entity.Property(x => x.Quantity).HasPrecision(18, 8);
            entity.Property(x => x.Price).HasPrecision(18, 8);
            entity.Property(x => x.ReservedMargin).HasPrecision(18, 4);
            entity.Property(x => x.FailureReason).HasMaxLength(512);
        });
    }
}
