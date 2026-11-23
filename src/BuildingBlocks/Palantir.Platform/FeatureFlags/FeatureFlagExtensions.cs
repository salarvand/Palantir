using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace Palantir.Platform.FeatureFlags;

public static class FeatureFlagExtensions
{
    public static IServiceCollection AddPalantirFeatureFlags(this IServiceCollection services)
    {
        services.AddFeatureManagement();
        return services;
    }

    public static RouteHandlerBuilder RequireFeature(this RouteHandlerBuilder builder, string featureName)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var featureManager = context.HttpContext.RequestServices.GetRequiredService<IFeatureManager>();
            var isEnabled = await featureManager.IsEnabledAsync(featureName);
            if (!isEnabled)
            {
                return Results.NotFound(new { Message = $"Feature '{featureName}' is currently disabled." });
            }

            return await next(context);
        });
    }
}
