using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Palantir.Platform.Observability;
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
                ctx.ProblemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow;

                var traceId = TraceContextSnapshot.GetCurrentTraceId();
                if (traceId is not null)
                {
                    ctx.ProblemDetails.Extensions["traceId"] = traceId;
                }

                var correlationId = ActivityTraceContextAccessor.ResolveCorrelationId(ctx.HttpContext, Activity.Current);
                if (!string.IsNullOrWhiteSpace(correlationId))
                {
                    ctx.ProblemDetails.Extensions["correlationId"] = correlationId;
                }
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

internal sealed class DomainSpecificationExceptionHandler(
    IProblemDetailsService problemDetailsService) : IExceptionHandler
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
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problem
        });
    }
}
