using System.Net;
using System.Net.Http.Json;
using Palantir.IntegrationTests.Fixtures;
using Shouldly;
using Xunit;

namespace Palantir.IntegrationTests;

[Collection(AspireCollection.Name)]
public sealed class OrderLifecycleTests(AspireAppFixture fixture)
{
    [Fact]
    public async Task HealthEndpoint_ShouldReturnOk_ForOmsService()
    {
        var response = await fixture.OmsClient.GetAsync("/health");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateOrder_WithoutAuthToken_ShouldReturnUnauthorized()
    {
        var command = new
        {
            orderId = Guid.NewGuid(),
            clientId = Guid.NewGuid(),
            symbol = "PAL/USD",
            side = 1,
            type = 2,
            quantity = 10m,
            price = 100m,
            timeInForce = 1,
            venueType = "Local"
        };

        var response = await fixture.OmsClient.PostAsJsonAsync("/api/v1/orders", command);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
