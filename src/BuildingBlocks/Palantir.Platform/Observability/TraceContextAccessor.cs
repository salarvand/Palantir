using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Palantir.Constants;

namespace Palantir.Platform.Observability;

public sealed record TraceContextSnapshot(
    string? TraceId,
    string? SpanId,
    byte TraceFlags,
    string? CorrelationId)
{
    public static TraceContextSnapshot Empty { get; } = new(null, null, 0, null);

    public static string? GetCurrentTraceId() =>
        Activity.Current is { TraceId: var traceId } && traceId != default
            ? traceId.ToHexString()
            : null;

    public static bool TryParseActivityContext(
        string? traceId,
        string? spanId,
        byte traceFlags,
        out ActivityContext context)
    {
        if (string.IsNullOrWhiteSpace(traceId) || string.IsNullOrWhiteSpace(spanId))
        {
            context = default;
            return false;
        }

        var flagsHex = (traceFlags & (byte)ActivityTraceFlags.Recorded) != 0 ? "01" : "00";
        return ActivityContext.TryParse($"00-{traceId}-{spanId}-{flagsHex}", null, out context);
    }
}

public interface ITraceContextAccessor
{
    TraceContextSnapshot Capture();
}

public sealed class ActivityTraceContextAccessor(IHttpContextAccessor? httpContextAccessor = null) : ITraceContextAccessor
{
    public TraceContextSnapshot Capture()
    {
        var activity = Activity.Current;
        string? traceId = null;
        string? spanId = null;
        byte traceFlags = 0;

        if (activity is not null && activity.TraceId != default)
        {
            traceId = activity.TraceId.ToHexString();
            spanId = activity.SpanId != default ? activity.SpanId.ToHexString() : null;
            traceFlags = (byte)activity.ActivityTraceFlags;
        }

        var correlationId = ResolveCorrelationId(httpContextAccessor?.HttpContext, activity);
        return new TraceContextSnapshot(traceId, spanId, traceFlags, correlationId);
    }

    public static string? ResolveCorrelationId(HttpContext? httpContext, Activity? activity)
    {
        if (httpContext is not null &&
            httpContext.Request.Headers.TryGetValue(ApiConstants.CorrelationIdHeader, out var headerValues))
        {
            var header = headerValues.FirstOrDefault()?.Trim();
            if (!string.IsNullOrWhiteSpace(header))
            {
                return header;
            }
        }

        var baggage = activity?.GetBaggageItem(ApiConstants.CorrelationIdHeader);
        if (!string.IsNullOrWhiteSpace(baggage))
        {
            return baggage;
        }

        return activity?.GetTagItem("correlation.id") as string;
    }
}
