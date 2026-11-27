using System.Text.Json;
using Npgsql;
using NpgsqlTypes;

namespace Palantir.Platform.Auditing.Sinks;

/// <summary>
/// High-throughput PostgreSQL binary COPY sink storing platform audit trails in <c>platform_audit.audit_entries</c>.
/// </summary>
public sealed class PostgresAuditSink(string connectionString) : IAuditSink
{
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private volatile bool _schemaInitialized;

    public string Name => "PostgreSQL";

    public async Task FlushAsync(IReadOnlyList<AuditEntry> batch, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await EnsureSchemaAsync(conn, ct);

        await using var writer = await conn.BeginBinaryImportAsync(
            """
            COPY platform_audit.audit_entries
                (audit_id, event_type, service_name, user_id, correlation_id, resource_type, resource_id, action, payload_json, timestamp_utc)
            FROM STDIN (FORMAT BINARY)
            """,
            ct);

        foreach (var entry in batch)
        {
            await writer.StartRowAsync(ct);
            await writer.WriteAsync(entry.AuditId, NpgsqlDbType.Uuid, ct);
            await writer.WriteAsync(entry.EventType, NpgsqlDbType.Text, ct);
            await writer.WriteAsync(entry.ServiceName, NpgsqlDbType.Text, ct);
            await writer.WriteAsync(entry.UserId ?? string.Empty, NpgsqlDbType.Text, ct);
            await writer.WriteAsync(entry.CorrelationId ?? string.Empty, NpgsqlDbType.Text, ct);
            await writer.WriteAsync(entry.ResourceType, NpgsqlDbType.Text, ct);
            await writer.WriteAsync(entry.ResourceId, NpgsqlDbType.Text, ct);
            await writer.WriteAsync(entry.Action, NpgsqlDbType.Text, ct);
            await writer.WriteAsync(JsonSerializer.Serialize(entry.Payload), NpgsqlDbType.Jsonb, ct);
            await writer.WriteAsync(entry.TimestampUtc, NpgsqlDbType.TimestampTz, ct);
        }

        await writer.CompleteAsync(ct);
    }

    private async Task EnsureSchemaAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        if (_schemaInitialized)
        {
            return;
        }

        await _initLock.WaitAsync(ct);
        try
        {
            if (_schemaInitialized)
            {
                return;
            }

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                CREATE SCHEMA IF NOT EXISTS platform_audit;
                CREATE TABLE IF NOT EXISTS platform_audit.audit_entries (
                    audit_id UUID PRIMARY KEY,
                    event_type TEXT NOT NULL,
                    service_name TEXT NOT NULL,
                    user_id TEXT NOT NULL,
                    correlation_id TEXT NOT NULL,
                    resource_type TEXT NOT NULL,
                    resource_id TEXT NOT NULL,
                    action TEXT NOT NULL,
                    payload_json JSONB NOT NULL,
                    timestamp_utc TIMESTAMPTZ NOT NULL
                );
                CREATE INDEX IF NOT EXISTS ix_platform_audit_entries_ts ON platform_audit.audit_entries (timestamp_utc DESC);
                """;
            await cmd.ExecuteNonQueryAsync(ct);
            _schemaInitialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }
}
