using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Palantir.Platform.Exceptions;

public static class ProblemDetailsExtensions
{
    public static IServiceCollection AddPalantirProblemDetails(this IServiceCollection services)
    {
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
