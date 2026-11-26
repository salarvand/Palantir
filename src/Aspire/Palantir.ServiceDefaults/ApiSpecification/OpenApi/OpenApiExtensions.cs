using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

namespace Palantir.ServiceDefaults.ApiSpecification.OpenApi;

public static class OpenApiExtensions
{
    public static IServiceCollection AddDefaultOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<SecuritySchemeDefinitionsTransformer>();
        });

        return services;
    }

    public static WebApplication UseDefaultOpenApi(this WebApplication app, string title = "Palantir Cloud-Native Platform API")
    {
        app.MapOpenApi();

        if (app.Environment.IsDevelopment())
        {
            app.MapScalarApiReference(options =>
            {
                options.WithTitle(title)
                       .WithTheme(ScalarTheme.Moon);
            });

            app.MapGet("/", () => TypedResults.Redirect("/scalar/v1"))
               .ExcludeFromDescription();
        }

        return app;
    }
}
