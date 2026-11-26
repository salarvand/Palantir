using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Npgsql;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace Palantir.Platform.Caching;

public interface IKeyValueStore
{
    Task<string?> GetAsync(string key, CancellationToken ct = default);
    Task SetAsync(string key, string value, TimeSpan ttl, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
}

public sealed class PostgresKeyValueStore : IKeyValueStore, IDistributedCache, IAsyncDisposable
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

    // IDistributedCache bridge so FusionCache shares the PostgreSQL L2 cache cleanly
    byte[]? IDistributedCache.Get(string key) =>
        GetAsync(key).GetAwaiter().GetResult() is { } val ? Encoding.UTF8.GetBytes(val) : null;

    async Task<byte[]?> IDistributedCache.GetAsync(string key, CancellationToken token) =>
        await GetAsync(key, token) is { } val ? Encoding.UTF8.GetBytes(val) : null;

    void IDistributedCache.Set(string key, byte[] value, DistributedCacheEntryOptions options) =>
        SetAsync(key, Encoding.UTF8.GetString(value), ResolveTtl(options)).GetAwaiter().GetResult();

    Task IDistributedCache.SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token) =>
        SetAsync(key, Encoding.UTF8.GetString(value), ResolveTtl(options), token);

    void IDistributedCache.Refresh(string key) { }
    Task IDistributedCache.RefreshAsync(string key, CancellationToken token) => Task.CompletedTask;
    void IDistributedCache.Remove(string key) => RemoveAsync(key).GetAwaiter().GetResult();
    Task IDistributedCache.RemoveAsync(string key, CancellationToken token) => RemoveAsync(key, token);

    private static TimeSpan ResolveTtl(DistributedCacheEntryOptions options) =>
        options.AbsoluteExpirationRelativeToNow
        ?? options.SlidingExpiration
        ?? (options.AbsoluteExpiration.HasValue ? options.AbsoluteExpiration.Value - DateTimeOffset.UtcNow : TimeSpan.FromMinutes(5));

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

public static class CachingExtensions
{
    public static IServiceCollection AddPalantirCaching(this IServiceCollection services, string postgresConnectionString)
    {
        services.TryAddSingleton(sp =>
            new PostgresKeyValueStore(postgresConnectionString, sp.GetRequiredService<ILogger<PostgresKeyValueStore>>()));
        services.TryAddSingleton<IKeyValueStore>(sp => sp.GetRequiredService<PostgresKeyValueStore>());
        services.TryAddSingleton<IDistributedCache>(sp => sp.GetRequiredService<PostgresKeyValueStore>());

        var serializer = new FusionCacheSystemTextJsonSerializer(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
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
            .WithSerializer(serializer)
            .WithRegisteredDistributedCache();

        return services;
    }
}
