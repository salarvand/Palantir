using System.Net.Http.Json;
using Palantir.BddTests.Support;
using Reqnroll;
using Shouldly;

namespace Palantir.BddTests.StepDefinitions;

[Binding]
public sealed class OrderManagementSteps
{
    private HttpResponseMessage? _response;

    [Given("I have an unauthenticated client")]
    public void GivenIHaveAnUnauthenticatedClient()
    {
        BddAspireHooks.OmsClient.DefaultRequestHeaders.Authorization = null;
    }

    [When("I check the OMS service health endpoint")]
    public async Task WhenICheckTheOmsServiceHealthEndpoint()
    {
        _response = await BddAspireHooks.OmsClient.GetAsync("/health");
    }

    [When(@"I submit a limit order for ""(.*)"" at price (.*) for quantity (.*)")]
    public async Task WhenISubmitALimitOrder(string symbol, decimal price, decimal quantity)
    {
        var command = new
        {
            orderId = Guid.NewGuid(),
            clientId = Guid.NewGuid(),
            symbol,
            side = 1,
            type = 2,
            quantity,
            price,
            timeInForce = 1,
            venueType = "Local"
        };

        _response = await BddAspireHooks.OmsClient.PostAsJsonAsync("/api/v1/orders", command);
    }

    [Then("the response status code should be {int}")]
    public void ThenTheResponseStatusCodeShouldBe(int statusCode)
    {
        _response.ShouldNotBeNull();
        ((int)_response!.StatusCode).ShouldBe(statusCode);
    }
}
