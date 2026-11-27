using System.Net;
using Palantir.IntegrationTests.Fixtures;
using Shouldly;
using Xunit;

namespace Palantir.IntegrationTests;

[Collection(AspireCollection.Name)]
public sealed class RiskAndAuditIntegrationTests(AspireAppFixture fixture)
{
    [Fact]
    public async Task HealthEndpoints_ShouldReturnOk_ForRiskAndAuditServices()
    {
        var riskHealth = await fixture.RiskClient.GetAsync("/health");
        var auditHealth = await fixture.AuditClient.GetAsync("/health");

        riskHealth.StatusCode.ShouldBe(HttpStatusCode.OK);
        auditHealth.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
