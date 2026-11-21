using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Palantir.Constants;
using Palantir.Platform.Endpoints;
using Palantir.RiskService.Engine;

namespace Palantir.RiskService.Endpoints;

public class RiskEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/risk")
            .WithTags("Risk Management");

        group.MapPost("/validate", EvaluateRiskAsync);

        group.MapGet("/limits/{clientId:guid}", GetClientLimitsAsync)
            .RequireAuthorization(Permissions.AllowViewRiskExposure);
    }

    private static IResult EvaluateRiskAsync(RiskEvaluationRequest request, RiskEngine engine)
    {
        var result = engine.EvaluatePreTradeRisk(request);
        return Results.Ok(result);
    }

    private static IResult GetClientLimitsAsync(Guid clientId)
    {
        return Results.Ok(new
        {
            ClientId = clientId,
            MaxSingleOrderValue = 1000000m,
            MaxLeverage = 10m,
            MarginRequirementRate = 0.10m,
            TradingHalted = false
        });
    }
}
