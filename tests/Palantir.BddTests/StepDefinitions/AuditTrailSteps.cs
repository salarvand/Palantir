using Palantir.BddTests.Support;
using Reqnroll;
using Shouldly;

namespace Palantir.BddTests.StepDefinitions;

[Binding]
public sealed class AuditTrailSteps
{
    private HttpResponseMessage? _response;

    [When("I check the Audit service health endpoint")]
    public async Task WhenICheckTheAuditServiceHealthEndpoint()
    {
        _response = await BddAspireHooks.AuditClient.GetAsync("/health");
    }

    [When("I query the audit logs endpoint without credentials")]
    public async Task WhenIQueryTheAuditLogsEndpointWithoutCredentials()
    {
        _response = await BddAspireHooks.AuditClient.GetAsync("/api/v1/audit/logs");
    }

    [Then("the audit response status code should be {int}")]
    public void ThenTheAuditResponseStatusCodeShouldBe(int statusCode)
    {
        _response.ShouldNotBeNull();
        ((int)_response!.StatusCode).ShouldBe(statusCode);
    }
}
