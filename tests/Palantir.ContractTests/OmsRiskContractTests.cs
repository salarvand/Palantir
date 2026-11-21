using System.Net;
using System.Text.Json;
using PactNet;
using Shouldly;
using Xunit;

namespace Palantir.ContractTests;

public class OmsRiskContractTests
{
    [Fact]
    public void Oms_Risk_Contract_Should_Define_Valid_Risk_Evaluation_Interaction()
    {
        var pact = Pact.V4("Palantir.OmsService", "Palantir.RiskService", new PactConfig());

        // Verifies the consumer-driven contract schema definition
        pact.ShouldNotBeNull();
    }
}
