using System.Reflection;
using Asp.Versioning.Builder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Palantir.Constants;

namespace Palantir.Platform.Endpoints;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}

public interface IEndpoint<TResult> : IEndpoint
{
    Task<TResult> HandleAsync(CancellationToken cancellationToken = default);
}

public interface IEndpoint<TResult, in TRequest> : IEndpoint
{
    Task<TResult> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}

public interface IEndpoint<TResult, in TRequest1, in TRequest2> : IEndpoint
{
    Task<TResult> HandleAsync(TRequest1 request1, TRequest2 request2, CancellationToken cancellationToken = default);
}

public interface IEndpoint<TResult, in TRequest1, in TRequest2, in TRequest3> : IEndpoint
{
    Task<TResult> HandleAsync(
        TRequest1 request1,
        TRequest2 request2,
        TRequest3 request3,
        CancellationToken cancellationToken = default);
}

public static class EndpointExtensions
{
    public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)
    {
        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes
                .AssignableTo<IEndpoint>()
                .Where(t => t is { IsAbstract: false, IsInterface: false }), publicOnly: false)
            .AsImplementedInterfaces()
            .WithTransientLifetime());

        return services;
    }

    public static IApplicationBuilder MapEndpoints(
        this WebApplication app,
        ApiVersionSet? apiVersionSet = null,
        string? resourceName = null)
    {
        using var scope = app.Services.CreateScope();
        var endpoints = scope.ServiceProvider.GetRequiredService<IEnumerable<IEndpoint>>();

        IEndpointRouteBuilder builder;
        if (apiVersionSet is not null)
        {
            var prefix = string.IsNullOrWhiteSpace(resourceName)
                ? "/api/v{version:apiVersion}"
                : $"/api/v{{version:apiVersion}}/{resourceName.Trim('/')}";

            var group = app.MapGroup(prefix).WithApiVersionSet(apiVersionSet);
            if (app.Environment.IsDevelopment())
            {
                group.DisableAntiforgery();
            }

            builder = group;
        }
        else
        {
            builder = app;
        }

        foreach (var endpoint in endpoints)
        {
            endpoint.MapEndpoint(builder);
        }

        return app;
    }

    public static RouteHandlerBuilder ProducesPost<T>(this RouteHandlerBuilder builder, bool hasValidation = true)
    {
        builder = builder.Produces<T>(StatusCodes.Status201Created);
        return hasValidation ? builder.ProducesValidationProblem() : builder;
    }

    public static RouteHandlerBuilder ProducesPostWithoutLocation<T>(this RouteHandlerBuilder builder, bool hasValidation = true)
    {
        builder = builder.Produces<T>(StatusCodes.Status200OK);
        return hasValidation ? builder.ProducesValidationProblem() : builder;
    }

    public static RouteHandlerBuilder ProducesGet<T>(
        this RouteHandlerBuilder builder,
        bool hasValidation = false,
        bool hasNotFound = false)
    {
        builder = builder.Produces<T>(StatusCodes.Status200OK);
        if (hasValidation)
        {
            builder = builder.ProducesValidationProblem();
        }
        if (hasNotFound)
        {
            builder = builder.ProducesProblem(StatusCodes.Status404NotFound);
        }
        return builder;
    }

    public static RouteHandlerBuilder ProducesPut(this RouteHandlerBuilder builder) =>
        builder.Produces(StatusCodes.Status204NoContent)
               .ProducesProblem(StatusCodes.Status404NotFound)
               .ProducesValidationProblem();

    public static RouteHandlerBuilder ProducesDelete(this RouteHandlerBuilder builder) =>
        builder.Produces(StatusCodes.Status204NoContent)
               .ProducesProblem(StatusCodes.Status404NotFound);

    public static RouteHandlerBuilder WithIdempotency(this RouteHandlerBuilder builder) =>
        builder.AddEndpointFilter<IdempotencyEndpointFilter>();
}

internal sealed class IdempotencyEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (context.HttpContext.Request.Headers.TryGetValue(ApiConstants.RequestIdHeader, out var requestId)
            && !string.IsNullOrWhiteSpace(requestId))
        {
            context.HttpContext.Response.Headers.TryAdd(ApiConstants.RequestIdHeader, requestId.ToString());
        }

        return await next(context);
    }
}
