using System.Reflection;
using Npgsql;

namespace Palantir.OmsService.Infrastructure;

public static class OrleansSchemaInitializer
{
    private const string ResourceName = "Palantir.OmsService.Infrastructure.OrleansPostgresSchema.sql";

    private const string MissingQueryKeysPatchSql = """
        INSERT INTO OrleansQuery(QueryKey, QueryText)
        VALUES
        (
            'CleanupDefunctSiloEntriesKey',
            'DELETE FROM OrleansMembershipTable
            WHERE DeploymentId = @DeploymentId
                AND @DeploymentId IS NOT NULL
                AND IAmAliveTime < @IAmAliveTime
                AND StartTime < @IAmAliveTime
                AND COALESCE(SuspectTimes, '''') = ''''
                AND Status = 6;'
        ),
        (
            'CleanupDefunctSiloEntryKey',
            'DELETE FROM OrleansMembershipTable
            WHERE DeploymentId = @DeploymentId AND Status = 6
                AND Address = @Address AND Port = @Port AND Generation = @Generation
                AND IAmAliveTime = @IAmAliveTime AND StartTime = @StartTime
                AND COALESCE(SuspectTimes, '''') = COALESCE(@SuspectTimes, '''');'
        ),
        (
            'DeleteStorageKey',
            'DELETE FROM OrleansStorage
            WHERE GrainIdHash = @GrainIdHash AND @GrainIdHash IS NOT NULL
                AND GrainTypeHash = @GrainTypeHash AND @GrainTypeHash IS NOT NULL
                AND GrainIdN0 = @GrainIdN0 AND @GrainIdN0 IS NOT NULL
                AND GrainIdN1 = @GrainIdN1 AND @GrainIdN1 IS NOT NULL
                AND GrainTypeString = @GrainTypeString AND @GrainTypeString IS NOT NULL
                AND ((@GrainIdExtensionString IS NOT NULL AND GrainIdExtensionString IS NOT NULL AND GrainIdExtensionString = @GrainIdExtensionString) OR @GrainIdExtensionString IS NULL AND GrainIdExtensionString IS NULL)
                AND ServiceId = @ServiceId AND @ServiceId IS NOT NULL
                AND Version IS NOT NULL AND Version = @GrainStateVersion AND @GrainStateVersion IS NOT NULL
            Returning Version % 2147483647 + 1 as NewGrainStateVersion;'
        )
        ON CONFLICT (QueryKey) DO NOTHING;
        """;

    public static async Task EnsureInitializedAsync(string connectionString, CancellationToken ct = default)
    {
        const int maxRetries = 10;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync(ct);

                await using var checkCmd = new NpgsqlCommand(
                    "SELECT to_regclass('public.orleansquery') IS NOT NULL;",
                    conn);
                var exists = (bool)(await checkCmd.ExecuteScalarAsync(ct) ?? false);

                await using var tx = await conn.BeginTransactionAsync(ct);
                if (!exists)
                {
                    var sql = await ReadSchemaSqlAsync(ct);
                    await using var initCmd = new NpgsqlCommand(sql, conn, tx);
                    await initCmd.ExecuteNonQueryAsync(ct);
                }

                await using var patchCmd = new NpgsqlCommand(MissingQueryKeysPatchSql, conn, tx);
                await patchCmd.ExecuteNonQueryAsync(ct);
                await tx.CommitAsync(ct);
                return;
            }
            catch (Exception) when (attempt < maxRetries)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), ct);
            }
        }
    }

    private static async Task<string> ReadSchemaSqlAsync(CancellationToken ct)
    {
        var assembly = Assembly.GetExecutingAssembly();
        await using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' was not found.");
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(ct);
    }
}
