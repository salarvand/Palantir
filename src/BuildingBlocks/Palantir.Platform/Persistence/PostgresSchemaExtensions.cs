using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Palantir.Platform.Persistence;

public static class PostgresSchemaExtensions
{
    public static async Task EnsurePostgresSchemaCreatedAsync<TContext>(
        this IServiceProvider services,
        string? schemaName = null,
        ILogger? logger = null,
        CancellationToken ct = default)
        where TContext : DbContext
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();
        const int maxRetries = 10;

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(schemaName))
                {
                    await db.Database.ExecuteSqlRawAsync($"CREATE SCHEMA IF NOT EXISTS \"{schemaName}\";", ct);
                }

                var creator = db.Database.GetService<IRelationalDatabaseCreator>();
                if (!await creator.ExistsAsync(ct))
                {
                    await creator.CreateAsync(ct);
                }

                try
                {
                    await creator.CreateTablesAsync(ct);
                }
                catch (PostgresException ex) when (ex.SqlState == "42P07")
                {
                    // Tables already exist in this schema
                }

                return;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                logger?.LogWarning(ex, "Waiting for PostgreSQL schema {Schema} ({Attempt}/{MaxRetries})...", schemaName ?? "public", attempt, maxRetries);
                await Task.Delay(TimeSpan.FromSeconds(2), ct);
            }
        }
    }
}
