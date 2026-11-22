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
        result.Executions[0].Price.ShouldBe(101.00m);
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
        asks[0].Quantity.ShouldBe(60m);
    }

    [Fact]
    public void OrderBook_Should_Maintain_Strict_Price_Time_Priority_At_Same_Price()
    {
        // Arrange
        var book = new OrderBook("PAL/USD");
        var firstOrderId = Guid.NewGuid();
        var secondOrderId = Guid.NewGuid();

        // Two sell orders at same price: first 20, then 30
        var ask1 = new OrderBookEntry(firstOrderId, Guid.NewGuid(), "PAL/USD", TradeSide.Sell, OrderType.Limit, 100.00m, 20m, DateTimeOffset.UtcNow);
        var ask2 = new OrderBookEntry(secondOrderId, Guid.NewGuid(), "PAL/USD", TradeSide.Sell, OrderType.Limit, 100.00m, 30m, DateTimeOffset.UtcNow.AddSeconds(1));
        book.Match(ask1);
        book.Match(ask2);

        // Incoming buy for 25
        var bid = new OrderBookEntry(Guid.NewGuid(), Guid.NewGuid(), "PAL/USD", TradeSide.Buy, OrderType.Limit, 100.00m, 25m, DateTimeOffset.UtcNow.AddSeconds(2));

        // Act
        var result = book.Match(bid);

        // Assert
        result.Executions.Count.ShouldBe(2);
        result.Executions[0].SellOrderId.ShouldBe(firstOrderId);
        result.Executions[0].Quantity.ShouldBe(20m); // First in, first filled
        result.Executions[1].SellOrderId.ShouldBe(secondOrderId);
        result.Executions[1].Quantity.ShouldBe(5m); // Remaining 5 taken from second

        var (bids, asks) = book.GetDepth();
        asks[0].Quantity.ShouldBe(25m); // 30 - 5 = 25 remaining
    }

    [Fact]
    public void OrderBook_Should_Execute_Market_Orders_Immediately()
    {
        // Arrange
        var book = new OrderBook("PAL/USD");

        var ask = new OrderBookEntry(Guid.NewGuid(), Guid.NewGuid(), "PAL/USD", TradeSide.Sell, OrderType.Limit, 150.00m, 10m, DateTimeOffset.UtcNow);
        book.Match(ask);

        // Incoming Market order
        var marketBid = new OrderBookEntry(Guid.NewGuid(), Guid.NewGuid(), "PAL/USD", TradeSide.Buy, OrderType.Market, 0m, 10m, DateTimeOffset.UtcNow.AddSeconds(1));

        // Act
        var result = book.Match(marketBid);

        // Assert
        result.IsFullyFilled.ShouldBeTrue();
        result.Executions.Count.ShouldBe(1);
        result.Executions[0].Price.ShouldBe(150.00m);
    }
}
