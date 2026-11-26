using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json;
using PactNet;
using PactNet.Matchers;
using Palantir.SettlementService.IntegrationEvents.Events;
using Shouldly;
using Xunit;

namespace Palantir.ContractTests;

public sealed class OmsRiskContractTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Oms_Risk_Http_Contract_Should_Verify_Risk_Validation_Interaction()
    {
        var pactDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "pacts"));
        var pact = Pact.V4("Palantir.OmsService", "Palantir.RiskService", new PactConfig
        {
            PactDir = pactDir,
            DefaultJsonSettings = JsonOptions
        }).WithHttpInteractions();

        var clientId = Guid.NewGuid();
        pact.UponReceiving("A pre-trade risk validation request from OMS")
            .WithRequest(HttpMethod.Post, "/api/v1/risk/validate")
            .WithHeader("Content-Type", "application/json; charset=utf-8")
            .WithJsonBody(new
            {
                clientId = Match.Type(clientId),
                symbol = Match.Type("PAL/USD"),
                side = Match.Integer(1),
                quantity = Match.Decimal(100m),
                price = Match.Decimal(150m),
                availableMargin = Match.Decimal(50_000m)
            })
            .WillRespond()
            .WithStatus(HttpStatusCode.OK)
            .WithHeader("Content-Type", "application/json; charset=utf-8")
            .WithJsonBody(new
            {
                approved = Match.Type(true),
                requiredMargin = Match.Decimal(1_500m)
            });

        await pact.VerifyAsync(async ctx =>
        {
            using var client = new HttpClient { BaseAddress = ctx.MockServerUri };
            var response = await client.PostAsJsonAsync("/api/v1/risk/validate", new
            {
                clientId,
                symbol = "PAL/USD",
                side = 1,
                quantity = 100m,
                price = 150m,
                availableMargin = 50_000m
            }, JsonOptions);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            var payload = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
            payload.GetProperty("approved").GetBoolean().ShouldBeTrue();
        });
    }

    [Fact]
    public async Task Settlement_Risk_Message_Contract_Should_Verify_CheckPreTradeRiskCommand()
    {
        var pactDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "pacts"));
        var pact = Pact.V3("Palantir.RiskService", "Palantir.SettlementService", new PactConfig
        {
            PactDir = pactDir,
            DefaultJsonSettings = JsonOptions
        }).WithMessageInteractions();

        var orderId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var command = new CheckPreTradeRiskCommand(orderId, clientId, "PAL/USD", 100m, 150m);

        await pact.ExpectsToReceive("a CheckPreTradeRiskCommand message")
            .WithMetadata("contentType", MediaTypeNames.Application.Json)
            .WithJsonContent(command)
            .VerifyAsync<CheckPreTradeRiskCommand>(received =>
            {
                received.OrderId.ShouldBe(orderId);
                received.Symbol.ShouldBe("PAL/USD");
                received.Quantity.ShouldBe(100m);
                return Task.CompletedTask;
            });
    }
}
