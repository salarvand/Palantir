using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Palantir.Constants;

namespace Palantir.Platform.Observability;

public sealed class TraceContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ActivityTraceContextAccessor.ResolveCorrelationId(context, Activity.Current);
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            Activity.Current?.SetTag("correlation.id", correlationId);
            Activity.Current?.SetBaggage(ApiConstants.CorrelationIdHeader, correlationId);

            context.Response.OnStarting(() =>
            {
                context.Response.Headers.TryAdd(ApiConstants.CorrelationIdHeader, correlationId);
                return Task.CompletedTask;
            });
        }

        await next(context);
    }
}
