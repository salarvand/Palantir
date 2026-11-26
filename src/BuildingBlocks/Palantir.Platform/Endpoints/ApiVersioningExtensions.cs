using Asp.Versioning;
using Asp.Versioning.Builder;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Palantir.Platform.Endpoints;

public static class ApiVersioningExtensions
{
    public static readonly ApiVersion V1 = new(1, 0);

    public static IServiceCollection AddPalantirApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = V1;
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("api-version")
            );
        })
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        services.AddEndpointsApiExplorer();

        return services;
    }

    public static ApiVersionSet NewV1ApiVersionSet(this WebApplication app) =>
        app.NewApiVersionSet()
           .HasApiVersion(V1)
           .ReportApiVersions()
           .Build();
}
