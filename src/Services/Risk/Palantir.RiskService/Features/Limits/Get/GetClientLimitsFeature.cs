using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Palantir.Constants;
using Palantir.Platform.Endpoints;

namespace Palantir.RiskService.Features.Limits.Get;

public sealed record ClientRiskLimitsResponse(
    Guid ClientId,
    decimal MaxSingleOrderValue,
    decimal MaxLeverage,
    decimal MarginRequirementRate,
    bool TradingHalted);

internal sealed class GetClientLimitsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/risk/limits/{clientId:guid}", (Guid clientId) =>
                TypedResults.Ok(new ClientRiskLimitsResponse(clientId, 1_000_000m, 10m, 0.10m, false)))
            .ProducesGet<ClientRiskLimitsResponse>()
            .WithTags("Risk Management")
            .WithName(nameof(GetClientLimitsEndpoint))
            .MapToApiVersion(ApiVersioningExtensions.V1)
            .RequireAuthorization(Permissions.AllowViewRiskExposure);
    }
}
