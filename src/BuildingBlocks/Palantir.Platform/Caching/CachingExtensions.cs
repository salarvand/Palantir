using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;
using ZiggyCreatures.Caching.Fusion.Serialization;

namespace Palantir.Platform.Caching;

public sealed class FusionCacheSystemTextJsonSerializer : IFusionCacheSerializer
{
    private readonly JsonSerializerOptions _options;

    public FusionCacheSystemTextJsonSerializer(JsonSerializerOptions? options = null)
    {
        _options = options ?? new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public byte[] Serialize<T>(T? obj) => JsonSerializer.SerializeToUtf8Bytes(obj, _options);

    public T? Deserialize<T>(byte[] data) => JsonSerializer.Deserialize<T>(data, _options);

    public ValueTask<byte[]> SerializeAsync<T>(T? obj, CancellationToken token = default) =>
        ValueTask.FromResult(Serialize(obj));

    public ValueTask<T?> DeserializeAsync<T>(byte[] data, CancellationToken token = default) =>
        ValueTask.FromResult(Deserialize<T>(data));
}

public static class CachingExtensions
{
    public static IServiceCollection AddPalantirCaching(this IServiceCollection services, string redisConnectionString)
    {
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));

        var serializer = new FusionCacheSystemTextJsonSerializer();
        services.AddSingleton<IFusionCacheSerializer>(serializer);

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
            .WithSerializer(serializer)
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
