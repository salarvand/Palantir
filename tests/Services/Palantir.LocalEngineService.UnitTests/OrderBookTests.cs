using Palantir.LocalEngineService.Matching;
using Palantir.SharedKernel;
using Shouldly;
using Xunit;

namespace Palantir.LocalEngineService.UnitTests;

public class OrderBookTests
{
    [Fact]
    public void OrderBook_Should_Rest_Limit_Order_When_No_Opposite_Orders_Exist()
    {
        // Arrange
        var book = new OrderBook("PAL/USD");
        var order = new OrderBookEntry(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "PAL/USD",
            TradeSide.Buy,
            OrderType.Limit,
            100.00m,
            50m,
            DateTimeOffset.UtcNow
        );

        // Act
        var result = book.Match(order);

        // Assert
        result.IsFullyFilled.ShouldBeFalse();
        result.FilledQuantity.ShouldBe(0m);
        result.Executions.ShouldBeEmpty();

        var (bids, asks) = book.GetDepth();
        bids.Count.ShouldBe(1);
        bids[0].Price.ShouldBe(100.00m);
        bids[0].Quantity.ShouldBe(50m);
    }

    [Fact]
    public void OrderBook_Should_Match_Crossing_Orders_At_Resting_Price()
    {
        // Arrange
        var book = new OrderBook("PAL/USD");

        // Resting Ask: Sell 50 @ 101.00
        var askOrder = new OrderBookEntry(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "PAL/USD",
            TradeSide.Sell,
            OrderType.Limit,
            101.00m,
            50m,
            DateTimeOffset.UtcNow
        );
        book.Match(askOrder);

        // Incoming Bid: Buy 50 @ 101.50 (crosses book)
        var bidOrder = new OrderBookEntry(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "PAL/USD",
            TradeSide.Buy,
            OrderType.Limit,
            101.50m,
            50m,
            DateTimeOffset.UtcNow
        );

        // Act
        var result = book.Match(bidOrder);

        // Assert
        result.IsFullyFilled.ShouldBeTrue();
        result.FilledQuantity.ShouldBe(50m);
        result.Executions.Count.ShouldBe(1);
        result.Executions[0].Price.ShouldBe(101.00m); // Execution occurs at the resting price
        result.Executions[0].Quantity.ShouldBe(50m);

        var (bids, asks) = book.GetDepth();
        bids.ShouldBeEmpty();
        asks.ShouldBeEmpty();
    }

    [Fact]
    public void OrderBook_Should_Handle_Partial_Fills_Correctly()
    {
        // Arrange
        var book = new OrderBook("PAL/USD");

        // Resting Ask: Sell 100 @ 100.00
        var askOrder = new OrderBookEntry(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "PAL/USD",
            TradeSide.Sell,
            OrderType.Limit,
            100.00m,
            100m,
            DateTimeOffset.UtcNow
        );
        book.Match(askOrder);

        // Incoming Bid: Buy 40 @ 100.00
        var bidOrder = new OrderBookEntry(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "PAL/USD",
            TradeSide.Buy,
            OrderType.Limit,
            100.00m,
            40m,
            DateTimeOffset.UtcNow
        );

        // Act
        var result = book.Match(bidOrder);

        // Assert
        result.IsFullyFilled.ShouldBeTrue();
        result.FilledQuantity.ShouldBe(40m);

        var (bids, asks) = book.GetDepth();
        asks.Count.ShouldBe(1);
        asks[0].Quantity.ShouldBe(60m); // 60 remaining
    }
}
