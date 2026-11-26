using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Saunter;
using Saunter.AsyncApiSchema.v2;

namespace Palantir.ServiceDefaults.ApiSpecification.AsyncApi;

public static class AsyncApiExtensions
{
    public static IHostApplicationBuilder AddDefaultAsyncApi(
        this IHostApplicationBuilder builder,
        string serviceTitle,
        string version = "1.0.0",
        params Type[] assemblyMarkerTypes)
    {
        builder.Services.AddAsyncApiSchemaGeneration(options =>
        {
            options.AssemblyMarkerTypes = assemblyMarkerTypes;
            options.AsyncApi = new AsyncApiDocument
            {
                Info = new Info(serviceTitle, version)
                {
                    Description = $"Event-driven AsyncAPI contract for {serviceTitle} over RabbitMQ/Wolverine."
                }
            };
        });

        return builder;
    }

    public static WebApplication UseDefaultAsyncApi(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return app;
        }

        app.MapAsyncApiDocuments();
        app.MapAsyncApiUi();

        return app;
    }
}
