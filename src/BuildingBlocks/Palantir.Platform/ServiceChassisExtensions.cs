using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Palantir.Constants;
using Palantir.Platform.Auditing;
using Palantir.Platform.Caching;
using Palantir.Platform.Endpoints;
using Palantir.Platform.Exceptions;
using Palantir.Platform.FeatureFlags;
using Palantir.Platform.HealthChecks;
using Palantir.Platform.Observability;
using Palantir.Platform.RateLimiting;
using Palantir.Platform.Security;
using Palantir.Platform.Validation;

namespace Palantir.Platform;

public static class ServiceChassisExtensions
{
    public static WebApplicationBuilder AddPalantirChassis(
        this WebApplicationBuilder builder,
        Assembly serviceAssembly)
    {
        var connectionString = builder.Configuration.GetConnectionString(AspireResourceNames.PalantirDb)
                               ?? "Host=localhost;Database=PalantirDb;Username=postgres;Password=postgres";

        var keycloakUrl = builder.Configuration["Keycloak:Url"]
                          ?? builder.Configuration["services:keycloak:http:0"]
                          ?? "http://localhost:8080";

        builder.Services.AddPalantirCaching(connectionString);
        builder.Services.AddPalantirSecurity(keycloakUrl);
        builder.Services.AddPalantirJwtAuthentication(builder.Configuration);
        builder.Services.AddPalantirProblemDetails();
        builder.Services.AddPalantirApiVersioning();
        builder.Services.AddPalantirFeatureFlags();
        builder.Services.AddPalantirValidators(serviceAssembly);
        builder.Services.AddEndpoints(serviceAssembly);
        builder.Services.TryAddSingleton<ITraceContextAccessor, ActivityTraceContextAccessor>();
        builder.Services.AddPalantirAuditing(connectionString);
        builder.Services.AddPalantirHealthChecks(connectionString);
        builder.Services.AddPalantirRateLimiting();

        return builder;
    }

    public static WebApplication UsePalantirChassis(this WebApplication app, string? resourceName = null)
    {
        app.UseMiddleware<TraceContextMiddleware>();
        app.UsePalantirExceptionHandler();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UsePalantirRateLimiting();
        app.MapEndpoints(app.NewV1ApiVersionSet(), resourceName);

        return app;
    }
}
