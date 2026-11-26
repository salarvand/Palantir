using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Palantir.Constants;
using Palantir.Platform.Caching;
using Palantir.Platform.Endpoints;
using Palantir.Platform.Exceptions;
using Palantir.Platform.FeatureFlags;
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

        return builder;
    }

    public static WebApplication UsePalantirChassis(this WebApplication app, string? resourceName = null)
    {
        app.UsePalantirExceptionHandler();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapEndpoints(app.NewV1ApiVersionSet(), resourceName);

        return app;
    }
}
