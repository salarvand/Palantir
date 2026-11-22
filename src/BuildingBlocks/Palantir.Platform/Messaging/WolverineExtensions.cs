using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine;

namespace Palantir.Platform.Messaging;

public static class WolverineExtensions
{
    public static IHostBuilder AddPalantirMessaging(this IHostBuilder hostBuilder, Action<WolverineOptions>? configure = null)
    {
        return hostBuilder.UseWolverine(opts =>
        {
            opts.Policies.AutoApplyTransactions();
            opts.Policies.UseDurableLocalQueues();
            configure?.Invoke(opts);
        });
    }
}
