using Palantir.OmsService.Features.Orders.Create;
using Palantir.SharedKernel;
using Shouldly;
using Xunit;

namespace Palantir.OmsService.UnitTests;

public sealed class OrderSpecificationTests
{
    [Fact]
    public void CreateOrderValidator_ShouldReuseDomainSpecifications()
    {
        var validator = new CreateOrderValidator();
        var valid = new CreateOrderCommand(Guid.NewGuid(), Guid.NewGuid(), "PAL/USD", TradeSide.Buy, OrderType.Limit, 10m, 150m, TimeInForce.GoodTilCancelled, "Local");
        var invalid = valid with { Quantity = 0m, Price = 0m };

        validator.Validate(valid).IsValid.ShouldBeTrue();
        validator.Validate(invalid).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void DomainGuard_ShouldFailWithSameSpecificationsAsHttpValidator()
    {
        var invalid = new CreateOrderCommand(Guid.NewGuid(), Guid.NewGuid(), "", TradeSide.Buy, OrderType.Limit, 0m, 0m, TimeInForce.GoodTilCancelled, "Local");

        var result = invalid.Validate(
            new OrderSymbolSpecification(),
            new OrderQuantitySpecification(),
            new LimitOrderPriceSpecification());

        result.IsValid.ShouldBeFalse();
        result.Failures.Count.ShouldBe(3);
    }
}
