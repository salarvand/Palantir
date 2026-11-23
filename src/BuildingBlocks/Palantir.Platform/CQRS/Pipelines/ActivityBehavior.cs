using System.Diagnostics;
using MediatR;

namespace Palantir.Platform.CQRS.Pipelines;

public sealed class ActivityBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly ActivitySource ActivitySource = new("Palantir.Platform");

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        using var activity = ActivitySource.StartActivity($"CQRS {requestName}");
        activity?.SetTag("cqrs.request_type", requestName);

        return await next();
    }
}
