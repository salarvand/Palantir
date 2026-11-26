using System.Data;
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
        string? preCreateSql = null,
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
                var creator = db.Database.GetService<IRelationalDatabaseCreator>();
                if (!await creator.ExistsAsync(ct))
                {
                    await creator.CreateAsync(ct);
                }

                if (!string.IsNullOrWhiteSpace(schemaName))
                {
                    await db.Database.ExecuteSqlRawAsync($"CREATE SCHEMA IF NOT EXISTS \"{schemaName}\";", ct);
                }

                if (!string.IsNullOrWhiteSpace(preCreateSql))
                {
                    await db.Database.ExecuteSqlRawAsync(preCreateSql, ct);

                    if (db.Database.GetDbConnection() is NpgsqlConnection npgsqlConn)
                    {
                        if (npgsqlConn.State != ConnectionState.Open)
                        {
                            await npgsqlConn.OpenAsync(ct);
                        }

                        await npgsqlConn.ReloadTypesAsync(ct);
                    }
                }

                if (await SchemaHasTablesAsync(db, schemaName ?? "public", ct))
                {
                    return;
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

    private static async Task<bool> SchemaHasTablesAsync(DbContext db, string schemaName, CancellationToken ct)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
        {
            await conn.OpenAsync(ct);
        }

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = @schema);";
        var param = cmd.CreateParameter();
        param.ParameterName = "@schema";
        param.Value = schemaName;
        cmd.Parameters.Add(param);

        var result = await cmd.ExecuteScalarAsync(ct);
        return result is true;
    }
}
