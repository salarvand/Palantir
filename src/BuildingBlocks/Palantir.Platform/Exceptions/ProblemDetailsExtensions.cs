using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Palantir.SharedKernel;

namespace Palantir.Platform.Exceptions;

public static class ProblemDetailsExtensions
{
    public static IServiceCollection AddPalantirProblemDetails(this IServiceCollection services)
    {
        services.AddExceptionHandler<DomainSpecificationExceptionHandler>();
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = ctx =>
            {
                ctx.ProblemDetails.Instance = $"{ctx.HttpContext.Request.Method} {ctx.HttpContext.Request.Path}";
                ctx.ProblemDetails.Extensions["traceId"] = Activity.Current?.Id ?? ctx.HttpContext.TraceIdentifier;
                ctx.ProblemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow;
            };
        });

        return services;
    }

    public static IApplicationBuilder UsePalantirExceptionHandler(this WebApplication app)
    {
        app.UseExceptionHandler();
        return app;
    }
}

internal sealed class DomainSpecificationExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DomainSpecificationException specException)
        {
            return false;
        }

        var errors = specException.Failures
            .GroupBy(f => f.RuleName)
            .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray());

        var problem = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Domain specification validation failed."
        };

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
