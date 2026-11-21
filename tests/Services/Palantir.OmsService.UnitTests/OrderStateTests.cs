using Palantir.Oms.Contracts;
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
    }
}
