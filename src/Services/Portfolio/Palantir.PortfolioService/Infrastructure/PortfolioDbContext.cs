using Microsoft.EntityFrameworkCore;
using Palantir.PortfolioService.Domain;

namespace Palantir.PortfolioService.Infrastructure;

public class PortfolioDbContext : DbContext
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Position> Positions => Set<Position>();

    public PortfolioDbContext(DbContextOptions<PortfolioDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Account>(b =>
        {
            b.HasKey(a => a.Id);
            b.Property(a => a.CashBalance).HasPrecision(18, 4);
            b.Property(a => a.ReservedMargin).HasPrecision(18, 4);
            b.HasMany(a => a.Positions).WithOne().HasForeignKey(p => p.AccountId);
        });

        modelBuilder.Entity<Position>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Symbol).IsRequired().HasMaxLength(20);
            b.Property(p => p.Quantity).HasPrecision(18, 8);
            b.Property(p => p.AverageEntryPrice).HasPrecision(18, 8);
            b.Property(p => p.CurrentPrice).HasPrecision(18, 8);
        });
    }
}
