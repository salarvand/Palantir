using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Palantir.Constants;
using Palantir.Platform.Auditing;
using Palantir.Platform.Auditing.Sinks;
using Palantir.Platform.Exceptions;
using Palantir.SharedKernel;
using Shouldly;
using Xunit;

namespace Palantir.Platform.UnitTests;

public sealed class AuditTracingAndProblemDetailsTests
{
    [Fact]
    public async Task OtlpAuditSink_ShouldAttachAuditSpanToOriginatingTraceId()
    {
        var recordedActivities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Palantir.Auditing",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = recordedActivities.Add
        };
        ActivitySource.AddActivityListener(listener);

        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();
        var entry = AuditEntry.Create(
            "OrderSubmitted",
            "OmsService",
            "Order",
            "ord-1",
            "Create",
            traceId: traceId.ToHexString(),
            spanId: spanId.ToHexString(),
            traceFlags: (byte)ActivityTraceFlags.Recorded);

        var sink = new OtlpAuditSink();
        await sink.FlushAsync([entry], CancellationToken.None);

        recordedActivities.Count.ShouldBe(1);
        recordedActivities[0].TraceId.ShouldBe(traceId);
        recordedActivities[0].ParentSpanId.ShouldBe(spanId);
        recordedActivities[0].GetTagItem("audit.trace_id").ShouldBe(traceId.ToHexString());
    }

    [Fact]
    public async Task StructuredLogAuditSink_ShouldBindOriginatingTraceIdDuringLogging()
    {
        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();
        var logger = new CapturingLogger();
        var sink = new StructuredLogAuditSink(logger);

        var entry = AuditEntry.Create(
            "OrderSubmitted",
            "OmsService",
            "Order",
            "ord-2",
            "Create",
            traceId: traceId.ToHexString(),
            spanId: spanId.ToHexString(),
            traceFlags: (byte)ActivityTraceFlags.Recorded);

        await sink.FlushAsync([entry], CancellationToken.None);

        logger.CapturedTraceIds.Count.ShouldBe(1);
        logger.CapturedTraceIds[0].ShouldBe(traceId.ToHexString());
        logger.CapturedParentSpanIds[0].ShouldBe(spanId.ToHexString());
        Activity.Current.ShouldBeNull();
    }

    [Fact]
    public void ProblemDetails_ShouldUse32CharTraceId_AndOmitWhenNoActivityIsActive()
    {
        var services = new ServiceCollection();
        services.AddPalantirProblemDetails();
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ProblemDetailsOptions>>().Value;

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Post;
        httpContext.Request.Path = "/api/v1/orders";
        httpContext.Request.Headers[ApiConstants.CorrelationIdHeader] = "corr-123";

        var withoutActivity = new ProblemDetailsContext { HttpContext = httpContext, ProblemDetails = new ProblemDetails() };
        options.CustomizeProblemDetails!(withoutActivity);
        withoutActivity.ProblemDetails.Extensions.ContainsKey("traceId").ShouldBeFalse();
        withoutActivity.ProblemDetails.Extensions["correlationId"].ShouldBe("corr-123");

        using var activity = new Activity("HttpRequest").Start();
        var withActivity = new ProblemDetailsContext { HttpContext = httpContext, ProblemDetails = new ProblemDetails() };
        options.CustomizeProblemDetails!(withActivity);
        withActivity.ProblemDetails.Extensions["traceId"].ShouldBe(activity.TraceId.ToHexString());
    }

    [Fact]
    public async Task DomainSpecificationExceptionHandler_ShouldWriteEnrichedProblemDetails()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPalantirProblemDetails();
        await using var provider = services.BuildServiceProvider();

        var handler = provider.GetServices<IExceptionHandler>().Single();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = provider
        };
        httpContext.Response.Body = new MemoryStream();
        httpContext.Request.Method = HttpMethods.Post;
        httpContext.Request.Path = "/api/v1/orders";
        httpContext.Request.Headers.Accept = "application/problem+json";

        using var activity = new Activity("ValidationRequest").Start();
        var ex = new DomainSpecificationException([new ValidationFailure("Quantity", "Quantity must be positive.")]);

        var handled = await handler.TryHandleAsync(httpContext, ex, CancellationToken.None);

        handled.ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        httpContext.Response.Body.Position = 0;
        var json = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();
        json.ShouldContain(activity.TraceId.ToHexString());
    }

    private sealed class CapturingLogger : ILogger<StructuredLogAuditSink>
    {
        public List<string?> CapturedTraceIds { get; } = [];
        public List<string?> CapturedParentSpanIds { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            CapturedTraceIds.Add(Activity.Current?.TraceId.ToHexString());
            CapturedParentSpanIds.Add(Activity.Current?.ParentSpanId.ToHexString());
        }
    }
}
