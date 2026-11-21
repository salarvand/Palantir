using Microsoft.EntityFrameworkCore;
using Palantir.IamService.Domain;

namespace Palantir.IamService.Infrastructure;

public class IamDbContext : DbContext
{
    public DbSet<TradingProfile> Profiles => Set<TradingProfile>();

    public IamDbContext(DbContextOptions<IamDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TradingProfile>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Username).IsRequired().HasMaxLength(100);
            b.Property(p => p.Email).IsRequired().HasMaxLength(256);
            b.Property(p => p.Role).IsRequired().HasMaxLength(50);
            b.Property(p => p.MaxLeverage).HasPrecision(18, 4);
            b.Property(p => p.DailyLossLimit).HasPrecision(18, 4);
            b.Property(p => p.AssignedPermissions)
                .HasConversion(
                    v => string.Join(",", v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                );
        });
    }
}
