using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Palantir.Constants;
using Palantir.Platform.Endpoints;
using Palantir.Platform.Validation;
using Palantir.RiskService.Engine;
using Palantir.SharedKernel;

namespace Palantir.RiskService.Features.Risk.Validate;

public sealed class RiskSymbolSpecification : ValidationSpecification<RiskEvaluationRequest>
{
    public override string RuleName => nameof(RiskEvaluationRequest.Symbol);
    public override string ErrorMessage => "Symbol must be non-empty and at most 20 characters.";
    public override bool IsSatisfiedBy(RiskEvaluationRequest req) =>
        !string.IsNullOrWhiteSpace(req.Symbol) && req.Symbol.Trim().Length <= 20;
}

public sealed class RiskQuantitySpecification : ValidationSpecification<RiskEvaluationRequest>
{
    public override string RuleName => nameof(RiskEvaluationRequest.Quantity);
    public override string ErrorMessage => "Quantity must be greater than zero.";
    public override bool IsSatisfiedBy(RiskEvaluationRequest req) => req.Quantity > 0;
}

public sealed class RiskPriceSpecification : ValidationSpecification<RiskEvaluationRequest>
{
    public override string RuleName => nameof(RiskEvaluationRequest.Price);
    public override string ErrorMessage => "Price must be greater than zero.";
    public override bool IsSatisfiedBy(RiskEvaluationRequest req) => req.Price > 0;
}

public sealed class ValidateRiskValidator : SpecificationValidator<RiskEvaluationRequest>
{
    public ValidateRiskValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();
        UseSpecification(new RiskSymbolSpecification());
        UseSpecification(new RiskQuantitySpecification());
        UseSpecification(new RiskPriceSpecification());
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
