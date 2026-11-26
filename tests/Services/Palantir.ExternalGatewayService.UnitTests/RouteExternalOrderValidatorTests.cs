using Palantir.ExternalGatewayService.Features.ExternalOrders.Route;
using Palantir.SharedKernel;
using Shouldly;
using Xunit;

namespace Palantir.ExternalGatewayService.UnitTests;

public sealed class RouteExternalOrderValidatorTests
{
    private readonly RouteExternalOrderValidator _validator = new();

    [Fact]
    public void Validate_ValidOrderRequest_ShouldPass()
    {
        var req = new ExternalOrderRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "AAPL",
            TradeSide.Buy,
            OrderType.Limit,
            10m,
            185.50m,
            TimeInForce.GoodTilCancelled,
            "External");

        _validator.Validate(req).IsValid.ShouldBeTrue();
    }
}
