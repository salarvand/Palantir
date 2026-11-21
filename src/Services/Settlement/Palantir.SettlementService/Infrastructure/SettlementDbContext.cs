using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Palantir.SettlementService.Sagas;

namespace Palantir.SettlementService.Infrastructure;

public class SettlementDbContext : SagaDbContext
{
    public DbSet<OrderSettlementState> SettlementStates => Set<OrderSettlementState>();

    public SettlementDbContext(DbContextOptions<SettlementDbContext> options) : base(options) { }

    protected override IEnumerable<ISagaClassMap> Configurations
    {
        get
        {
            yield return new OrderSettlementStateMap();
        }
    }
}

public class OrderSettlementStateMap : SagaClassMap<OrderSettlementState>
{
    protected override void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<OrderSettlementState> entity, ModelBuilder model)
    {
        entity.Property(x => x.CurrentState).HasMaxLength(64);
        entity.Property(x => x.Symbol).HasMaxLength(32);
        entity.Property(x => x.Quantity).HasPrecision(18, 8);
        entity.Property(x => x.Price).HasPrecision(18, 8);
        entity.Property(x => x.ReservedMargin).HasPrecision(18, 4);
    }
}
