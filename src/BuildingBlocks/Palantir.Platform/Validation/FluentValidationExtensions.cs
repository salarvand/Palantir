using System.Reflection;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Palantir.SharedKernel;

namespace Palantir.Platform.Validation;

public static class ValidationExtensions
{
    public static IServiceCollection AddPalantirValidators(this IServiceCollection services, Assembly assembly)
    {
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);
        return services;
    }

    public static RouteHandlerBuilder WithValidation<TRequest>(this RouteHandlerBuilder builder)
        where TRequest : class =>
        builder.AddEndpointFilter<FluentValidationEndpointFilter<TRequest>>();

    /// <summary>
    /// Validates incoming HTTP request arguments directly against reusable domain specifications.
    /// </summary>
    public static RouteHandlerBuilder WithSpecificationValidation<TRequest>(
        this RouteHandlerBuilder builder,
        params IValidationSpecification<TRequest>[] specifications)
        where TRequest : class =>
        builder.AddEndpointFilter(new SpecificationValidationEndpointFilter<TRequest>(specifications));
}

internal sealed class SpecificationValidationEndpointFilter<TRequest>(
    IValidationSpecification<TRequest>[] specifications) : IEndpointFilter
    where TRequest : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();
        if (request is null || specifications.Length == 0)
        {
            return await next(context);
        }

        var result = request.Validate(specifications);
        if (!result.IsValid)
        {
            var errors = result.Failures
                .GroupBy(f => f.RuleName)
                .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray());

            return TypedResults.ValidationProblem(errors);
        }

        return await next(context);
    }
}

internal sealed class FluentValidationEndpointFilter<TRequest> : IEndpointFilter
    where TRequest : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices.GetService<IValidator<TRequest>>();
        if (validator is null)
        {
            return await next(context);
        }

        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();
        if (request is null)
        {
            return await next(context);
        }

        var result = await validator.ValidateAsync(request, context.HttpContext.RequestAborted);
        if (!result.IsValid)
        {
            return TypedResults.ValidationProblem(result.ToDictionary());
        }

        return await next(context);
    }
}
