using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Npgsql;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization;

namespace Palantir.Platform.Caching;

public interface IKeyValueStore
{
    Task<string?> GetAsync(string key, CancellationToken ct = default);
    Task SetAsync(string key, string value, TimeSpan ttl, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
}

public sealed class PostgresKeyValueStore : IKeyValueStore, IAsyncDisposable
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<PostgresKeyValueStore> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private volatile bool _initialized;

    public PostgresKeyValueStore(string connectionString, ILogger<PostgresKeyValueStore> logger)
    {
        _dataSource = NpgsqlDataSource.Create(connectionString);
        _logger = logger;
    }

    public async Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        await EnsureTableExistsAsync(ct);

        await using var cmd = _dataSource.CreateCommand(
            """
            SELECT cache_value
            FROM platform_cache
            WHERE cache_key = $1 AND expires_at_utc > NOW();
            """);
        cmd.Parameters.AddWithValue(key);

        var result = await cmd.ExecuteScalarAsync(ct);
        return result as string;
    }

    public async Task SetAsync(string key, string value, TimeSpan ttl, CancellationToken ct = default)
    {
        await EnsureTableExistsAsync(ct);

        var expiresAt = DateTimeOffset.UtcNow.Add(ttl);
        await using var cmd = _dataSource.CreateCommand(
            """
            INSERT INTO platform_cache (cache_key, cache_value, expires_at_utc)
            VALUES ($1, $2, $3)
            ON CONFLICT (cache_key) DO UPDATE
            SET cache_value = EXCLUDED.cache_value,
                expires_at_utc = EXCLUDED.expires_at_utc;
            """);
        cmd.Parameters.AddWithValue(key);
        cmd.Parameters.AddWithValue(value);
        cmd.Parameters.AddWithValue(expiresAt);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        await EnsureTableExistsAsync(ct);

        await using var cmd = _dataSource.CreateCommand("DELETE FROM platform_cache WHERE cache_key = $1;");
        cmd.Parameters.AddWithValue(key);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private async Task EnsureTableExistsAsync(CancellationToken ct)
    {
        if (_initialized) return;

        await _initLock.WaitAsync(ct);
        try
        {
            if (_initialized) return;

            await using var cmd = _dataSource.CreateCommand(
                """
                CREATE UNLOGGED TABLE IF NOT EXISTS platform_cache (
                    cache_key TEXT PRIMARY KEY,
                    cache_value TEXT NOT NULL,
                    expires_at_utc TIMESTAMPTZ NOT NULL
                );
                CREATE INDEX IF NOT EXISTS idx_platform_cache_expires ON platform_cache (expires_at_utc);
                """);
            await cmd.ExecuteNonQueryAsync(ct);
            _initialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize PostgreSQL platform_cache table.");
            throw;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public ValueTask DisposeAsync()
    {
        _initLock.Dispose();
        return _dataSource.DisposeAsync();
    }
}

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
    public static IServiceCollection AddPalantirCaching(this IServiceCollection services, string postgresConnectionString)
    {
        services.TryAddSingleton<IKeyValueStore>(sp =>
            new PostgresKeyValueStore(postgresConnectionString, sp.GetRequiredService<ILogger<PostgresKeyValueStore>>()));

        var serializer = new FusionCacheSystemTextJsonSerializer();
        services.TryAddSingleton<IFusionCacheSerializer>(serializer);

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
            .WithSerializer(serializer);

        return services;
    }
}
