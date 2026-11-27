using System.Net.Http.Json;
using Palantir.BddTests.Support;
using Reqnroll;
using Shouldly;

namespace Palantir.BddTests.StepDefinitions;

[Binding]
public sealed class RiskValidationSteps
{
    private HttpResponseMessage? _response;

    [When("I check the Risk service health endpoint")]
    public async Task WhenICheckTheRiskServiceHealthEndpoint()
    {
        _response = await BddAspireHooks.RiskClient.GetAsync("/health");
    }

    [When(@"I request risk validation for ""(.*)"" with quantity (.*) and price (.*)")]
    public async Task WhenIRequestRiskValidation(string symbol, decimal quantity, decimal price)
    {
        var request = new
        {
            clientId = Guid.NewGuid(),
            symbol,
            side = 1,
            quantity,
            price,
            availableMargin = 50_000m
        };

        _response = await BddAspireHooks.RiskClient.PostAsJsonAsync("/api/v1/risk/validate", request);
    }

    [Then("the risk response status code should be {int}")]
    public void ThenTheRiskResponseStatusCodeShouldBe(int statusCode)
    {
        _response.ShouldNotBeNull();
        ((int)_response!.StatusCode).ShouldBe(statusCode);
    }
}
