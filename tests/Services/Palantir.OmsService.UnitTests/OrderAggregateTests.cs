using Palantir.SharedKernel;
using Shouldly;
using Xunit;

namespace Palantir.OmsService.UnitTests;

public class OrderAggregateTests
{
    [Fact]
    public void Order_Create_ShouldEmit_OrderCreatedDomainEvent()
    {
        // Arrange
        var orderId = OrderId.From(Guid.NewGuid());
        var clientId = ClientId.From(Guid.NewGuid());
        var symbol = SymbolId.From("PAL/USD");
        var qty = new Quantity(100m);
        var price = new Price(150.00m);

        // Act
        var order = Order.Create(
            orderId,
            clientId,
            symbol,
            TradeSide.Buy,
            OrderType.Limit,
            qty,
            price,
            TimeInForce.GoodTilCancelled
        );

        // Assert
        order.Id.ShouldBe(orderId);
        order.Status.ShouldBe(OrderStatus.New);
        order.Quantity.ShouldBe(qty);
        order.Price.ShouldBe(price);
        order.RemainingQuantity.ShouldBe(qty);

        var events = order.DomainEvents.ToList();
        events.Count.ShouldBe(1);
        events[0].ShouldBeOfType<OrderCreatedDomainEvent>();

        var createdEvent = (OrderCreatedDomainEvent)events[0];
        createdEvent.OrderId.ShouldBe(orderId);
        createdEvent.Symbol.ShouldBe(symbol);
    }

    [Fact]
    public void Order_Create_WithZeroQuantity_ShouldThrowArgumentException()
    {
        var orderId = OrderId.From(Guid.NewGuid());
        var clientId = ClientId.From(Guid.NewGuid());
        var symbol = SymbolId.From("PAL/USD");

        Should.Throw<ArgumentException>(() =>
        {
            Order.Create(
                orderId,
                clientId,
                symbol,
                TradeSide.Buy,
                OrderType.Limit,
                new Quantity(0m),
                new Price(100m),
                TimeInForce.GoodTilCancelled
            );
        });
    }

    [Fact]
    public void Order_Create_LimitOrderWithZeroPrice_ShouldThrowArgumentException()
    {
        var orderId = OrderId.From(Guid.NewGuid());
        var clientId = ClientId.From(Guid.NewGuid());
        var symbol = SymbolId.From("PAL/USD");

        Should.Throw<ArgumentException>(() =>
        {
            Order.Create(
                orderId,
                clientId,
                symbol,
                TradeSide.Buy,
                OrderType.Limit,
                new Quantity(100m),
                new Price(0m),
                TimeInForce.GoodTilCancelled
            );
        });
    }

    [Fact]
    public void Order_ApproveRisk_ShouldTransitionToSubmitted()
    {
        var order = CreateSampleOrder();

        order.ApproveRisk(1500m);

        order.Status.ShouldBe(OrderStatus.Submitted);
        order.ReservedMargin.ShouldBe(1500m);

        var uncommitted = order.DequeueUncommittedEvents();
        uncommitted.ShouldContain(e => e is OrderRiskApprovedDomainEvent);
    }

    [Fact]
    public void Order_ApplyExecution_ShouldCalculateWeightedAveragePrice_AndTransitionToFilled()
    {
        var order = CreateSampleOrder(quantity: 100m, price: 150m);
        order.ApproveRisk(1500m);
        order.DequeueUncommittedEvents();

        // Partial fill 1: 40 shares @ 149.00
        order.ApplyExecution(TradeId.From(Guid.NewGuid()), new Quantity(40m), new Price(149.00m));
        order.Status.ShouldBe(OrderStatus.PartiallyFilled);
        order.CumulativeFilledQuantity.Value.ShouldBe(40m);
        order.RemainingQuantity.Value.ShouldBe(60m);
        order.AverageFilledPrice.Value.ShouldBe(149.00m);

        // Partial fill 2: 60 shares @ 151.00 (Total 100 shares)
        order.ApplyExecution(TradeId.From(Guid.NewGuid()), new Quantity(60m), new Price(151.00m));
        order.Status.ShouldBe(OrderStatus.Filled);
        order.CumulativeFilledQuantity.Value.ShouldBe(100m);
        order.RemainingQuantity.Value.ShouldBe(0m);

        // Weighted Average Price: (40*149 + 60*151) / 100 = (5960 + 9060) / 100 = 150.20
        order.AverageFilledPrice.Value.ShouldBe(150.20m);
        order.IsTerminal.ShouldBeTrue();
    }

    [Fact]
    public void Order_InTerminalState_ShouldNotAllowFurtherFillsOrCancellation()
    {
        var order = CreateSampleOrder(quantity: 50m, price: 100m);
        order.ApplyExecution(TradeId.From(Guid.NewGuid()), new Quantity(50m), new Price(100m));
        order.Status.ShouldBe(OrderStatus.Filled);

        Should.Throw<InvalidOperationException>(() =>
        {
            order.Cancel("Late cancellation");
        });

        Should.Throw<InvalidOperationException>(() =>
        {
            order.ApplyExecution(TradeId.From(Guid.NewGuid()), new Quantity(10m), new Price(100m));
        });
    }

    private static Order CreateSampleOrder(decimal quantity = 100m, decimal price = 150.00m)
    {
        return Order.Create(
            OrderId.From(Guid.NewGuid()),
            ClientId.From(Guid.NewGuid()),
            SymbolId.From("PAL/USD"),
            TradeSide.Buy,
            OrderType.Limit,
            new Quantity(quantity),
            new Price(price),
            TimeInForce.GoodTilCancelled
        );
    }
}
