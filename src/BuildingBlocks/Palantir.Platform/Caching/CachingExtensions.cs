using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;

namespace Palantir.Platform.Caching;

public static class CachingExtensions
{
    public static IServiceCollection AddPalantirCaching(this IServiceCollection services, string redisConnectionString)
    {
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddFusionCache()
            .WithOptions(opts =>
            {
                opts.DefaultEntryOptions = new FusionCacheEntryOptions
                {
                    Duration = TimeSpan.FromMinutes(5),
                    IsFailSafeEnabled = true,
                    FailSafeMaxDuration = TimeSpan.FromHours(1),
                    FailSafeThrottleDuration = TimeSpan.FromSeconds(30)
                };
            })
            .WithRegisteredSerializer()
            .WithBackplane(sp =>
            {
                var redis = sp.GetRequiredService<IConnectionMultiplexer>();
                return new RedisBackplane(new RedisBackplaneOptions
                {
                    Configuration = redisConnectionString
                });
            });

        return services;
    }
}
