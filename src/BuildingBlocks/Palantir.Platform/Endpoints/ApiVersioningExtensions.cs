using Asp.Versioning;
using Microsoft.Extensions.DependencyInjection;

namespace Palantir.Platform.Endpoints;

public static class ApiVersioningExtensions
{
    public static IServiceCollection AddPalantirApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("api-version")
            );
        });

        services.AddEndpointsApiExplorer();

        return services;
    }
}
