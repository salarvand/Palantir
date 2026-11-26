using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Palantir.Constants;
using Palantir.Platform.Endpoints;
using Palantir.Platform.Validation;
using Palantir.RiskService.Engine;

namespace Palantir.RiskService.Features.Risk.Validate;

public sealed class ValidateRiskValidator : AbstractValidator<RiskEvaluationRequest>
{
    public ValidateRiskValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.Symbol).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Price).GreaterThan(0);
    }
}

internal sealed class ValidateRiskEndpoint : IEndpoint<IResult, RiskEvaluationRequest, RiskEngine>
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/risk/validate", (RiskEvaluationRequest request, RiskEngine engine) =>
                HandleAsync(request, engine))
            .ProducesPostWithoutLocation<RiskEvaluationResult>()
            .WithValidation<RiskEvaluationRequest>()
            .WithTags("Risk Management")
            .WithName(nameof(ValidateRiskEndpoint))
            .MapToApiVersion(ApiVersioningExtensions.V1)
            .RequireAuthorization(Permissions.AllowViewRiskExposure);
    }

    public Task<IResult> HandleAsync(
        RiskEvaluationRequest request,
        RiskEngine engine,
        CancellationToken cancellationToken = default)
    {
        var result = engine.EvaluatePreTradeRisk(request);
        return Task.FromResult<IResult>(TypedResults.Ok(result));
    }
}
