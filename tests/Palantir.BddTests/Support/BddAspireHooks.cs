using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Palantir.Constants;
using Reqnroll;

namespace Palantir.BddTests.Support;

[Binding]
public sealed class BddAspireHooks
{
    private static DistributedApplication? _app;

    public static HttpClient OmsClient { get; private set; } = default!;
    public static HttpClient RiskClient { get; private set; } = default!;
    public static HttpClient AuditClient { get; private set; } = default!;

    [BeforeTestRun]
    public static async Task StartAspireAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Palantir_AppHost>(cts.Token);

        _app = await builder.BuildAsync(cts.Token);
        await _app.StartAsync(cts.Token);

        await Task.WhenAll(
            _app.ResourceNotifications.WaitForResourceHealthyAsync(AspireResourceNames.OmsService, cts.Token),
            _app.ResourceNotifications.WaitForResourceHealthyAsync(AspireResourceNames.RiskService, cts.Token),
            _app.ResourceNotifications.WaitForResourceHealthyAsync(AspireResourceNames.AuditService, cts.Token));

        OmsClient = _app.CreateHttpClient(AspireResourceNames.OmsService);
        RiskClient = _app.CreateHttpClient(AspireResourceNames.RiskService);
        AuditClient = _app.CreateHttpClient(AspireResourceNames.AuditService);
    }

    [AfterTestRun]
    public static async Task StopAspireAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
