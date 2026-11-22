using Palantir.Oms.Contracts;
using Palantir.OmsService.Grains;
using Palantir.SharedKernel;
using Shouldly;
using Xunit;

namespace Palantir.OmsService.UnitTests;

public class OrderStateTests
{
    [Fact]
    public void OrderSnapshot_Should_Initialize_Correctly()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var clientId = Guid.NewGuid();

        var request = new SubmitOrderRequest(
            orderId,
            clientId,
            "PAL/USD",
            TradeSide.Buy,
            OrderType.Limit,
            100m,
            105.00m,
            TimeInForce.GoodTilCancelled,
            "Local"
        );

        // Assert
        request.OrderId.ShouldBe(orderId);
        request.ClientId.ShouldBe(clientId);
        request.Symbol.ShouldBe("PAL/USD");
        request.Quantity.ShouldBe(100m);
        request.Price.ShouldBe(105.00m);
        request.Side.ShouldBe(TradeSide.Buy);
        request.Type.ShouldBe(OrderType.Limit);
        request.TimeInForce.ShouldBe(TimeInForce.GoodTilCancelled);
        request.VenueType.ShouldBe("Local");
        request.ExpiryDuration.ShouldBeNull();
    }

    [Fact]
    public void SubmitOrderRequest_With_ExpiryDuration_For_GoodTilTime()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var duration = TimeSpan.FromMinutes(15);

        var request = new SubmitOrderRequest(
            orderId,
            clientId,
            "BTC/USD",
            TradeSide.Sell,
            OrderType.Limit,
            2.5m,
            65000m,
            TimeInForce.GoodTilTime,
            "External",
            duration
        );

        // Assert
        request.TimeInForce.ShouldBe(TimeInForce.GoodTilTime);
        request.ExpiryDuration.ShouldBe(duration);
        request.VenueType.ShouldBe("External");
    }

    [Fact]
    public void OrderGrainState_Should_Track_Execution_Metrics()
    {
        // Arrange
        var state = new OrderGrainState
        {
            OrderId = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            Symbol = "ETH/USD",
            Side = TradeSide.Buy,
            Type = OrderType.Limit,
            Quantity = 10m,
            Price = 3000m,
            Status = OrderStatus.Accepted,
            TimeInForce = TimeInForce.GoodTilCancelled,
            VenueType = "Local",
            CumulativeFilledQuantity = 0m,
            AverageFilledPrice = 0m,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastUpdatedAtUtc = DateTimeOffset.UtcNow
        };

        // Act - Simulate partial fill
        state.CumulativeFilledQuantity += 4m;
        state.AverageFilledPrice = 2995m;
        state.Status = OrderStatus.PartiallyFilled;

        // Assert
        state.Status.ShouldBe(OrderStatus.PartiallyFilled);
        state.CumulativeFilledQuantity.ShouldBe(4m);
        state.AverageFilledPrice.ShouldBe(2995m);

        // Act - Simulate final fill
        state.CumulativeFilledQuantity += 6m;
        state.AverageFilledPrice = 3000m;
        state.Status = OrderStatus.Filled;

        // Assert
        state.Status.ShouldBe(OrderStatus.Filled);
        state.CumulativeFilledQuantity.ShouldBe(10m);
    }

    [Fact]
    public void OrderGrainState_Should_Support_Terminal_State_Transitions()
    {
        var state = new OrderGrainState
        {
            OrderId = Guid.NewGuid(),
            Status = OrderStatus.Expired,
            RejectionReason = "Order expired (Good-Til-Time elapsed)"
        };

        state.Status.ShouldBe(OrderStatus.Expired);
        state.RejectionReason.ShouldContain("Good-Til-Time");
    }
}
