using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;

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

    public static WolverineOptions UsePostgresOutbox(this WolverineOptions opts, string connectionString, string schemaName = "wolverine")
    {
        opts.PersistMessagesWithPostgresql(connectionString, schemaName);
        opts.UseEntityFrameworkCoreTransactions();
        opts.Policies.UseDurableOutboxOnAllSendingEndpoints();
        opts.Policies.UseDurableInboxOnAllListeners();
        return opts;
    }
}
