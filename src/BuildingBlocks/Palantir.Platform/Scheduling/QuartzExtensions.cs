using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Palantir.Platform.Scheduling;

public static class QuartzExtensions
{
    /// <summary>
    /// Configures Quartz.NET with a persistent PostgreSQL job store and System.Text.Json serialization.
    /// </summary>
    public static IServiceCollection AddPalantirScheduling(
        this IServiceCollection services,
        string connectionString,
        Action<IServiceCollectionQuartzConfigurator>? configure = null)
    {
        services.AddQuartz(q =>
        {
            q.UsePersistentStore(store =>
            {
                store.UseProperties = true;
                store.UsePostgres(connectionString);
                store.UseSystemTextJsonSerializer();
            });

            configure?.Invoke(q);
        });

        services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });

        return services;
    }
}
