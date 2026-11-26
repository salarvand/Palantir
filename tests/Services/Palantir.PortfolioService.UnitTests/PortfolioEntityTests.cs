using Palantir.PortfolioService.Domain;
using Shouldly;
using Xunit;

namespace Palantir.PortfolioService.UnitTests;

public sealed class PortfolioEntityTests
{
    [Fact]
    public void Account_AvailableMargin_ShouldReflectCashMinusReserved()
    {
        var account = new Account(Guid.NewGuid(), Guid.NewGuid(), 100_000m)
        {
            ReservedMargin = 25_000m
        };

        account.AvailableMargin.ShouldBe(75_000m);
    }

    [Fact]
    public void Position_UnrealizedPnL_ShouldCalculateDelta()
    {
        var pos = new Position
        {
            Id = Guid.NewGuid(),
            Symbol = "PAL/USD",
            Quantity = 100m,
            AverageEntryPrice = 140m,
            CurrentPrice = 152.50m
        };

        pos.UnrealizedPnL.ShouldBe(1_250m);
    }
}
