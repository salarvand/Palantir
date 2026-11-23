using Palantir.LocalEngineService.Matching;
using Palantir.SharedKernel;
using Shouldly;
using Xunit;

namespace Palantir.LocalEngineService.UnitTests;

public class OrderBookMatchingTests
{
    [Fact]
    public void OrderBook_CancelOrder_ShouldRemoveFromRestingBook()
    {
        // Arrange
        var book = new OrderBook("PAL/USD");
        var orderId = Guid.NewGuid();
        var entry = new OrderBookEntry(
            orderId,
            Guid.NewGuid(),
            "PAL/USD",
            TradeSide.Buy,
            OrderType.Limit,
            150.00m,
            100m,
            DateTimeOffset.UtcNow
        );
        book.Match(entry);

        var (bidsBefore, _) = book.GetDepth();
        bidsBefore.Count.ShouldBe(1);

        // Act
        var cancelled = book.Cancel(orderId);

        // Assert
        cancelled.ShouldBeTrue();
        var (bidsAfter, _) = book.GetDepth();
        bidsAfter.ShouldBeEmpty();
    }

    [Fact]
    public void OrderBook_PriceTimePriority_ShouldFillEarliestOrderFirst()
    {
        // Arrange
        var book = new OrderBook("PAL/USD");
        var earlyAskId = Guid.NewGuid();
        var lateAskId = Guid.NewGuid();

        // 1. First resting ask: Sell 50 @ 150.00
        book.Match(new OrderBookEntry(
            earlyAskId,
            Guid.NewGuid(),
            "PAL/USD",
            TradeSide.Sell,
            OrderType.Limit,
            150.00m,
            50m,
            DateTimeOffset.UtcNow.AddMinutes(-2)
        ));

        // 2. Second resting ask: Sell 50 @ 150.00 (same price level, later time)
        book.Match(new OrderBookEntry(
            lateAskId,
            Guid.NewGuid(),
            "PAL/USD",
            TradeSide.Sell,
            OrderType.Limit,
            150.00m,
            50m,
            DateTimeOffset.UtcNow.AddMinutes(-1)
        ));

        // 3. Incoming buy order for 50 shares
        var incomingBuy = new OrderBookEntry(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "PAL/USD",
            TradeSide.Buy,
            OrderType.Limit,
            150.00m,
            50m,
            DateTimeOffset.UtcNow
        );

        // Act
        var result = book.Match(incomingBuy);

        // Assert
        result.IsFullyFilled.ShouldBeTrue();
        result.Executions.Count.ShouldBe(1);
        result.Executions[0].SellOrderId.ShouldBe(earlyAskId); // Priority to earliest order
    }
}
