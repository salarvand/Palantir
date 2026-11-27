using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Npgsql;
using Palantir.Constants;
using Respawn;
using Xunit;

namespace Palantir.IntegrationTests.Fixtures;

public sealed class AspireAppFixture : IAsyncLifetime
{
    private DistributedApplication? _app;

    public HttpClient GatewayClient { get; private set; } = default!;
    public HttpClient OmsClient { get; private set; } = default!;
    public HttpClient RiskClient { get; private set; } = default!;
    public HttpClient AuditClient { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        using var startupTimeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));

        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Palantir_AppHost>(startupTimeout.Token);

        _app = await appHost.BuildAsync(startupTimeout.Token);
        await _app.StartAsync(startupTimeout.Token);

        await Task.WhenAll(
            _app.ResourceNotifications.WaitForResourceHealthyAsync(AspireResourceNames.OmsService, startupTimeout.Token),
            _app.ResourceNotifications.WaitForResourceHealthyAsync(AspireResourceNames.RiskService, startupTimeout.Token),
            _app.ResourceNotifications.WaitForResourceHealthyAsync(AspireResourceNames.AuditService, startupTimeout.Token));

        GatewayClient = _app.CreateHttpClient(AspireResourceNames.Gateway);
        OmsClient = _app.CreateHttpClient(AspireResourceNames.OmsService);
        RiskClient = _app.CreateHttpClient(AspireResourceNames.RiskService);
        AuditClient = _app.CreateHttpClient(AspireResourceNames.AuditService);
    }

    public async Task ResetDatabaseAsync(params string[] schemas)
    {
        if (_app is null)
        {
            return;
        }

        var connectionString = await _app.GetConnectionStringAsync(AspireResourceNames.PalantirDb);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        var respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = schemas.Length > 0 ? schemas : ["public", "audit", "iam"]
        });

        await respawner.ResetAsync(conn);
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
