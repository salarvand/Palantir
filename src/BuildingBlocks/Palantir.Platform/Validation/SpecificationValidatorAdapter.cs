using FluentValidation;
using Palantir.SharedKernel;

namespace Palantir.Platform.Validation;

/// <summary>
/// FluentValidation adapter bridging <see cref="IValidationSpecification{T}"/> into HTTP endpoint validation.
/// </summary>
public abstract class SpecificationValidator<T> : AbstractValidator<T>
{
    protected void UseSpecification(IValidationSpecification<T> specification)
    {
        RuleFor(x => x)
            .Must(specification.IsSatisfiedBy)
            .WithName(specification.RuleName)
            .WithMessage(specification.ErrorMessage);
    }

    protected void UseSpecification<TProperty>(
        Func<T, TProperty> selector,
        IValidationSpecification<TProperty> specification)
    {
        RuleFor(x => x)
            .Must(instance => specification.IsSatisfiedBy(selector(instance)))
            .WithName(specification.RuleName)
            .WithMessage(specification.ErrorMessage);
    }
}
