using Palantir.LocalEngineService.Grpc;
using Palantir.OmsService.IntegrationEvents.Events;
using Shouldly;
using Xunit;

namespace Palantir.ContractTests;

public class OmsToLocalEngineContractTests
{
    [Fact]
    public void GrpcSubmitOrderRequest_ShouldHave_ExpectedFields()
    {
        var request = new GrpcSubmitOrderRequest
        {
            OrderId = Guid.NewGuid().ToString(),
            ClientId = Guid.NewGuid().ToString(),
            Symbol = "PAL/USD",
            Side = 1,
            Type = 2,
            Quantity = "100.00000000",
            Price = "150.25000000",
            TimeInForce = 1
        };

        request.OrderId.ShouldNotBeNullOrWhiteSpace();
        request.Symbol.ShouldBe("PAL/USD");
        request.Side.ShouldBe(1);
        request.Price.ShouldBe("150.25000000");
    }

    [Fact]
    public void GrpcExecutionReport_ShouldHave_ExpectedFields()
    {
        var report = new GrpcExecutionReport
        {
            OrderId = Guid.NewGuid().ToString(),
            Status = "Filled",
            FilledQuantity = "100.00000000",
            AveragePrice = "150.25000000",
            TradeId = Guid.NewGuid().ToString(),
            Message = "Executed in memory.",
            TimestampUtcTicks = DateTimeOffset.UtcNow.Ticks
        };

        report.Status.ShouldBe("Filled");
        report.TradeId.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void InitiateOrderSettlementCommand_ShouldImplement_SagaContract()
    {
        var orderId = Guid.NewGuid();
        var clientId = Guid.NewGuid();

        var cmd = new InitiateOrderSettlementCommand(
            orderId,
            clientId,
            "PAL/USD",
            100m,
            150.25m
        );

        cmd.OrderId.ShouldBe(orderId);
        cmd.ClientId.ShouldBe(clientId);
        cmd.Symbol.ShouldBe("PAL/USD");
        cmd.Quantity.ShouldBe(100m);
    }
}
